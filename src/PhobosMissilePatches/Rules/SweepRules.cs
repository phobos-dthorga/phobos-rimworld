namespace Phobos.MissilePatches.Rules;

/// <summary>
/// The game-free rules of a spread-out blast: how long one frame may work on it, and in what order the map's things are hit.
/// Compiled into the tests without the game.
/// </summary>
public static class SweepBudgetRule
{
    /// <summary>The frame budget a player may choose, in milliseconds of real time (agent defaults, for the owner to tune).</summary>
    public const float MinFrameBudgetMs = 2f;
    public const float MaxFrameBudgetMs = 50f;
    public const float DefaultFrameBudgetMs = 12f;

    /// <summary>
    /// A frame always hits at least this many things, so a blast can't creep when a single thing is slow to destroy
    /// (agent default).
    /// </summary>
    public const int MinimumThingsPerFrame = 8;

    /// <summary>A long blast earns a message after this many seconds of real time, and a second one when it ends.</summary>
    public const float LongBlastSeconds = 2f;

    /// <summary>Whether this frame may hit one more thing: always up to the minimum, then only while the budget lasts.</summary>
    public static bool MayContinue(double elapsedMs, float budgetMs, int doneThisFrame) =>
        doneThisFrame < MinimumThingsPerFrame || elapsedMs < budgetMs;

    public static float ClampBudget(float budgetMs) =>
        budgetMs < MinFrameBudgetMs ? MinFrameBudgetMs : budgetMs > MaxFrameBudgetMs ? MaxFrameBudgetMs : budgetMs;
}

/// <summary>
/// Things are hit from the map's centre outward, so a blast that takes several seconds reads as a wave crossing the map
/// (agent choice; the mod's own order was the order things were spawned in). Lower ranks go first.
/// </summary>
public static class SweepOrderRule
{
    public static long Rank(int x, int z, int centreX, int centreZ)
    {
        long dx = x - centreX;
        long dz = z - centreZ;
        return dx * dx + dz * dz;
    }
}
