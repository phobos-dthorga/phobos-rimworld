using System;

namespace Phobos.Factory.Rules;

/// <summary>
/// When a finished factory batch may join a stack already in the output cell. Pipe System adds the batch to
/// a matching stack whenever that stack holds no more than the bill's output limit (maxOutputCount), and
/// whatever doesn't fit is never placed. The game counts two identical items as stackable even when their
/// stack limit is 1, and most of VFE Factory's limits equal the stack limit, so a full stack swallows every
/// later batch. Read from Pipe System's Process.TrySpawnAtCell and the game's Thing.CanStackWith,
/// 29 Oct 2026. No game types: this file is compiled into the unit tests too.
/// </summary>
public static class OutputRule
{
    /// <summary>Whether a whole batch fits on a stack already in the output cell.</summary>
    public static bool BatchFits(int stackCount, int batch, int stackLimit) => stackCount + batch <= stackLimit;

    /// <summary>
    /// The output limit for a generated bill: the wanted one, but never above the item's stack limit, so
    /// Pipe System's "too full (max N units)" names a number the item can reach. At least 1, because 0 tells
    /// Pipe System to drop a blocked batch around the machine instead of waiting.
    /// </summary>
    public static float WaitLimit(float wanted, int stackLimit) => Math.Max(1f, Math.Min(wanted, stackLimit));
}
