using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Environment;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// Fighting fires on vehicles (Framework 0.241.0; docs/development/vehicle-fire.md, FR10; the owner's answers of 23 Nov 2026):
/// - **zones**: a burning vehicle's fire starts in the engine bay, the crew compartment or the running gear, from the part hit just before it
///   caught, and spreads from the running gear into the engine bay and from there into the crew compartment;
/// - **the hand extinguisher**, a fitting with three charges: someone aboard, or a colonist beside the vehicle, knocks the fire down; a
///   mechanic refills it at the vehicle from a refill made at the parts bench, as every repair of ours is done (never a hauler's top-up);
/// - **automatic suppression**, an upgrade node on our armoured vehicles and a fitting on other authors': the engine bay's and the crew
///   compartment's systems each discharge by themselves when their own zone burns, put that zone out, and are spent until a mechanic
///   recharges them with a suppressant bottle.
/// Cheap: the game only marks the moment a fire catches (a postfix), handled at the next tick; the rest is read on the rare tick, and only
/// while the vehicle burns. Works for any faction's vehicle that has the equipment fitted.
/// </summary>
[StaticConstructorOnStartup]
public static class FireFighting
{
    public const string ExtinguisherAbility = "PhobosExtinguisher";
    public const string BracketKey = "PhobosExtinguisherBracket";
    public const string BracketRole = "ExtinguisherBracket";
    public const string EngineSystemKey = "PhobosSuppressionEngine";
    public const string CrewSystemKey = "PhobosSuppressionCrew";
    /// <summary>The fitting abilities and mount roles that give another author's armoured vehicle its systems.</summary>
    public const string EngineSystemAbility = "PhobosSuppressionEngine";
    public const string CrewSystemAbility = "PhobosSuppressionCrew";
    public const string EngineMountRole = "SuppressionEngineMount";
    public const string CrewMountRole = "SuppressionCrewMount";
    static readonly FireZones[] SystemZones = { FireZones.EngineBay, FireZones.CrewCompartment };

    internal static PhobosFireFightingRulesDef Rules => PhobosFireFightingRulesDef.Rules;

    /// <summary>Vehicles a fire has just caught on, handled at the next tick by the game component.</summary>
    internal static readonly List<CompPhobosFireFighting> Ignited = new();

    public static string KeyOf(FireZones zone) => zone == FireZones.CrewCompartment ? CrewSystemKey : EngineSystemKey;
    static string AbilityOf(FireZones zone) => zone == FireZones.CrewCompartment ? CrewSystemAbility : EngineSystemAbility;
    public static bool IsSystemKey(string? key) => key == EngineSystemKey || key == CrewSystemKey;
    public static bool IsOwnGoods(string? key) => key == BracketKey || IsSystemKey(key);

    public static string ZoneWord(FireZones zone) => zone switch
    {
        FireZones.CrewCompartment => "PhobosFireFighting_ZoneCrew".Translate(),
        FireZones.RunningGear => "PhobosFireFighting_ZoneGear".Translate(),
        _ => "PhobosFireFighting_ZoneEngine".Translate(),
    };

    // ---- At start-up ----

    /// <summary>
    /// Appends the extinguisher's bracket to every design with a loadout but trailers, and the suppression systems' parts: to a design whose
    /// upgrade tree has suppression nodes (ours), and to another author's design that counts as armoured, as mounts its fittings go on.
    /// Appended last, after the glass, so the saved order of every earlier part stays as it was.
    /// </summary>
    internal static void Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        if (props.trailer || def.components == null) return;
        string cab = props.roleKeys.ContainsKey("Cabin") ? "Cabin" : "Engine";
        Append(def, props, BracketKey, BracketRole, Rules.bracketLabel, Rules.bracketHealth, cab);
        FireZones nodes = NodeZones(def);
        bool others = nodes == FireZones.None && Armoured(def);
        foreach (FireZones zone in SystemZones)
        {
            bool byNode = (nodes & zone) != 0;
            if (!byNode && !others) continue;
            string role = byNode ? KeyOf(zone) : zone == FireZones.CrewCompartment ? CrewMountRole : EngineMountRole;
            Append(def, props, KeyOf(zone), role, zone == FireZones.CrewCompartment ? Rules.crewSystemLabel : Rules.engineSystemLabel,
                Rules.systemHealth, zone == FireZones.CrewCompartment ? cab : "Engine");
        }
        if (!def.comps.Any(c => c is CompProperties_PhobosFireFighting)) def.comps.Add(new CompProperties_PhobosFireFighting());
    }

    static void Append(VehicleDef def, CompProperties_PhobosVehicleSystems props, string key, string role, string label, int health, string cellsOf)
    {
        if (def.components.Any(p => p.key == key)) return;
        var add = new PartToAdd
        {
            key = key, label = label, health = health, depth = VehicleComponent.VehiclePartDepth.Internal, hitWeight = 0.02f, cellsOf = cellsOf,
            armor = new List<StatModifier>(), countsTowardDestruction = false,
        };
        def.components.Add(VehicleSystems.MakePart(def, add, props));
        props.roleKeys[role] = new List<string> { key };
        props.addedKeys.Add(key);
        props.uncountedKeys.Add(key);
    }

    /// <summary>The zones a design's own upgrade tree offers systems for (our vehicles' nodes).</summary>
    static FireZones NodeZones(VehicleDef def)
    {
        FireZones zones = FireZones.None;
        foreach (CompProperties comp in def.comps)
        {
            if (comp is not CompProperties_UpgradeTree { def.nodes: { } nodes }) continue;
            foreach (UpgradeNode node in nodes)
            {
                if (node.upgrades == null) continue;
                foreach (Upgrade upgrade in node.upgrades)
                {
                    if (upgrade is PhobosSuppressionUpgrade suppression) zones |= suppression.zone;
                }
            }
        }
        return zones;
    }

    /// <summary>Whether another author's design counts as armoured (the owner's card: "both"): its extension's mark, or else its outer
    /// plates' sharp armour against the rules' figure, Combat Extended's when it runs.</summary>
    internal static bool Armoured(VehicleDef def)
    {
        bool? marked = def.GetModExtension<PhobosFittedEquipmentExtension>()?.armoured;
        if (marked.HasValue) return marked.Value;
        float outer = 0f;
        foreach (VehicleComponentProperties part in def.components)
        {
            if (part.depth != VehicleComponent.VehiclePartDepth.External || part.armor == null) continue;
            foreach (StatModifier armour in part.armor)
            {
                if (armour.stat == StatDefOf.ArmorRating_Sharp && armour.value > outer) outer = armour.value;
            }
        }
        bool combatExtended = ModsConfig.IsActive("CETeam.CombatExtended");
        return FireFightingRule.Armoured(outer, combatExtended ? Rules.armouredFromCombatExtended : Rules.armouredFrom, null);
    }

    // ---- What is fitted ----

    public static CompPhobosFireFighting? CompOf(Thing? vehicle) => (vehicle as VehiclePawn)?.GetComp<CompPhobosFireFighting>();

    /// <summary>A part that is there and not shot away.</summary>
    internal static bool PartWorks(VehiclePawn vehicle, string key) => vehicle.statHandler?.GetComponent(key) is { Health: > 0f };

    /// <summary>The suppression systems fitted: an unlocked node of ours, or another author's vehicle's fitting.</summary>
    public static FireZones FittedSystems(VehiclePawn vehicle)
    {
        FireZones zones = FireZones.None;
        if (vehicle.CompUpgradeTree is { } tree && tree.Props?.def?.nodes is { } nodes)
        {
            foreach (UpgradeNode node in nodes)
            {
                if (node.upgrades == null || !tree.NodeUnlocked(node)) continue;
                foreach (Upgrade upgrade in node.upgrades)
                {
                    if (upgrade is PhobosSuppressionUpgrade suppression) zones |= suppression.zone;
                }
            }
        }
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems)
        {
            foreach (FireZones zone in SystemZones)
            {
                if (systems.Fittings.Grants(AbilityOf(zone))) zones |= zone;
            }
        }
        return zones;
    }

    /// <summary>From a suppression node fitted or removed: a fitted system is whole and ready; a removed one is no longer spent.</summary>
    internal static void SystemFitted(VehiclePawn vehicle, FireZones zone, bool fitted)
    {
        if (CompOf(vehicle) is not { } comp) return;
        comp.spent &= ~zone;
        if (fitted && vehicle.statHandler?.GetComponent(KeyOf(zone)) is { } part) part.SetHealth(part.MaxHealth);
        vehicle.GetComp<CompPhobosVehicleSystems>()?.RememberAllPartHealths();
        if (Seen.Wants("vehicles.suppression-fitted", vehicle, fitted ? "fitted" : "removed"))
            Seen.Note("vehicles.suppression-fitted", vehicle, ZoneWord(zone), fitted ? "fitted" : "removed");
    }

    /// <summary>From a fitting or a tune coming or going: a fitted extinguisher holds the charges its kit held (Framework 0.334.0: the
    /// fitting's kitCarries, through <see cref="CompPhobosFireFighting"/>'s kit store), and one taken off gives them back to its kit; one fitted
    /// with no kit read (another faction's set) arrives full. Another author's suppression fitting arrives ready.</summary>
    internal static void FittingsChanged(CompPhobosVehicleSystems systems)
    {
        if (CompOf(systems.Vehicle) is not { } comp) return;
        if (systems.Fittings.Grants(ExtinguisherAbility) && comp.charges < 0) comp.charges = Rules.extinguisherCharges;
        foreach (FireZones zone in SystemZones)
        {
            if (!systems.Fittings.Grants(AbilityOf(zone))) comp.spent &= ~zone;
        }
    }

    // ---- The fire ----

    /// <summary>From the systems: a bound part was hit. Remembered with its zone, as the cause of a fire that catches soon after.</summary>
    internal static void NoteHit(CompPhobosVehicleSystems systems, VehicleComponent part)
    {
        if (CompOf(systems.Vehicle) is not { } comp) return;
        foreach (PhobosFireZoneRole role in Rules.roleZones)
        {
            if (!systems.PartPlays(part, role.role)) continue;
            comp.lastHitZone = role.zone;
            comp.lastHitTick = Find.TickManager.TicksGame;
            return;
        }
    }

    /// <summary>
    /// One look at a vehicle's fire: on the tick after it catches, and on each rare tick. A new fire takes its zone; a burning one spreads;
    /// a ready system whose zone burns discharges. Nothing is read while the vehicle neither burns nor has zones to clear.
    /// </summary>
    internal static void Look(CompPhobosFireFighting comp, bool rareTick = true)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (vehicle == null) return;
        float size = vehicle.Spawned ? AttachedFires.Biggest(vehicle) : 0f;
        if (size <= 0f)
        {
            if (comp.burning != FireZones.None)
            {
                // Burning tyres smoulder on once the fire is out (Framework 0.245.0, FR11).
                TyreFire.WentOut(comp, comp.burning);
                comp.ClearZones();
            }
            else if (rareTick) TyreFire.Smoulder(comp);
            return;
        }
        PhobosFireFightingRulesDef rules = Rules;
        int now = Find.TickManager.TicksGame;
        if (comp.burning == FireZones.None)
        {
            double sinceHit = comp.lastHitTick < 0 ? -1 : (now - comp.lastHitTick) / (double)GenDate.TicksPerHour;
            FireZones zone = FireFightingRule.StartZone(comp.lastHitZone, sinceHit, rules.hitLitHours, rules.fallbackZone);
            comp.Ignite(zone, now);
            if (Seen.Wants("vehicles.fire-zone", vehicle, zone.ToString())) Seen.Note("vehicles.fire-zone", vehicle, ZoneWord(zone), zone.ToString());
        }
        else
        {
            FireZones was = comp.burning;
            FireZones now_ = FireFightingRule.Spread(was, comp.HoursBurning(FireZones.RunningGear, now), comp.HoursBurning(FireZones.EngineBay, now),
                rules.gearToEngineHours, rules.engineToCrewHours);
            FireZones reached = now_ & ~was;
            if (reached != FireZones.None)
            {
                comp.Ignite(reached, now);
                if ((reached & FireZones.CrewCompartment) != 0 && vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosFireFighting_ReachedCrew".Translate(vehicle.LabelShort.Named("VEHICLE")), vehicle, MessageTypeDefOf.NegativeEvent);
                }
                if (Seen.Wants("vehicles.fire-spread", vehicle, reached.ToString())) Seen.Note("vehicles.fire-spread", vehicle, reached.ToString(), reached.ToString());
            }
            // A burning tyre may burst (Framework 0.245.0, FR11): once a rare tick, never on the tick a fire attaches.
            if (rareTick) TyreFire.Burst(comp, size);
        }
        Suppress(comp);
    }

    /// <summary>Each ready system whose zone burns discharges: that zone goes out, and the fire with it once no zone burns.</summary>
    static void Suppress(CompPhobosFireFighting comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        FireZones ready = comp.Ready;
        if (ready == FireZones.None) return;
        foreach (FireZones zone in SystemZones)
        {
            if (!FireFightingRule.Discharges(comp.burning, zone, (ready & zone) != 0)) continue;
            (FireZones left, double share) = FireFightingRule.Suppress(comp.burning, zone);
            FireZones wereBurning = comp.burning;
            comp.spent |= zone;
            comp.burning = left;
            // Its spread clock stops, and the zone feeding it starts again, so it is not relit at the next look (fault-read 25 Nov 2026, D2).
            comp.Quenched(zone, Find.TickManager.TicksGame);
            if (left == FireZones.None)
            {
                // Tyres that burned may smoulder and light again (FR11), however the fire went out (fault-read 25 Nov 2026, D3).
                TyreFire.WentOut(comp, wereBurning);
                AttachedFires.PutOut(vehicle);
                comp.ClearZones();
            }
            else AttachedFires.ScaleTo(vehicle, (float)share);
            VehicleStory.SuppressionFired(vehicle, ZoneWord(zone));
            // Framework 0.253.0: heard going off (S143, the owner's pick).
            if (vehicle.Spawned) VehicleSystemSoundDefOf.PhobosVehicleSystems_SuppressionDischarge.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
            if (vehicle.Faction == Faction.OfPlayer)
            {
                string key = left == FireZones.None ? "PhobosFireFighting_SuppressedOut" : "PhobosFireFighting_SuppressedPart";
                Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE"), ZoneWord(zone).Named("ZONE")), vehicle, MessageTypeDefOf.NeutralEvent);
            }
            if (Seen.Wants("vehicles.suppression-fired", vehicle, zone.ToString()))
            {
                Seen.Note("vehicles.suppression-fired", vehicle, ZoneWord(zone), zone.ToString(),
                    new[] { new KeyValuePair<string, double>("left", share) });
            }
            if (left == FireZones.None) return;
        }
    }

    // ---- The hand extinguisher ----

    /// <summary>Someone aboard who can use the extinguisher, or none.</summary>
    internal static Pawn? UserAboard(VehiclePawn vehicle)
    {
        List<Pawn>? aboard = vehicle.AllPawnsAboard;
        if (aboard == null) return null;
        foreach (Pawn pawn in aboard)
        {
            if (Capable(pawn)) return pawn;
        }
        return null;
    }

    internal static bool Capable(Pawn pawn) =>
        pawn is { Dead: false, Downed: false } && pawn.RaceProps.Humanlike && (pawn.health?.capacities?.CapableOf(PawnCapacityDefOf.Manipulation) ?? false);

    /// <summary>Why the extinguisher cannot be used now, or null.</summary>
    internal static string? UseRefusal(CompPhobosFireFighting comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!comp.ExtinguisherFitted) return "PhobosFireFighting_NoExtinguisher".Translate(vehicle.LabelShort.Named("VEHICLE"));
        if (comp.Charges <= 0) return "PhobosFireFighting_Empty".Translate(vehicle.LabelShort.Named("VEHICLE"));
        if (!vehicle.Spawned || AttachedFires.Biggest(vehicle) <= 0f) return "PhobosFireFighting_NotBurning".Translate(vehicle.LabelShort.Named("VEHICLE"));
        return null;
    }

    /// <summary>The nearest colonist free to come and use it, when nobody aboard can (the owner's request of 23 Nov 2026: the button sends
    /// someone, so the right-click is only for choosing who).</summary>
    internal static Pawn? Sendable(CompPhobosFireFighting comp) =>
        Phobos.Framework.Work.SendNearest.Find(comp.Vehicle, Capable, PathEndMode.Touch, draftedToo: true);

    /// <summary>The button: someone aboard uses it at once; otherwise the nearest free colonist is sent to use it from beside the vehicle.</summary>
    internal static void UseOrSend(CompPhobosFireFighting comp)
    {
        if (UseRefusal(comp) != null) return;
        VehiclePawn vehicle = comp.Vehicle;
        if (UserAboard(vehicle) is { } user)
        {
            UseExtinguisher(comp, user);
            return;
        }
        if (Sendable(comp) is not { } sent) return;
        if (!Phobos.Framework.Work.SendNearest.Order(sent, JobMaker.MakeJob(FireFightingDefOf.PhobosVehicleSystems_UseExtinguisher, vehicle))) return;
        Messages.Message("PhobosFireFighting_Sent".Translate(sent.LabelShort.Named("PAWN"), vehicle.LabelShort.Named("VEHICLE")), sent,
            MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("vehicles.extinguisher-sent", vehicle)) Seen.Note("vehicles.extinguisher-sent", vehicle, sent.LabelShort);
    }

    /// <summary>A use: one charge spent and the fire knocked down; out, if it was small. Says what happened in a message.</summary>
    internal static void UseExtinguisher(CompPhobosFireFighting comp, Pawn user)
    {
        if (UseRefusal(comp) != null) return;
        VehiclePawn vehicle = comp.Vehicle;
        float before = AttachedFires.Biggest(vehicle);
        AttachedFires.KnockDown(vehicle, Rules.knockDown);
        comp.charges = FireFightingRule.AfterUse(comp.charges);
        bool out_ = AttachedFires.Biggest(vehicle) <= 0f;
        if (out_)
        {
            // Tyres that burned may smoulder and light again (FR11), however the fire went out (fault-read 25 Nov 2026, D3).
            TyreFire.WentOut(comp, comp.burning);
            comp.ClearZones();
        }
        // Framework 0.253.0: its own sound (S142, the owner's pick), in place of the game's Interact_BeatFire.
        VehicleSystemSoundDefOf.PhobosVehicleSystems_Extinguisher.PlayOneShot(new TargetInfo(vehicle.Position, vehicle.Map));
        FleckMaker.ThrowMicroSparks(vehicle.DrawPos, vehicle.Map);
        string key = out_ ? "PhobosFireFighting_UsedOut" : "PhobosFireFighting_UsedDown";
        Messages.Message(key.Translate(user.LabelShort.Named("PAWN"), vehicle.LabelShort.Named("VEHICLE"), comp.Charges.Named("LEFT")), vehicle,
            MessageTypeDefOf.NeutralEvent);
        if (Seen.Wants("vehicles.extinguisher-used", vehicle, out_ ? "out" : "down"))
        {
            Seen.Note("vehicles.extinguisher-used", vehicle, user.LabelShort, out_ ? "out" : "down",
                new[] { new KeyValuePair<string, double>("before", before), new KeyValuePair<string, double>("left", comp.Charges) });
        }
    }

    // ---- Refill and recharge (the repairs') ----

    /// <summary>The extinguisher's refill, while it is short of charges or its bracket is damaged: the bracket and one refill.</summary>
    internal static VehicleComponent? RefillWanted(CompPhobosFireFighting comp)
    {
        if (!comp.ExtinguisherFittedAtAll || Rules.refill == null) return null;
        VehicleComponent? bracket = comp.Vehicle.statHandler?.GetComponent(BracketKey);
        if (bracket == null) return null;
        return comp.charges < Rules.extinguisherCharges || bracket.Health < bracket.MaxHealth ? bracket : null;
    }

    /// <summary>The fitted systems wanting a recharge: spent, or their part damaged.</summary>
    internal static IEnumerable<(FireZones zone, VehicleComponent part)> RechargeWanted(CompPhobosFireFighting comp)
    {
        if (Rules.bottle == null) yield break;
        FireZones fitted = comp.Fitted;
        foreach (FireZones zone in SystemZones)
        {
            if ((fitted & zone) == 0 || comp.Vehicle.statHandler?.GetComponent(KeyOf(zone)) is not { } part) continue;
            if ((comp.spent & zone) != 0 || part.Health < part.MaxHealth) yield return (zone, part);
        }
    }

    internal static void Refilled(CompPhobosFireFighting comp, VehicleComponent bracket)
    {
        comp.charges = Rules.extinguisherCharges;
        bracket.SetHealth(bracket.MaxHealth);
    }

    internal static void Recharged(CompPhobosFireFighting comp, VehicleComponent part)
    {
        comp.spent &= ~(part.props.key == CrewSystemKey ? FireZones.CrewCompartment : FireZones.EngineBay);
        part.SetHealth(part.MaxHealth);
    }
}

public sealed class CompProperties_PhobosFireFighting : CompProperties
{
    public CompProperties_PhobosFireFighting() => compClass = typeof(CompPhobosFireFighting);
}

/// <summary>A vehicle's fire zones, its extinguisher's charges and its spent suppression systems (FR10). Its buttons only present: each
/// asks <see cref="FireFighting"/>.</summary>
public sealed class CompPhobosFireFighting : VehicleComp, IPhobosKitStore
{
    /// <summary>The extinguisher fitting's kitCarries (Framework 0.334.0): its charges travel with the kit.</summary>
    public const string ExtinguisherStore = "extinguisherCharges";

    public bool TryGetKitShare(string store, out float share)
    {
        share = 0f;
        if (store != ExtinguisherStore || charges < 0) return false;
        share = FireFighting.Rules.extinguisherCharges <= 0 ? 0f : Mathf.Clamp01((float)charges / FireFighting.Rules.extinguisherCharges);
        return true;
    }

    public bool TrySetKitShare(string store, float share)
    {
        if (store != ExtinguisherStore) return false;
        charges = Mathf.RoundToInt(Mathf.Clamp01(share) * FireFighting.Rules.extinguisherCharges);
        return true;
    }

    internal FireZones burning;
    internal int gearSince = -1, engineSince = -1, crewSince = -1;
    /// <summary>The extinguisher's charges; below none before one was ever fitted to this vehicle.</summary>
    internal int charges = -1;
    internal FireZones spent;
    // The part hit last and when, for the cause of a fire; not saved (a fire after a load takes the fallback zone).
    internal FireZones lastHitZone;
    internal int lastHitTick = -1;
    /// <summary>Until when its tyres smoulder after a fire in the running gear went out, or below none (Framework 0.245.0, FR11).</summary>
    internal int smoulderUntil = -1;

    CompPhobosVehicleSystems? Systems => Vehicle?.GetComp<CompPhobosVehicleSystems>();

    /// <summary>An extinguisher in its bracket, whatever its charges.</summary>
    public bool ExtinguisherFittedAtAll => Systems is { Usable: true } systems && systems.Fittings.Grants(FireFighting.ExtinguisherAbility);
    /// <summary>An extinguisher in a bracket not shot away: one that can be used.</summary>
    public bool ExtinguisherFitted => ExtinguisherFittedAtAll && FireFighting.PartWorks(Vehicle, FireFighting.BracketKey);
    /// <summary>The charges left in it, its bracket shot away or not (fault-read 25 Nov 2026, D10: a broken bracket read as "empty"); whether it
    /// can be used is <see cref="ExtinguisherFitted"/>'s answer.</summary>
    public int Charges => ExtinguisherFittedAtAll ? Mathf.Max(0, charges) : 0;
    public FireZones Fitted => FireFighting.FittedSystems(Vehicle);
    /// <summary>Fitted, not spent, and its part not shot away.</summary>
    public FireZones Ready
    {
        get
        {
            FireZones ready = Fitted & ~spent;
            if ((ready & FireZones.EngineBay) != 0 && !FireFighting.PartWorks(Vehicle, FireFighting.EngineSystemKey)) ready &= ~FireZones.EngineBay;
            if ((ready & FireZones.CrewCompartment) != 0 && !FireFighting.PartWorks(Vehicle, FireFighting.CrewSystemKey)) ready &= ~FireZones.CrewCompartment;
            return ready;
        }
    }

    internal void Ignite(FireZones zones, int now)
    {
        burning |= zones;
        if ((zones & FireZones.RunningGear) != 0 && gearSince < 0) gearSince = now;
        if ((zones & FireZones.EngineBay) != 0 && engineSince < 0) engineSince = now;
        if ((zones & FireZones.CrewCompartment) != 0 && crewSince < 0) crewSince = now;
    }

    /// <summary>A zone put out by its suppression system: its clock stops, and the zone that feeds it (running gear to engine, engine to
    /// crew) starts its clock again, so it must burn its full time before it reaches the zone again (fault-read 25 Nov 2026, D2).</summary>
    internal void Quenched(FireZones zone, int now)
    {
        if ((zone & FireZones.RunningGear) != 0) gearSince = -1;
        if ((zone & FireZones.EngineBay) != 0)
        {
            engineSince = -1;
            if ((burning & FireZones.RunningGear) != 0) gearSince = now;
        }
        if ((zone & FireZones.CrewCompartment) != 0)
        {
            crewSince = -1;
            if ((burning & FireZones.EngineBay) != 0) engineSince = now;
        }
    }

    internal double HoursBurning(FireZones zone, int now)
    {
        int since = zone switch { FireZones.RunningGear => gearSince, FireZones.CrewCompartment => crewSince, _ => engineSince };
        return since < 0 ? 0 : (now - since) / (double)GenDate.TicksPerHour;
    }

    internal void ClearZones()
    {
        burning = FireZones.None;
        gearSince = engineSince = crewSince = -1;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref burning, "PhobosFireFighting_Burning");
        Scribe_Values.Look(ref gearSince, "PhobosFireFighting_GearSince", -1);
        Scribe_Values.Look(ref engineSince, "PhobosFireFighting_EngineSince", -1);
        Scribe_Values.Look(ref crewSince, "PhobosFireFighting_CrewSince", -1);
        Scribe_Values.Look(ref charges, "PhobosFireFighting_Charges", -1);
        Scribe_Values.Look(ref spent, "PhobosFireFighting_Spent");
        Scribe_Values.Look(ref smoulderUntil, "PhobosFireFighting_SmoulderUntil", -1);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle == null || !Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer || !ExtinguisherFittedAtAll) yield break;
        if (AttachedFires.Biggest(Vehicle) <= 0f) yield break;
        var use = new Command_Action
        {
            defaultLabel = "PhobosFireFighting_Use".Translate(),
            defaultDesc = "PhobosFireFighting_UseDesc".Translate(Charges.Named("LEFT"), FireFighting.Rules.extinguisherCharges.Named("FULL")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Things/ExtinguisherKit"),
            action = () => FireFighting.UseOrSend(this),
        };
        string? refusal = FireFighting.UseRefusal(this);
        if (refusal == null && FireFighting.UserAboard(Vehicle) == null && FireFighting.Sendable(this) == null)
            refusal = "PhobosFireFighting_NobodyFree".Translate(Vehicle.LabelShort.Named("VEHICLE"));
        if (refusal != null) use.Disable(refusal);
        yield return use;
    }

    public override string? CompInspectStringExtra()
    {
        if (Vehicle == null) return null;
        var lines = new List<string>();
        if (ExtinguisherFittedAtAll)
            lines.Add("PhobosFireFighting_Inspect".Translate(Charges.Named("LEFT"), FireFighting.Rules.extinguisherCharges.Named("FULL")).Resolve());
        FireZones fitted = Fitted;
        if (fitted != FireZones.None)
        {
            FireZones ready = Ready;
            foreach (FireZones zone in new[] { FireZones.EngineBay, FireZones.CrewCompartment })
            {
                if ((fitted & zone) == 0) continue;
                string state = (ready & zone) != 0 ? "PhobosFireFighting_Ready".Translate() : "PhobosFireFighting_Spent".Translate();
                lines.Add("PhobosFireFighting_InspectSystem".Translate(FireFighting.ZoneWord(zone).Named("ZONE"), state.Named("STATE")).Resolve());
            }
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }
}

/// <summary>The vehicles a fire has just caught on, handled one tick later, so a suppression system answers at once (FR10).</summary>
public sealed class GameComponent_PhobosFireFighting : GameComponent
{
    public GameComponent_PhobosFireFighting(Game game) => FireFighting.Ignited.Clear();

    public override void GameComponentTick()
    {
        if (FireFighting.Ignited.Count == 0) return;
        var now = new List<CompPhobosFireFighting>(FireFighting.Ignited);
        FireFighting.Ignited.Clear();
        foreach (CompPhobosFireFighting comp in now)
        {
            if (comp.Vehicle is { Destroyed: false }) FireFighting.Look(comp, rareTick: false);
        }
    }
}

/// <summary>
/// A fire attaching to a vehicle (FR10): the vehicle is only noted, and looked at on the next tick, since the fire is spawned inside this
/// method (never act inside a spawn). A postfix; the game's attaching is untouched.
/// </summary>
[HarmonyPatch(typeof(FireUtility), nameof(FireUtility.TryAttachFire))]
public static class Patch_TryAttachFire_FireFighting
{
    public static void Postfix(Thing t)
    {
        if (t is not VehiclePawn vehicle || FireFighting.CompOf(vehicle) is not { } comp || FireFighting.Ignited.Contains(comp)) return;
        FireFighting.Ignited.Add(comp);
    }
}

/// <summary>Right-click a burning vehicle of the player's with a colonist selected: the colonist uses its extinguisher from beside it
/// (FR10(a)). A refusal says why. Presentation only: the job's end asks the service.</summary>
public class FloatMenuOptionProvider_PhobosExtinguisher : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && pawn.IsColonistPlayerControlled && base.SelectedPawnValid(pawn, context);

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer || FireFighting.CompOf(vehicle) is not { ExtinguisherFittedAtAll: true } comp) yield break;
        if (AttachedFires.Biggest(vehicle) <= 0f) yield break;
        Pawn user = context.FirstSelectedPawn;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        string? refusal = FireFighting.UseRefusal(comp);
        if (refusal == null && !FireFighting.Capable(user)) refusal = "PhobosFireFighting_UserCannot".Translate(user.LabelShort.Named("PAWN"));
        if (refusal == null && !user.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly)) refusal = "NoPath".Translate();
        if (refusal != null)
        {
            yield return new FloatMenuOption("PhobosFireFighting_MenuCannot".Translate(name, refusal.Named("REASON")), null);
            yield break;
        }
        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("PhobosFireFighting_Menu".Translate(name), () =>
        {
            Job job = JobMaker.MakeJob(FireFightingDefOf.PhobosVehicleSystems_UseExtinguisher, vehicle);
            user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }), user, vehicle);
    }
}

/// <summary>A colonist walks to the burning vehicle, takes its extinguisher from the bracket and uses it, a charge spent.</summary>
public class JobDriver_PhobosUseExtinguisher : JobDriver
{
    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => FireFighting.CompOf(Vehicle) is not { } comp || FireFighting.UseRefusal(comp) != null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil use = Toils_General.Wait(FireFighting.Rules.useTicks, TargetIndex.A);
        use.WithProgressBarToilDelay(TargetIndex.A);
        yield return use;
        yield return new Toil
        {
            initAction = () =>
            {
                if (FireFighting.CompOf(Vehicle) is { } comp) FireFighting.UseExtinguisher(comp, pawn);
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}

[DefOf]
public static class FireFightingDefOf
{
    public static JobDef PhobosVehicleSystems_UseExtinguisher = null!;
    static FireFightingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(FireFightingDefOf));
}

/// <summary>Names the player's vehicles on a map whose fitted suppression system is spent, and whose extinguisher is empty (FR10), so a
/// crew is not sent into a fight trusting a system that has already fired. Presentation only.</summary>
public class Alert_PhobosSuppressionSpent : Alert
{
    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();

    public Alert_PhobosSuppressionSpent()
    {
        defaultLabel = "PhobosFireFighting_SpentAlert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn vehicle || FireFighting.CompOf(vehicle) is not { } comp) continue;
                FireZones fitted = comp.Fitted;
                FireZones down = fitted & ~comp.Ready;
                bool empty = comp.ExtinguisherFittedAtAll && comp.Charges <= 0;
                if (down == FireZones.None && !empty) continue;
                culprits.Add(vehicle);
                Seen.Note("vehicles.suppression-alert", vehicle);
                var what = new List<string>();
                foreach (FireZones zone in new[] { FireZones.EngineBay, FireZones.CrewCompartment })
                {
                    if ((down & zone) != 0) what.Add(FireFighting.ZoneWord(zone));
                }
                if (empty) what.Add("PhobosFireFighting_AlertExtinguisher".Translate());
                lines.Add("PhobosFireFighting_SpentLine".Translate(vehicle.LabelShort.Named("VEHICLE"), string.Join(", ", what).Named("WHAT")).Resolve());
            }
        }
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() => "PhobosFireFighting_SpentIntro".Translate() + "\n\n" + string.Join("\n", lines);
}
