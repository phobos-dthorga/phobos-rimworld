using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// Which events a log of marks on a graph keeps, game-free (Framework 0.84.0, the graphs' event marks): the same event again within a short
/// gap is one event (a reading flickering across a stage's edge marks once), and the log holds only so many days and so many marks, oldest
/// going first.
/// </summary>
public static class EventMarkRule
{
    /// <summary>Whether an event is too soon after the last one of the same kind to mark again; never, when there was none.</summary>
    public static bool TooSoon(int? lastSameTick, int now, int gapTicks) => lastSameTick is int last && now - last < gapTicks;

    /// <summary>
    /// How many of the oldest marks to drop, given their ticks oldest first: every one older than the age limit, then the oldest beyond the
    /// count limit.
    /// </summary>
    public static int DropCount(IReadOnlyList<int> ticksOldestFirst, int now, int maxAgeTicks, int maxCount)
    {
        int drop = 0;
        while (drop < ticksOldestFirst.Count && now - ticksOldestFirst[drop] > maxAgeTicks) drop++;
        int left = ticksOldestFirst.Count - drop;
        if (left > maxCount) drop += left - maxCount;
        return drop;
    }
}
