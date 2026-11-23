using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A comp on a towed vehicle that wants to know when the hitch has moved it to new cells (docs/development/farming-batch-two.md): a towed
/// vehicle is moved by setting its position, and whether Vehicle Framework's own move event fires for that is not known, so the hitch says so
/// itself. Called on the towed vehicle's comps after each move; keep it cheap.
/// </summary>
public interface IVehicleTowedMove
{
    void TowedMoved();
}

/// <summary>
/// Chains of tows (Framework 0.193.0; docs/development/farming-batch-two.md, "The linkage"): a vehicle on a hitch may tow in turn, so a
/// tractor carries a plough on its linkage and a cart behind the plough, and an articulated tractor's rear half carries both. Only the vehicle
/// at the front drives: it counts the whole chain's weight and draught, and each hitch behind it follows when the one in front has moved. A
/// vehicle on the recovery tow tows nothing; a chain never loops, and holds at most <c>hitchMostBehind</c> vehicles behind the one that drives.
/// </summary>
public sealed partial class CompPhobosHitch
{
    /// <summary>A guard on every walk along a chain, far above any chain the hitch allows.</summary>
    const int ChainGuard = 16;

    /// <summary>The hitch towing this vehicle, when it is itself towed: in a chain.</summary>
    public CompPhobosHitch? Tower => Hitches.Towing(Vehicle);

    /// <summary>Where the axle of what this hitch tows stands, and which way it points.</summary>
    internal TrailerPose Pose => new(axleX, axleZ, heading);

    /// <summary>The vehicle at the front of this vehicle's chain, the one that drives: the vehicle itself when nothing tows it.</summary>
    public static VehiclePawn HeadOf(VehiclePawn vehicle)
    {
        VehiclePawn head = vehicle;
        for (int i = 0; i < ChainGuard && Hitches.Towing(head) is { } by; i++) head = by.Vehicle;
        return head;
    }

    /// <summary>The vehicles behind this one, nearest first: what its hitch tows, what that tows, and so on.</summary>
    public static IEnumerable<VehiclePawn> Behind(VehiclePawn vehicle)
    {
        VehiclePawn? next = vehicle.GetComp<CompPhobosHitch>()?.Trailer;
        for (int i = 0; next != null && i < ChainGuard; i++)
        {
            yield return next;
            next = next.GetComp<CompPhobosHitch>()?.Trailer;
        }
    }

    /// <summary>
    /// The cell a vehicle of this line stands on, counted from the cell of the vehicle that drives it, when the whole line stands straight
    /// behind facing <paramref name="rot"/>: each one's axle its drawbar's length behind the hitch in front, as the follow rule leaves them
    /// on a straight. Null when <paramref name="member"/> is not behind <paramref name="head"/>. For a field run to plan where its tractor
    /// must stand for the implement to lie on a row (the fault-read of 21 Nov 2026, R9).
    /// </summary>
    public static IntVec3? StraightBehind(VehiclePawn head, VehiclePawn member, Rot4 rot)
    {
        int facing = rot.AsInt;
        double heading = facing * 90.0;
        (double cx, double cz) = HitchRule.TrueCentre(0, 0, head.def.Size.x, head.def.Size.z, facing);
        VehiclePawn front = head;
        foreach (VehiclePawn next in Behind(head))
        {
            if (Geometry(next) is not { } props) return null;
            (double hx, double hz) = HitchRule.BehindPoint(cx, cz, heading, HitchBehind(front.def));
            TrailerPose pose = HitchRule.AxleFor(hx, hz, heading, props.HitchToAxle);
            (cx, cz) = HitchRule.CentreOf(pose, props.AxleToCentre);
            if (next == member)
            {
                (int x, int z) = HitchRule.CellFor(cx, cz, next.def.Size.x, next.def.Size.z, facing);
                return new IntVec3(x, 0, z);
            }
            front = next;
        }
        return null;
    }

    /// <summary>Whether <paramref name="ahead"/> tows <paramref name="vehicle"/>, directly or through vehicles between them.</summary>
    public static bool IsAheadOf(VehiclePawn ahead, VehiclePawn vehicle)
    {
        VehiclePawn at = vehicle;
        for (int i = 0; i < ChainGuard && Hitches.Towing(at) is { } by; i++)
        {
            if (by.Vehicle == ahead) return true;
            at = by.Vehicle;
        }
        return false;
    }

    static int CountBehind(VehiclePawn vehicle)
    {
        int count = 0;
        foreach (VehiclePawn _ in Behind(vehicle)) count++;
        return count;
    }

    /// <summary>The weight of a towed vehicle and of everything behind it, in kilograms.</summary>
    static double ChainKilograms(VehiclePawn first)
    {
        double kilograms = CompPhobosSling.LoadKilograms(first);
        foreach (VehiclePawn behind in Behind(first)) kilograms += CompPhobosSling.LoadKilograms(behind);
        return kilograms;
    }

    /// <summary>Why this hitch, itself towed, cannot take this candidate into the chain, or null (the loop and the length).</summary>
    string? ChainRefusal(VehiclePawn candidate)
    {
        if (IsAheadOf(candidate, Vehicle)) return "PhobosHitch_ChainLoop".Translate(candidate.LabelShort.Named("TRAILER"), Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        bool towed = Tower != null;
        if (towed && TrailerProps(candidate) == null) return "PhobosHitch_ChainTrailersOnly".Translate(Vehicle.LabelShort.Named("TRACTOR"), candidate.LabelShort.Named("TRAILER")).Resolve();
        if (!towed && CountBehind(candidate) == 0) return null;
        VehiclePawn head = HeadOf(Vehicle);
        int behind = CountBehind(head) + 1 + CountBehind(candidate);
        if (behind > Rules.hitchMostBehind)
        {
            return "PhobosHitch_ChainTooLong".Translate(head.LabelShort.Named("TRACTOR"), Rules.hitchMostBehind.Named("COUNT")).Resolve();
        }
        return null;
    }

    /// <summary>The way this vehicle points, in degrees: its own facing, or, while towed, the heading of its pose behind the hitch in front.</summary>
    public double Heading() => Tower is { } by ? by.heading : VehicleHeading(Vehicle);

    /// <summary>The middle of this vehicle, in map units: where it is drawn, or, while towed, its true place behind the hitch in front (its
    /// cells snap to the grid; its pose does not).</summary>
    (double X, double Z) TrueCentre()
    {
        if (Tower is { } by && Geometry(Vehicle) is { } props) return HitchRule.CentreOf(by.Pose, props.AxleToCentre);
        Vector3 centre = ((Thing)Vehicle).DrawPos;
        return (centre.x, centre.z);
    }

    /// <summary>Stops the vehicle that drives the chain this hitch is in.</summary>
    void StopTrain()
    {
        if (HeadOf(Vehicle).vehiclePather is { Moving: true } pather) pather.StopDead();
    }

    /// <summary>What a chain carries has changed here: every hitch in front counts its weight again, and the driving vehicle's drivetrain too.</summary>
    void ChainChanged()
    {
        CompPhobosHitch? at = Tower;
        for (int i = 0; at != null && i < ChainGuard; i++)
        {
            at.weightTick = -1;
            at = at.Tower;
        }
        HeadOf(Vehicle).GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MarkMassDirty();
    }

    /// <summary>
    /// What a towed vehicle carries has changed (grain into a chaser bin): every hitch in front of it counts its weight again, and the
    /// vehicle that drives the line works its mass out afresh.
    /// </summary>
    public static void WeightChanged(VehiclePawn towed)
    {
        CompPhobosHitch? at = Hitches.Towing(towed);
        if (at == null) return;
        for (int i = 0; at != null && i < ChainGuard; i++)
        {
            at.weightTick = -1;
            at = at.Tower;
        }
        HeadOf(towed).GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MarkMassDirty();
    }

    /// <summary>Whether a vehicle of this kind would stand clear on these cells behind this one: on the map, on ground it may stand on and
    /// off every other thing in its way, by the same test a towed vehicle's every move passes.</summary>
    public bool RoomFor(VehicleDef kind, IntVec3 cell, Rot4 rot)
    {
        Map map = Vehicle.Map;
        CellRect rect = kind.VehicleRect(cell, rot);
        return rect.InBounds(map) && Clear(rect, map, Vehicle);
    }

    /// <summary>The hitch in front has moved this vehicle, or is settling it: what this one tows follows in turn.</summary>
    void FollowTower(bool moving)
    {
        if (trailer == null || HoldsTowed || !Vehicle.Spawned || !trailer.Spawned || trailer.Map != Vehicle.Map) return;
        RefreshWeight();
        Follow(moving);
    }

    /// <summary>Tells the towed vehicle's comps that the hitch has moved it to new cells.</summary>
    static void SayMoved(VehiclePawn towed)
    {
        List<ThingComp> comps = towed.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleTowedMove mover) mover.TowedMoved();
        }
        // A field run working a row with this vehicle works its strip now, where it has just been set down.
        HeadOf(towed).GetComp<CompPhobosFieldRun>()?.WorkerMoved(towed);
    }

    /// <summary>The last hitch in this vehicle's chain that could take one more, for the "Hitch behind" button on the vehicle in front.</summary>
    CompPhobosHitch? LastFree()
    {
        CompPhobosHitch? last = null;
        foreach (VehiclePawn behind in Behind(Vehicle)) last = behind.GetComp<CompPhobosHitch>();
        return last is { trailer: null, pending: null } && last.Refusal() == null ? last : null;
    }
}
