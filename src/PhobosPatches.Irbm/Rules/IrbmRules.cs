namespace Phobos.MissilePatches.Rules;

/// <summary>What shelters a thing from a strategic blast (a setting; owner, 7 November 2026: Rimatomics-like by default).</summary>
public enum ShelterMode
{
    /// <summary>Any natural rock roof, thin or thick: Dubs Rimatomics' rule. Mountain interiors survive and are never swept.</summary>
    AnyRockRoof,
    /// <summary>Only thick rock roof: the missile mod's own rule.</summary>
    ThickRockOnly,
    /// <summary>Nothing shelters.</summary>
    Nothing,
}

/// <summary>Combat Extended's wall fragments during a strike (a setting; owner's request, 7 November 2026).</summary>
public enum FragmentMode
{
    None,
    /// <summary>A tenth of Combat Extended's own intensity.</summary>
    Reduced,
    AsCombatExtended,
}

/// <summary>What becomes of a pawn the blast kills.</summary>
public enum CorpseMode
{
    Vaporised,
    LeftAsTheyFall,
}

/// <summary>How one thing is hit.</summary>
public enum ThingHit
{
    /// <summary>The missile mod's own way: TakeDamage, with every mod's damage and death handling.</summary>
    Damage,
    /// <summary>Gone without trace: Destroy(Vanish). No leavings, filth, effects or Combat Extended fragments.</summary>
    Vanish,
    /// <summary>Destroyed with leavings and rubble: Destroy(KillFinalize), without the damage path.</summary>
    KillFinalize,
}

/// <summary>Frame budgets of a strategic blast. Game-free.</summary>
public static class IrbmBudgetRule
{
    /// <summary>The player's frame budget, in milliseconds of real time (agent defaults, for the owner to tune).</summary>
    public const float MinFrameBudgetMs = 2f;
    public const float MaxFrameBudgetMs = 50f;
    public const float DefaultFrameBudgetMs = 12f;

    /// <summary>Above this the settings window warns of stutter (agent's estimate: two frames at 60 frames a second, less a margin).</summary>
    public const float StutterWarningMs = 25f;

    /// <summary>
    /// While the screen is white or fading back (the missile mod fades over 5 s), a slice may take this much: the player sees nothing
    /// but white, so a slower frame costs nothing (agent default).
    /// </summary>
    public const float WhiteScreenBudgetMs = 40f;
    public const float WhiteScreenSeconds = 5f;

    /// <summary>A slice always hits at least this many things, so a blast can't creep when one thing is slow (agent default).</summary>
    public const int MinimumThingsPerSlice = 8;

    /// <summary>A blast still running after this many seconds says so in a message, and again when it ends.</summary>
    public const float LongBlastSeconds = 2f;

    public static float FrameBudget(float setting, bool whiteScreenBudget, float secondsSinceStart)
    {
        float normal = setting < MinFrameBudgetMs ? MinFrameBudgetMs : setting > MaxFrameBudgetMs ? MaxFrameBudgetMs : setting;
        if (whiteScreenBudget && secondsSinceStart < WhiteScreenSeconds && normal < WhiteScreenBudgetMs) return WhiteScreenBudgetMs;
        return normal;
    }
}

/// <summary>Whether a cell's roof shelters what stands under it. Game-free.</summary>
public static class ShelterRule
{
    /// <summary>A warhead the missile mod marks as penetrating thick roof ignores every shelter.</summary>
    public static ShelterMode Effective(ShelterMode setting, bool warheadPenetratesThickRoof) =>
        warheadPenetratesThickRoof ? ShelterMode.Nothing : setting;

    /// <summary>Constructed roofs never shelter: the blast takes them first.</summary>
    public static bool Sheltered(ShelterMode mode, bool roofed, bool naturalRoof, bool thickRoof) => mode switch
    {
        ShelterMode.AnyRockRoof => roofed && naturalRoof,
        ShelterMode.ThickRockOnly => roofed && thickRoof,
        _ => false,
    };
}

/// <summary>
/// How one thing is hit, so that the result is the missile mod's (whatever its damage would destroy is destroyed) at a fraction of
/// the cost. Game-free.
/// </summary>
public static class HitRule
{
    /// <summary>
    /// Pawns always take the damage, to die properly. So does anything the damage could not destroy: harmless damage (EMP, stun),
    /// things without hit points or not destroyable, and hit points above the amount; the missile mod's own rule then decides. A
    /// building takes the damage too while Combat Extended's fragments are live, since they come only from damage. Everything else
    /// vanishes, or with the rubble setting is destroyed with its leavings.
    /// </summary>
    public static ThingHit Choose(bool isPawn, bool isBuilding, bool harmsHealth, bool useHitPoints, bool destroyable, int hitPoints,
        float amount, bool rubble, bool fragmentsLive)
    {
        if (isPawn || !harmsHealth || !useHitPoints || !destroyable || amount < hitPoints) return ThingHit.Damage;
        if (isBuilding && fragmentsLive) return ThingHit.Damage;
        if (isBuilding && rubble) return ThingHit.KillFinalize;
        return ThingHit.Vanish;
    }
}

/// <summary>
/// Things are hit from the map's centre outward, so a blast that takes a moment reads as a wave crossing the map (agent choice; the
/// mod's own order was the order things were spawned in). Lower ranks go first.
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
