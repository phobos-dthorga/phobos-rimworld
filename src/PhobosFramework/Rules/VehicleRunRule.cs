namespace Phobos.Framework.Rules;

/// <summary>What one look at a vehicle on a drive of ours finds (Framework 0.182.0).</summary>
public enum DriveWatch
{
    /// <summary>On its way, and it has moved lately.</summary>
    OnWay,
    /// <summary>On its way by its orders, but standing still longer than its patience.</summary>
    Stuck,
    /// <summary>Stopped on the cell it was sent to.</summary>
    Arrived,
    /// <summary>Stopped somewhere else: the drive ended short.</summary>
    StoppedShort,
    /// <summary>The engine was switched off.</summary>
    EngineOff,
    /// <summary>It is driving somewhere else: the player, or someone, gave it another order.</summary>
    OtherOrders,
}

/// <summary>
/// The rules any errand shares when a vehicle drives itself (Framework 0.182.0; docs/development/farming-batch-one.md, "Shared with the
/// gathering run"): what one look at a drive finds, when a drive has stalled, and when to give up. Lifted from the gathering run's three
/// drives (to a thing, back to its place, home) so the harvest run reads a drive the same way. Game-free.
/// </summary>
public static class VehicleRunRule
{
    /// <summary>
    /// What one look at a drive finds, in the order the gathering run has always asked: the engine off ends everything; another order comes
    /// next; a vehicle under way is waited for unless it has stood still past its patience; one that has stopped has arrived or stopped short.
    /// </summary>
    public static DriveWatch Watch(bool engineOn, bool otherOrders, bool underWay, int ticksSinceMoved, int patienceTicks, bool atCell)
    {
        if (!engineOn) return DriveWatch.EngineOff;
        if (otherOrders) return DriveWatch.OtherOrders;
        if (underWay) return Stuck(ticksSinceMoved, patienceTicks) ? DriveWatch.Stuck : DriveWatch.OnWay;
        return atCell ? DriveWatch.Arrived : DriveWatch.StoppedShort;
    }

    /// <summary>Whether a drive has stalled: it has not moved for longer than its patience. No patience means never.</summary>
    public static bool Stuck(int ticksSinceMoved, int patienceTicks) => patienceTicks > 0 && ticksSinceMoved > patienceTicks;

    /// <summary>Whether to give up after this many drives in a row that reached nothing. No limit means never.</summary>
    public static bool GiveUp(int failedInARow, int limit) => limit > 0 && failedInARow >= limit;
}
