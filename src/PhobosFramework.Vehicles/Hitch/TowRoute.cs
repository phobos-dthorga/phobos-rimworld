using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The wider way round, its route check (Framework 0.277.0; docs/development/systems/hitches-and-trailers.md). When the vehicle that drives a
/// tow takes a new path, the whole line behind it is driven along that path beforehand, by the same follow rule, facings and test of the
/// ground the tow uses in play (<see cref="HitchRule"/>). Where a trailer would run into something, a snag's cost is laid around that spot
/// (<see cref="TowClearanceCosts.Snag"/>) and Vehicle Framework is asked for another path, as it asks itself when a vehicle blocks the way;
/// after the rules' tries the vehicle drives the last path and the player is told it may stop. Needs Vehicle Framework's newer path finder,
/// whose grids are where a snag's cost goes; without it, nothing here runs. All derived, never saved.
/// </summary>
public sealed partial class CompPhobosHitch
{
    /// <summary>Vehicle Framework's own way of asking for a new path while driving (its NeedNew answer), private to its path follower.</summary>
    static readonly MethodInfo? RequestNewPath = AccessTools.Method("Vehicles.VehiclePathFollower:RequestNewPath");

    VehiclePath? routeChecked;
    IntVec3 routeFor = IntVec3.Invalid;
    int routeTries;

    /// <summary>Whether this vehicle drives a tow: something on its hitch, and nothing towing it.</summary>
    public static bool DrivesATow(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosHitch>() is { trailer: not null } hitch && hitch.Tower == null;

    /// <summary>
    /// Whether the wider way round applies to this vehicle now: it drives a tow, and no field run of its own is driving a row (Framework
    /// 0.278.1). A row is one straight order with the implement's strip on the row's cells; the clearance costs would draw a long row beside
    /// a fence a cell away from it, and the implement would work the wrong strip.
    /// </summary>
    public static bool TakesWiderWay(VehiclePawn vehicle) => DrivesATow(vehicle) && !CompPhobosFieldRun.DrivingARow(vehicle);

    /// <summary>From the middle of a vehicle of this kind back to its hitch, in cells: its Def's own point, or just behind its rear edge.</summary>
    static float HitchBehind(ThingDef def) => def.GetModExtension<PhobosHitchPointExtension>() is { behindCentre: >= 0f } point
        ? point.behindCentre
        : def.Size.z / 2f + Rules.hitchGapCells;

    /// <summary>Called each tick the vehicle that drives the tow moves: a path not yet looked at is checked once.</summary>
    void LookAtRoute()
    {
        if (!VehicleSystemsMod.Settings.hitchWiderWay || Vehicle.vehiclePather is not { } pather || !TakesWiderWay(Vehicle)) return;
        if (pather.curPath is not { IsValid: true } path || ReferenceEquals(path, routeChecked)) return;
        if (pather.RequestStatus == VehiclePathFollower.PathRequestStatus.Calculating) return;
        if (TowClearanceCosts.On(Vehicle.Map) is not { } grid) return;
        routeChecked = path;
        IntVec3 destination = pather.Destination.Cell;
        if (destination != routeFor)
        {
            routeFor = destination;
            routeTries = 0;
        }
        routeTries++;
        List<IntVec3>? snag = RouteSnag(path);
        if (snag == null)
        {
            if (routeTries > 1 && Seen.Wants("vehicles.hitch.wider-way", Vehicle)) Seen.Note("vehicles.hitch.wider-way", Vehicle, trailer!.LabelShort, variant: "found",
                values: new List<KeyValuePair<string, double>> { new("tries", routeTries) });
            return;
        }
        grid.Snag(snag);
        if (VehicleSystemsMod.HitchLog.DebugEnabled)
        {
            VehicleSystemsMod.HitchLog.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + "): route " + routeTries + " to " + destination + " would snag "
                + trailer!.LabelShort + " at " + snag[0] + (routeTries < Rules.towRouteTries ? "; asking for another" : "; driving it anyway"));
        }
        if (routeTries < Rules.towRouteTries && RequestNewPath != null)
        {
            RequestNewPath.Invoke(pather, null);
            return;
        }
        if (routeTries == Rules.towRouteTries && Vehicle.Faction == Faction.OfPlayer)
        {
            Say("PhobosHitch_NoWideWay".Translate(Vehicle.LabelShort.Named("TRACTOR"), trailer!.LabelShort.Named("TRAILER")).Resolve(),
                new LookTargets(Vehicle, trailer), MessageTypeDefOf.CautionInput, historical: false);
            Seen.Note("vehicles.hitch.wider-way", Vehicle, trailer.LabelShort, variant: "none");
        }
    }

    /// <summary>The vehicle has stopped: the next order's route starts its tries afresh.</summary>
    void ForgetRoute()
    {
        routeChecked = null;
        routeFor = IntVec3.Invalid;
        routeTries = 0;
    }

    /// <summary>One vehicle of the line, as the route check drives it.</summary>
    sealed class RouteLink
    {
        public VehiclePawn Towed = null!;
        public TowGeometry Geometry;
        public TrailerPose Pose;
        public int Facing;
        /// <summary>From the middle of the vehicle in front of this one back to the hitch this one hangs on.</summary>
        public float Behind;
    }

    /// <summary>
    /// Drives the whole line along a path beforehand: null when every vehicle of it would find room all the way, or the cells that would stop
    /// the first one that would not. The cells are the ground under the nearest facing that is not clear.
    /// </summary>
    List<IntVec3>? RouteSnag(VehiclePath path)
    {
        Map map = Vehicle.Map;
        var line = new List<RouteLink>();
        float behind = HitchBehind(Vehicle.def);
        CompPhobosHitch? at = this;
        for (int i = 0; at?.trailer != null && i < ChainGuard; i++)
        {
            if (Geometry(at.trailer) is not { } props) break;
            line.Add(new RouteLink { Towed = at.trailer, Geometry = props, Pose = at.Pose, Facing = at.trailer.Rotation.AsInt, Behind = behind });
            behind = HitchBehind(at.trailer.def);
            at = at.trailer.GetComp<CompPhobosHitch>();
        }
        if (line.Count == 0) return null;
        var ours = new HashSet<VehiclePawn> { Vehicle };
        foreach (RouteLink link in line) ours.Add(link.Towed);

        PhobosVehicleRulesDef rules = Rules;
        (double hx, double hz) = HitchPoint();
        IntVec3 from = Vehicle.Position;
        int most = Mathf.Min(path.NodesLeft, rules.towCheckMostCells);
        for (int n = 0; n < most; n++)
        {
            IntVec3 node = path.Peek(n);
            if (node == from) continue;
            Rot8 rot = Rot8.DirectionFromCells(from, node);
            double heading = HeadingOf(rot);
            Vector3 centre = Ext_Vehicles.TrueCenter(node, rot, Vehicle.VehicleDef);
            (double nx, double nz) = HitchRule.BehindPoint(centre.x, centre.z, heading, line[0].Behind);
            foreach ((double sx, double sz) in TowRouteRule.Between(hx, hz, nx, nz, rules.towCheckSteps))
            {
                if (StepLine(line, sx, sz, heading, map, ours) is { } snag) return snag;
            }
            (hx, hz) = (nx, nz);
            from = node;
        }
        return null;
    }

    /// <summary>One step of the line behind a hitch point: each vehicle follows the one in front, as in play. The snag's cells, or null.</summary>
    static List<IntVec3>? StepLine(List<RouteLink> line, double hx, double hz, double heading, Map map, HashSet<VehiclePawn> ours)
    {
        for (int i = 0; i < line.Count; i++)
        {
            RouteLink link = line[i];
            if (i > 0)
            {
                RouteLink front = line[i - 1];
                (double cx, double cz) = HitchRule.CentreOf(front.Pose, front.Geometry.AxleToCentre);
                heading = front.Pose.Heading;
                (hx, hz) = HitchRule.BehindPoint(cx, cz, heading, link.Behind);
            }
            TrailerPose pose = HitchRule.Follow(hx, hz, link.Pose, link.Geometry.HitchToAxle);
            link.Pose = HitchRule.Limit(hx, hz, pose, heading, link.Geometry.JackKnife, link.Geometry.HitchToAxle);
            if (StandFacing(link, map, ours) is { } snag) return snag;
        }
        return null;
    }

    /// <summary>Whether a vehicle of the line could stand where its pose puts it, on either facing near a diagonal as in play; the facing it
    /// takes is kept for the next step. The cells that stop it on its nearest facing, or null.</summary>
    static List<IntVec3>? StandFacing(RouteLink link, Map map, HashSet<VehiclePawn> ours)
    {
        (double cx, double cz) = HitchRule.CentreOf(link.Pose, link.Geometry.AxleToCentre);
        (int first, int second) = HitchRule.Facings(link.Pose.Heading, link.Facing);
        IntVec2 size = link.Towed.def.Size;
        List<IntVec3>? snag = null;
        for (int k = 0; k < (second >= 0 ? 2 : 1); k++)
        {
            int facing = k == 0 ? first : second;
            (int x, int z) = HitchRule.CellFor(cx, cz, size.x, size.z, facing);
            CellRect rect = link.Towed.VehicleRect(new IntVec3(x, 0, z), new Rot4(facing));
            List<IntVec3>? stops = Stops(rect, map, ours);
            if (stops == null)
            {
                link.Facing = facing;
                return null;
            }
            snag ??= stops;
        }
        return snag;
    }

    /// <summary>The cells of a footprint a vehicle of the line cannot stand on (off the map, ground nobody can walk, another vehicle), or null
    /// when it is clear: the same test as <see cref="Clear"/>, with every vehicle of the line its own.</summary>
    static List<IntVec3>? Stops(CellRect rect, Map map, HashSet<VehiclePawn> ours)
    {
        List<IntVec3>? stops = null;
        foreach (IntVec3 c in rect)
        {
            if (!c.InBounds(map))
            {
                (stops ??= new List<IntVec3>()).Add(c.ClampInsideMap(map));
                continue;
            }
            if (!c.Walkable(map))
            {
                (stops ??= new List<IntVec3>()).Add(c);
                continue;
            }
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is VehiclePawn other && !ours.Contains(other))
                {
                    (stops ??= new List<IntVec3>()).Add(c);
                    break;
                }
            }
        }
        return stops;
    }
}
