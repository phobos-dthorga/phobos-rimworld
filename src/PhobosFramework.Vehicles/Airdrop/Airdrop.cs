using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Marks an aircraft that can drop people, cargo and vehicles by parachute (D'thorga Framework 0.288.0; docs/development/airdrop.md), put on
/// its VehicleDef in XML. Another author's aircraft is given it by a patch, with <see cref="onlyWithStockSetting"/>.
/// </summary>
public class PhobosAirdropExtension : DefModExtension
{
    /// <summary>Its jump doors: people leave one a second from each (about one a second per door, secondary sources only).</summary>
    public int jumpDoors = 2;
    /// <summary>Bundles of cargo down its ramp a second (authored).</summary>
    public float bundlesPerSecond = 2f;
    /// <summary>Vehicles it drops on one pass, one after another (authored: the C-17 drops a sequence; one at a time here).</summary>
    public int platforms = 1;
    /// <summary>The heaviest vehicle it may drop on a platform, kilograms.</summary>
    public float heaviestPlatformKg = 9000f;
    /// <summary>Other mods' comps, by full type name, whose held vehicles are its hold (read by name; the C-17's CompC17Airlifter).</summary>
    public List<string> holdComps = new();
    /// <summary>Another author's aircraft: it drops on ours only with the player's "other authors' aircraft" setting.</summary>
    public bool onlyWithStockSetting;
    /// <summary>
    /// Another author's own airdrop mode, replaced by ours while the setting is on: the comp's full type name, its field and the value that
    /// means "airdrop" (the C-17's CompC17Airlifter, mode, Airdrop). Left out, ours is offered beside the aircraft's other arrivals.
    /// </summary>
    public string? stockModeComp;
    public string stockModeField = "mode";
    public string stockModeValue = "Airdrop";
    /// <summary>A raid's aircraft carries this many spare personnel parachutes beyond one each (authored).</summary>
    public int raiderSpareChutes;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (jumpDoors < 1 || bundlesPerSecond <= 0f || platforms < 0 || heaviestPlatformKg < 0f) yield return "PhobosAirdropExtension: jumpDoors, bundlesPerSecond, platforms and heaviestPlatformKg must be positive";
    }
}

[DefOf]
public static class AirdropDefOf
{
    public static ThingDef Phobos_PersonnelParachute = null!;
    public static ThingDef Phobos_CargoParachute = null!;
    public static ThingDef Phobos_HeavyDropRig = null!;
    public static ThingDef Phobos_SpentParachute = null!;
    public static ThingDef Phobos_SpentCargoParachute = null!;
    public static ThingDef Phobos_SpentDropPlatform = null!;
    public static ThingDef Phobos_GuidedCargoParachute = null!;
    public static ThingDef Phobos_AnkleBraces = null!;
    public static ThingDef Phobos_LandedCargoBundle = null!;
    public static ThingDef Phobos_DropZoneMarker = null!;
    public static ThingDef Phobos_ParachuteFaller_Person = null!;
    public static ThingDef Phobos_ParachuteFaller_Bundle = null!;
    public static ThingDef Phobos_ParachuteFaller_Vehicle = null!;
    public static ThoughtDef Phobos_JumpedFromAircraft = null!;
    public static ThoughtDef Phobos_HardLanding = null!;
    public static RecordDef Phobos_Jumps = null!;

    static AirdropDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(AirdropDefOf));
}

/// <summary>
/// What an aircraft can drop now, and taking it out (Framework 0.288.0): its passengers, its cargo in bundles of up to a tonne, and the
/// vehicles in its hold, each needing its parachute from the cargo. Read by the buttons, the arrival options and the drop itself.
/// </summary>
public static class Airdrop
{
    /// <summary>The aircraft's airdrop figures, when it may drop now: carries the extension and, if another author's, the setting is on.</summary>
    public static PhobosAirdropExtension? Ext(VehiclePawn? vehicle)
    {
        if (vehicle?.def.GetModExtension<PhobosAirdropExtension>() is not { } ext) return null;
        return ext.onlyWithStockSetting && !VehicleSystemsMod.Settings.stockAircraftAirdrop ? null : ext;
    }

    /// <summary>Whether this is a parachute or rig (packed or spent): never dropped as cargo.</summary>
    public static bool IsChute(ThingDef def) =>
        def == AirdropDefOf.Phobos_PersonnelParachute || def == AirdropDefOf.Phobos_CargoParachute || def == AirdropDefOf.Phobos_HeavyDropRig
        || def == AirdropDefOf.Phobos_GuidedCargoParachute;

    /// <summary>People who jump: everyone in a seat that does nothing (passengers, a crew chief), awake.</summary>
    public static IEnumerable<Pawn> Jumpers(VehiclePawn v)
    {
        foreach (VehicleRoleHandler handler in v.handlers)
        {
            if (handler.role.HandlingTypes != 0 || handler.thingOwner == null) continue;
            foreach (Pawn pawn in handler.thingOwner.InnerListForReading)
            {
                if (!pawn.Downed && !pawn.Dead && pawn.RaceProps.Humanlike) yield return pawn;
            }
        }
    }

    /// <summary>Its cargo that drops in bundles: everything in its cargo but people and parachutes.</summary>
    public static IEnumerable<Thing> Cargo(VehiclePawn v) =>
        v.inventory.innerContainer.InnerListForReading.Where(t => t is not Pawn && t is not Corpse && !IsChute(t.def));

    /// <summary>The vehicles in its hold: ours (CompPhobosHold) and any other mod's named in the extension.</summary>
    public static IEnumerable<VehiclePawn> HeldVehicles(VehiclePawn v)
    {
        if (v.GetComp<CompPhobosHold>() is { } hold)
        {
            foreach (VehiclePawn held in hold.Held) yield return held;
        }
        if (v.def.GetModExtension<PhobosAirdropExtension>() is not { holdComps.Count: > 0 } ext) yield break;
        foreach (ThingComp comp in v.AllComps)
        {
            if (comp is not IThingHolder holder || !ext.holdComps.Contains(comp.GetType().FullName ?? "")) continue;
            foreach (Thing thing in holder.GetDirectlyHeldThings().ToList())
            {
                if (thing is VehiclePawn vehicle) yield return vehicle;
            }
        }
    }

    /// <summary>Takes a held vehicle out of whichever holder has it, ready to drop. False when nothing held it.</summary>
    public static bool TakeHeld(VehiclePawn v, VehiclePawn held)
    {
        if (held.holdingOwner == null) return false;
        return held.holdingOwner.Remove(held);
    }

    /// <summary>How many of a parachute it carries.</summary>
    public static int Count(VehiclePawn v, ThingDef def) => v.inventory.innerContainer.TotalStackCountOfDef(def);

    /// <summary>Uses one parachute from its cargo. False when it has none.</summary>
    public static bool UseOne(VehiclePawn v, ThingDef def) => UseOne(v, def, out _);

    /// <summary>
    /// Uses one parachute from its cargo, the best packed first, and gives the packer's quality (the game's, 0 Awful to 6 Legendary; -1 when
    /// it has none). False when it has none (Framework 0.293.0).
    /// </summary>
    public static bool UseOne(VehiclePawn v, ThingDef def, out int quality)
    {
        quality = -1;
        Thing? stack = v.inventory.innerContainer.InnerListForReading.Where(t => t.def == def)
            .OrderByDescending(t => t.TryGetQuality(out QualityCategory q) ? (int)q : -1).FirstOrDefault();
        if (stack == null) return false;
        if (stack.TryGetQuality(out QualityCategory found)) quality = (int)found;
        Thing one = stack.SplitOff(1);
        if (!one.Destroyed) one.Destroy();
        return true;
    }

    /// <summary>How many cargo parachutes it carries, guided and plain (Framework 0.293.0).</summary>
    public static int CargoChutes(VehiclePawn v) => Count(v, AirdropDefOf.Phobos_GuidedCargoParachute) + Count(v, AirdropDefOf.Phobos_CargoParachute);

    /// <summary>The cargo parachute it uses next: a guided one while it has any, as real crews choose them for precision; null when none.</summary>
    public static ThingDef? NextCargoChute(VehiclePawn v) =>
        Count(v, AirdropDefOf.Phobos_GuidedCargoParachute) > 0 ? AirdropDefOf.Phobos_GuidedCargoParachute
        : Count(v, AirdropDefOf.Phobos_CargoParachute) > 0 ? AirdropDefOf.Phobos_CargoParachute : null;

    /// <summary>
    /// Takes the next bundle out of its cargo, up to a tonne (ParachuteRule.BundleKilograms): whole stacks while they fit, then part of the
    /// next one. Null when no cargo is left. A thing heavier than a tonne by itself goes alone.
    /// </summary>
    public static List<Thing>? TakeBundle(VehiclePawn v)
    {
        var bundle = new List<Thing>();
        double kg = 0;
        foreach (Thing thing in Cargo(v).ToList())
        {
            double each = thing.GetStatValue(StatDefOf.Mass);
            double room = ParachuteRule.BundleKilograms - kg;
            int count = each <= 0 ? thing.stackCount : Math.Min(thing.stackCount, (int)Math.Floor(room / each));
            if (count <= 0)
            {
                if (bundle.Count == 0) count = 1;
                else break;
            }
            Thing taken = count >= thing.stackCount ? thing : thing.SplitOff(count);
            v.inventory.innerContainer.Remove(taken);
            bundle.Add(taken);
            kg += each * taken.stackCount;
            if (kg >= ParachuteRule.BundleKilograms) break;
        }
        return bundle.Count == 0 ? null : bundle;
    }

    /// <summary>How many bundles its cargo makes now (without taking any), for the buttons and messages.</summary>
    public static int BundlesNeeded(VehiclePawn v)
    {
        double kg = 0;
        foreach (Thing thing in Cargo(v)) kg += thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
        return kg <= 0 ? 0 : (int)Math.Ceiling(kg / ParachuteRule.BundleKilograms);
    }

    /// <summary>The vehicle's weight for a platform: Vehicle Framework's mass stat, as the C-17's own hold weighs it.</summary>
    public static float VehicleKg(VehiclePawn held) => held.GetStatValue(VehicleStatDefOf.Mass);

    /// <summary>What it would drop now and what it would hold back for want of parachutes, in words; or why it cannot drop at all.</summary>
    public static string? Refusal(VehiclePawn v, out string summary)
    {
        summary = "";
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (Ext(v) is not { } ext) return "PhobosAirdrop_RefuseCannot".Translate(who).Resolve();
        int people = Jumpers(v).Count(), bundles = BundlesNeeded(v), vehicles = HeldVehicles(v).Count(h => VehicleKg(h) <= ext.heaviestPlatformKg);
        if (people + bundles + vehicles == 0) return "PhobosAirdrop_RefuseNothing".Translate(who).Resolve();
        int chutes = Count(v, AirdropDefOf.Phobos_PersonnelParachute), cargoChutes = CargoChutes(v),
            rigs = Count(v, AirdropDefOf.Phobos_HeavyDropRig);
        int goPeople = Math.Min(people, chutes), goBundles = Math.Min(bundles, cargoChutes), goVehicles = Math.Min(vehicles, rigs);
        if (goPeople + goBundles + goVehicles == 0) return "PhobosAirdrop_RefuseNoChutes".Translate(who).Resolve();
        summary = "PhobosAirdrop_Summary".Translate(goPeople.Named("PEOPLE"), goBundles.Named("BUNDLES"), goVehicles.Named("VEHICLES")).Resolve();
        if (goPeople < people || goBundles < bundles || goVehicles < vehicles)
        {
            summary += " " + "PhobosAirdrop_SummaryHeldBack".Translate((people - goPeople).Named("PEOPLE"), (bundles - goBundles).Named("BUNDLES"),
                (vehicles - goVehicles).Named("VEHICLES")).Resolve();
        }
        return null;
    }

    /// <summary>
    /// Whether the aircraft's own airdrop mode is set now (another author's: the comp, field and value its extension names, read by name),
    /// so ours replaces it. True when the extension names no such mode.
    /// </summary>
    public static bool StockModeOn(VehiclePawn v, PhobosAirdropExtension ext)
    {
        if (ext.stockModeComp == null) return true;
        foreach (ThingComp comp in v.AllComps)
        {
            if (comp.GetType().FullName != ext.stockModeComp) continue;
            object? value = HarmonyLib.AccessTools.Field(comp.GetType(), ext.stockModeField)?.GetValue(comp);
            return value?.ToString() == ext.stockModeValue;
        }
        return false;
    }
}
