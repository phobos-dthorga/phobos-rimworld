using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle's glass (Framework 0.238.0; docs/development/vehicle-glass.md, VG1 to VG4, VG7, VG10, VG11; the owner's answers of 22 Nov 2026:
/// build the base with real art first). One part on every closed-cab vehicle, any mod's, born whole and left out of the vehicle's destruction
/// sum: sound, cracked or gone. It breaks from hits on the cab's plates (a reactor of its own), from explosions near the vehicle (one hook),
/// from fire (the fire wear table) and from the strike; a vehicle whose glass is gone has an open cab. It is re-glazed with the glass chain's
/// goods, never mended for free. <see cref="GlassRule"/> holds the arithmetic.
/// </summary>
public static class Glass
{
    /// <summary>The glass part's key, appended by our loadouts. Never change it: saves hold the part by it.</summary>
    public const string Key = "PhobosGlass";
    /// <summary>The role the glass is bound to, for the fire wear table.</summary>
    public const string Role = "Glass";

    static PhobosGlassRulesDef Rules => PhobosGlassRulesDef.Current;

    static bool On => VehicleSystemsMod.Settings.vehicleGlass;

    // ---- At start-up ----

    /// <summary>Appends the glass to a closed-cab design as its loadout is applied, and puts the glass's reactor on the outer plates over it.</summary>
    internal static void Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        if (props.trailer || def.components == null || def.components.Any(p => p.key == Key)) return;
        bool closed = def.GetModExtension<PhobosVehicleCabinExtension>()?.closed ?? props.loadout?.cabin.closed ?? true;
        if (!closed || def.GetModExtension<PhobosGlassExtension>()?.none == true) return;
        var add = new PartToAdd
        {
            key = Key,
            label = Rules.partLabel,
            health = Rules.partHealth,
            depth = VehicleComponent.VehiclePartDepth.Internal,
            hitWeight = 0.05f,
            cellsOf = "Cabin",
            armor = new List<StatModifier>(),
            countsTowardDestruction = false,
        };
        VehicleComponentProperties part = VehicleSystems.MakePart(def, add, props);
        def.components.Add(part);
        props.roleKeys[Role] = new List<string> { Key };
        props.addedKeys.Add(Key);
        props.uncountedKeys.Add(Key);
        List<IntVec2>? cells = part.hitbox?.cells;
        foreach (VehicleComponentProperties plate in def.components)
        {
            if (plate.depth != VehicleComponent.VehiclePartDepth.External) continue;
            bool over = cells == null || cells.Count == 0 || (plate.hitbox?.cells?.Any(c => cells.Contains(c)) ?? false);
            if (!over) continue;
            plate.reactors ??= new List<Reactor>();
            if (!plate.reactors.Exists(r => r is Reactor_PhobosGlass)) plate.reactors.Add(new Reactor_PhobosGlass());
        }
    }

    // ---- What the glass is ----

    public static VehicleComponent? PartOf(VehiclePawn? vehicle) => vehicle?.statHandler?.GetComponent(Key);

    public static GlassKind KindOf(VehiclePawn vehicle)
    {
        if (vehicle.VehicleDef.GetModExtension<PhobosGlassExtension>() is { } extension) return extension.kind;
        if (ArmouredCabFitted(vehicle)) return GlassKind.Armoured;
        return vehicle.GetComp<CompPhobosVehicleSystems>()?.Props.loadout?.glass ?? GlassKind.Ordinary;
    }

    static bool ArmouredCabFitted(VehiclePawn vehicle)
    {
        CompUpgradeTree? tree = vehicle.CompUpgradeTree;
        UpgradeNode? node = tree?.Props?.def?.nodes?.FirstOrDefault(n => n.key == Rules.armouredCabNode);
        return node != null && tree!.NodeUnlocked(node);
    }

    public static double Share(VehicleComponent glass) => glass.MaxHealth > 0f ? glass.Health / glass.MaxHealth : 0;

    public static GlassState StateOf(VehicleComponent glass) => GlassRule.StateOf(Share(glass));

    /// <summary>Whether this vehicle's glass is gone, so its cab is open to the air: false with no glass part or the setting off.</summary>
    public static bool Gone(VehiclePawn? vehicle) => On && PartOf(vehicle) is { } glass && glass.Health <= 0f;

    public static string StateWord(GlassState state) => state switch
    {
        GlassState.Gone => "PhobosVehicleSystems_GlassGone".Translate(),
        GlassState.Cracked => "PhobosVehicleSystems_GlassCracked".Translate(),
        _ => "PhobosVehicleSystems_GlassSound".Translate(),
    };

    public static string KindWord(GlassKind kind) => kind switch
    {
        GlassKind.Armoured => "PhobosVehicleSystems_GlassArmoured".Translate(),
        GlassKind.VisionBlocks => "PhobosVehicleSystems_GlassVisionBlocks".Translate(),
        _ => "PhobosVehicleSystems_GlassOrdinary".Translate(),
    };

    // ---- What breaks it ----

    /// <summary>Takes <paramref name="amount"/> off the glass, saying so when it cracks or goes. <paramref name="cause"/> is for the notes.</summary>
    public static void Damage(VehiclePawn vehicle, float amount, string cause)
    {
        if (!On || amount <= 0f || PartOf(vehicle) is not { } glass || glass.Health <= 0f) return;
        GlassState before = StateOf(glass);
        double shareBefore = Share(glass);
        glass.SetHealth(Mathf.Max(0f, glass.Health - amount));    // raises Vehicle Framework's HealthChanged itself
        GlassState after = StateOf(glass);
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(vehicle) ?? vehicle.GetComp<CompPhobosVehicleSystems>();
        comp?.Repairs.Invalidate();
        Splinter(vehicle, glass, shareBefore - Share(glass), after == GlassState.Gone && before != GlassState.Gone);
        if (after == before) return;
        if (Seen.Wants("vehicles.glass", vehicle, after == GlassState.Gone ? "gone" : "cracked"))
        {
            Seen.Note("vehicles.glass", vehicle, cause + ", " + KindOf(vehicle), after == GlassState.Gone ? "gone" : "cracked");
        }
        VehicleStory.GlassBroken(vehicle, after == GlassState.Gone);
    }

    // ---- Splinters (VG5), behind their own setting ----

    /// <summary>
    /// The people behind the glass take small cuts as it breaks, by its kind and how much went at once; by chance one finds an eye. Seats out
    /// in the open are not behind the glass. A cut, so worn armour, goggles and helmets stop it by the game's own rule.
    /// </summary>
    static void Splinter(VehiclePawn vehicle, VehicleComponent glass, double shareLost, bool gone)
    {
        if (!VehicleSystemsMod.Settings.glassSplinters || vehicle.handlers == null || vehicle.Destroyed) return;
        int each = GlassRule.Splinters(KindOf(vehicle), shareLost, gone);
        if (each <= 0) return;
        List<IntVec2>? cells = glass.props.hitbox?.cells;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler?.role == null || handler.role.Exposed || handler.thingOwner == null || handler.thingOwner.Count == 0) continue;
            if (cells is { Count: > 0 } && handler.role.Hitbox != null && !handler.role.Hitbox.Hitbox.Any(c => cells.Contains(c))) continue;
            foreach (Thing thing in handler.thingOwner.InnerListForReading.ToList())
            {
                if (thing is not Pawn pawn || pawn.Dead || !pawn.RaceProps.IsFlesh) continue;
                for (int i = 0; i < each && !pawn.Dead; i++)
                {
                    var cut = new DamageInfo(DamageDefOf.Cut, (float)GlassRule.SplinterDamage, 0f);
                    BodyPartRecord? eye = Rand.Chance((float)GlassRule.SplinterEyeChance)
                        ? pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.tags.Contains(BodyPartTagDefOf.SightSource)).RandomElementWithFallback()
                        : null;
                    if (eye != null) cut.SetHitPart(eye);
                    else cut.SetBodyRegion(BodyPartHeight.Top, BodyPartDepth.Outside);
                    pawn.TakeDamage(cut);
                }
                VehicleStory.GlassSplinters(vehicle, pawn);
                if (Seen.Wants("vehicles.glass-splinters", vehicle)) Seen.Note("vehicles.glass-splinters", vehicle, pawn.LabelShort + ", " + each + " splinters");
            }
        }
    }

    // ---- The driver's view (VG6) and an aircraft's canopy (VG13) ----

    /// <summary>Whether the glass is cracked or gone: an aircraft's canopy no longer holds its pressure.</summary>
    public static bool Breached(VehiclePawn? vehicle) => On && PartOf(vehicle) is { } glass && StateOf(glass) != GlassState.Sound;

    /// <summary>The share of its speed the driver dares through damaged glass, in the dark or bad weather; 1 with sound glass, the setting
    /// off, an aircraft (its canopy acts on height instead) or nobody to see.</summary>
    public static float ViewFactor(VehiclePawn vehicle)
    {
        if (!On || vehicle.VehicleDef.type != VehicleType.Land || PartOf(vehicle) is not { } glass) return 1f;
        GlassState state = StateOf(glass);
        if (state == GlassState.Sound) return 1f;
        Map? map = vehicle.MapHeld;
        bool dark = map != null && GenCelestial.CurCelestialSunGlow(map) < 0.3f;
        WeatherDef? weather = map?.weatherManager?.curWeather;
        bool bad = weather != null && (weather.rainRate > 0f || weather.snowRate > 0f || weather.accuracyMultiplier < 0.9f);
        return (float)GlassRule.ViewFactor(KindOf(vehicle), state, dark, bad);
    }

    /// <summary>From the reactor on a plate over the glass: a share of the hit reaches the glass by its kind. One hit counts once, though
    /// Combat Extended reports a beaten plate twice.</summary>
    internal static void PlateHit(VehiclePawn vehicle, DamageInfo dinfo)
    {
        if (!On || vehicle?.statHandler == null || Current.Game == null || dinfo.Amount <= 0f) return;
        int now = Find.TickManager.TicksGame;
        if (lastHitVehicle == vehicle && lastHitTick == now && lastHitInstigator == dinfo.Instigator) return;
        lastHitVehicle = vehicle;
        lastHitTick = now;
        lastHitInstigator = dinfo.Instigator;
        Damage(vehicle, (float)(dinfo.Amount * GlassRule.PlateHitShare(KindOf(vehicle))), "a hit");
    }

    static VehiclePawn? lastHitVehicle;
    static int lastHitTick = -1;
    static Thing? lastHitInstigator;

    /// <summary>From an explosion's start: every vehicle with glass within its reach takes the blast's share by its distance.</summary>
    internal static void Explosion(Explosion explosion)
    {
        if (!On || explosion?.Map == null || explosion.damType == null || !explosion.damType.isExplosive || !explosion.damType.harmsHealth) return;
        float reach = (float)(explosion.radius * GlassRule.BlastReach);
        IReadOnlyList<Pawn> pawns = explosion.Map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] is not VehiclePawn vehicle || PartOf(vehicle) is not { Health: > 0f }) continue;
            IntVec3 nearest = vehicle.OccupiedRect().ClosestCellTo(explosion.Position);
            float distance = nearest.DistanceTo(explosion.Position);
            if (distance >= reach) continue;
            float damage = (float)GlassRule.BlastDamage(explosion.damAmount, distance, explosion.radius, KindOf(vehicle));
            Damage(vehicle, damage, "a blast");
        }
    }

    /// <summary>From the strike's route for vehicles: its flash and blast wind, by a share of whole glass from 0 to 1 (NK2, NK3).</summary>
    public static void Strike(VehiclePawn vehicle, float share)
    {
        if (PartOf(vehicle) is not { } glass) return;
        double toughness = KindOf(vehicle) switch { GlassKind.Armoured => 0.3, GlassKind.VisionBlocks => 0.25, _ => 1.0 };
        Damage(vehicle, (float)(glass.MaxHealth * Mathf.Clamp01(share) * toughness), "the strike");
    }

    // ---- Re-glazing ----

    /// <summary>What re-glazing takes now, as one option, or null when the glass is sound or a good is not loaded.</summary>
    internal static List<(ThingDef def, int count)>? ReglazeGoods(VehiclePawn vehicle, VehicleComponent glass)
    {
        if (StateOf(glass) == GlassState.Sound) return null;
        (int laminated, int panes, int armoured, int blocks) = GlassRule.Goods(KindOf(vehicle), 1 - Share(glass));
        var option = new List<(ThingDef def, int count)>();
        if (!Add(option, Rules.laminatedPane, laminated) || !Add(option, Rules.glassPane, panes) || !Add(option, Rules.armouredPane, armoured)
            || !Add(option, Rules.visionBlock, blocks)) return null;
        return option.Count > 0 ? option : null;
    }

    static bool Add(List<(ThingDef def, int count)> option, ThingDef? good, int count)
    {
        if (count <= 0) return true;
        if (good == null) return false;
        option.Add((good, count));
        return true;
    }

    internal static float ReglazeWork(VehicleComponent glass) => (float)GlassRule.ReglazeWork(1 - Share(glass), Rules.reglazeBaseWork, Rules.reglazeWorkForAll);
}

/// <summary>On every outer plate over the glass (Framework 0.238.0): hands each hit to <see cref="Glass"/>. Colours nothing on Vehicle
/// Framework's Health tab, as the spall reactor.</summary>
public class Reactor_PhobosGlass : Reactor
{
    public Reactor_PhobosGlass()
    {
        highlightColor = Color.white;
    }

    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration) =>
        Glass.PlateHit(vehicle, dinfo);
}
