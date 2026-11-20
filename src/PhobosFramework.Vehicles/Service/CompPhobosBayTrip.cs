using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

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
public sealed class CompPhobosBayTrip : CompPhobosVehicleRun, IVehicleDriverArrives
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

    /// <summary>Why it cannot go, or why it waits, in the player's words: for its own line, the Fleet board and the alert. Derived, never saved.</summary>
    string? blocked;
    /// <summary>Places a drive of this trip failed to reach. Derived, never saved.</summary>
    readonly HashSet<IntVec3> badCells = new();

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public BayTripStage Stage => stage;
    public bool KeptOut => keptOut;
    public string? Blocked => blocked;
    public Thing? Bay => bay;

    public override bool Running => stage is BayTripStage.Driving or BayTripStage.DrivingHome;

    protected override bool WaitingForDriver => stage is BayTripStage.CallingDriver or BayTripStage.CallingDriverHome;

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
        _ => null,
    };

    /// <summary>The player's switch. Only <see cref="BayTrips.SetKeptOut"/> calls this.</summary>
    internal void SetKeptOut(bool value)
    {
        keptOut = value;
        blocked = null;
        if (value && stage != BayTripStage.None) End(crewOut: true, backOff: false);
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
        if (Vehicle.Faction != Faction.OfPlayer || !BayTrips.On || keptOut)
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
        // Standing at a bay already, the mechanics have it; and a vehicle the player parked there is never moved.
        if (systems.Fittings.NearRig()) return;
        if (BayTrips.Busy(Vehicle, this) || GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return;
        if (!need.Payable)
        {
            blocked = need.Missing;
            return;
        }
        if (!BayTrips.TryFindPlace(Vehicle, badCells, out Thing? found, out IntVec3 cell, out Rot4 facing, out string? why))
        {
            blocked = why;
            return;
        }
        if (VehicleAutoDrive.CannotDrive(Vehicle, needsDriver: false) is { } cannot)
        {
            blocked = "PhobosBayTrip_CannotDrive".Translate(cannot.Named("REASON")).Resolve();
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
            End(crewOut: true, backOff: false);
            return;
        }
        if (!home && (bay is not { Spawned: true } || Vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } systems || !BayTrips.NeedOf(systems).Any))
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
            // Enemies: it stops where it is and its crew gets out; it looks again at its next look.
            Seen.Note("vehicles.bay-trip.threat", Vehicle);
            End(crewOut: true, backOff: false);
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
        if (Vehicle.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && systems.Fittings.NearRig()) ArriveAtBay();
        else FailedDrive(home);
    }

    /// <summary>A drive that reached nothing: another place is tried, and after enough of them the trip is given up for a day.</summary>
    void FailedDrive(bool home)
    {
        if (standCell.IsValid) badCells.Add(standCell);
        failedDrives++;
        if (!home && !VehicleRunRule.GiveUp(failedDrives, Rules.bayTripFailedDrives)
            && BayTrips.TryFindPlace(Vehicle, badCells, out Thing? found, out IntVec3 cell, out Rot4 facing, out _)
            && BeginDrive(cell, facing))
        {
            bay = found;
            standFacing = facing;
            return;
        }
        GiveUp(home ? "PhobosBayTrip_NoWayHome" : "PhobosBayTrip_NoWay");
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
        blocked = null;
        brought.Clear();
        badCells.Clear();
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
        Scribe_Collections.Look(ref brought, "PhobosBayTripBrought", LookMode.Reference);
        ExposeRun(RunSavePrefix);
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

    static BayTripDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BayTripDefOf));
}
