using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The base of any errand a vehicle runs by driving itself about a map (Framework 0.182.0; docs/development/farming-batch-one.md, "Shared
/// with the gathering run"): taking the engine over and handing it back, ordering a drive and watching it (on its way, stuck, arrived,
/// stopped short, the engine off, other orders), counting drives that reached nothing, calling for a driver when nobody is aboard, and
/// saving what a drive needs to resume. Lifted from the gathering run, whose three drives each watched the same way, so a harvest run and
/// whatever comes later share one copy. The errand itself (what to fetch, where to unload, what to say) stays in the comp that runs it.
/// VehicleAutoDrive does the driving; VehicleRunRule decides what a look at a drive means.
/// </summary>
public abstract class CompPhobosVehicleRun : VehicleComp, IVehicleWantsDriver
{
    /// <summary>The cell the drive in hand makes for, or where the vehicle stands for its work; invalid for none.</summary>
    protected IntVec3 standCell = IntVec3.Invalid;
    /// <summary>How many drives in a row reached nothing.</summary>
    protected int failedDrives;
    /// <summary>Whether this errand started the engine, so it may switch it off again at its end.</summary>
    protected bool engineStarted;
    int lastMovedTick;
    IntVec3 lastPosition = IntVec3.Invalid;

    /// <summary>Whether an errand is under way.</summary>
    public abstract bool Running { get; }

    /// <summary>Whether an errand was ordered with nobody aboard to drive, and waits for a driver.</summary>
    protected abstract bool WaitingForDriver { get; }

    /// <summary>How long a drive may stand still before it counts as stuck, in ticks; none for never.</summary>
    protected abstract int StuckTicks { get; }

    /// <summary>Whether it waits for a driver now: an errand ordered, none running, and not enough crew aboard to drive.</summary>
    public bool WantsDriver => WaitingForDriver && !Running && Vehicle.Spawned && Vehicle.Faction == Faction.OfPlayer && !Vehicle.HasEnoughOperators;

    // ---- The engine ----

    /// <summary>
    /// Starts the engine if it is off, remembering whether this errand did, and calls off whatever drive the vehicle had: the player has
    /// just asked for the errand. False if the engine would not start; Vehicle Framework says why.
    /// </summary>
    protected bool TakeOver()
    {
        if (!VehicleAutoDrive.StartEngine(Vehicle, out bool started)) return false;
        engineStarted = started;
        VehicleAutoDrive.ClearOrders(Vehicle);
        return true;
    }

    /// <summary>Hands the vehicle back at an errand's end, switching off an engine this errand started where the end allows it.</summary>
    protected void HandBack(bool switchOff)
    {
        if (engineStarted && Vehicle.Spawned && switchOff) VehicleAutoDrive.StopEngine(Vehicle);
        engineStarted = false;
    }

    // ---- A drive ----

    /// <summary>Orders a drive to a cell, ending facing this way if given, and starts watching it. False if the order was refused.</summary>
    protected bool BeginDrive(IntVec3 cell, Rot4? endFacing = null)
    {
        if (!VehicleAutoDrive.DriveTo(Vehicle, cell, endFacing)) return false;
        standCell = cell;
        lastMovedTick = Find.TickManager.TicksGame;
        lastPosition = Vehicle.Position;
        return true;
    }

    /// <summary>
    /// One look at the drive in hand (VehicleRunRule.Watch). Notes when the vehicle last moved, for the stuck test. Call it once a look,
    /// from a rare tick.
    /// </summary>
    protected DriveWatch WatchDrive()
    {
        bool engineOn = Vehicle.Drafted;
        bool otherOrders = engineOn && VehicleAutoDrive.HeadingElsewhere(Vehicle, standCell);
        int now = Find.TickManager.TicksGame;
        if (engineOn && !otherOrders && Vehicle.Position != lastPosition)
        {
            lastPosition = Vehicle.Position;
            lastMovedTick = now;
        }
        bool underWay = engineOn && !otherOrders && (VehicleAutoDrive.HeadingFor(Vehicle, standCell) || VehicleAutoDrive.Moving(Vehicle));
        return VehicleRunRule.Watch(engineOn, otherOrders, underWay, now - lastMovedTick, StuckTicks, Vehicle.Position == standCell);
    }

    /// <summary>Stops the drive in hand where it is; another order is left alone.</summary>
    protected void HaltDrive() => VehicleAutoDrive.Halt(Vehicle, standCell);

    // ---- Calling for a driver (Framework 0.177.0) ----

    /// <summary>
    /// Calls for a driver and says so: a colonist with Drive work comes, and the comp starts its errand once the crew is aboard.
    /// <paramref name="what"/> names the errand in the player's language; <paramref name="seenAs"/> is the case its note carries.
    /// </summary>
    protected void CallForDriver(string what, string seenAs)
    {
        DriverCalls.Call(Vehicle.Map, this, true);
        Messages.Message("PhobosDriverCall_Waiting".Translate(Vehicle.LabelShort.Named("VEHICLE"), DriverCalls.WorkLabel.Named("WORK"),
            what.Named("WHAT")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("vehicles.driver-call.waiting", Vehicle, variant: seenAs);
    }

    /// <summary>Stops calling for a driver, saying so when the player asked.</summary>
    protected void CallOffDriver(bool say)
    {
        DriverCalls.Call(Vehicle.MapHeld, this, false);
        if (say)
        {
            Messages.Message("PhobosDriverCall_Cancelled".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle,
                MessageTypeDefOf.SilentInput, historical: false);
        }
    }

    // ---- The save ----

    /// <summary>
    /// Saves what a drive needs to resume, under the errand's own key prefix, so each errand keeps the keys its saves already hold (the
    /// gathering run's "PhobosGather").
    /// </summary>
    protected void ExposeRun(string prefix)
    {
        Scribe_Values.Look(ref standCell, prefix + "StandCell", IntVec3.Invalid);
        Scribe_Values.Look(ref lastMovedTick, prefix + "LastMoved");
        Scribe_Values.Look(ref lastPosition, prefix + "LastPosition", IntVec3.Invalid);
        Scribe_Values.Look(ref failedDrives, prefix + "FailedDrives");
        Scribe_Values.Look(ref engineStarted, prefix + "EngineStarted");
    }
}
