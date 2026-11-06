using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>What a condition's clock looks like right now, read from the game for <see cref="ConditionDangerRule"/>.</summary>
public readonly struct ConditionClock
{
    public readonly bool LifeThreateningNow;
    public readonly float DeathMtbDays;
    public readonly float LethalSeverity;
    public readonly float Severity;
    public readonly float SeverityPerDay;

    public ConditionClock(bool lifeThreateningNow, float deathMtbDays, float lethalSeverity, float severity, float severityPerDay)
    {
        LifeThreateningNow = lifeThreateningNow;
        DeathMtbDays = deathMtbDays;
        LethalSeverity = lethalSeverity;
        Severity = severity;
        SeverityPerDay = severityPerDay;
    }

    /// <summary>Hours to death by this condition alone, with the pawn's time to bleed out passed in (it belongs to the pawn, not the condition).</summary>
    public float HoursToDeath(float hoursUntilBloodLossDeath = -1f) =>
        ConditionDangerRule.ExpectedHoursToDeath(DeathMtbDays, Severity, LethalSeverity, SeverityPerDay, hoursUntilBloodLossDeath);
}

/// <summary>
/// The game-facing side of <see cref="ConditionDangerRule"/>: reads Defs and live conditions, caches per Def. Nothing here
/// changes game state.
/// </summary>
public static class ConditionDanger
{
    static readonly Dictionary<HediffDef, float> dangerByDef = new();
    static readonly List<StageDanger> stageBuffer = new();

    /// <summary>The lowest severity at which the condition threatens life, carries a death clock or is lethal; <see cref="ConditionDangerRule.NoDanger"/> when never.</summary>
    public static float DangerSeverity(HediffDef def)
    {
        if (dangerByDef.TryGetValue(def, out float danger)) return danger;
        stageBuffer.Clear();
        if (def.stages != null)
        {
            for (int i = 0; i < def.stages.Count; i++)
            {
                HediffStage stage = def.stages[i];
                stageBuffer.Add(new StageDanger(stage.minSeverity, stage.lifeThreatening, stage.deathMtbDays));
            }
        }
        danger = ConditionDangerRule.DangerSeverity(stageBuffer, def.lethalSeverity);
        dangerByDef[def] = danger;
        return danger;
    }

    /// <summary>How fast the condition's severity moves per day by the game's own comps, tending included; zero for one with no comps.</summary>
    public static float SeverityPerDay(Hediff hediff)
    {
        if (hediff is not HediffWithComps withComps || withComps.comps == null) return 0f;
        float perDay = 0f;
        for (int i = 0; i < withComps.comps.Count; i++)
        {
            if (withComps.comps[i] is HediffComp_SeverityModifierBase modifier) perDay += modifier.SeverityChangePerDay();
        }
        return perDay;
    }

    /// <summary>The condition's clock as it stands.</summary>
    public static ConditionClock Clock(Hediff hediff)
    {
        HediffStage? stage = hediff.CurStage;
        return new ConditionClock(hediff.IsCurrentlyLifeThreatening, stage?.deathMtbDays ?? -1f, hediff.def.lethalSeverity, hediff.Severity, SeverityPerDay(hediff));
    }

    /// <summary>Hours until the pawn bleeds out by the game's own estimate, or -1 when not bleeding.</summary>
    public static float HoursUntilBloodLossDeath(Pawn pawn)
    {
        int ticks = HealthUtility.TicksUntilDeathDueToBloodLoss(pawn);
        return ticks == int.MaxValue ? -1f : ticks / (float)GenDate.TicksPerHour;
    }
}
