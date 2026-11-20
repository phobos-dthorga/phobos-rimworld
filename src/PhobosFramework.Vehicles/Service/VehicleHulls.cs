using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a destroyed vehicle leaves (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM2 and EM3). Vehicle Framework can leave
/// a hull where a vehicle is destroyed (the vehicle's own frame at a tenth of its health, drawn as the vehicle) but none of its callers asks
/// for one; with the setting on, every faction's destroyed vehicle leaves one, except where a design says it never may (a raiders' aircraft
/// is never a prize, the owner's choice). Taking a hull apart gives salvaged parts, not its materials: Vehicle Framework already gave a
/// quarter of them back where the vehicle died, and the parts are another quarter of its worth (SalvageRule).
/// </summary>
[StaticConstructorOnStartup]
public static class VehicleHulls
{
    [ThreadStatic] static int suppressed;

    /// <summary>The salvaged parts item, or null if it is not loaded.</summary>
    public static ThingDef? SalvagedParts => salvaged ??= DefDatabase<ThingDef>.GetNamedSilentFail("PhobosVehicleSystems_SalvagedParts");
    static ThingDef? salvaged;

    /// <summary>While open, a destroyed vehicle leaves no hull: a raiders' aircraft brought down, which is never a prize.</summary>
    public static IDisposable Suppress() => new Scope();

    sealed class Scope : IDisposable
    {
        public Scope() => suppressed++;
        public void Dispose() => suppressed--;
    }

    /// <summary>The cells a hull covers standing here (Framework 0.197.0, for a hull set down from a sling). A hull is a building, so the
    /// game's own footprint is the true one; a vehicle's is Vehicle Framework's (VehicleRect), which differs for an even side facing
    /// south or west. Kept here, with hulls, and away from the code that sets vehicles down, where the game's footprint is a fault
    /// (tests/test_vehicle_set_down_footprint.py).</summary>
    public static CellRect Footprint(Thing hull, IntVec3 cell, Rot4 rotation) => GenAdj.OccupiedRect(cell, rotation, hull.def.size);

    /// <summary>Whether this vehicle, being destroyed, leaves a hull now.</summary>
    public static bool Leaves(VehiclePawn vehicle) =>
        suppressed == 0 && VehicleSystemsMod.Settings.FlagOn("hullsStay") && vehicle.Spawned && vehicle.VehicleDef.buildDef != null;

    /// <summary>
    /// At start-up, every vehicle's frame learns to be a hull: it is taken apart for salvaged parts, not its materials. A frame exists
    /// only as a hull once a vehicle is built (Vehicle Framework turns a finished frame into the vehicle at once), so building is untouched.
    /// </summary>
    static VehicleHulls()
    {
        int given = 0;
        foreach (VehicleBuildDef def in DefDatabase<VehicleBuildDef>.AllDefsListForReading)
        {
            def.comps ??= new List<CompProperties>();
            if (def.comps.Exists(c => c is CompProperties_PhobosHull)) continue;
            def.comps.Add(new CompProperties_PhobosHull());
            def.resourcesFractionWhenDeconstructed = 0f;
            given++;
        }
        if (VehicleSystemsMod.Logger.DebugEnabled) VehicleSystemsMod.Logger.Debug("Hulls: " + given + " vehicle frames give salvaged parts when taken apart.");
    }
}

/// <summary>Vehicle Framework's destruction leaves a hull where the setting says so (Framework 0.187.0, EM2). A prefix that never skips it:
/// it only asks for the hull its own code already makes.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Kill), new[] { typeof(DamageInfo?), typeof(DestroyMode), typeof(bool) })]
public static class VehicleHullOnKill
{
    public static void Prefix(VehiclePawn __instance, DestroyMode destroyMode, ref bool spawnWreckage)
    {
        if (spawnWreckage || destroyMode != DestroyMode.KillFinalize || !VehicleHulls.Leaves(__instance)) return;
        spawnWreckage = true;
        Seen.Note("vehicles.hull-left", __instance);
    }
}

public class CompProperties_PhobosHull : CompProperties
{
    public CompProperties_PhobosHull() => compClass = typeof(CompPhobosHull);
}

/// <summary>A destroyed vehicle's hull: says what it is, and gives salvaged parts when taken apart (Framework 0.187.0, EM3).</summary>
public class CompPhobosHull : ThingComp
{
    public override string CompInspectStringExtra() => "PhobosVehicleSystems_HullInspect".Translate(Parts().Named("COUNT")).Resolve();

    /// <summary>The salvaged parts it gives: a quarter of its vehicle's worth (SalvageRule).</summary>
    public int Parts()
    {
        if (VehicleHulls.SalvagedParts is not { } salvaged || parent is not VehicleBuilding { VehicleDef: { } vehicle }) return 0;
        return Phobos.Framework.Rules.SalvageRule.HullParts(vehicle.BaseMarketValue, Phobos.Framework.Rules.SalvageRule.HullShare, salvaged.BaseMarketValue);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (mode != DestroyMode.Deconstruct || previousMap == null || VehicleHulls.SalvagedParts is not { } salvaged) return;
        int count = Parts();
        if (count <= 0) return;
        Thing parts = ThingMaker.MakeThing(salvaged);
        parts.stackCount = count;
        GenPlace.TryPlaceThing(parts, parent.Position, previousMap, ThingPlaceMode.Near);
        if (Seen.Wants("vehicles.hull-salvaged", parts)) Seen.Note("vehicles.hull-salvaged", parts, count.ToString());
    }
}
