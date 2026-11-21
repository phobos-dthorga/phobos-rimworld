using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Which cabin is being made, for its generation step: set around the game's pocket map generation, as the game's own portal does.</summary>
public static class CabinGeneration
{
    public static CompPhobosVehicleCabin? Generating;
    public static CabinPlan? Plan;
}

/// <summary>
/// Lays out a walk-in cabin from its plan: a metal floor under a roof over every cell, and the seats, litters, tanks and way out where the
/// plan puts them. The map's edge is the cabin's wall, so no wall is built. Seats face the aisle; litters lie along the cabin.
/// </summary>
public class GenStep_PhobosVehicleCabin : GenStep
{
    public override int SeedPart => 482_270_311;

    public override void Generate(Map map, GenStepParams parms)
    {
        CompPhobosVehicleCabin? comp = CabinGeneration.Generating;
        CabinPlan? plan = CabinGeneration.Plan;
        if (comp == null || plan == null) return;
        TerrainDef floor = DefDatabase<TerrainDef>.GetNamedSilentFail("MetalTile") ?? TerrainDefOf.Concrete;
        foreach (IntVec3 cell in map.AllCells)
        {
            map.terrainGrid.SetTerrain(cell, floor);
            map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
        }
        for (int x = 0; x < plan.Width; x++)
        {
            for (int z = 0; z < plan.Length; z++)
            {
                var cell = new IntVec3(x, 0, z);
                switch (plan.Cells[x, z])
                {
                    case CabinCell.Seat:
                        // A seat on the left half faces right, across the aisle, and the other way on the right half.
                        Place(VehicleCabinDefOf.PhobosCabinSeat, cell, map, x < plan.Width / 2f - 0.5f ? Rot4.East : Rot4.West);
                        break;
                    case CabinCell.LitterFoot:
                        Place(VehicleCabinDefOf.PhobosCabinLitter, cell, map, Rot4.North);
                        break;
                    case CabinCell.BedFoot:
                        // The roomy deck tent's beds are its own, comfier ones (Framework 0.234.0; the owner's choice of 22 Nov 2026).
                        Place(DeckTents.Roomy(comp.Vehicle) ? VehicleCabinDefOf.PhobosCabinBedRoomy : VehicleCabinDefOf.PhobosCabinBed, cell, map, Rot4.North);
                        break;
                    case CabinCell.Tank:
                        Place(VehicleCabinDefOf.PhobosCabinTank, cell, map, Rot4.North);
                        break;
                    case CabinCell.WayOut:
                        if (Place(VehicleCabinDefOf.PhobosCabinWayOut, cell, map, Rot4.North) is Building_PhobosCabinWayOut wayOut) wayOut.Vehicle = comp.Vehicle;
                        break;
                }
            }
        }
    }

    static Thing Place(ThingDef def, IntVec3 cell, Map map, Rot4 rot)
    {
        Thing thing = ThingMaker.MakeThing(def);
        thing.SetFaction(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, rot);
    }
}

/// <summary>
/// The way out of a walk-in cabin: leads to wherever its vehicle now stands, or refuses with the reason while it is in the air. Saves only its
/// vehicle. Its button jumps the view to the vehicle.
/// </summary>
public class Building_PhobosCabinWayOut : Building
{
    public VehiclePawn? Vehicle;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        if (Vehicle == null) yield break;
        var view = new Command_Action
        {
            defaultLabel = "PhobosCabin_ViewVehicle".Translate(Vehicle.LabelShort.Named("VEHICLE")),
            defaultDesc = "PhobosCabin_ViewVehicleDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")),
            icon = Vehicle.VehicleDef.uiIcon is { } icon && icon != BaseContent.BadTex ? icon : SystemIcons.Switch,
            action = () => CameraJumper.TryJumpAndSelect(Vehicle),
        };
        if (!Vehicle.Spawned) view.Disable("PhobosCabin_LeaveInFlight".Translate(Vehicle.LabelShort.Named("VEHICLE")));
        yield return view;
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        string line = VehicleCabins.LeaveRefusal(Vehicle) is { } why ? why : "PhobosCabin_WayOutReady".Translate(Vehicle!.LabelShort.Named("VEHICLE")).Resolve();
        return text.NullOrEmpty() ? line : text + "\n" + line;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref Vehicle, "PhobosCabin_Vehicle");
    }
}
