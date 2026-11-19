using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
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
    /// driver aboard, or a vehicle that needs none: VehiclePawn.CanMove, CanDraft and CanMoveFinal); ours add the hitch, fuel, and every refusal
    /// of ours to move (MoveRefusals: worn-out brakes and other system stages, set-up work, a boat on the bottom). Before Framework 0.175.1 the
    /// system stages were left out, so an errand could drive a truck the player's own order would not (docs/development/vehicles-at-work-tie-ins.md, F1).
    /// </summary>
    public static string? CannotDrive(VehiclePawn vehicle)
    {
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!vehicle.Spawned) return "PhobosAutoDrive_NotOnMap".Translate().Resolve();
        if (Hitches.Towing(vehicle) is { } towedBy)
        {
            return "PhobosAutoDrive_Towed".Translate(name, towedBy.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        if (MoveRefusals.For(vehicle) is { } refused)
        {
            if (Seen.Wants("vehicles.errand-refused", vehicle)) Seen.Note("vehicles.errand-refused", vehicle, refused);
            return refused;
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

    /// <summary>
    /// Where the vehicle stands, and which way it faces, to have its tail against one of these cells and its body clear of them, facing away
    /// (Framework 0.144.0): for unloading over its tail into storage. <paramref name="onlyFacing"/> keeps it to one facing (a bunker's open
    /// end). The middle of a side comes first, then the nearest to the vehicle; places in <paramref name="avoid"/> are passed over. The body
    /// must fit there and Vehicle Framework's own tests must let it drive there, as for <see cref="TryFindStandCell"/>.
    /// </summary>
    public static bool TryFindBackUpSpot(VehiclePawn vehicle, ICollection<IntVec3> cells, Rot4? onlyFacing, ICollection<IntVec3>? avoid,
        out IntVec3 stand, out Rot4 facing)
    {
        stand = IntVec3.Invalid;
        facing = Rot4.Invalid;
        Map map = vehicle.Map;
        HashSet<IntVec3> place = cells as HashSet<IntVec3> ?? new HashSet<IntVec3>(cells);
        IntVec3 from = vehicle.Position;
        var options = new List<(IntVec3 Stand, Rot4 Facing, int Middle, int Distance)>();
        for (int r = 0; r < 4; r++)
        {
            var rot = new Rot4(r);
            if (onlyFacing is { } only && only != rot) continue;
            IntVec3 away = rot.FacingCell;
            IntVec3 across = rot.RighthandCell;
            int back = CellsBehindCentre(vehicle, rot);
            foreach (IntVec3 edge in place)
            {
                if (place.Contains(edge + away)) continue;
                // The tail cell, the first behind the body, is this edge cell.
                IntVec3 centre = edge + away * (back + 1);
                if (!centre.InBounds(map) || (avoid != null && avoid.Contains(centre))) continue;
                int middle = (OnSameSide(place, edge + across, away) ? 1 : 0) + (OnSameSide(place, edge - across, away) ? 1 : 0);
                options.Add((centre, rot, middle, centre.DistanceToSquared(from)));
            }
        }
        options.Sort((a, b) => a.Middle != b.Middle ? b.Middle.CompareTo(a.Middle) : a.Distance.CompareTo(b.Distance));
        int wayTests = 0;
        foreach ((IntVec3 centre, Rot4 rot, _, _) in options)
        {
            if (!BodyFits(vehicle, centre, rot, place)) continue;
            if (centre != from)
            {
                if (PathingHelper.AnyVehicleBlockingPathAt(centre, vehicle) != null) continue;
                if (wayTests++ >= MaxWayTestsPerRing * 2) return false;
                if (!vehicle.CanReachVehicle(centre, PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn)) continue;
            }
            stand = centre;
            facing = rot;
            return true;
        }
        return false;
    }

    static bool OnSameSide(HashSet<IntVec3> place, IntVec3 cell, IntVec3 away) => place.Contains(cell) && !place.Contains(cell + away);

    /// <summary>How many cells of the body lie behind its centre cell when it faces this way.</summary>
    static int CellsBehindCentre(VehiclePawn vehicle, Rot4 rot)
    {
        CellRect body = vehicle.VehicleRect(IntVec3.Zero, rot);
        IntVec3 back = -rot.FacingCell;
        int count = 0;
        while (count < 64 && body.Contains(back * (count + 1))) count++;
        return count;
    }

    /// <summary>Whether the body fits with its centre here, facing this way: on the map, on ground it can drive, and clear of these cells.</summary>
    static bool BodyFits(VehiclePawn vehicle, IntVec3 centre, Rot4 rot, HashSet<IntVec3> clearOf)
    {
        Map map = vehicle.Map;
        if (!centre.Standable(vehicle, map)) return false;
        foreach (IntVec3 cell in vehicle.VehicleRect(centre, rot))
        {
            if (!cell.InBounds(map) || clearOf.Contains(cell) || !vehicle.Drivable(cell)) return false;
        }
        return true;
    }

    /// <summary>Orders the drive to a cell, with the engine already running. False if the job tracker refused it.</summary>
    public static bool DriveTo(VehiclePawn vehicle, IntVec3 cell) => DriveTo(vehicle, cell, null);

    /// <summary>
    /// Orders the drive to a cell, ending facing this way if given: Vehicle Framework turns a vehicle to the end facing it was given when it
    /// arrives (VehiclePathFollower.PatherArrived), as after the player's own right-click with a facing. Set after the order, as its own
    /// TryOrderMoveTo does, since taking the order can clear it.
    /// </summary>
    public static bool DriveTo(VehiclePawn vehicle, IntVec3 cell, Rot4? endFacing)
    {
        if (!vehicle.Drafted) return false;
        Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
        if (!vehicle.jobs.TryTakeOrderedJob(job, JobTag.Misc)) return false;
        if (endFacing is { } rot) vehicle.vehiclePather?.SetEndRotation(Full(rot));
        return true;
    }

    /// <summary>Turns a standing vehicle to face this way, as Vehicle Framework does when one arrives.</summary>
    public static void Face(VehiclePawn vehicle, Rot4 rot)
    {
        if (vehicle.Rotation != rot) vehicle.FullRotation = Full(rot);
    }

    static Rot8 Full(Rot4 rot) => rot.AsInt switch
    {
        1 => Rot8.East,
        2 => Rot8.South,
        3 => Rot8.West,
        _ => Rot8.North,
    };

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
