namespace Phobos.MissilePatches.Rules;

/// <summary>
/// How many defenders RimPacts should size an ally's assault on a struck town against. Game-free. RimPacts sizes its assault squad to
/// the defenders still standing when the squad is raised (read in RimPacts 1.7, 7 Nov 2026): it caps the pawn generation to that
/// count times a tech factor, tops the squad up towards the same count, then trims to it. After a strategic strike that is almost
/// nobody, so the player's ally sent one or two soldiers into a burning town and lost. Counting the garrison from before the strike
/// instead lets every one of those steps run as it would have without the missile (owner, 7 Nov 2026: "the nuked town should face the
/// squad it normally would").
/// </summary>
public static class AssaultSizeRule
{
    /// <summary>
    /// The defender count RimPacts should use. <paramref name="preStrikeDefenders"/> is null when the town was not struck recently and
    /// negative when it was struck before counts were kept; RimPacts' own count stands in both. When nobody is standing RimPacts' count
    /// also stands, so its own "defenders wiped out, the attack wins" path still runs. Never lower than RimPacts' count.
    /// </summary>
    public static int Headcount(int standing, int? preStrikeDefenders)
    {
        if (standing <= 0 || preStrikeDefenders is not { } before || before < 0) return standing;
        return before > standing ? before : standing;
    }

    /// <summary>Whether a strike is recent enough to still count for an assault (the rules' window, in ticks).</summary>
    public static bool Recent(int strikeTick, int now, int windowTicks) => now >= strikeTick && now - strikeTick <= windowTicks;
}
