using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle driving itself on a map, on an errand of ours (Framework 0.143.0; docs/development/vehicle-gathering-runs.md): whether it can now
/// and why not, starting its engine, finding a place to stand near a cell, ordering the drive, telling our order from the player's, and
/// halting. It drives with the game's own Goto job through the job tracker's ordered job, as Vehicle Framework's own right-click does
/// (FloatMenuOptionProvider_OrderVehicle.PawnGotoAction, VehiclePathFollower.TryOrderMoveTo), so nothing is patched. Any errand a vehicle runs
/// by itself goes through here: a gathering run today, an unloading trip or a fuel run later.
/// </summary>
public static class VehicleAutoDrive
{
    /// <summary>How many places on a ring may each be tested for a way there, nearest first: the test is Vehicle Framework's region search.</summary>
    const int MaxWayTestsPerRing = 6;

    /// <summary>
    /// Why the vehicle cannot drive itself now, or null. Vehicle Framework decides whether it may move and whether it has the crew to drive (a
    /// driver aboard, or a vehicle that needs none: VehiclePawn.CanMove, CanDraft and CanMoveFinal); ours add the hitch, set-up work and fuel.
    /// </summary>
    public static string? CannotDrive(VehiclePawn vehicle)
    {
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!vehicle.Spawned) return "PhobosAutoDrive_NotOnMap".Translate().Resolve();
        if (Hitches.Towing(vehicle) is { } towedBy)
        {
            return "PhobosAutoDrive_Towed".Translate(name, towedBy.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        foreach (CompPhobosSetUp setUp in vehicle.AllComps.OfType<CompPhobosSetUp>())
        {
            if (setUp.HoldRefusal is { } held) return held;
        }
        if (vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosAutoDrive_NoFuel".Translate(name).Resolve();
        if (!vehicle.CanMove) return "PhobosAutoDrive_CannotMove".Translate(name).Resolve();
        if (!vehicle.Drafted)
        {
            AcceptanceReport draft = vehicle.CanDraft();
            if (!draft.Accepted) return draft.Reason.NullOrEmpty() ? "PhobosAutoDrive_NoDriver".Translate(name).Resolve() : draft.Reason;
        }
        if (!vehicle.CanMoveFinal) return "PhobosAutoDrive_NoDriver".Translate(name).Resolve();
        return null;
    }

    /// <summary>
    /// Switches the engine on if it is off, as a driver would. Returns whether it is on now; <paramref name="started"/> says whether this call
    /// started it, so the errand can switch it off again at its end. Vehicle Framework refuses with its own message without the crew to run it.
    /// </summary>
    public static bool StartEngine(VehiclePawn vehicle, out bool started)
    {
        started = false;
        if (vehicle.Drafted) return true;
        vehicle.ignition.Drafted = true;
        started = vehicle.Drafted;
        return started;
    }

    /// <summary>Switches the engine off, if it is on and the vehicle is standing.</summary>
    public static void StopEngine(VehiclePawn vehicle)
    {
        if (vehicle.Drafted && vehicle.vehiclePather is not { Moving: true }) vehicle.ignition.Drafted = false;
    }

    /// <summary>
    /// The nearest place to the vehicle on the square ring <paramref name="ring"/> cells round <paramref name="target"/> where its body fits,
    /// no other vehicle stands and it can drive: Vehicle Framework's own tests, as its right-click runs them (PathingHelper.TryFindNearestStandableCell).
    /// </summary>
    public static bool TryFindStandCell(VehiclePawn vehicle, IntVec3 target, int ring, out IntVec3 cell)
    {
        cell = IntVec3.Invalid;
        Map map = vehicle.Map;
        IntVec3 from = vehicle.Position;
        List<IntVec3> places = CellRect.CenteredOn(target, ring).EdgeCells
            .Where(c => c.InBounds(map))
            .OrderBy(c => c.DistanceToSquared(from))
            .ToList();
        int wayTests = 0;
        foreach (IntVec3 place in places)
        {
            if (!place.Standable(vehicle, map) || !vehicle.DrivableRectOnCell(place, Ext_Vehicles.DestinationHitboxReq.AnyRotation)) continue;
            if (place == from)
            {
                cell = place;
                return true;
            }
            if (PathingHelper.AnyVehicleBlockingPathAt(place, vehicle) != null) continue;
            if (wayTests++ >= MaxWayTestsPerRing) return false;
            if (!vehicle.CanReachVehicle(place, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn)) continue;
            cell = place;
            return true;
        }
        return false;
    }

    /// <summary>Orders the drive to a cell, with the engine already running. False if the job tracker refused it.</summary>
    public static bool DriveTo(VehiclePawn vehicle, IntVec3 cell)
    {
        if (!vehicle.Drafted) return false;
        Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
        return vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }

    /// <summary>Whether the vehicle's job now is our drive to this cell.</summary>
    public static bool HeadingFor(VehiclePawn vehicle, IntVec3 cell) => vehicle.CurJobDef == JobDefOf.Goto && vehicle.CurJob.targetA.Cell == cell;

    /// <summary>Whether it is on a drive somewhere else: a Goto the player or someone else gave.</summary>
    public static bool HeadingElsewhere(VehiclePawn vehicle, IntVec3 cell) => vehicle.CurJobDef == JobDefOf.Goto && vehicle.CurJob.targetA.Cell != cell;

    /// <summary>Whether it is rolling now.</summary>
    public static bool Moving(VehiclePawn vehicle) => vehicle.vehiclePather is { Moving: true };

    /// <summary>Stops our drive where it is; another order is left alone.</summary>
    public static void Halt(VehiclePawn vehicle, IntVec3 cell)
    {
        if (!HeadingFor(vehicle, cell)) return;
        vehicle.jobs.EndCurrentJob(JobCondition.InterruptForced);
        vehicle.vehiclePather?.StopDead();
    }

    /// <summary>Calls off whatever drive it was given before an errand of ours takes it over: the player has just asked for the errand.</summary>
    public static void ClearOrders(VehiclePawn vehicle)
    {
        if (vehicle.CurJobDef == JobDefOf.Goto) vehicle.jobs.EndCurrentJob(JobCondition.InterruptForced);
        if (Moving(vehicle)) vehicle.vehiclePather?.StopDead();
    }
}
