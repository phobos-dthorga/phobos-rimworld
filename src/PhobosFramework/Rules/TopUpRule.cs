namespace Phobos.Framework.Rules;

/// <summary>
/// The rule behind "colonists top up machines that hold materials" (Framework 0.134.0; docs/development/top-up-design.md), game-free:
/// whether a store that wants more is worth a hauler's walk, and how much to bring. A store wants its target less what it holds; it is
/// worth a walk once it wants at least its least load (one drone's worth of steel at a drone platform), so a hauler never crosses the
/// map for a single piece. An order the player gives by hand is always carried out.
/// </summary>
public static class TopUpRule
{
    /// <summary>Whether to fetch for a store that wants <paramref name="wanted"/> of its material.</summary>
    /// <param name="wanted">How much more it wants (its target less what it holds).</param>
    /// <param name="leastLoad">The least worth a walk; anything below 1 counts as 1.</param>
    /// <param name="forced">The player ordered it by hand.</param>
    public static bool Worth(int wanted, int leastLoad, bool forced)
    {
        if (wanted <= 0) return false;
        return forced || wanted >= System.Math.Max(1, leastLoad);
    }

    /// <summary>How much to bring: what it wants, less what others are already bringing; never below 0.</summary>
    public static int ToBring(int wanted, int alreadyComing) => System.Math.Max(0, wanted - System.Math.Max(0, alreadyComing));
}
