using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
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

    /// <summary>The route kit is on the vehicle, working or not (Framework 0.284.0): a failed kit holds the route rather than ending it.</summary>
    public bool Carried(VehiclePawn vehicle) => KitCarried(vehicle);

    public static bool KitCarried(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Carries(KitGrant);

    public RemoteLink LinkOf(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned || vehicle.GetComp<CompPhobosRoute>() is not { } route) return default;
        return route.Mode switch
        {
            RouteMode.Follow when Convoys.Leads(route.Leader, vehicle.Map) => new RemoteLink(true, route.Leader!.Position),
            // Only on the map its loop was set on (Framework 0.284.0): elsewhere it waits, unlinked, for its return.
            RouteMode.Loop when route.Points.Count >= 2 && route.OnLoopMap => new RemoteLink(true, route.Points[0]),
            _ => default,
        };
    }

    public void WantChanged(VehiclePawn vehicle)
    {
    }
}

/// <summary>What a loop vehicle is doing by itself at a stop (Framework 0.299.0).</summary>
public enum LoopStopWork
{
    /// <summary>Not begun: it has just arrived, or has no gear of its own to work with.</summary>
    None,
    /// <summary>Driving tail first to the bunker or stockpile the drop-off is on.</summary>
    BackingUp,
    /// <summary>Tipping its load.</summary>
    Unloading,
    /// <summary>Lifting its load with the grab.</summary>
    Gathering,
    /// <summary>A tanker pumping its load out at the drop-off (Framework 0.303.0), or in at the pick-up (0.305.0).</summary>
    Pumping,
    /// <summary>Its own work is over, done or not: it leaves, or waits out the stop's time.</summary>
    Done,
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
    /// <summary>The map its loop was set on (Framework 0.284.0), by its unique ID; -1 before then, taken up at the next look.</summary>
    int loopMapId = -1;
    /// <summary>Why it cannot drive on now, told once when it starts (Framework 0.284.0). Derived at each look; never saved.</summary>
    string? waitingWhy;
    /// <summary>Whether it is running its engine at a stop to charge its battery (Framework 0.286.0). Never saved: worked out again.</summary>
    bool charging;
    /// <summary>What it is doing by itself at this stop, and how that went (Framework 0.299.0; the owner's cards of 26 Nov 2026).</summary>
    LoopStopWork stopWork;
    bool workedItself;
    bool emptied;
    bool loadedAny;
    IntVec3 backUpCell = IntVec3.Invalid;
    int backUpFacing = -1;
    /// <summary>Drives to the back-up spot tried since arriving; never saved. After three it tips where it stands.</summary>
    int backUpTries;
    /// <summary>Whether a worker's test has been handed back since a load: it is not saved, and the worker does nothing until it has it.</summary>
    bool workResumed;
    /// <summary>Whether it set its tank pumping at this stop (Framework 0.303.0), so it stops the pump and packs up on leaving. Saved.</summary>
    bool pumped;
    /// <summary>Whether that pumping is filling it at the pick-up (Framework 0.305.0) rather than emptying it at the drop-off. Saved.</summary>
    bool pumpLoading;
    /// <summary>When its tank's load last fell, and to what; never saved: worked out again from the next look.</summary>
    int pumpProgressTick = -1;
    float pumpLastLoad = -1f;
    /// <summary>Compartments whose pump stood idle since the load last fell, and whether the player has been told; never saved.</summary>
    int pumpIdleTries;
    bool pumpIdleTold;

    IVehicleUnloadWorker? Unloader => Vehicle.AllComps.OfType<IVehicleUnloadWorker>().FirstOrDefault();
    IVehicleGatherWorker? Grab => Vehicle.AllComps.OfType<IVehicleGatherWorker>().FirstOrDefault();
    /// <summary>Its own tank, when it pumps its own load: a trailer's, set up by a colonist's hand, is left to colonists.</summary>
    CompPhobosTank? Tank => Vehicle.GetComp<CompPhobosTank>() is { Fitted: not null, SetUpByHand: false } tank ? tank : null;
    static bool AllAboard(Thing thing) => true;

    public RouteMode Mode => mode;
    public VehiclePawn? Ahead => ahead;
    public VehiclePawn? Leader => leader;
    public IReadOnlyList<IntVec3> Points => points;

    /// <summary>Whether the vehicle is on the map its loop's points belong to.</summary>
    public bool OnLoopMap => loopMapId < 0 || Vehicle.Spawned && Vehicle.Map.uniqueID == loopMapId;

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
        // Anyone following this vehicle comes along behind it, so a convoy can be joined whole; they count against the limit too
        // (Framework 0.284.0: a leader with followers could join another convoy past it).
        var mine = Convoys.Followers(Vehicle);
        if (!RouteRule.MayJoin(behind.Count, 1 + mine.Count, Rules.maxFollowers))
        {
            return "PhobosRoute_ConvoyFull".Translate(lead.LabelShort.Named("LEADER"), Rules.maxFollowers.Named("MOST"));
        }
        if (mode != RouteMode.None) Stop(quietly: true);
        mode = RouteMode.Follow;
        leader = lead;
        ahead = behind.Count > 0 ? behind[^1].Vehicle : lead;
        orderCell = IntVec3.Invalid;
        // They take the new leader as theirs, keeping their order behind this vehicle: no follower leads a convoy of its own, so no ring
        // of vehicles can keep itself linked (Framework 0.284.0).
        foreach (var follower in mine) follower.leader = lead;
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
        loopMapId = Vehicle.Map.uniqueID;
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
        if (mode != RouteMode.Loop || !OnLoopMap) return;
        var at = RouteRule.InsertAt(points.Select(p => (p.x, p.z)).ToList(), (cell.x, cell.z));
        points.Insert(at, cell);
        stops.Insert(at, false);
        if (leg >= at) leg++;
    }

    /// <summary>Stops following or the loop, and hands the vehicle back. Its own followers close up behind the one ahead of it.</summary>
    public void Stop(bool quietly = false)
    {
        // Not only while spawned (Framework 0.284.0): a follower destroyed or gone from the map is stopped as it leaves, and the one
        // behind it must close up then, or two followers would make for one place.
        if (mode == RouteMode.Follow && leader != null)
        {
            foreach (var behind in Convoys.Followers(leader)) if (behind.ahead == Vehicle) behind.ahead = ahead;
        }
        if (orderCell.IsValid && Vehicle.Spawned) VehicleAutoDrive.Halt(Vehicle, orderCell);
        EndStopWork();
        mode = RouteMode.None;
        ahead = leader = null;
        points.Clear();
        stops.Clear();
        orderCell = IntVec3.Invalid;
        dwellUntil = -1;
        loopMapId = -1;
        waitingWhy = null;
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
        // Another source took it (the station's button), its kit was taken off, or the setting is off: it lets go. A kit still on but
        // not working (its power shed, its part shot away) holds the route instead: the remote control comp tells the player (0.284.0).
        if (remote == null || !remote.WantedBy(RouteControl.SourceKey) || !RouteControl.On || !RouteControl.KitCarried(Vehicle))
        {
            Stop(quietly: !RouteControl.On);
            return;
        }
        if (mode == RouteMode.Loop && loopMapId < 0) loopMapId = Vehicle.Map.uniqueID;
        // Lost, waiting or acting on it: the remote control comp's choices have it.
        if (remote.State != RemoteState.Linked) return;
        if (mode == RouteMode.Follow) LookFollowing();
        else LookLooping();
    }

    /// <summary>
    /// Whether it can drive now, its engine started (Framework 0.284.0). Asked before every drive order rather than asking Vehicle Framework to
    /// start the engine, which refuses with a message each time: a vehicle out of fuel or waiting on set-up work is told once and waits,
    /// undrafted, where colonists can refuel it, and goes on by itself once it can.
    /// </summary>
    bool Ready()
    {
        var why = VehicleAutoDrive.CannotDrive(Vehicle);
        if (why != waitingWhy)
        {
            waitingWhy = why;
            if (why != null)
            {
                // Seen in play (Framework 0.295.0, row 507): a vehicle on a route waits where it is, told once.
                Seen.Note("vehicles.route-waiting", Vehicle, why);
                Messages.Message("PhobosRoute_Waits".Translate(Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")), Vehicle,
                    MessageTypeDefOf.NegativeEvent, false);
            }
        }
        return why == null && VehicleAutoDrive.StartEngine(Vehicle, out _);
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
        // Working at a stop, it stays with the work even where backing up took it beyond the arrival distance (Framework 0.299.0).
        if (stopWork != LoopStopWork.None || RouteRule.Reached(Vehicle.Position.DistanceTo(point), Rules.loopArriveCells))
        {
            var now = Find.TickManager.TicksGame;
            if (stops[leg])
            {
                if (dwellUntil < 0)
                {
                    dwellUntil = now + Rules.loopDwellTicks;
                    if (Seen.Wants("vehicles.route-stop", Vehicle, leg == 0 ? "pick-up" : "drop-off")) Seen.Note("vehicles.route-stop", Vehicle, Vehicle.LabelShort, leg == 0 ? "pick-up" : "drop-off");
                }
                StopWorkStep(point, dropOff: leg != 0);
                if (!RouteRule.LeavesStop(stopWork == LoopStopWork.Done && workedItself, leg != 0, emptied, loadedAny, now >= dwellUntil))
                {
                    // The engine runs while its own gear works; otherwise off, as at any stop.
                    if (stopWork is LoopStopWork.None or LoopStopWork.Done) EngineAtStop();
                    return;
                }
                EndStopWork();
            }
            dwellUntil = -1;
            charging = false;
            leg = RouteRule.Next(leg, points.Count);
            orderCell = IntVec3.Invalid;
            point = points[leg];
        }
        DriveToward(point);
    }

    // ---- Working at a stop by itself (Framework 0.299.0; the owner's cards of 26 Nov 2026) ----

    /// <summary>
    /// One look at a stop: at the drop-off a vehicle that can tip backs up to the bunker or stockpile the stop is on (or tips where it stands
    /// if the stop is on none) and tips everything aboard; at the pick-up one with the loading grab lifts what its "Gather what" allows within
    /// reach. Without such gear it does nothing here, and waits for colonists as before. The workers are the gathering runs' own.
    /// </summary>
    void StopWorkStep(IntVec3 point, bool dropOff)
    {
        switch (stopWork)
        {
            case LoopStopWork.None:
                BeginStopWork(point, dropOff);
                break;
            case LoopStopWork.BackingUp:
                BackingUpStep();
                break;
            case LoopStopWork.Unloading:
                if (Unloader is not { } unloader)
                {
                    stopWork = LoopStopWork.Done;
                    break;
                }
                if (!workResumed)
                {
                    workResumed = true;
                    unloader.ResumeUnloading(AllAboard);
                }
                if (unloader.Unloading) break;
                AfterUnloading(unloader.LastUnloadEnd, unloader.LastUnloadReason);
                break;
            case LoopStopWork.Pumping:
                PumpingStep();
                break;
            case LoopStopWork.Gathering:
                if (Grab is not { } grab || Vehicle.GetComp<CompPhobosGatherRun>() is not { } run)
                {
                    stopWork = LoopStopWork.Done;
                    break;
                }
                if (!workResumed)
                {
                    workResumed = true;
                    grab.ResumeGathering(run.WantedOnLoop);
                }
                if (grab.Gathering) break;
                loadedAny |= grab.GatheredLoads > 0;
                stopWork = LoopStopWork.Done;
                break;
        }
    }

    void BeginStopWork(IntVec3 point, bool dropOff)
    {
        if (dropOff)
        {
            // A tanker pumps its load out where it stands (Framework 0.303.0): into what its pump reaches there, as when set pumping by hand.
            if (Tank is { HasLoad: true } tank)
            {
                if (!Ready()) return;
                StartPumping(tank, loading: false);
                return;
            }
            if (Unloader is not { CanUnloadAtAll: true } unloader || Vehicle.inventory.innerContainer.Count == 0)
            {
                stopWork = LoopStopWork.Done;
                return;
            }
            if (!Ready()) return;
            if (PlaceAt(point) is { } place && VehicleAutoDrive.TryFindBackUpSpot(Vehicle, place.Cells, place.BackUpFacing(), null, out IntVec3 stand, out Rot4 facing))
            {
                if (stand == Vehicle.Position)
                {
                    VehicleAutoDrive.Face(Vehicle, facing);
                }
                else if (VehicleAutoDrive.DriveTo(Vehicle, stand, facing))
                {
                    stopWork = LoopStopWork.BackingUp;
                    backUpCell = stand;
                    backUpFacing = facing.AsInt;
                    backUpTries = 1;
                    return;
                }
            }
            StartUnloading(unloader);
            return;
        }
        // A tanker with room fills itself where it stands (Framework 0.305.0): from what its pump reaches there, as when set loading by hand.
        if (Tank is { } filler && HasRoom(filler))
        {
            if (!Ready()) return;
            StartPumping(filler, loading: true);
            return;
        }
        if (Grab is not { CanGatherAtAll: true } grab || Vehicle.GetComp<CompPhobosGatherRun>() is not { } run)
        {
            stopWork = LoopStopWork.Done;
            return;
        }
        if (!Ready()) return;
        GatherWorkEnd started = grab.StartGathering(run.WantedOnLoop, out _);
        workedItself = true;
        if (started == GatherWorkEnd.None)
        {
            stopWork = LoopStopWork.Gathering;
            workResumed = true;
            if (Seen.Wants("vehicles.route-self-work", Vehicle, "lift")) Seen.Note("vehicles.route-self-work", Vehicle, Vehicle.LabelShort, "lift");
            return;
        }
        // Nothing in reach that it gathers: it waits for colonists.
        stopWork = LoopStopWork.Done;
    }

    /// <summary>On its way tail first: there, it turns and tips; stopped short or blocked, it tries again, and after three tries tips where it is.</summary>
    void BackingUpStep()
    {
        if (Unloader is not { } unloader)
        {
            stopWork = LoopStopWork.Done;
            return;
        }
        if (VehicleAutoDrive.Moving(Vehicle) && VehicleAutoDrive.HeadingFor(Vehicle, backUpCell)) return;
        var facing = backUpFacing >= 0 ? new Rot4(backUpFacing) : Vehicle.Rotation;
        if (Vehicle.Position == backUpCell)
        {
            VehicleAutoDrive.Face(Vehicle, facing);
            StartUnloading(unloader);
            return;
        }
        if (backUpTries < 3 && Ready() && VehicleAutoDrive.DriveTo(Vehicle, backUpCell, facing))
        {
            backUpTries++;
            return;
        }
        StartUnloading(unloader);
    }

    void StartUnloading(IVehicleUnloadWorker unloader)
    {
        workedItself = true;
        UnloadEnd started = unloader.StartUnloading(AllAboard, out string? refusal);
        if (started == UnloadEnd.None)
        {
            stopWork = LoopStopWork.Unloading;
            workResumed = true;
            if (Seen.Wants("vehicles.route-self-work", Vehicle, "tip")) Seen.Note("vehicles.route-self-work", Vehicle, Vehicle.LabelShort, "tip");
            return;
        }
        AfterUnloading(started, refusal);
    }

    /// <summary>Emptied, it leaves at once; with some still aboard, the player is told why once, and it waits out the stop's time for colonists.</summary>
    void AfterUnloading(UnloadEnd end, string? reason)
    {
        stopWork = LoopStopWork.Done;
        emptied = end == UnloadEnd.Emptied;
        if (emptied || end == UnloadEnd.Stopped) return;
        Messages.Message("PhobosRoute_StopNotEmptied".Translate(Vehicle.LabelShort.Named("VEHICLE"), (reason ?? "").Named("REASON")), Vehicle,
            MessageTypeDefOf.CautionInput, false);
    }

    // ---- A tanker pumping at its stops: out at the drop-off (Framework 0.303.0), in at the pick-up (0.305.0); the owner's word, 26 Nov 2026 ----

    /// <summary>
    /// Sets the tank up where it stands and its pump going, quietly: its inspect line says what it does. It pumps out into, or in from, whatever
    /// its pump reaches there, as it would set going by hand (plumbing or a pipeline under it, a tank or vehicle beside it set the other way).
    /// It fills only compartments already set to a contents, with that contents. Unable to set up (the pump damaged, the load frozen in), it
    /// says why once and waits out the stop's time for colonists.
    /// </summary>
    void StartPumping(CompPhobosTank tank, bool loading)
    {
        workedItself = true;
        if (!tank.PumpReady)
        {
            if (tank.CannotDeploy() is { } why)
            {
                stopWork = LoopStopWork.Done;
                Messages.Message("PhobosRoute_PumpNotSetUp".Translate(Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")), Vehicle,
                    MessageTypeDefOf.CautionInput, false);
                return;
            }
            tank.SetDeployed(true, quietly: true);
            if (!tank.PumpReady)
            {
                stopWork = LoopStopWork.Done;
                return;
            }
        }
        pumpLoading = loading;
        if (!SelectedWants(tank)) NextWantedCompartment(tank);
        tank.SetMode(loading ? TankPumpMode.Load : TankPumpMode.Unload, quietly: true);
        pumped = true;
        stopWork = LoopStopWork.Pumping;
        pumpProgressTick = Find.TickManager.TicksGame;
        pumpLastLoad = LoadOf(tank);
        pumpIdleTries = 0;
        pumpIdleTold = false;
        string variant = loading ? "filling" : "started";
        if (Seen.Wants("vehicles.route-pump", Vehicle, variant)) Seen.Note("vehicles.route-pump", Vehicle, Vehicle.LabelShort, variant);
    }

    /// <summary>
    /// One look while it pumps. Emptied at the drop-off, or full at the pick-up, it leaves. A compartment done with, or one whose pump moves
    /// nothing for a while (RouteRule.PumpIdle), and it turns to the next it still wants. Once every one has stood idle: at the pick-up with
    /// something taken on, it leaves with what it has [A]; otherwise the player is told why once, and it keeps pumping until the stop's time is
    /// up, in case room is made or more comes [A].
    /// </summary>
    void PumpingStep()
    {
        if (Tank is not { } tank)
        {
            stopWork = LoopStopWork.Done;
            return;
        }
        float load = LoadOf(tank);
        if (pumpLoading && load > pumpLastLoad + 0.01f) loadedAny = true;
        if (!pumpLoading && !tank.HasLoad || pumpLoading && !HasRoom(tank))
        {
            emptied = !pumpLoading;
            if (pumpLoading) loadedAny = true;
            stopWork = LoopStopWork.Done;
            string variant = pumpLoading ? "filled" : "emptied";
            if (Seen.Wants("vehicles.route-pump", Vehicle, variant)) Seen.Note("vehicles.route-pump", Vehicle, Vehicle.LabelShort, variant);
            return;
        }
        // Set to another mode or packed away by a player's hand: the pumping is theirs now.
        if (tank.Mode != (pumpLoading ? TankPumpMode.Load : TankPumpMode.Unload) || !tank.PumpReady)
        {
            stopWork = LoopStopWork.Done;
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (pumpLoading ? load > pumpLastLoad + 0.01f : load < pumpLastLoad - 0.01f)
        {
            pumpLastLoad = load;
            pumpProgressTick = now;
            pumpIdleTries = 0;
            return;
        }
        bool selectedDone = !SelectedWants(tank);
        if (!selectedDone && !RouteRule.PumpIdle(now - pumpProgressTick, Rules.loopPumpIdleTicks)) return;
        if (!selectedDone) pumpIdleTries++;
        if (pumpIdleTries >= tank.Compartments.Count(c => Wants(tank, c)))
        {
            // Filled part way and the source has run dry: it carries what it has.
            if (pumpLoading && loadedAny)
            {
                stopWork = LoopStopWork.Done;
                if (Seen.Wants("vehicles.route-pump", Vehicle, "filled")) Seen.Note("vehicles.route-pump", Vehicle, Vehicle.LabelShort, "filled");
                return;
            }
            if (!pumpIdleTold)
            {
                pumpIdleTold = true;
                string why = tank.PumpStatus ?? (pumpLoading ? "PhobosRoute_PumpNothingGives" : "PhobosRoute_PumpNothingTakes").Translate().Resolve();
                Messages.Message((pumpLoading ? "PhobosRoute_PumpNotFilled" : "PhobosRoute_PumpNotEmptied").Translate(Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")),
                    Vehicle, MessageTypeDefOf.CautionInput, false);
            }
        }
        NextWantedCompartment(tank);
        pumpLastLoad = LoadOf(tank);
        pumpProgressTick = now;
    }

    static float LoadOf(CompPhobosTank tank) => tank.Compartments.Sum(c => c.amount);

    /// <summary>A compartment set to a contents with room for more of it, while the shell holds a load.</summary>
    static bool Room(CompPhobosTank tank, TankCompartment c) => c.contents != null && c.amount < tank.CompartmentCapacity - 0.5f && tank.ShellTakesLoad;

    static bool HasRoom(CompPhobosTank tank) => tank.Compartments.Any(c => Room(tank, c));

    /// <summary>Whether the pump still has work in this compartment: a load to give at the drop-off, room to fill at the pick-up.</summary>
    bool Wants(CompPhobosTank tank, TankCompartment c) => pumpLoading ? Room(tank, c) : c.HasLoad;

    bool SelectedWants(CompPhobosTank tank) => Wants(tank, tank.Compartments[tank.SelectedIndex]);

    /// <summary>Moves the pump on to the next compartment it still has work in, if any other.</summary>
    void NextWantedCompartment(CompPhobosTank tank)
    {
        for (int i = 0; i < tank.Compartments.Count; i++)
        {
            tank.SelectNextCompartment(quietly: true);
            if (SelectedWants(tank)) return;
        }
    }

    /// <summary>The colony's storage building or stockpile the stop's cell is on, if any.</summary>
    UnloadPlace? PlaceAt(IntVec3 cell)
    {
        Map map = Vehicle.Map;
        if (!cell.InBounds(map)) return null;
        foreach (Thing thing in cell.GetThingList(map))
        {
            if (thing is Building_Storage store && store.Faction == Faction.OfPlayer) return new UnloadPlace(store, null);
        }
        return map.zoneManager.ZoneAt(cell) is Zone_Stockpile zone && zone.Cells.Count > 0 ? new UnloadPlace(null, zone) : null;
    }

    /// <summary>Stops its own work, quietly, and forgets the stop: on leaving it, or when the loop ends.</summary>
    void EndStopWork()
    {
        if (stopWork == LoopStopWork.Unloading && Unloader is { Unloading: true } unloader) unloader.StopUnloading();
        if (stopWork == LoopStopWork.Gathering && Grab is { Gathering: true } grab) grab.StopGathering();
        // Its pump back to hold and its hoses in, quietly, whoever set it up: set up, it could not drive on (Framework 0.303.0).
        if (pumped && Tank is { } tank)
        {
            if (tank.Mode != TankPumpMode.Hold) tank.SetMode(TankPumpMode.Hold, quietly: true);
            if (tank.Deployed) tank.SetDeployed(false, quietly: true);
        }
        pumped = false;
        pumpLoading = false;
        pumpProgressTick = -1;
        pumpLastLoad = -1f;
        pumpIdleTries = 0;
        pumpIdleTold = false;
        stopWork = LoopStopWork.None;
        workedItself = emptied = loadedAny = false;
        backUpCell = IntVec3.Invalid;
        backUpFacing = -1;
        backUpTries = 0;
        workResumed = false;
    }

    /// <summary>
    /// Its engine off while it waits at a stop (Framework 0.284.0, the owner's card of 25 Nov 2026), so colonists refuel and unload it as any
    /// parked vehicle; unless its battery is nearly flat (0.286.0), when the engine runs to charge it until it is well up again.
    /// </summary>
    void EngineAtStop()
    {
        var charge = Vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.TryGetQuantity("charge", out var c) ? c : double.NaN;
        charging = RouteRule.ChargesAtStop(charging, charge, Rules.loopChargeStartBelow, Rules.loopChargeStopAbove);
        if (charging)
        {
            if (!Vehicle.Drafted) Ready();
        }
        else if (Vehicle.Drafted)
        {
            VehicleAutoDrive.StopEngine(Vehicle);
        }
    }

    /// <summary>Orders a drive to a place near the target, unless one in hand already makes for it or the vehicle is there.</summary>
    void DriveToward(IntVec3 target)
    {
        var map = Vehicle.Map;
        target = target.ClampInsideMap(map);
        var driving = orderCell.IsValid && VehicleAutoDrive.HeadingFor(Vehicle, orderCell);
        var orderToTarget = orderCell.IsValid ? orderCell.DistanceTo(target) : double.MaxValue;
        if (!RouteRule.NeedsOrder(Vehicle.Position.DistanceTo(target), driving, orderToTarget, Rules.followNearCells, Rules.followSlackCells)) return;
        if (!Ready()) return;
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
        // A failed kit keeps the buttons that end a route (Framework 0.284.0); starting one needs it working.
        if (!RouteControl.KitCarried(vehicle)) yield break;
        switch (mode)
        {
            case RouteMode.None:
                if (!RouteControl.KitWorks(vehicle)) break;
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
        // Framework 0.284.0: a failed kit, a loop away from its map, and what keeps it from driving on, each said.
        if (Remote is { KitDown: true }) return "PhobosRemote_InspectKitDown".Translate();
        if (mode == RouteMode.Loop && !OnLoopMap) return "PhobosRoute_InspectAwayFromLoop".Translate();
        var state = Remote?.State ?? RemoteState.Off;
        if (state is RemoteState.Linked && waitingWhy != null) return "PhobosRoute_InspectCannotDrive".Translate(waitingWhy.Named("REASON"));
        if (state is RemoteState.Holding) return "PhobosRoute_InspectHolding".Translate();
        if (state is RemoteState.ComingBack) return "PhobosRoute_InspectComingBack".Translate();
        if (state is RemoteState.CarryingOn) return "PhobosRoute_InspectCarryingOn".Translate();
        if (state is RemoteState.Waiting) return "PhobosRoute_InspectWaiting".Translate();
        if (mode == RouteMode.Follow)
        {
            return "PhobosRoute_InspectFollowing".Translate((ahead?.LabelShort ?? "?").Named("AHEAD"), (leader?.LabelShort ?? "?").Named("LEADER"));
        }
        // Framework 0.299.0: its own work at a stop, said.
        if (stopWork == LoopStopWork.BackingUp) return "PhobosRoute_InspectBackingUp".Translate();
        if (stopWork == LoopStopWork.Unloading) return "PhobosRoute_InspectTipping".Translate();
        if (stopWork == LoopStopWork.Gathering) return "PhobosRoute_InspectLifting".Translate();
        if (stopWork == LoopStopWork.Pumping) return pumpLoading ? "PhobosRoute_InspectFilling".Translate() : "PhobosRoute_InspectPumping".Translate();
        if (dwellUntil >= 0)
        {
            var left = Mathf.Max(0, dwellUntil - Find.TickManager.TicksGame);
            if (charging)
            {
                return "PhobosRoute_InspectChargingAtStop".Translate((leg == 0 ? "PhobosRoute_PickUp" : "PhobosRoute_DropOff").Translate().Named("STOP"),
                    left.ToStringTicksToPeriod().Named("TIME"));
            }
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
        Scribe_Values.Look(ref loopMapId, "PhobosRoute_LoopMap", -1);
        Scribe_Values.Look(ref stopWork, "PhobosRoute_StopWork");
        Scribe_Values.Look(ref workedItself, "PhobosRoute_WorkedItself");
        Scribe_Values.Look(ref emptied, "PhobosRoute_Emptied");
        Scribe_Values.Look(ref loadedAny, "PhobosRoute_LoadedAny");
        Scribe_Values.Look(ref backUpCell, "PhobosRoute_BackUpCell", IntVec3.Invalid);
        Scribe_Values.Look(ref backUpFacing, "PhobosRoute_BackUpFacing", -1);
        Scribe_Values.Look(ref pumped, "PhobosRoute_Pumped");
        Scribe_Values.Look(ref pumpLoading, "PhobosRoute_PumpLoading");
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
