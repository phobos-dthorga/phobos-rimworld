using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What a vehicle with the route kit is doing on its own.</summary>
public enum RouteMode
{
    None,
    /// <summary>Keeping behind the vehicle ahead of it in a convoy.</summary>
    Follow,
    /// <summary>Driving round its loop, pausing at its two stops.</summary>
    Loop,
}

/// <summary>
/// The route level as a source for the remote control comp (Framework 0.281.0; docs/development/autonomous-vehicles.md, step 7a; the
/// owner's choices of 18 and 25 Nov 2026). A vehicle carrying the route kit fitting, with its power on, is linked while its convoy's leader
/// is on its map and driven (a crew at the wheel, or a station), or while it runs a loop. Needs no radio. Reads fields; works nothing out.
/// </summary>
public sealed class RouteControl : IRemoteControlSource
{
    public const string SourceKey = "route";
    /// <summary>What the route kit fitting grants, and the electrical load that powers it (Fittings_Route.xml).</summary>
    public const string KitGrant = "RouteControl";
    public const string KitLoad = "routeKit";

    /// <summary>The setting "Vehicles follow a leader and run loops by themselves" (on by default; the risk is moderate). Live.</summary>
    public static bool On => VehicleSystemsMod.Settings.routeControl;

    public string Key => SourceKey;
    public bool StationToggle => false;
    public bool Enabled => On;
    public string TextPrefix => "PhobosRoute_";
    public Texture2D? Icon => null;

    /// <summary>The route kit is on the vehicle, its part standing and its power on.</summary>
    public bool Fitted(VehiclePawn vehicle) => KitWorks(vehicle);

    public static bool KitWorks(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Grants(KitGrant) && systems.LoadActive(KitLoad);

    public RemoteLink LinkOf(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned || vehicle.GetComp<CompPhobosRoute>() is not { } route) return default;
        return route.Mode switch
        {
            RouteMode.Follow when Convoys.Leads(route.Leader, vehicle.Map) => new RemoteLink(true, route.Leader!.Position),
            RouteMode.Loop when route.Points.Count >= 2 => new RemoteLink(true, route.Points[0]),
            _ => default,
        };
    }

    public void WantChanged(VehiclePawn vehicle)
    {
    }
}

/// <summary>Convoys: a leader and up to four followers, each keeping behind the one ahead of it (the owner, 18 Nov 2026).</summary>
public static class Convoys
{
    /// <summary>Whether this vehicle leads now: on that map and driven, by a crew at its wheel or from afar.</summary>
    public static bool Leads(VehiclePawn? leader, Map map) =>
        leader is { Spawned: true, Destroyed: false } && leader.Map == map
        && (RemoteControl.CrewDrives(leader) || leader.GetComp<CompPhobosRemoteControl>() is { Drives: true });

    /// <summary>A leader's followers on its map, in their order behind it.</summary>
    public static List<CompPhobosRoute> Followers(VehiclePawn leader)
    {
        var all = new List<CompPhobosRoute>();
        if (!leader.Spawned) return all;
        foreach (var pawn in leader.Map.mapPawns.SpawnedPawnsInFaction(leader.Faction))
        {
            if (pawn is VehiclePawn v && v.GetComp<CompPhobosRoute>() is { Mode: RouteMode.Follow } r && r.Leader == leader) all.Add(r);
        }
        var ordered = new List<CompPhobosRoute>();
        Thing current = leader;
        while (ordered.Count < all.Count)
        {
            var next = all.FirstOrDefault(r => r.Ahead == current && !ordered.Contains(r));
            if (next == null) break;
            ordered.Add(next);
            current = next.Vehicle;
        }
        // Any whose vehicle ahead has gone keep their place at the end.
        ordered.AddRange(all.Where(r => !ordered.Contains(r)));
        return ordered;
    }

    /// <summary>Sends every follower of this leader on its own way; each stays where it is.</summary>
    public static void BreakUp(VehiclePawn leader)
    {
        foreach (var follower in Followers(leader)) follower.Stop(quietly: true);
        Messages.Message("PhobosRoute_BrokenUp".Translate(leader.LabelShort.Named("VEHICLE")), leader, MessageTypeDefOf.SilentInput, false);
    }

    /// <summary>The convoy a vehicle belongs to: its leader (itself if it leads none and follows none).</summary>
    public static VehiclePawn LeaderOf(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosRoute>() is { Mode: RouteMode.Follow, Leader: { } leader } ? leader : vehicle;
}

/// <summary>
/// Lets a vehicle follow a leader or run a loop by itself once the route kit fitting is on it. It takes no figures (they are in
/// PhobosRemoteControlRulesDef); D'thorga Framework gives it to every design with the kit's sensor bar, so XML seldom names it.
/// </summary>
public class CompProperties_PhobosRoute : CompProperties
{
    public CompProperties_PhobosRoute() => compClass = typeof(CompPhobosRoute);
}

/// <summary>
/// A vehicle that follows a leader or runs a loop by itself (Framework 0.281.0; docs/development/autonomous-vehicles.md, step 7a). It asks
/// the remote control comp to drive it (by the route source), so the lost-link choices, letters and the move check are that comp's: when
/// its leader is lost it does what was chosen for it, hold by default. While it runs it looks every 60 ticks [A]: a follower aims behind
/// the vehicle ahead and orders a drive only when that has moved on (RouteRule); a loop drives point to point and waits at its stops.
/// <para>Cost: a field read each tick while it does nothing; while it runs, one look a second, a few distances and now and then a drive
/// order. Drawing its route costs only while it is selected.</para>
/// </summary>
public sealed class CompPhobosRoute : VehicleComp
{
    const int LookTicks = 60;
    RouteMode mode;
    VehiclePawn? ahead;
    VehiclePawn? leader;
    List<IntVec3> points = new();
    List<bool> stops = new();
    int leg;
    int dwellUntil = -1;
    IntVec3 orderCell = IntVec3.Invalid;

    public RouteMode Mode => mode;
    public VehiclePawn? Ahead => ahead;
    public VehiclePawn? Leader => leader;
    public IReadOnlyList<IntVec3> Points => points;

    static PhobosRemoteControlRulesDef Rules => PhobosRemoteControlRulesDef.Current;

    CompPhobosRemoteControl? Remote => Vehicle.GetComp<CompPhobosRemoteControl>();

    // ---- Orders, from the buttons and the fleet board ----

    /// <summary>Whether this vehicle may take the route now: the kit works and the setting allows it. Null, or why not.</summary>
    public string? CannotRoute()
    {
        if (!RouteControl.On) return "PhobosRoute_NotNow".Translate(Vehicle.LabelShort.Named("VEHICLE"));
        if (!RouteControl.KitWorks(Vehicle)) return "PhobosRoute_NoKit".Translate(Vehicle.LabelShort.Named("VEHICLE"));
        return Remote == null ? "PhobosRoute_NoKit".Translate(Vehicle.LabelShort.Named("VEHICLE")) : null;
    }

    /// <summary>Joins the convoy the target leads or belongs to, at its end. Null when done, or why not.</summary>
    public string? Follow(VehiclePawn target)
    {
        if (CannotRoute() is { } why) return why;
        if (target == Vehicle || target.Map != Vehicle.Map) return "PhobosRoute_CannotFollow".Translate(target.LabelShort.Named("TARGET"));
        var lead = Convoys.LeaderOf(target);
        if (lead == Vehicle || Convoys.LeaderOf(Vehicle) == lead && mode == RouteMode.Follow) return "PhobosRoute_CannotFollow".Translate(target.LabelShort.Named("TARGET"));
        var behind = Convoys.Followers(lead);
        if (!RouteRule.MayJoin(behind.Count, Rules.maxFollowers))
        {
            return "PhobosRoute_ConvoyFull".Translate(lead.LabelShort.Named("LEADER"), Rules.maxFollowers.Named("MOST"));
        }
        if (mode != RouteMode.None) Stop(quietly: true);
        // Anyone following this vehicle comes along behind it, so a convoy can be joined whole.
        mode = RouteMode.Follow;
        leader = lead;
        ahead = behind.Count > 0 ? behind[^1].Vehicle : lead;
        orderCell = IntVec3.Invalid;
        Remote!.SetWanted(true, RouteControl.SourceKey);
        if (Seen.Wants("vehicles.route-follow", Vehicle)) Seen.Note("vehicles.route-follow", Vehicle, "behind " + ahead.LabelShort);
        return null;
    }

    /// <summary>Starts a loop between two stops. Null when done, or why not.</summary>
    public string? RunLoop(IntVec3 pickUp, IntVec3 dropOff)
    {
        if (CannotRoute() is { } why) return why;
        if (mode != RouteMode.None) Stop(quietly: true);
        mode = RouteMode.Loop;
        points = new List<IntVec3> { pickUp, dropOff };
        stops = new List<bool> { true, true };
        leg = 0;
        dwellUntil = -1;
        orderCell = IntVec3.Invalid;
        Remote!.SetWanted(true, RouteControl.SourceKey);
        if (Seen.Wants("vehicles.route-loop", Vehicle)) Seen.Note("vehicles.route-loop", Vehicle, "two stops");
        return null;
    }

    /// <summary>Adds a waypoint on the leg it lengthens least (RouteRule.InsertAt).</summary>
    public void AddWaypoint(IntVec3 cell)
    {
        if (mode != RouteMode.Loop) return;
        var at = RouteRule.InsertAt(points.Select(p => (p.x, p.z)).ToList(), (cell.x, cell.z));
        points.Insert(at, cell);
        stops.Insert(at, false);
        if (leg >= at) leg++;
    }

    /// <summary>Stops following or the loop, and hands the vehicle back. Its own followers close up behind the one ahead of it.</summary>
    public void Stop(bool quietly = false)
    {
        if (mode == RouteMode.Follow && Vehicle.Spawned && leader != null)
        {
            foreach (var behind in Convoys.Followers(leader)) if (behind.ahead == Vehicle) behind.ahead = ahead;
        }
        if (orderCell.IsValid && Vehicle.Spawned) VehicleAutoDrive.Halt(Vehicle, orderCell);
        mode = RouteMode.None;
        ahead = leader = null;
        points.Clear();
        stops.Clear();
        orderCell = IntVec3.Invalid;
        dwellUntil = -1;
        if (Remote is { } remote && remote.WantedBy(RouteControl.SourceKey)) remote.SetWanted(false, null);
        if (!quietly) Messages.Message("PhobosRoute_Stopped".Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.SilentInput, false);
    }


    // ---- Driving ----

    public override void CompTick()
    {
        base.CompTick();
        if (mode == RouteMode.None || !Vehicle.Spawned || !Gen.IsHashIntervalTick(Vehicle, LookTicks)) return;
        Look();
    }

    void Look()
    {
        var remote = Remote;
        // Another source took it (the station's button), its kit stopped working, or the setting is off: it lets go.
        if (remote == null || !remote.WantedBy(RouteControl.SourceKey) || !RouteControl.On || !RouteControl.KitWorks(Vehicle))
        {
            Stop(quietly: !RouteControl.On);
            return;
        }
        // Lost, waiting or acting on it: the remote control comp's choices have it.
        if (remote.State != RemoteState.Linked) return;
        if (!Vehicle.Drafted && !VehicleAutoDrive.StartEngine(Vehicle, out _)) return;
        if (mode == RouteMode.Follow) LookFollowing();
        else LookLooping();
    }

    void LookFollowing()
    {
        // The vehicle ahead has gone (destroyed, parked, left the map): keep behind the leader instead.
        if (ahead is not { Spawned: true } || ahead.Map != Vehicle.Map || ahead.GetComp<CompPhobosRoute>() is { Mode: not RouteMode.Follow } && ahead != leader)
        {
            ahead = leader;
        }
        if (ahead == null) return;
        var facing = ahead.Rotation.FacingCell;
        var gap = RouteRule.FollowGap(ahead.VehicleDef.size.z, Vehicle.VehicleDef.size.z, Rules.followGapCells);
        var (x, z) = RouteRule.FollowTarget((ahead.Position.x, ahead.Position.z), (facing.x, facing.z), gap);
        DriveToward(new IntVec3(x, 0, z));
    }

    void LookLooping()
    {
        if (points.Count < 2) return;
        leg = Mathf.Clamp(leg, 0, points.Count - 1);
        var point = points[leg];
        if (RouteRule.Reached(Vehicle.Position.DistanceTo(point), Rules.loopArriveCells))
        {
            var now = Find.TickManager.TicksGame;
            if (stops[leg])
            {
                if (dwellUntil < 0)
                {
                    dwellUntil = now + Rules.loopDwellTicks;
                    if (Seen.Wants("vehicles.route-stop", Vehicle, leg == 0 ? "pick-up" : "drop-off")) Seen.Note("vehicles.route-stop", Vehicle, Vehicle.LabelShort, leg == 0 ? "pick-up" : "drop-off");
                }
                if (now < dwellUntil) return;
            }
            dwellUntil = -1;
            leg = RouteRule.Next(leg, points.Count);
            orderCell = IntVec3.Invalid;
            point = points[leg];
        }
        DriveToward(point);
    }

    /// <summary>Orders a drive to a place near the target, unless one in hand already makes for it or the vehicle is there.</summary>
    void DriveToward(IntVec3 target)
    {
        var map = Vehicle.Map;
        target = target.ClampInsideMap(map);
        var driving = orderCell.IsValid && VehicleAutoDrive.HeadingFor(Vehicle, orderCell);
        var orderToTarget = orderCell.IsValid ? orderCell.DistanceTo(target) : double.MaxValue;
        if (!RouteRule.NeedsOrder(Vehicle.Position.DistanceTo(target), driving, orderToTarget, Rules.followNearCells, Rules.followSlackCells)) return;
        for (var ring = 0; ring <= Rules.standRingCells; ring++)
        {
            if (!VehicleAutoDrive.TryFindStandCell(Vehicle, target, ring, out var cell)) continue;
            if (cell == Vehicle.Position) return;
            if (VehicleAutoDrive.DriveTo(Vehicle, cell)) orderCell = cell;
            return;
        }
    }

    // ---- Buttons, words and pictures ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        var vehicle = Vehicle;
        if (vehicle.Faction != Faction.OfPlayer || !vehicle.Spawned || !RouteControl.On) yield break;
        // A leader needs no kit of its own: it can always send its convoy away.
        var followers = Convoys.Followers(vehicle);
        if (followers.Count > 0)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosRoute_GizmoBreakUp".Translate(followers.Count.Named("COUNT")),
                defaultDesc = "PhobosRoute_GizmoBreakUpDesc".Translate(),
                icon = RouteIcons.BreakUp,
                action = () => Convoys.BreakUp(vehicle),
            };
        }
        if (!RouteControl.KitWorks(vehicle)) yield break;
        switch (mode)
        {
            case RouteMode.None:
                yield return new Command_Action
                {
                    defaultLabel = "PhobosRoute_GizmoFollow".Translate(),
                    defaultDesc = "PhobosRoute_GizmoFollowDesc".Translate(Rules.maxFollowers.Named("MOST")),
                    icon = RouteIcons.Follow,
                    action = BeginFollowTargeting,
                };
                yield return new Command_Action
                {
                    defaultLabel = "PhobosRoute_GizmoLoop".Translate(),
                    defaultDesc = "PhobosRoute_GizmoLoopDesc".Translate(),
                    icon = RouteIcons.Loop,
                    action = BeginLoopTargeting,
                };
                break;
            case RouteMode.Follow:
                yield return new Command_Action
                {
                    defaultLabel = "PhobosRoute_GizmoStopFollowing".Translate(),
                    defaultDesc = "PhobosRoute_GizmoStopFollowingDesc".Translate(),
                    icon = RouteIcons.Stop,
                    action = () => Stop(),
                };
                break;
            case RouteMode.Loop:
                yield return new Command_Action
                {
                    defaultLabel = "PhobosRoute_GizmoWaypoint".Translate(),
                    defaultDesc = "PhobosRoute_GizmoWaypointDesc".Translate(),
                    icon = RouteIcons.Waypoint,
                    action = () => Find.Targeter.BeginTargeting(CellsOnMap(), target => AddWaypoint(target.Cell)),
                };
                yield return new Command_Action
                {
                    defaultLabel = "PhobosRoute_GizmoStopLoop".Translate(),
                    defaultDesc = "PhobosRoute_GizmoStopLoopDesc".Translate(),
                    icon = RouteIcons.Stop,
                    action = () => Stop(),
                };
                break;
        }
    }

    static TargetingParameters CellsOnMap() => new() { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false };

    void BeginFollowTargeting()
    {
        var parameters = new TargetingParameters
        {
            canTargetPawns = true,
            canTargetBuildings = false,
            validator = t => t.Thing is VehiclePawn v && v != Vehicle && v.Faction == Faction.OfPlayer && v.Spawned && v.Map == Vehicle.Map,
        };
        Find.Targeter.BeginTargeting(parameters, target =>
        {
            if (target.Thing is not VehiclePawn chosen) return;
            if (Follow(chosen) is { } why) Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, false);
        });
    }

    void BeginLoopTargeting()
    {
        Messages.Message("PhobosRoute_PickUpFirst".Translate(), MessageTypeDefOf.SilentInput, false);
        Find.Targeter.BeginTargeting(CellsOnMap(), first =>
        {
            Messages.Message("PhobosRoute_DropOffNext".Translate(), MessageTypeDefOf.SilentInput, false);
            Find.Targeter.BeginTargeting(CellsOnMap(), second =>
            {
                if (RunLoop(first.Cell, second.Cell) is { } why) Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, false);
            });
        });
    }

    public override string CompInspectStringExtra()
    {
        if (mode == RouteMode.None) return string.Empty;
        var state = Remote?.State ?? RemoteState.Off;
        if (state is RemoteState.Holding) return "PhobosRoute_InspectHolding".Translate();
        if (state is RemoteState.ComingBack) return "PhobosRoute_InspectComingBack".Translate();
        if (state is RemoteState.CarryingOn) return "PhobosRoute_InspectCarryingOn".Translate();
        if (state is RemoteState.Waiting) return "PhobosRoute_InspectWaiting".Translate();
        if (mode == RouteMode.Follow)
        {
            return "PhobosRoute_InspectFollowing".Translate((ahead?.LabelShort ?? "?").Named("AHEAD"), (leader?.LabelShort ?? "?").Named("LEADER"));
        }
        if (dwellUntil >= 0)
        {
            var left = Mathf.Max(0, dwellUntil - Find.TickManager.TicksGame);
            return "PhobosRoute_InspectWaitingAtStop".Translate((leg == 0 ? "PhobosRoute_PickUp" : "PhobosRoute_DropOff").Translate().Named("STOP"),
                left.ToStringTicksToPeriod().Named("TIME"));
        }
        var to = stops.Count > leg && stops[leg] ? (leg == 0 ? "PhobosRoute_PickUp" : "PhobosRoute_DropOff").Translate().Resolve() : "PhobosRoute_Waypoint".Translate().Resolve();
        return "PhobosRoute_InspectLooping".Translate(to.Named("TO"));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (mode == RouteMode.Follow && ahead is { Spawned: true })
        {
            GenDraw.DrawLineBetween(Vehicle.DrawPos, ahead.DrawPos, SimpleColor.Cyan);
        }
        else if (mode == RouteMode.Loop && points.Count >= 2)
        {
            for (var i = 0; i < points.Count; i++)
            {
                GenDraw.DrawLineBetween(points[i].ToVector3Shifted(), points[(i + 1) % points.Count].ToVector3Shifted(), SimpleColor.Cyan);
                if (stops[i]) GenDraw.DrawTargetHighlight(new LocalTargetInfo(points[i]));
            }
        }
    }

    /// <summary>Off the map it does nothing; a follower off the map leaves its convoy, a loop waits for its return.</summary>
    public override void PostDeSpawn(Map map, DestroyMode destroyMode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, destroyMode);
        if (mode == RouteMode.Follow) Stop(quietly: true);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref mode, "PhobosRoute_Mode");
        Scribe_References.Look(ref ahead, "PhobosRoute_Ahead");
        Scribe_References.Look(ref leader, "PhobosRoute_Leader");
        Scribe_Collections.Look(ref points, "PhobosRoute_Points", LookMode.Value);
        Scribe_Collections.Look(ref stops, "PhobosRoute_Stops", LookMode.Value);
        Scribe_Values.Look(ref leg, "PhobosRoute_Leg");
        Scribe_Values.Look(ref dwellUntil, "PhobosRoute_DwellUntil", -1);
        Scribe_Values.Look(ref orderCell, "PhobosRoute_OrderCell", IntVec3.Invalid);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            points ??= new List<IntVec3>();
            stops ??= new List<bool>();
            if (stops.Count != points.Count) stops = points.Select((_, i) => i == 0 || i == points.Count / 2).ToList();
        }
    }
}

/// <summary>The route buttons' pictures: borrowed until their own are drawn, with a visible fallback of ours (SystemIcons).</summary>
[StaticConstructorOnStartup]
public static class RouteIcons
{
    public static readonly Texture2D Follow = SystemIcons.For("Phobos/VehicleSystems/Hitch/Hitch");
    public static readonly Texture2D Loop = SystemIcons.For("Phobos/VehicleSystems/BayTrips/ServiceYard");
    public static readonly Texture2D Waypoint = SystemIcons.For("Phobos/VehicleSystems/BayTrips/VehiclePark");
    public static readonly Texture2D Stop = SystemIcons.For("Phobos/VehicleSystems/Dash/Brake");
    public static readonly Texture2D BreakUp = SystemIcons.For("Phobos/VehicleSystems/Hitch/Unhitch");
}

/// <summary>Registers the route level as a source of the remote control comp at start-up (Framework 0.281.0).</summary>
[StaticConstructorOnStartup]
public static class RouteStartup
{
    static RouteStartup() => RemoteControl.Register(new RouteControl());
}
