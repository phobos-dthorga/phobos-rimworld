using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What a loading chute does at each look (Framework 0.126.0; docs/development/universal-hoppers.md, UH13): passes up to a few stacks from
/// its cell into the cargo of a standing vehicle of its own side that covers its spout, as much as the vehicle's room allows (GrabRule's lift
/// rule with no lift limit). It never loads another side's vehicle, and holds its goods when nothing is there.
/// </summary>
public static class LoadingChute
{
    /// <summary>Does one look. Returns the inspect line saying what it waits for, or null while it is loading or empty.</summary>
    public static string? Pass(CompPhobosLoadingChute chute)
    {
        Thing building = chute.parent;
        Map map = building.Map;
        List<Thing> held = building.Position.GetThingList(map).Where(t => t.def.category == ThingCategory.Item).ToList();
        if (held.Count == 0) return null;
        VehiclePawn? vehicle = VehicleAt(chute.Spout, map, building.Faction);
        if (vehicle == null) return "PhobosVehicleSystems_ChuteNoVehicle".Translate().Resolve();
        if (vehicle.vehiclePather is { Moving: true }) return "PhobosVehicleSystems_ChuteVehicleMoving".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        double free = vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) - MassUtility.InventoryMass(vehicle);
        int passed = 0;
        foreach (Thing thing in held)
        {
            if (passed >= chute.Props.stacksPerLook) break;
            float unit = thing.GetStatValue(StatDefOf.Mass);
            int count = GrabRule.CountForLift(thing.stackCount, unit, double.MaxValue, free);
            if (count <= 0) continue;
            int moved = vehicle.AddOrTransfer(thing, count);
            if (moved <= 0) continue;
            free -= moved * unit;
            passed++;
            Seen.Note("vehicles.chute-loaded", vehicle, building.LabelShort);
        }
        if (passed > 0) return null;
        return "PhobosVehicleSystems_ChuteVehicleFull".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    /// <summary>A spawned vehicle of this side whose footprint covers the cell, or null.</summary>
    static VehiclePawn? VehicleAt(IntVec3 cell, Map map, Faction? side)
    {
        if (side == null || !cell.InBounds(map)) return null;
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(side))
        {
            if (pawn is VehiclePawn vehicle && vehicle.OccupiedRect().Contains(cell)) return vehicle;
        }
        return null;
    }
}
