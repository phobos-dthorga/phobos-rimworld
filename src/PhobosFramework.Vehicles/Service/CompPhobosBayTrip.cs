using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Sends a player's vehicle that needs repair to a dyno bay and home again without being told: it calls a colonist with Drive
/// work to take the wheel, while the setting "Vehicles are taken to a dyno bay for repair by themselves" is on. D'thorga Framework adds
/// it in code to every land vehicle with a loadout, trailers apart; it has no fields, and a modder never writes it in XML.</summary>
public sealed class CompProperties_PhobosBayTrip : CompProperties
{
    public CompProperties_PhobosBayTrip() => compClass = typeof(CompPhobosBayTrip);
}

/// <summary>Where a bay trip stands. Saved by name: append only.</summary>
public enum BayTripStage
{
    None,
    /// <summary>A place at a bay is kept for it, and it waits for a colonist with Drive work.</summary>
    CallingDriver,
    Driving,
    /// <summary>It stands in the bay's reach, engine off, nobody aboard, while the mechanics work.</summary>
    AtBay,
    CallingDriverHome,
    DrivingHome,
    /// <summary>It cannot drive: a recovery vehicle is coming for it, or is towing it (batch 3).</summary>
    Towed,
    /// <summary>Let go near its place, it is moved its last cells into the bay's reach while the recovery vehicle stands by.</summary>
    Shunted,
    /// <summary>It cannot drive and nothing can tow it: colonists push it to its place by hand (batch 4).</summary>
    Pushed,
    /// <summary>A recovery vehicle: sent for a wreck, it waits for a driver.</summary>
    FetchCalling,
    FetchDriving,
    /// <summary>A recovery vehicle beside the wreck, waiting for a colonist to couple it.</summary>
    FetchCoupling,
    /// <summary>A recovery vehicle with the wreck coupled, waiting for a driver again.</summary>
    FetchCallingTow,
    FetchTowing,
    /// <summary>A recovery vehicle standing by while the wreck it let go is shunted into the bay's reach.</summary>
    FetchStanding,
}

/// <summary>
/// A vehicle's trip to a dyno bay and home again, taken without being told (Framework 0.201.0; docs/development/vehicle-bay-trips.md;
/// owner, 20 Nov 2026: broken vehicles lay littered about a large colony). Every land vehicle of the player's has one. About every thousand
/// ticks an idle one looks: has it work that sends it (<see cref="BayTrips.NeedOf"/>), can that work be paid for, is it free, and is there a
/// working bay with room in reach. If so it keeps the place and calls a driver; the colonist who comes drives it there, gets out, and the
/// mechanics take over. When the work is done, or a day has passed with nothing done, it is driven home.
/// The trip's state lives here and nowhere else (the lesson from Vehicles at Work): short jobs, a limit on each stage. The driving, the
/// stall watch and the driver call are the shared base's (<see cref="CompPhobosVehicleRun"/>); what is decided is <see cref="BayTripRule"/>'s.
/// </summary>
public sealed partial class CompPhobosBayTrip : CompPhobosVehicleRun, IVehicleDriverArrives
{
    const string RunSavePrefix = "PhobosBayTrip";

    BayTripStage stage;
    /// <summary>The player's switch on the Fleet board: this vehicle never goes by itself.</summary>
    bool keptOut;
    /// <summary>The dyno bay it goes to or stands at.</summary>
    Thing? bay;
    /// <summary>The way it ends a drive facing; invalid for any.</summary>
    Rot4 standFacing = Rot4.Invalid;
    int nextLookTick;
    /// <summary>When the stage in hand began.</summary>
    int stageTick;
    /// <summary>At the bay: when work was last ready to be done.</summary>
    int lastWorkTick;
    /// <summary>It left the bay with work undone: it waits a day before it comes back.</summary>
    bool leftUnfinished;
    /// <summary>The colonists this trip called aboard: they get out at its end. Crew anyone else boarded is never put out.</summary>
    List<Pawn> brought = new();
    /// <summary>The player sent it into a service yard from the Fleet board (Framework 0.210.0): it parks there and the trip ends.</summary>
    bool toYardByOrder;
    /// <summary>The player ordered this trip from the Fleet board, into a yard or out of one (Framework 0.276.4; the owner's card of 24 Nov 2026,
    /// "Your order always goes"): it goes whatever the bay-trip setting or the kept-out switch say. Cleared when the trip ends, done or not.</summary>
    bool byOrder;

    /// <summary>Why it cannot go, or why it waits, in the player's words: for its own line, the Fleet board and the alert. Derived, never saved.</summary>
    string? blocked;
    /// <summary>Places a drive of this trip failed to reach. Derived, never saved.</summary>
    readonly HashSet<IntVec3> badCells = new();

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    public BayTripStage Stage => stage;
    public bool KeptOut => keptOut;
    public string? Blocked => blocked;
    public Thing? Bay => bay;

    public override bool Running => stage is BayTripStage.Driving or BayTripStage.DrivingHome or BayTripStage.FetchDriving or BayTripStage.FetchTowing;

    protected override bool WaitingForDriver => stage is BayTripStage.CallingDriver or BayTripStage.CallingDriverHome
        or BayTripStage.FetchCalling or BayTripStage.FetchCallingTow;

    protected override int StuckTicks => Rules.bayTripStuckTicks;

    /// <summary>A vehicle that can be driven across a map to a bay: on land, and not an aircraft. Trailers never get this comp.</summary>
    public bool TakesPart => Vehicle.VehicleDef.type == VehicleType.Land && Vehicle.GetComp<CompVehicleLauncher>() == null;

    /// <summary>The cells kept for it where its drive ends, or null: no other trip may choose a place that overlaps them.</summary>
    public CellRect? KeptCells => stage is BayTripStage.None or BayTripStage.AtBay || !standCell.IsValid ? null
        : Vehicle.VehicleRect(standCell, standFacing.IsValid ? standFacing : Vehicle.Rotation);

    /// <summary>What it is doing on its trip, in a few words, or null when it is on none.</summary>
    public string? DoingLine => stage switch
    {
        BayTripStage.CallingDriver => "PhobosBayTrip_LineCalling".Translate(DriverCalls.WorkLabel.Named("WORK")).Resolve(),
        BayTripStage.Driving => "PhobosBayTrip_LineDriving".Translate().Resolve(),
        BayTripStage.AtBay => "PhobosBayTrip_LineAtBay".Translate().Resolve(),
        BayTripStage.CallingDriverHome => "PhobosBayTrip_LineCallingHome".Translate(DriverCalls.WorkLabel.Named("WORK")).Resolve(),
        BayTripStage.DrivingHome => "PhobosBayTrip_LineDrivingHome".Translate().Resolve(),
        _ => CarryLine(),
    };

    /// <summary>The player's switch. Only <see cref="BayTrips.SetKeptOut"/> calls this.</summary>
    internal void SetKeptOut(bool value)
    {
        keptOut = value;
        blocked = null;
        // The switch stops automatic trips; a trip the player ordered still goes (Framework 0.276.4).
        if (value && stage != BayTripStage.None && !byOrder) End(crewOut: true, backOff: false);
    }

    void SetStage(BayTripStage next)
    {
        stage = next;
        stageTick = Find.TickManager.TicksGame;
    }

    // ---- The looks ----

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!Vehicle.Spawned) return;
        int now = Find.TickManager.TicksGame;
        if (stage == BayTripStage.None)
        {
            if (!BayTripRule.LookDue(now, nextLookTick)) return;
            nextLookTick = BayTripRule.NextLook(now, Rules.bayTripLookTicks, Vehicle.thingIDNumber, GenTicks.TickRareInterval);
            IdleLook();
            return;
        }
        if (!BayTripRule.GoesOn(Vehicle.Faction == Faction.OfPlayer, BayTrips.On, keptOut, byOrder))
        {
            End(crewOut: true, backOff: false);
            return;
        }
        switch (stage)
        {
            case BayTripStage.CallingDriver:
            case BayTripStage.CallingDriverHome:
                CallingLook(now);
                break;
            case BayTripStage.Driving:
            case BayTripStage.DrivingHome:
                DrivingLook();
                break;
            case BayTripStage.Towed:
                TowedLook();
                break;
            case BayTripStage.Shunted:
            case BayTripStage.Pushed:
                MovedLook(now);
                break;
            case BayTripStage.FetchCalling:
            case BayTripStage.FetchCallingTow:
                FetchCallingLook(now);
                break;
            case BayTripStage.FetchDriving:
                FetchDrivingLook();
                break;
            case BayTripStage.FetchCoupling:
                FetchCouplingLook(now);
                break;
            case BayTripStage.FetchTowing:
                FetchTowingLook();
                break;
            case BayTripStage.FetchStanding:
                FetchStandingLook(now);
                break;
            case BayTripStage.AtBay:
                if (!BayTripRule.LookDue(now, nextLookTick)) return;
                nextLookTick = BayTripRule.NextLook(now, Rules.bayTripLookTicks, Vehicle.thingIDNumber, GenTicks.TickRareInterval);
                AtBayLook(now);
                break;
        }
    }

    /// <summary>An idle vehicle's look: whether to set off, and if it cannot, why.</summary>
    void IdleLook()
    {
        blocked = null;
        if (!BayTrips.On || keptOut || Vehicle.Faction != Faction.OfPlayer || !Vehicle.Map.IsPlayerHome || !TakesPart) return;
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems) return;
        BayTripNeed need = BayTrips.NeedOf(systems);
        if (!need.Any) return;
        // Standing at a bay or in a working service yard already, the mechanics have it; and a vehicle the player parked there is never moved.
        if (BayTrips.AtBayOrYard(systems)) return;
        if (BayTrips.Busy(Vehicle, this) || GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return;
        if (!need.Payable)
        {
            blocked = need.Missing;
            return;
        }
        // A vehicle that will not drive is carried as it stands, so its place is one it fits facing as it faces now.
        string? cannot = VehicleAutoDrive.CannotDrive(Vehicle, needsDriver: false);
        if (!BayTrips.TryFindPlace(Vehicle, badCells, cannot != null ? Vehicle.Rotation : null, out Thing? found, out IntVec3 cell, out Rot4 facing, out string? why))
        {
            blocked = why;
            return;
        }
        if (cannot != null)
        {
            BeginCarried(found!, cell, cannot);
            return;
        }
        bay = found;
        standCell = cell;
        standFacing = facing;
        failedDrives = 0;
        leftUnfinished = false;
        brought.Clear();
        SetStage(BayTripStage.CallingDriver);
        DriverCalls.Call(Vehicle.Map, this, true);
        Seen.Note("vehicles.bay-trip.called", Vehicle);
    }

    /// <summary>
    /// One look while a driver is called. Crew the player boarded, or an engine the player started, ends the trip: the player has plans for
    /// the vehicle. Once the crew we called is aboard the drive starts, a look after the last of them boarded, since Vehicle Framework's
    /// own answer to "can it move" is stale just after a boarding (the lesson from Vehicles at Work).
    /// </summary>
    void CallingLook(int now)
    {
        bool home = stage == BayTripStage.CallingDriverHome;
        brought.RemoveAll(p => p == null || p.Destroyed);
        if (Vehicle.AllPawnsAboard.Any(p => !brought.Contains(p)) || (Vehicle.Drafted && brought.Count == 0))
        {
            End(crewOut: true, backOff: false);
            return;
        }
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            // Nothing sets off with enemies about. A trip to the bay ends, to be looked at again; one home waits where it is.
            if (!home) End(crewOut: true, backOff: false);
            return;
        }
        // A vehicle the player sent into a yard goes whether or not it has work (Framework 0.210.0).
        if (!home && !toYardByOrder && (bay is not { Spawned: true } || Vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems || !BayTrips.NeedOf(systems).Any))
        {
            // The bay has gone, or someone repaired it where it stands.
            End(crewOut: true, backOff: false);
            return;
        }
        // A vehicle that needs nobody to drive it sets off at once.
        if (Vehicle.HasEnoughOperators && (brought.Count > 0 || Vehicle.AllPawnsAboard.Count == 0))
        {
            StartDrive(home);
            return;
        }
        blocked = BayTripRule.Overstayed(now - stageTick, Rules.bayTripDriverWaitTicks)
            ? "PhobosBayTrip_NoDriver".Translate(DriverCalls.WorkLabel.Named("WORK")).Resolve()
            : null;
    }

    /// <summary>A colonist we called has taken a seat (<see cref="DriverCalls.Arrived"/>): they are this trip's, and get out at its end.</summary>
    public void DriverArrived(Pawn driver)
    {
        if (WaitingForDriver && !brought.Contains(driver)) brought.Add(driver);
    }

    void StartDrive(bool home)
    {
        DriverCalls.Call(Vehicle.Map, this, false);
        blocked = null;
        if (VehicleAutoDrive.CannotDrive(Vehicle) != null || !TakeOver() || !BeginDrive(standCell, standFacing.IsValid ? standFacing : null))
        {
            GiveUp("PhobosBayTrip_WouldNotStart");
            return;
        }
        SetStage(home ? BayTripStage.DrivingHome : BayTripStage.Driving);
        Seen.Note("vehicles.bay-trip.driving", Vehicle, variant: home ? "home" : "to the bay");
    }

    /// <summary>One look on a drive: on its way it waits; there, it stops and its crew gets out; stalled or stopped short, it tries another place.</summary>
    void DrivingLook()
    {
        bool home = stage == BayTripStage.DrivingHome;
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            // Enemies: it stops where it is and its crew gets out. On the way to the bay the trip ends, to be looked at again; on the way
            // home it keeps its place and calls a driver again once they have gone, or it would be left where it stopped for good.
            Seen.Note("vehicles.bay-trip.threat", Vehicle);
            if (!home)
            {
                End(crewOut: true, backOff: false);
                return;
            }
            HaltDrive();
            HandBack(switchOff: true);
            CrewOut();
            SetStage(BayTripStage.CallingDriverHome);
            DriverCalls.Call(Vehicle.Map, this, true);
            return;
        }
        switch (WatchDrive())
        {
            case DriveWatch.OnWay:
                return;
            case DriveWatch.OtherOrders:
                // The player ordered it elsewhere, with our driver aboard: it is theirs now.
                End(crewOut: false, backOff: true);
                return;
            case DriveWatch.EngineOff:
                // Switched off, or it broke down on the way: nobody is left sitting in it.
                End(crewOut: true, backOff: true);
                return;
            case DriveWatch.Stuck:
                HaltDrive();
                FailedDrive(home);
                return;
            case DriveWatch.StoppedShort:
                FailedDrive(home);
                return;
        }
        if (home)
        {
            ArriveHome();
            return;
        }
        if (toYardByOrder && ServiceYards.InWorkingYard(Vehicle))
        {
            // Sent in by the player: it is parked there as if the player had parked it, and the yard's lines and pumps take it from here.
            if (standFacing.IsValid) VehicleAutoDrive.Face(Vehicle, standFacing);
            Seen.Note("vehicles.service-yard.sent", Vehicle);
            End(crewOut: true, backOff: false);
            return;
        }
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && BayTrips.AtBayOrYard(systems)) ArriveAtBay();
        else FailedDrive(home);
    }

    /// <summary>A drive that reached nothing: another place is tried, and after enough of them the trip is given up for a day.</summary>
    void FailedDrive(bool home)
    {
        if (standCell.IsValid) badCells.Add(standCell);
        failedDrives++;
        if (!home && !toYardByOrder && !VehicleRunRule.GiveUp(failedDrives, Rules.bayTripFailedDrives)
            && BayTrips.TryFindPlace(Vehicle, badCells, null, out Thing? found, out IntVec3 cell, out Rot4 facing, out _)
            && BeginDrive(cell, facing))
        {
            bay = found;
            standFacing = facing;
            return;
        }
        GiveUp(home ? "PhobosBayTrip_NoWayHome" : toYardByOrder ? "PhobosServiceYard_NoWayToYard" : "PhobosBayTrip_NoWay");
    }

    /// <summary>Ends a trip that could not go on: the crew gets out where it stands, and it says why until its next look, a day on.</summary>
    void GiveUp(string key)
    {
        Seen.Note("vehicles.bay-trip.gave-up", Vehicle, variant: key);
        End(crewOut: true, backOff: true);
        blocked = key.Translate().Resolve();
    }

    void ArriveAtBay()
    {
        HandBack(switchOff: true);
        CrewOut();
        badCells.Clear();
        failedDrives = 0;
        // The place it stands on is its own now: KeptCells is for places not yet reached, and a look at the bay reads this cell.
        standCell = Vehicle.Position;
        lastWorkTick = Find.TickManager.TicksGame;
        nextLookTick = lastWorkTick + Rules.bayTripLookTicks;
        SetStage(BayTripStage.AtBay);
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems) VehicleStory.Log(systems, BayTripDefOf.PhobosLog_TakenToBay);
        Seen.Note("vehicles.bay-trip.at-bay", Vehicle);
    }

    /// <summary>
    /// One look at the bay. It stays while work that sent it remains or any work is ready; once neither is so it goes home; and after a
    /// day in which nothing was ready to be done it gives up its place. A vehicle moved from where we left it is the player's again.
    /// </summary>
    void AtBayLook(int now)
    {
        if (Vehicle.Position != standCell || Vehicle.Drafted || Vehicle.AllPawnsAboard.Count > 0
            || Vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems)
        {
            End(crewOut: false, backOff: false);
            return;
        }
        bool ready = systems.ReadyService() != null || systems.Repairs.Ready != null || BayTrips.FreeRepairWaits(systems);
        if (ready) lastWorkTick = now;
        bool unfinished = BayTrips.NeedOf(systems).Any || ready;
        if (unfinished && !BayTripRule.Overstayed(now - lastWorkTick, Rules.bayTripIdleTicks))
        {
            blocked = null;
            return;
        }
        if (GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return;
        Leave(unfinished);
    }

    /// <summary>Calls a driver to take it home: its depot bay, else the vehicle park, else a spot nearby clear of every bay.</summary>
    void Leave(bool unfinished)
    {
        if (VehicleAutoDrive.CannotDrive(Vehicle, needsDriver: false) is { } cannot)
        {
            // Done, or given up, but it will not drive: it stays, and says so.
            blocked = "PhobosBayTrip_CannotLeave".Translate(cannot.Named("REASON")).Resolve();
            return;
        }
        if (!BayTrips.TryFindHome(Vehicle, out IntVec3 cell, out Rot4 facing, out BayTripHome kind))
        {
            // Nowhere better to stand: it stays where it is, and the trip is over.
            End(crewOut: false, backOff: unfinished);
            return;
        }
        standCell = cell;
        standFacing = facing;
        failedDrives = 0;
        leftUnfinished = unfinished;
        blocked = null;
        brought.Clear();
        SetStage(BayTripStage.CallingDriverHome);
        DriverCalls.Call(Vehicle.Map, this, true);
        if (Seen.Wants("vehicles.bay-trip.leaving", Vehicle)) Seen.Note("vehicles.bay-trip.leaving", Vehicle, variant: kind.ToString());
    }

    /// <summary>
    /// Takes this vehicle out of a full service yard to make room for one that needs it (Framework 0.210.0; [A]): only an idle vehicle on no
    /// trip, free and able to drive, to its depot bay, the vehicle park or a spot nearby, as a trip home goes. False when it cannot go.
    /// </summary>
    internal bool LeaveYardForRoom() => TakeOutOfYard(byPlayer: false) == null;

    /// <summary>
    /// Drives this vehicle out of its yard, as a trip home goes; the reason it cannot, in the player's words, or null once it is on its way.
    /// The player's own order (the Fleet board's Take out of yard) may move a vehicle kept out of bay trips; making room never does.
    /// </summary>
    internal string? TakeOutOfYard(bool byPlayer)
    {
        if (stage != BayTripStage.None || (keptOut && !byPlayer) || !TakesPart || BayTrips.Busy(Vehicle, this))
        {
            return "PhobosServiceYard_CannotLeaveBusy".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        }
        if (VehicleAutoDrive.CannotDrive(Vehicle, needsDriver: false) is { } cannot) return cannot;
        if (!BayTrips.TryFindHome(Vehicle, out IntVec3 cell, out Rot4 facing, out BayTripHome kind, leavingYard: true))
        {
            return "PhobosServiceYard_CannotLeaveNowhere".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        }
        standCell = cell;
        standFacing = facing;
        failedDrives = 0;
        leftUnfinished = false;
        blocked = null;
        brought.Clear();
        byOrder = byPlayer;
        SetStage(BayTripStage.CallingDriverHome);
        DriverCalls.Call(Vehicle.Map, this, true);
        if (byPlayer && (!BayTrips.On || keptOut)) Seen.Note("vehicles.service-yard.order-goes", Vehicle, variant: "taken out");
        if (byPlayer) { if (Seen.Wants("vehicles.service-yard.taken-out", Vehicle, kind.ToString())) Seen.Note("vehicles.service-yard.taken-out", Vehicle, variant: kind.ToString()); }
        else if (Seen.Wants("vehicles.service-yard.made-room", Vehicle, kind.ToString())) Seen.Note("vehicles.service-yard.made-room", Vehicle, variant: kind.ToString());
        return null;
    }

    /// <summary>
    /// The player's order from the Fleet board (Framework 0.210.0): drive into this service yard and park there. Null once it is on its way,
    /// else the reason in the player's words. A vehicle that will not drive is not carried in by this order: bay trips do that for work.
    /// </summary>
    internal string? SendToYard(Zone_PhobosServiceYard yard)
    {
        if (stage != BayTripStage.None || !TakesPart || BayTrips.Busy(Vehicle, this))
        {
            return "PhobosServiceYard_CannotSendBusy".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        }
        if (VehicleAutoDrive.CannotDrive(Vehicle, needsDriver: false) is { } cannot) return cannot;
        if (!yard.Works) return "PhobosServiceYard_CannotSendNotWorking".Translate(yard.label.Named("YARD")).Resolve();
        Map map = Vehicle.Map;
        List<CellRect> kept = BayTrips.KeptBy(map, Vehicle);
        int wayTests = 0;
        if (!VehicleParks.TryFindPlaceIn(Vehicle, new Zone[] { yard }, cells => BayTrips.ClearOfVehicles(Vehicle, cells, map, kept),
                body => BayTrips.BodyFits(Vehicle, body, map, kept, null),
                c => Vehicle.CanReachVehicle(c, Verse.AI.PathEndMode.OnCell, Danger.Deadly, TraverseMode.ByPawn), 16, ref wayTests, null,
                out IntVec3 cell, out Rot4 facing))
        {
            return "PhobosServiceYard_CannotSendNoRoom".Translate(yard.label.Named("YARD")).Resolve();
        }
        bay = yard.State?.Dynos.FirstOrDefault()?.parent;
        standCell = cell;
        standFacing = facing;
        failedDrives = 0;
        leftUnfinished = false;
        toYardByOrder = true;
        byOrder = true;
        if (!BayTrips.On || keptOut) Seen.Note("vehicles.service-yard.order-goes", Vehicle, variant: "sent");
        blocked = null;
        brought.Clear();
        SetStage(BayTripStage.CallingDriver);
        DriverCalls.Call(map, this, true);
        return null;
    }

    void ArriveHome()
    {
        if (standFacing.IsValid) VehicleAutoDrive.Face(Vehicle, standFacing);
        Seen.Note("vehicles.bay-trip.home", Vehicle);
        End(crewOut: true, backOff: leftUnfinished);
    }

    /// <summary>The colonists this trip called aboard get out, if they are still aboard.</summary>
    void CrewOut()
    {
        foreach (Pawn pawn in brought)
        {
            if (pawn is { Destroyed: false } && Vehicle.Spawned && Vehicle.AllPawnsAboard.Contains(pawn)) Vehicle.DisembarkPawn(pawn);
        }
        brought.Clear();
    }

    /// <summary>
    /// Ends the trip where it stands. With <paramref name="crewOut"/> the engine we started goes off and the crew we called gets out;
    /// without, the vehicle is left as it is, for the player who took it. <paramref name="backOff"/> makes it wait a day before it looks again.
    /// </summary>
    void End(bool crewOut, bool backOff)
    {
        if (Running && standCell.IsValid && Vehicle.Spawned) HaltDrive();
        // A recovery vehicle lets its wreck go and tells it; what a carried vehicle kept is forgotten.
        ReleaseCarry();
        DriverCalls.Call(Vehicle.MapHeld, this, false);
        if (crewOut)
        {
            HandBack(switchOff: true);
            CrewOut();
        }
        else
        {
            engineStarted = false;
            brought.Clear();
        }
        stage = BayTripStage.None;
        bay = null;
        standCell = IntVec3.Invalid;
        standFacing = Rot4.Invalid;
        failedDrives = 0;
        leftUnfinished = false;
        toYardByOrder = false;
        byOrder = false;
        blocked = null;
        badCells.Clear();
        int now = Find.TickManager.TicksGame;
        nextLookTick = backOff ? now + Rules.bayTripBackOffTicks
            : BayTripRule.NextLook(now, Rules.bayTripLookTicks, Vehicle.thingIDNumber, GenTicks.TickRareInterval);
    }

    // ---- The map, the save and the screen ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        MapRegistry<CompPhobosBayTrip>.Add(Vehicle.Map, this);
        // The call for a driver is not saved with the map: one waiting when the game was saved calls again.
        if (WaitingForDriver) DriverCalls.Call(Vehicle.Map, this, true);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map (a caravan, a tow away) ends a trip where it is: whoever is aboard goes with the vehicle.
        MapRegistry<CompPhobosBayTrip>.Remove(map, this);
        DriverCalls.Call(map, this, false);
        stage = BayTripStage.None;
        bay = null;
        standCell = IntVec3.Invalid;
        standFacing = Rot4.Invalid;
        engineStarted = false;
        leftUnfinished = false;
        toYardByOrder = false;
        byOrder = false;
        blocked = null;
        brought.Clear();
        badCells.Clear();
        // The other vehicle of a tow notices at its next look, and lets go or tries again.
        ClearCarry();
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        bool saving = Scribe.mode == LoadSaveMode.Saving;
        Scribe_Values.Look(ref keptOut, "PhobosBayTripKeptOut");
        Scribe_Values.Look(ref stage, "PhobosBayTripStage", BayTripStage.None);
        Scribe_Values.Look(ref nextLookTick, "PhobosBayTripNextLook");
        // A vehicle on no trip writes nothing more.
        if (saving && stage == BayTripStage.None) return;
        if (saving)
        {
            if (bay is { Destroyed: true }) bay = null;
            brought.RemoveAll(p => p == null || p.Destroyed);
        }
        Scribe_References.Look(ref bay, "PhobosBayTripBay");
        Scribe_Values.Look(ref standFacing, "PhobosBayTripFacing", Rot4.Invalid);
        Scribe_Values.Look(ref stageTick, "PhobosBayTripStageTick");
        Scribe_Values.Look(ref lastWorkTick, "PhobosBayTripLastWork");
        Scribe_Values.Look(ref leftUnfinished, "PhobosBayTripLeftUnfinished");
        Scribe_Values.Look(ref toYardByOrder, "PhobosBayTripToYardByOrder");
        // Framework 0.276.4: an older save has none, so a trip in flight there stays automatic.
        Scribe_Values.Look(ref byOrder, "PhobosBayTripByOrder");
        Scribe_Collections.Look(ref brought, "PhobosBayTripBrought", LookMode.Reference);
        ExposeRun(RunSavePrefix);
        ExposeCarry();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            brought ??= new List<Pawn>();
            brought.RemoveAll(p => p == null);
        }
    }

    public override string? CompInspectStringExtra()
    {
        if (Vehicle.Faction != Faction.OfPlayer) return null;
        return DoingLine ?? blocked ?? (keptOut && BayTrips.On ? "PhobosBayTrip_LineKeptOut".Translate().Resolve() : null);
    }
}

[DefOf]
public static class BayTripDefOf
{
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_TakenToBay = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_TowedToBay = null!;
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_PushedToBay = null!;
    public static JobDef PhobosVehicleSystems_PushVehicle = null!;

    static BayTripDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BayTripDefOf));
}
