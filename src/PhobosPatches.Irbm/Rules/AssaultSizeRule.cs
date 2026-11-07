namespace Phobos.MissilePatches.Rules;

/// <summary>
/// How big an ally's assault on a struck town should be. Game-free. RimPacts trims its assault squad to the defenders still standing
/// when it arrives, times a tech factor (read in RimPacts 1.7, 7 Nov 2026). After a strategic strike that is almost nobody, so the
/// player's ally sent one or two soldiers into a burning town and lost. This trims to the garrison before the strike instead, so the
/// ally faces the squad it would have faced without the missile (owner, 7 Nov 2026). A town struck before we kept counts is not
/// trimmed at all: RimPacts' generated squad stands.
/// </summary>
public static class AssaultSizeRule
{
    /// <summary>No trimming: every pawn RimPacts generated stays.</summary>
    public const int NoTrim = int.MaxValue;

    /// <summary>
    /// The squad size to trim to. <paramref name="preStrikeDefenders"/> is null when the town was never struck (RimPacts' figure
    /// stands), negative when it was struck before counts were kept (no trim), else the garrison counted just before the strike.
    /// </summary>
    public static int Target(int rimPactsTarget, int? preStrikeDefenders, float techFactor)
    {
        if (preStrikeDefenders is not { } before) return rimPactsTarget;
        if (before < 0) return NoTrim;
        int scaled = (int)System.Math.Ceiling(before * techFactor);
        return scaled > rimPactsTarget ? scaled : rimPactsTarget;
    }

    /// <summary>RimPacts' own tech factor: a tenth fewer attackers per tech level the attacker leads by, never below half.</summary>
    public static float TechFactor(int attackerTechLevel, int defenderTechLevel)
    {
        int lead = attackerTechLevel - defenderTechLevel;
        if (lead < 0) lead = 0;
        float factor = 1f - 0.1f * lead;
        return factor < 0.5f ? 0.5f : factor;
    }

    /// <summary>Whether a strike is recent enough to still count for an assault (the rules' window, in ticks).</summary>
    public static bool Recent(int strikeTick, int now, int windowTicks) => now >= strikeTick && now - strikeTick <= windowTicks;
}
