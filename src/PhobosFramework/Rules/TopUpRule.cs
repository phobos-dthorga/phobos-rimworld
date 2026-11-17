namespace Phobos.Framework.Rules;

/// <summary>How full topping up fills a store (Framework 0.136.0; owner, 18 Nov 2026: "Provide a configurable setting?"). Each level
/// only ever raises the machine's own fill level, never lowers it.</summary>
public enum TopUpLevel
{
    /// <summary>To the level the machine's own fill order fills it to.</summary>
    OwnLevel,
    /// <summary>At least half its capacity.</summary>
    Half,
    /// <summary>At least three-quarters of its capacity.</summary>
    ThreeQuarters,
    /// <summary>To its capacity.</summary>
    Full,
}

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

    /// <summary>The share of capacity a level fills to; 0 for the machine's own level.</summary>
    public static double Share(TopUpLevel level) => level switch
    {
        TopUpLevel.Half => 0.5,
        TopUpLevel.ThreeQuarters => 0.75,
        TopUpLevel.Full => 1.0,
        _ => 0.0,
    };

    /// <summary>
    /// How much a store wants at a fill level: its own want (<paramref name="ownWanted"/>, what its own fill order would bring), or more
    /// where the level's share of <paramref name="capacity"/> asks more, never past the capacity. With no capacity known (the machine's
    /// Def names none, or it reads 0) it is the store's own want. Never below 0.
    /// </summary>
    public static int Wanted(int ownWanted, int held, int capacity, TopUpLevel level)
    {
        int own = System.Math.Max(0, ownWanted);
        double share = Share(level);
        if (share <= 0 || capacity <= 0) return own;
        // The share is rounded up to a whole piece: half of 75 is 38, so "at least half" holds.
        int target = (int)System.Math.Ceiling(capacity * share - 1e-9);
        int holds = System.Math.Max(0, held);
        int byLevel = System.Math.Max(0, target - holds);
        int roomLeft = System.Math.Max(0, capacity - holds);
        return System.Math.Max(own, System.Math.Min(byLevel, roomLeft));
    }

    /// <summary>How much to bring: what it wants, less what others are already bringing; never below 0.</summary>
    public static int ToBring(int wanted, int alreadyComing) => System.Math.Max(0, wanted - System.Math.Max(0, alreadyComing));
}
