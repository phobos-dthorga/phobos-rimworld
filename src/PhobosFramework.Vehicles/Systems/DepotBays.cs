using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

public sealed class CompProperties_PhobosDepotBay : CompProperties
{
    /// <summary>
    /// The parking pad in front of the bay, in cells, starting at the bay's front edge: a vehicle whose centre stands on it is in the bay.
    /// Authored. Framework 0.94.2 (owner, 11 Nov 2026: "needs to be brought back a little"): 6 long and against the bay, where it was 10
    /// long a cell out, sized for a whole tanker. A vehicle parks nose to the bay (<see cref="CompPhobosDepotBay.ParkingCell"/>), so a
    /// 10-cell tanker's centre is 6 cells out and still on the pad, while a short truck's is close in.
    /// </summary>
    public int padWidth = 3;
    public int padLength = 6;
    /// <summary>How much fuel the pump moves in an hour. Authored.</summary>
    public float pumpPerHour = 30f;
    /// <summary>The share of the usual work a job takes on a vehicle in its bay. Authored.</summary>
    public float workFactor = 0.7f;

    public CompProperties_PhobosDepotBay() => compClass = typeof(CompPhobosDepotBay);
}

/// <summary>A vehicle's home (docs/development/vehicle-depot-bays.md): one vehicle to a bay, a pad it parks on, a pump that fuels it and a shelf for its parts. Buttons read this and call <see cref="DepotBays"/>.</summary>
public sealed class CompPhobosDepotBay : ThingComp
{
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;

    VehiclePawn? assigned;
    CompRefuelable? tank;
    CompPowerTrader? power;

    /// <summary>What the pump last did, for the inspect line. Derived, never saved.</summary>
    string? status;

    public CompProperties_PhobosDepotBay Props => (CompProperties_PhobosDepotBay)props;
    public VehiclePawn? Assigned => assigned is { Destroyed: false } ? assigned : null;
    public CellRect Pad => PadFor(parent.Position, parent.Rotation, Props);

    /// <summary>The pad's cells: from the row in front of the bay out to its length, its width centred on the bay. Built from its two
    /// corners, so every facing gives the same pad (the game's footprint rule shifts an even length by a cell on two of the four).</summary>
    public static CellRect PadFor(IntVec3 position, Rot4 rotation, CompProperties_PhobosDepotBay props)
    {
        IntVec3 across = rotation.RighthandCell;
        IntVec3 near = position + rotation.FacingCell - across * ((props.padWidth - 1) / 2);
        IntVec3 far = position + rotation.FacingCell * props.padLength + across * (props.padWidth / 2);
        return CellRect.FromLimits(near, far);
    }

    /// <summary>Where a vehicle is sent to park: its centre half its length out, so its nose stops at the bay, and never off the pad.</summary>
    public IntVec3 ParkingCell(VehiclePawn vehicle)
    {
        int length = UnityEngine.Mathf.Max(vehicle.def.size.x, vehicle.def.size.z);
        int cellsOut = UnityEngine.Mathf.Clamp(1 + length / 2, 1, Props.padLength);
        return parent.Position + parent.Rotation.FacingCell * cellsOut;
    }

    public bool Holds(VehiclePawn vehicle) => vehicle.Spawned && vehicle.Map == parent.Map && Pad.Contains(vehicle.Position);

    internal void SetAssigned(VehiclePawn? vehicle) => assigned = vehicle;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref assigned, "PhobosDepotBayVehicle");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        tank = parent.GetComp<CompRefuelable>();
        power = parent.GetComp<CompPowerTrader>();
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        status = null;
        VehiclePawn? vehicle = Assigned;
        if (vehicle == null || !Holds(vehicle)) return;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel == null || fuel.Props.electricPowered || tank == null) return;
        if (fuel.Props.fuelType != tank.Props.fuelFilter.AnyAllowedDef)
        {
            status = "PhobosVehicleSystems_BayWrongFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
            return;
        }
        if (power is { PowerOn: false } || !tank.HasFuel) return;
        float amount = UnityEngine.Mathf.Min(fuel.FuelCapacity - fuel.Fuel, tank.Fuel, Props.pumpPerHour * HoursPerRareTick);
        if (amount <= 0f) return;
        tank.ConsumeFuel(amount);
        fuel.Refuel(amount);
        status = "PhobosVehicleSystems_BayRefuelling".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    public override string CompInspectStringExtra()
    {
        VehiclePawn? vehicle = Assigned;
        if (vehicle == null) return "PhobosVehicleSystems_BayEmpty".Translate().Resolve();
        return status ?? "PhobosVehicleSystems_BayHomeOf".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction == Faction.OfPlayer) yield return BayGizmos.AssignButton(this);
    }
}

/// <summary>The service the bays' buttons and the vehicle's own comp go through.</summary>
public static class DepotBays
{
    static List<ThingDef>? defs;

    static List<ThingDef> Defs => defs ??= DefDatabase<ThingDef>.AllDefsListForReading.Where(d => d.GetCompProperties<CompProperties_PhobosDepotBay>() != null).ToList();

    public static IEnumerable<CompPhobosDepotBay> On(Map map)
    {
        foreach (ThingDef def in Defs)
        {
            foreach (Building building in map.listerBuildings.AllBuildingsColonistOfDef(def))
            {
                CompPhobosDepotBay? bay = building.GetComp<CompPhobosDepotBay>();
                if (bay != null) yield return bay;
            }
        }
    }

    public static CompPhobosDepotBay? Of(VehiclePawn vehicle) => vehicle.Spawned ? On(vehicle.Map).FirstOrDefault(b => b.Assigned == vehicle) : null;

    /// <summary>The player's vehicles on this bay's map, for choosing one.</summary>
    public static IEnumerable<VehiclePawn> Candidates(CompPhobosDepotBay bay) =>
        bay.parent.Map.mapPawns.AllPawnsSpawned.OfType<VehiclePawn>().Where(v => v.Faction == Faction.OfPlayer);

    /// <summary>Makes this vehicle the bay's, taking it from any other bay first: a bay holds one vehicle, and a vehicle has one bay.</summary>
    public static void Assign(CompPhobosDepotBay bay, VehiclePawn? vehicle)
    {
        if (vehicle != null)
        {
            foreach (CompPhobosDepotBay other in On(bay.parent.Map))
            {
                if (other.Assigned == vehicle) other.SetAssigned(null);
            }
        }
        bay.SetAssigned(vehicle);
        Messages.Message(vehicle != null
            ? "PhobosVehicleSystems_BayAssigned".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve()
            : "PhobosVehicleSystems_BayCleared".Translate().Resolve(), bay.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Why this vehicle cannot be sent home now, or null.</summary>
    public static string? CannotGo(VehiclePawn vehicle, CompPhobosDepotBay bay)
    {
        if (bay.Holds(vehicle)) return "PhobosVehicleSystems_BayAlreadyThere".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!vehicle.CanMoveFinal) return "PhobosVehicleSystems_BayNoDriver".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        foreach (CompPhobosSetUp setUp in vehicle.AllComps.OfType<CompPhobosSetUp>())
        {
            if (setUp.HoldRefusal is { } held) return held;
        }
        AcceptanceReport draft = vehicle.CanDraft();
        if (!draft.Accepted) return draft.Reason;
        if (!vehicle.CanReachVehicle(bay.ParkingCell(vehicle), PathEndMode.OnCell, Danger.Deadly)) return "PhobosVehicleSystems_BayCannotReach".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        return null;
    }

    /// <summary>Drives the vehicle to its bay. Only ever on the player's order, never by itself (owner, 9 Nov 2026).</summary>
    public static void GoTo(CompPhobosVehicleSystems comp, CompPhobosDepotBay bay)
    {
        VehiclePawn vehicle = comp.Vehicle;
        string? refused = CannotGo(vehicle, bay);
        if (refused != null)
        {
            Messages.Message(refused, vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (!vehicle.Drafted) vehicle.ignition.Drafted = true;
        vehicle.vehiclePather.StartPath(new LocalTargetInfo(bay.ParkingCell(vehicle)), PathEndMode.OnCell);
        comp.GoingToBay = true;
        Messages.Message("PhobosVehicleSystems_BayGoing".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }
}
