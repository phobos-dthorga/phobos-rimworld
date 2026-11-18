namespace Phobos.Framework.Rules;

/// <summary>What a gathering run is doing (Framework 0.143.0; docs/development/vehicle-gathering-runs.md). Saved by name: append only.</summary>
public enum GatherPhase
{
    /// <summary>No run.</summary>
    Idle,
    /// <summary>Choosing what to fetch next.</summary>
    Seeking,
    /// <summary>On its way to a place beside what it chose.</summary>
    Driving,
    /// <summary>Picking things up where it stands.</summary>
    Working,
}

/// <summary>Why a vehicle's gathering worker (a truck's grab) stopped, or <see cref="None"/> while it works or has just started.</summary>
public enum GatherWorkEnd
{
    None,
    /// <summary>Nothing it was asked for is left within its reach.</summary>
    NothingInReach,
    /// <summary>The cargo has no room left.</summary>
    Full,
    /// <summary>Something failed: fuel, the engine, the controls, the hydraulics. The worker says what.</summary>
    Fault,
    /// <summary>The player stopped the worker by hand.</summary>
    Stopped,
}

/// <summary>Why a gathering run ended, for its report. Saved by name: append only.</summary>
public enum GatherRunEnd
{
    None,
    Full,
    NothingLeft,
    NoWay,
    Orders,
    EngineOff,
    Threat,
    Fault,
    Stopped,
}

/// <summary>
/// The rules of a gathering run (Framework 0.143.0; docs/development/vehicle-gathering-runs.md): where a vehicle stands to reach a thing, what a
/// worker's stop means for the run, and when a drive has failed. Game-free, so any vehicle and any worker share them.
/// </summary>
public static class GatherRule
{
    /// <summary>
    /// The square ring, in cells from a thing, on which a vehicle's centre stands to reach it: one beyond the farthest its body runs from its
    /// centre in any facing (half its longer side, as the game lays out a footprint of that size), so no facing of the body covers the thing.
    /// </summary>
    public static int StandOffRing(int sizeX, int sizeZ) => System.Math.Max(System.Math.Max(sizeX, sizeZ), 1) / 2 + 1;

    /// <summary>
    /// Whether a thing on that ring is within reach of the body's edge whichever way the vehicle faces: the ring is no farther than the body's
    /// narrowest side runs from its centre, plus the reach.
    /// </summary>
    public static bool RingAlwaysInReach(int sizeX, int sizeZ, int reachCells) =>
        StandOffRing(sizeX, sizeZ) <= (System.Math.Max(System.Math.Min(sizeX, sizeZ), 1) - 1) / 2 + reachCells;

    /// <summary>What a worker's stop means for the run: null to look for the next thing to fetch, or how the run ends.</summary>
    public static GatherRunEnd? AfterWork(GatherWorkEnd end) => end switch
    {
        GatherWorkEnd.Full => GatherRunEnd.Full,
        GatherWorkEnd.Fault => GatherRunEnd.Fault,
        GatherWorkEnd.Stopped => GatherRunEnd.Stopped,
        _ => null,
    };

    /// <summary>Whether a drive has stalled: it has not moved for longer than its patience. No patience means never.</summary>
    public static bool Stuck(int ticksSinceMoved, int patienceTicks) => patienceTicks > 0 && ticksSinceMoved > patienceTicks;

    /// <summary>Whether to give the run up after this many drives in a row that reached nothing. No limit means never.</summary>
    public static bool GiveUp(int failedInARow, int limit) => limit > 0 && failedInARow >= limit;

    /// <summary>Whether the run ended because its work was done (full, or nothing left), rather than cut short.</summary>
    public static bool EndedWell(GatherRunEnd end) => end is GatherRunEnd.Full or GatherRunEnd.NothingLeft;

    /// <summary>
    /// Whether a run that started the engine switches it off at its end. Not after other orders (the player may be driving it away), when the
    /// engine is already off, or when enemies come (the driver may want to move it at once).
    /// </summary>
    public static bool SwitchesOffAtEnd(GatherRunEnd end) => end is not (GatherRunEnd.Orders or GatherRunEnd.EngineOff or GatherRunEnd.Threat);
}
