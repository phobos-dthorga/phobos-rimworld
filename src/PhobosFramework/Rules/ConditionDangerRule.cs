using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>One stage of a condition, read for danger only: where it starts, and whether it threatens life or carries a death clock.</summary>
public readonly struct StageDanger
{
    public readonly float MinSeverity;
    public readonly bool LifeThreatening;
    /// <summary>The game's mean time between death rolls at this stage, in days; zero or less for none.</summary>
    public readonly float DeathMtbDays;

    public StageDanger(float minSeverity, bool lifeThreatening, float deathMtbDays)
    {
        MinSeverity = minSeverity;
        LifeThreatening = lifeThreatening;
        DeathMtbDays = deathMtbDays;
    }
}

/// <summary>
/// How dangerous a condition is, from what its Def says: the severity from which it threatens life, and how long a patient has.
/// Game-free; every Phobos mod that asks "is this killing them, and how soon" asks here, so the answer is the same everywhere.
/// </summary>
public static class ConditionDangerRule
{
    /// <summary>The danger severity of a condition that is never dangerous.</summary>
    public const float NoDanger = float.MaxValue;
    /// <summary>The hours to death of a condition with no clock at all.</summary>
    public const float NoClock = float.PositiveInfinity;
    public const float HoursPerDay = 24f;

    /// <summary>The lowest severity at which a stage threatens life or carries a death clock, or the lethal severity; <see cref="NoDanger"/> when none.</summary>
    public static float DangerSeverity(IReadOnlyList<StageDanger> stages, float lethalSeverity)
    {
        float danger = lethalSeverity > 0f ? lethalSeverity : NoDanger;
        for (int i = 0; i < stages.Count; i++)
        {
            StageDanger stage = stages[i];
            if ((stage.LifeThreatening || stage.DeathMtbDays > 0f) && stage.MinSeverity < danger) danger = stage.MinSeverity;
        }
        return danger;
    }

    /// <summary>
    /// Hours until the condition kills, taking the soonest of three clocks: the death clock's mean at the current stage (the game
    /// rolls against it, so this is an expectation, not a promise), the time for a rising severity to reach the lethal one, and
    /// the time to bleed out as the caller measured it. <see cref="NoClock"/> when none applies.
    /// </summary>
    public static float ExpectedHoursToDeath(float deathMtbDays, float severity, float lethalSeverity, float severityPerDay, float hoursUntilBloodLossDeath)
    {
        float hours = NoClock;
        if (deathMtbDays > 0f) hours = deathMtbDays * HoursPerDay;
        if (lethalSeverity > 0f && severityPerDay > 0f)
        {
            float left = lethalSeverity - severity;
            float toLethal = (left > 0f ? left : 0f) / severityPerDay * HoursPerDay;
            if (toLethal < hours) hours = toLethal;
        }
        if (hoursUntilBloodLossDeath >= 0f && hoursUntilBloodLossDeath < hours) hours = hoursUntilBloodLossDeath;
        return hours;
    }
}
