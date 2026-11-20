using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle that cannot drive, carried to the bay (Framework 0.201.0; docs/development/vehicle-bay-trips.md, batches 3 and 4; owner, 20 Nov
/// 2026: "Tow, or push light ones by hand"). Two vehicles may take part, each keeping its own half of the state here:
/// <list type="bullet">
/// <item><b>The wreck</b> keeps its place at the bay and waits (<see cref="BayTripStage.Towed"/>), is moved its last cells into the bay's
/// reach (<see cref="BayTripStage.Shunted"/>), or is pushed all the way by hand (<see cref="BayTripStage.Pushed"/>).</item>
/// <item><b>The recovery vehicle</b> calls a driver, drives up to the wreck, waits for a colonist to couple it (the hitch's own job),
/// calls a driver again, tows it to a stop beyond its place so it trails in behind, lets it go, stands by while it is shunted, and
/// drives back to where it started.</item>
/// </list>
/// The tow is the hitch's real tow (CompPhobosHitch: the towed weight reaches the drivetrain, the brakes and clutch wear). The shunt and
/// the push move the wreck a cell at a time along a way found once (<see cref="GridPathRule"/>), as the winch drags a vehicle.
/// </summary>
public sealed partial class CompPhobosBayTrip
{
    /// <summary>How often a move that must wait (someone in the way, not enough hands yet) looks again, in ticks.</summary>
    const int MoveRetryTicks = 30;
    /// <summary>A pusher counts while their last push was this lately, in ticks.</summary>
    const int PusherFreshTicks = 60;

    /// <summary>The other vehicle of a tow: the recovery vehicle, for the wreck; the wreck, for the recovery vehicle.</summary>
    VehiclePawn? partner;
    /// <summary>Where a recovery vehicle started from, and the way it faced: it drives back there.</summary>
    IntVec3 fetchHome = IntVec3.Invalid;
    Rot4 fetchFacing = Rot4.Invalid;

    // Derived, never saved: the way a moved vehicle follows is found again after a load.
    List<IntVec3>? path;
    int pathIndex;
    int moveWait;
    int pushersNeeded;
    readonly Dictionary<Pawn, int> pushing = new();

    enum MoveStep
    {
        Moved,
        Wait,
        Blocked,
        There,
    }

    CompPhobosBayTrip? PartnerTrip => partner is { Spawned: true } ? partner.GetComp<CompPhobosBayTrip>() : null;

    CompPhobosHitch? Hitch => Vehicle.GetComp<CompPhobosHitch>();

    bool Fetching => stage is BayTripStage.FetchCalling or BayTripStage.FetchDriving or BayTripStage.FetchCoupling
        or BayTripStage.FetchCallingTow or BayTripStage.FetchTowing or BayTripStage.FetchStanding;

    /// <summary>Whether it is being pushed to the bay by hand, or waits for hands.</summary>
    public bool BeingPushed => stage == BayTripStage.Pushed && Vehicle.Spawned;

    /// <summary>How many people it takes to push it where it stands.</summary>
    public int PushersNeeded => pushersNeeded;

    string PartnerName => partner?.LabelShort ?? "";

    // ---- The wreck ----

    /// <summary>
    /// A vehicle that will not drive, with a place kept at a bay: towed if a recovery vehicle is free, else pushed by hand if it is light
    /// enough, else it stays and says why (<see cref="BayTripRule.Carry"/>).
    /// </summary>
    void BeginCarried(Thing found, IntVec3 cell, string cannot)
    {
        bool towing = Settings.bayTripTowing, pushingOn = Settings.bayTripPushing;
        CompPhobosBayTrip? tug = towing ? BayTrips.FindTug(Vehicle) : null;
        int needed = PushRule.Pushers(CompPhobosHitch.RequiredAt(Vehicle, Vehicle.Position), Rules.pushKgfPerPerson);
        BayTripCarry how = BayTripRule.Carry(false, towing, tug != null, pushingOn, needed, Rules.pushMostPeople);
        if (how == BayTripCarry.None)
        {
            blocked = "PhobosBayTrip_CannotDrive".Translate(cannot.Named("REASON")).Resolve();
            if (towing) blocked += " " + "PhobosBayTrip_NoTug".Translate().Resolve();
            if (pushingOn)
            {
                blocked += " " + (needed > Rules.pushMostPeople * 100
                    ? "PhobosBayTrip_CannotPush".Translate().Resolve()
                    : "PhobosBayTrip_TooHeavyToPush".Translate(needed.Named("COUNT"), Rules.pushMostPeople.Named("MOST")).Resolve());
            }
            return;
        }
        bay = found;
        standCell = cell;
        standFacing = Vehicle.Rotation;
        failedDrives = 0;
        leftUnfinished = false;
        brought.Clear();
        path = null;
        pushing.Clear();
        if (how == BayTripCarry.Tow)
        {
            partner = tug!.Vehicle;
            SetStage(BayTripStage.Towed);
            tug.BeginFetch(this);
            Seen.Note("vehicles.bay-trip.tow", Vehicle, variant: "called");
            return;
        }
        path = PlanPath();
        if (path == null)
        {
            bay = null;
            standCell = IntVec3.Invalid;
            standFacing = Rot4.Invalid;
            // The search for a way is not free: it is not made again until a day has passed.
            nextLookTick = Find.TickManager.TicksGame + Rules.bayTripBackOffTicks;
            blocked = "PhobosBayTrip_NoWayToPush".Translate().Resolve();
            return;
        }
        pathIndex = 0;
        pushersNeeded = needed;
        moveWait = MoveRetryTicks;
        lastWorkTick = Find.TickManager.TicksGame;
        SetStage(BayTripStage.Pushed);
        Seen.Note("vehicles.bay-trip.push", Vehicle, variant: "called");
    }

    /// <summary>One look while it waits for its tow or is towed: a recovery vehicle that is no longer coming ends the trip, to try again.</summary>
    void TowedLook()
    {
        if (PartnerTrip is { Fetching: true } tug && tug.partner == Vehicle) return;
        End(crewOut: false, backOff: false);
    }

    /// <summary>The recovery vehicle could not bring it: the trip ends, and it says why until it looks again, a day on.</summary>
    internal void TowFailed(string key)
    {
        if (stage is not (BayTripStage.Towed or BayTripStage.Shunted)) return;
        if (Seen.Wants("vehicles.bay-trip.carry-failed", Vehicle)) Seen.Note("vehicles.bay-trip.carry-failed", Vehicle, variant: key);
        string tug = PartnerName;
        partner = null;
        End(crewOut: false, backOff: true);
        blocked = key.Translate(tug.Named("TUG")).Resolve();
    }

    /// <summary>The recovery vehicle has let it go near its place: in the bay's reach it has arrived; short of it, it is shunted the rest.</summary>
    internal void AfterTow()
    {
        if (stage != BayTripStage.Towed) return;
        if (InBayReach())
        {
            ArriveCarried(towed: true);
            return;
        }
        path = PlanPath();
        if (path == null)
        {
            TowFailed("PhobosBayTrip_LeftShort");
            return;
        }
        pathIndex = 0;
        moveWait = Rules.winchTicksPerCell;
        lastWorkTick = Find.TickManager.TicksGame;
        SetStage(BayTripStage.Shunted);
        Seen.Note("vehicles.bay-trip.shunted", Vehicle);
    }

    bool InBayReach() => Vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && BayTrips.AtBayOrYard(systems);

    /// <summary>It stands at the bay, brought by a tow or by hand: the mechanics take over, as after a drive.</summary>
    void ArriveCarried(bool towed)
    {
        string by = PartnerName;
        badCells.Clear();
        failedDrives = 0;
        path = null;
        pushing.Clear();
        partner = null;
        standCell = Vehicle.Position;
        lastWorkTick = Find.TickManager.TicksGame;
        nextLookTick = lastWorkTick + Rules.bayTripLookTicks;
        SetStage(BayTripStage.AtBay);
        // Where it was left, it lays its pipes again, as after the winch.
        Hitches.LookAtPipes(Vehicle);
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems)
        {
            VehicleStory.Log(systems, towed ? BayTripDefOf.PhobosLog_TowedToBay : BayTripDefOf.PhobosLog_PushedToBay, subject: by);
        }
        if (towed) Seen.Note("vehicles.bay-trip.towed-in", Vehicle);
        else Seen.Note("vehicles.bay-trip.pushed-in", Vehicle);
    }

    // ---- Moved a cell at a time: shunted by the recovery vehicle, or pushed by hand ----

    public override void CompTick()
    {
        base.CompTick();
        if (stage != BayTripStage.Shunted && stage != BayTripStage.Pushed) return;
        if (!Vehicle.Spawned || --moveWait > 0) return;
        if (stage == BayTripStage.Shunted) ShuntStep();
        else PushStep();
    }

    /// <summary>One slow look at a move: one that has not advanced for too long is given up.</summary>
    void MovedLook(int now)
    {
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            // Nobody pushes with enemies about: the clock waits with them.
            lastWorkTick = now;
            return;
        }
        if (!BayTripRule.Overstayed(now - lastWorkTick, Rules.bayTripPushWaitTicks)) return;
        if (stage == BayTripStage.Shunted) TowFailed("PhobosBayTrip_LeftShort");
        else PushFailed("PhobosBayTrip_NotEnoughHands");
    }

    /// <summary>The way from where it stands to its place, facing as it faces now, or null when there is none within the search's limit.</summary>
    List<IntVec3>? PlanPath()
    {
        Rot4 rot = Vehicle.Rotation;
        Map map = Vehicle.Map;
        List<CellRect> kept = BayTrips.KeptBy(map, Vehicle);
        List<(int X, int Z)>? found = GridPathRule.Find(Vehicle.Position.x, Vehicle.Position.z, standCell.x, standCell.z,
            (x, z) => BayTrips.BodyFits(Vehicle, Vehicle.VehicleRect(new IntVec3(x, 0, z), rot), map, kept, null), Rules.bayTripPathNodes);
        return found?.Select(c => new IntVec3(c.X, 0, c.Z)).ToList();
    }

    bool FitsAt(IntVec3 cell, Rot4 rot, List<CellRect> kept) => BayTrips.BodyFits(Vehicle, Vehicle.VehicleRect(cell, rot), Vehicle.Map, kept, null);

    /// <summary>
    /// Moves it one cell along its way. It turns to roll along its length when it fits that way here and all the way on; a cell someone
    /// stands on is waited for; a way that has closed is found again once.
    /// </summary>
    MoveStep StepAlong()
    {
        Map map = Vehicle.Map;
        List<CellRect> kept = BayTrips.KeptBy(map, Vehicle);
        if (path == null || (pathIndex < path.Count && (path[pathIndex] - Vehicle.Position).LengthManhattan != 1))
        {
            path = PlanPath();
            pathIndex = 0;
            if (path == null) return MoveStep.Blocked;
        }
        if (pathIndex >= path.Count) return MoveStep.There;
        IntVec3 next = path[pathIndex];
        Rot4 travel = Rot4.FromIntVec3(next - Vehicle.Position);
        Rot4 facing = Vehicle.Rotation;
        if (facing != travel && facing != travel.Opposite && FitsAt(Vehicle.Position, travel, kept))
        {
            bool allTheWay = true;
            for (int i = pathIndex; i < path.Count && allTheWay; i++) allTheWay = FitsAt(path[i], travel, kept);
            if (allTheWay) BayTrips.Turn(Vehicle, travel);
        }
        CellRect body = Vehicle.VehicleRect(next, Vehicle.Rotation);
        if (!BayTrips.BodyFits(Vehicle, body, map, kept, null))
        {
            path = PlanPath();
            pathIndex = 0;
            return path == null ? MoveStep.Blocked : MoveStep.Wait;
        }
        if (BayTrips.SomeoneOn(Vehicle, body, map)) return MoveStep.Wait;
        VehicleShift.MoveOneCell(Vehicle, next);
        pathIndex++;
        lastWorkTick = Find.TickManager.TicksGame;
        return pathIndex >= path.Count ? MoveStep.There : MoveStep.Moved;
    }

    /// <summary>At the end of its way it turns as its place was chosen, if it fits, and must stand in the bay's reach.</summary>
    bool SettleAtPlace()
    {
        if (standFacing.IsValid && Vehicle.Rotation != standFacing && FitsAt(Vehicle.Position, standFacing, BayTrips.KeptBy(Vehicle.Map, Vehicle)))
        {
            BayTrips.Turn(Vehicle, standFacing);
        }
        return InBayReach();
    }

    void ShuntStep()
    {
        // The recovery vehicle stands by with its crew aboard while its winch does the work.
        if (PartnerTrip is not { stage: BayTripStage.FetchStanding } tug || tug.partner != Vehicle || tug.Vehicle.AllPawnsAboard.Count == 0)
        {
            TowFailed("PhobosBayTrip_LeftShort");
            return;
        }
        switch (StepAlong())
        {
            case MoveStep.Blocked:
                TowFailed("PhobosBayTrip_LeftShort");
                return;
            case MoveStep.Wait:
                moveWait = MoveRetryTicks;
                return;
            case MoveStep.Moved:
                moveWait = Rules.winchTicksPerCell;
                return;
        }
        if (SettleAtPlace()) ArriveCarried(towed: true);
        else TowFailed("PhobosBayTrip_LeftShort");
    }

    // ---- Pushed by hand ----

    /// <summary>A colonist pushing it reports in (the push job, every few ticks while they stand at their place).</summary>
    public void NotePusher(Pawn pawn) => pushing[pawn] = Find.TickManager.TicksGame;

    /// <summary>How many are pushing now: those who reported lately.</summary>
    public int PushersNow()
    {
        int now = Find.TickManager.TicksGame, count = 0;
        foreach (KeyValuePair<Pawn, int> pusher in pushing)
        {
            if (now - pusher.Value <= PusherFreshTicks && pusher.Key is { Spawned: true }) count++;
        }
        return count;
    }

    /// <summary>
    /// Where this colonist pushes from: a cell beside the body that they can stand on, that the body is not about to move onto and that
    /// nobody else has taken; the one they stand on if it will do, else the nearest. Invalid when there is none.
    /// </summary>
    public IntVec3 PushSpotFor(Pawn pawn)
    {
        Map map = Vehicle.Map;
        CellRect body = Vehicle.OccupiedRect();
        CellRect? ahead = path != null && pathIndex < path.Count ? Vehicle.VehicleRect(path[pathIndex], Vehicle.Rotation) : null;
        IntVec3 best = IntVec3.Invalid;
        int bestDistance = int.MaxValue;
        foreach (IntVec3 c in body.ExpandedBy(1))
        {
            if (body.Contains(c) || !c.InBounds(map) || !c.Standable(map) || (ahead is { } front && front.Contains(c))) continue;
            if (c == pawn.Position) return c;
            bool taken = false;
            List<Thing> things = c.GetThingList(map);
            for (int i = 0; i < things.Count && !taken; i++) taken = things[i] is Pawn other && other != pawn;
            if (taken) continue;
            int distance = c.DistanceToSquared(pawn.Position);
            if (distance >= bestDistance) continue;
            best = c;
            bestDistance = distance;
        }
        return best;
    }

    void PushStep()
    {
        double required = CompPhobosHitch.RequiredAt(Vehicle, Vehicle.Position);
        pushersNeeded = PushRule.Pushers(required, Rules.pushKgfPerPerson);
        if (!PushRule.CanPush(pushersNeeded, Rules.pushMostPeople))
        {
            // The ground under it has turned soft, or a wheel has gone: more hands than can reach it.
            PushFailed("PhobosBayTrip_TooHeavyHere");
            return;
        }
        int hands = PushersNow();
        if (hands < pushersNeeded)
        {
            moveWait = MoveRetryTicks;
            return;
        }
        switch (StepAlong())
        {
            case MoveStep.Blocked:
                PushFailed("PhobosBayTrip_NoWayToPush");
                return;
            case MoveStep.Wait:
                moveWait = MoveRetryTicks;
                return;
            case MoveStep.Moved:
                moveWait = PushRule.TicksPerCell(Rules.pushTicksPerCell, required, hands, Rules.pushKgfPerPerson);
                if (Seen.Wants("vehicles.bay-trip.push", Vehicle, "moving")) Seen.Note("vehicles.bay-trip.push", Vehicle, variant: "moving");
                return;
        }
        if (SettleAtPlace()) ArriveCarried(towed: false);
        else PushFailed("PhobosBayTrip_NoWayToPush");
    }

    void PushFailed(string key)
    {
        if (Seen.Wants("vehicles.bay-trip.carry-failed", Vehicle)) Seen.Note("vehicles.bay-trip.carry-failed", Vehicle, variant: key);
        End(crewOut: false, backOff: true);
        blocked = key.Translate().Resolve();
    }

    // ---- The recovery vehicle ----

    /// <summary>Whether the wreck still waits for this vehicle.</summary>
    bool TowStillOn(BayTripStage wreckStage) => PartnerTrip is { } wreck && wreck.partner == Vehicle && wreck.stage == wreckStage;

    /// <summary>Sent to fetch a wreck: it remembers where it stood, and calls a driver.</summary>
    internal void BeginFetch(CompPhobosBayTrip wreck)
    {
        partner = wreck.Vehicle;
        bay = wreck.bay;
        fetchHome = Vehicle.Position;
        fetchFacing = Vehicle.Rotation;
        standCell = IntVec3.Invalid;
        standFacing = Rot4.Invalid;
        failedDrives = 0;
        leftUnfinished = false;
        blocked = null;
        brought.Clear();
        badCells.Clear();
        SetStage(BayTripStage.FetchCalling);
        DriverCalls.Call(Vehicle.Map, this, true);
    }

    /// <summary>
    /// One look while the recovery vehicle calls a driver, before the wreck is coupled or after. Crew the player boarded, or an engine the
    /// player started, hands the vehicle back to them and frees the wreck to try again.
    /// </summary>
    void FetchCallingLook(int now)
    {
        if (!TowStillOn(BayTripStage.Towed))
        {
            FetchOver();
            return;
        }
        brought.RemoveAll(p => p == null || p.Destroyed);
        if (Vehicle.AllPawnsAboard.Any(p => !brought.Contains(p)) || (Vehicle.Drafted && brought.Count == 0))
        {
            LetGo();
            End(crewOut: true, backOff: false);
            return;
        }
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return;
        if (Vehicle.HasEnoughOperators && (brought.Count > 0 || Vehicle.AllPawnsAboard.Count == 0))
        {
            DriverCalls.Call(Vehicle.Map, this, false);
            blocked = null;
            if (stage == BayTripStage.FetchCalling) DriveToWreck();
            else DriveToBay();
            return;
        }
        blocked = BayTripRule.Overstayed(now - stageTick, Rules.bayTripDriverWaitTicks)
            ? "PhobosBayTrip_NoDriver".Translate(DriverCalls.WorkLabel.Named("WORK")).Resolve()
            : null;
    }

    /// <summary>Sets off for a stop beside the wreck, within the hitch's reach of it.</summary>
    void DriveToWreck()
    {
        if (VehicleAutoDrive.CannotDrive(Vehicle) != null || !TakeOver())
        {
            FetchFail("PhobosBayTrip_TugWouldNotStart");
            return;
        }
        // A ring this many cells out lies within the hitch's reach at its corners too.
        int farthest = (int)(Rules.hitchReachCells / 1.42f);
        IntVec3 beside = IntVec3.Invalid;
        for (int ring = 2; ring <= farthest; ring++)
        {
            if (VehicleAutoDrive.TryFindStandCell(Vehicle, partner!.Position, ring, farthestFirst: false, c => !badCells.Contains(c), out beside)) break;
        }
        if (!beside.IsValid || !BeginDrive(beside))
        {
            FetchFail("PhobosBayTrip_NoRoomBesideWreck");
            return;
        }
        SetStage(BayTripStage.FetchDriving);
        Seen.Note("vehicles.bay-trip.tow", Vehicle, variant: "fetching");
    }

    /// <summary>One look on the drive to the wreck: stopped within the hitch's reach, it asks for the wreck to be coupled and its driver gets out.</summary>
    void FetchDrivingLook()
    {
        if (!TowStillOn(BayTripStage.Towed))
        {
            FetchOver();
            return;
        }
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            FetchFail("PhobosBayTrip_TowThreat");
            return;
        }
        DriveWatch drive = WatchDrive();
        if (drive == DriveWatch.OnWay) return;
        if (drive == DriveWatch.OtherOrders)
        {
            LetGo();
            End(crewOut: false, backOff: true);
            return;
        }
        if (drive == DriveWatch.EngineOff)
        {
            FetchFail("PhobosBayTrip_TugWouldNotStart");
            return;
        }
        if (drive == DriveWatch.Stuck) HaltDrive();
        CompPhobosHitch? hitch = Hitch;
        if (hitch != null && hitch.Refusal() == null && hitch.RefusalFor(partner!) == null)
        {
            hitch.Choose(partner!);
            if (hitch.Pending == partner)
            {
                // The driver gets out: whoever couples it, the hitch's own job does the work.
                HandBack(switchOff: true);
                CrewOut();
                SetStage(BayTripStage.FetchCoupling);
                return;
            }
        }
        if (standCell.IsValid) badCells.Add(standCell);
        failedDrives++;
        if (VehicleRunRule.GiveUp(failedDrives, Rules.bayTripFailedDrives))
        {
            FetchFail("PhobosBayTrip_NoRoomBesideWreck");
            return;
        }
        DriveToWreck();
    }

    /// <summary>One look while the wreck waits to be coupled: coupled, a driver is called again; refused or too long, the tow is given up.</summary>
    void FetchCouplingLook(int now)
    {
        if (!TowStillOn(BayTripStage.Towed))
        {
            FetchOver();
            return;
        }
        CompPhobosHitch? hitch = Hitch;
        if (hitch?.Trailer == partner)
        {
            brought.Clear();
            SetStage(BayTripStage.FetchCallingTow);
            DriverCalls.Call(Vehicle.Map, this, true);
            Seen.Note("vehicles.bay-trip.tow", Vehicle, variant: "coupled");
            return;
        }
        // The hitch gave it up (no room behind it), or nobody came.
        if (hitch?.Pending != partner) FetchFail("PhobosBayTrip_NoRoomToCouple");
        else if (BayTripRule.Overstayed(now - stageTick, Rules.bayTripCoupleWaitTicks)) FetchFail("PhobosBayTrip_NobodyCoupled");
    }

    /// <summary>
    /// Sets off with the wreck for a stop beyond its place: about the tow's own length past it, on the far side from here, so the wreck
    /// trailing behind comes to rest at its place or near it.
    /// </summary>
    void DriveToBay()
    {
        if (PartnerTrip is not { } wreck || VehicleAutoDrive.CannotDrive(Vehicle) != null || !TakeOver())
        {
            FetchFail("PhobosBayTrip_TugWouldNotStart");
            return;
        }
        IntVec3 place = wreck.standCell;
        CellRect keep = wreck.KeptCells ?? CellRect.SingleCell(place);
        int towLength = UnityEngine.Mathf.Max(2, UnityEngine.Mathf.RoundToInt((Vehicle.Position - partner!.Position).LengthHorizontal));
        int half = UnityEngine.Mathf.Max(Vehicle.def.size.x, Vehicle.def.size.z) / 2 + 1;
        CellRect clear = keep.ExpandedBy(half);
        IntVec3 stop = IntVec3.Invalid;
        foreach (int ring in new[] { towLength, towLength + 1, towLength + 2, towLength - 1, towLength + 4 })
        {
            if (ring < 2) continue;
            if (VehicleAutoDrive.TryFindStandCell(Vehicle, place, ring, farthestFirst: true, c => !clear.Contains(c) && !badCells.Contains(c), out stop)) break;
        }
        if (!stop.IsValid || !BeginDrive(stop))
        {
            FetchFail("PhobosBayTrip_NoRoomAtBay");
            return;
        }
        SetStage(BayTripStage.FetchTowing);
        Seen.Note("vehicles.bay-trip.tow", Vehicle, variant: "towing");
    }

    /// <summary>One look on the tow: stopped, it lets the wreck go and stands by while the wreck settles or is shunted the rest.</summary>
    void FetchTowingLook()
    {
        if (!TowStillOn(BayTripStage.Towed))
        {
            FetchOver();
            return;
        }
        DriveWatch drive = WatchDrive();
        if (drive == DriveWatch.OnWay && !GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return;
        if (drive == DriveWatch.OtherOrders)
        {
            // The player drives it now, wreck and all: the tow is theirs.
            PartnerTrip?.TowFailed("PhobosBayTrip_TowTaken");
            partner = null;
            End(crewOut: false, backOff: true);
            return;
        }
        if (drive is DriveWatch.OnWay or DriveWatch.Stuck) HaltDrive();
        CompPhobosBayTrip wreck = PartnerTrip!;
        Hitch?.Unhitch(quiet: true);
        VehicleAutoDrive.StopEngine(Vehicle);
        SetStage(BayTripStage.FetchStanding);
        if (drive == DriveWatch.EngineOff || GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            // It broke down on the way, or enemies came: the wreck is left where it stands, in reach or not.
            if (wreck.InBayReach()) wreck.AfterTow();
            else wreck.TowFailed("PhobosBayTrip_LeftShort");
            return;
        }
        wreck.AfterTow();
    }

    /// <summary>One look while it stands by: once the wreck has settled or been given up, it drives back to where it started.</summary>
    void FetchStandingLook(int now)
    {
        if (TowStillOn(BayTripStage.Shunted) && !BayTripRule.Overstayed(now - stageTick, Rules.bayTripBackOffTicks)) return;
        GoHome();
    }

    /// <summary>Lets go of the wreck, coupled or only asked for, without a word.</summary>
    void LetGo()
    {
        CompPhobosHitch? hitch = Hitch;
        if (hitch == null || partner == null) return;
        if (hitch.Pending == partner) hitch.CancelPending();
        if (hitch.Trailer == partner) hitch.Unhitch(quiet: true);
    }

    /// <summary>The tow could not be done: the wreck is told why, let go, and the recovery vehicle goes home.</summary>
    void FetchFail(string key)
    {
        CompPhobosBayTrip? wreck = PartnerTrip;
        LetGo();
        wreck?.TowFailed(key);
        GoHome();
    }

    /// <summary>The wreck no longer waits for us (repaired where it stood, taken by the player): let go and go home.</summary>
    void FetchOver()
    {
        LetGo();
        GoHome();
    }

    /// <summary>The recovery vehicle drives back to where it started: at once with its driver still aboard, else it calls one.</summary>
    void GoHome()
    {
        partner = null;
        DriverCalls.Call(Vehicle.Map, this, false);
        if (Running && standCell.IsValid) HaltDrive();
        IntVec3 home = fetchHome;
        Rot4 facing = fetchFacing;
        fetchHome = IntVec3.Invalid;
        fetchFacing = Rot4.Invalid;
        if (!home.IsValid || Vehicle.Position == home)
        {
            End(crewOut: true, backOff: false);
            return;
        }
        standCell = home;
        standFacing = facing;
        failedDrives = 0;
        leftUnfinished = false;
        badCells.Clear();
        bool crewOurs = Vehicle.AllPawnsAboard.Count > 0 && Vehicle.AllPawnsAboard.All(p => brought.Contains(p));
        if (crewOurs && Vehicle.HasEnoughOperators && VehicleAutoDrive.CannotDrive(Vehicle) == null && TakeOver() && BeginDrive(home, facing))
        {
            SetStage(BayTripStage.DrivingHome);
            return;
        }
        if (Vehicle.AllPawnsAboard.Count > 0)
        {
            // It will not start with who is aboard: they get out, and it stays where it is.
            End(crewOut: true, backOff: true);
            return;
        }
        SetStage(BayTripStage.CallingDriverHome);
        DriverCalls.Call(Vehicle.Map, this, true);
    }

    // ---- Shared with the trip's own ending and saving ----

    /// <summary>Before a trip ends where it stands: a recovery vehicle lets its wreck go and tells it.</summary>
    void ReleaseCarry()
    {
        if (Fetching && partner != null)
        {
            CompPhobosBayTrip? wreck = PartnerTrip;
            LetGo();
            partner = null;
            wreck?.TowFailed("PhobosBayTrip_TowCalledOff");
        }
        ClearCarry();
    }

    void ClearCarry()
    {
        partner = null;
        fetchHome = IntVec3.Invalid;
        fetchFacing = Rot4.Invalid;
        path = null;
        pathIndex = 0;
        pushersNeeded = 0;
        pushing.Clear();
    }

    void ExposeCarry()
    {
        if (Scribe.mode == LoadSaveMode.Saving && partner is { Destroyed: true }) partner = null;
        Scribe_References.Look(ref partner, "PhobosBayTripPartner");
        Scribe_Values.Look(ref fetchHome, "PhobosBayTripFetchHome", IntVec3.Invalid);
        Scribe_Values.Look(ref fetchFacing, "PhobosBayTripFetchFacing", Rot4.Invalid);
        Scribe_Values.Look(ref pushersNeeded, "PhobosBayTripPushers");
    }

    string? CarryLine() => stage switch
    {
        BayTripStage.Towed => (Hitches.Towing(Vehicle) != null ? "PhobosBayTrip_LineTowed" : "PhobosBayTrip_LineAwaitingTow").Translate(PartnerName.Named("TUG")).Resolve(),
        BayTripStage.Shunted => "PhobosBayTrip_LineShunted".Translate(PartnerName.Named("TUG")).Resolve(),
        BayTripStage.Pushed => "PhobosBayTrip_LinePushed".Translate(PushersNow().Named("NOW"), pushersNeeded.Named("COUNT")).Resolve(),
        BayTripStage.FetchCalling => "PhobosBayTrip_LineFetchCalling".Translate(PartnerName.Named("WRECK"), DriverCalls.WorkLabel.Named("WORK")).Resolve(),
        BayTripStage.FetchDriving => "PhobosBayTrip_LineFetchDriving".Translate(PartnerName.Named("WRECK")).Resolve(),
        BayTripStage.FetchCoupling => "PhobosBayTrip_LineFetchCoupling".Translate(PartnerName.Named("WRECK")).Resolve(),
        BayTripStage.FetchCallingTow => "PhobosBayTrip_LineFetchCallingTow".Translate(PartnerName.Named("WRECK"), DriverCalls.WorkLabel.Named("WORK")).Resolve(),
        BayTripStage.FetchTowing => "PhobosBayTrip_LineFetchTowing".Translate(PartnerName.Named("WRECK")).Resolve(),
        BayTripStage.FetchStanding => "PhobosBayTrip_LineFetchStanding".Translate(PartnerName.Named("WRECK")).Resolve(),
        _ => null,
    };
}
