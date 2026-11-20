using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A field machine drives its rows straight (the fault-read of 21 Nov 2026, R3). A row is one drive to its far end, and Vehicle Framework
/// takes the cheapest way there: it adds each thing's path cost to a cell (PathGridCalculator.ThingCost), and a crop plant's is 14 against
/// bare soil's 2, so the cheapest way along a row of standing crop was beside it, over the strip already cut. Every vehicle with a field run
/// therefore counts plants it can drive through as costing nothing, through Vehicle Framework's own per-thing costs on the vehicle's Def
/// (customThingCosts): standing crop and cut ground cost the same, and the straight line is the cheapest. Trees and anything else a vehicle
/// cannot pass are left as they are, and so is any cost a Def already gives a plant. Done once, when Defs are loaded, before any map
/// works out its path costs. A real machine is not slowed by the crop it works either. Whether it holds a machine to its row in play is
/// still to be seen there.
/// </summary>
[StaticConstructorOnStartup]
public static class FieldMachinePaths
{
    static FieldMachinePaths()
    {
        List<ThingDef> plants = new();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.category == ThingCategory.Plant && def.plant is { IsTree: false } && def.passability != Traversability.Impassable) plants.Add(def);
        }
        int machines = 0;
        foreach (VehicleDef vehicle in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            if (vehicle.GetCompProperties<CompProperties_PhobosFieldRun>() == null || vehicle.properties?.customThingCosts is not { } costs) continue;
            foreach (ThingDef plant in plants)
            {
                if (!costs.ContainsKey(plant)) costs[plant] = 0;
            }
            machines++;
        }
        if (machines > 0 && VehicleSystemsMod.Logger.DebugEnabled)
        {
            VehicleSystemsMod.Logger.Debug("Field machines drive through " + plants.Count + " kinds of plant at no path cost: " + machines + " kinds of vehicle.");
        }
    }
}
