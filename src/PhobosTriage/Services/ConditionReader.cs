using System.Collections.Generic;
using Phobos.Framework.Health;
using Phobos.Triage.Rules;
using RimWorld;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>Reads a live condition into the game-free reading the class rule wants. Nothing here changes game state.</summary>
public static class ConditionReader
{
    static readonly PawnCapacityDef[] vitals = { PawnCapacityDefOf.Consciousness, PawnCapacityDefOf.Breathing, PawnCapacityDefOf.BloodPumping };

    public static bool IsVital(PawnCapacityDef capacity)
    {
        for (int i = 0; i < vitals.Length; i++)
        {
            if (vitals[i] == capacity) return true;
        }
        return false;
    }

    /// <summary>The patient's lowest vital capacity.</summary>
    public static float LowestVital(Pawn pawn)
    {
        float lowest = 1f;
        for (int i = 0; i < vitals.Length; i++)
        {
            float level = pawn.health.capacities.GetLevel(vitals[i]);
            if (level < lowest) lowest = level;
        }
        return lowest;
    }

    public static ConditionReading Read(Pawn pawn, Hediff hediff)
    {
        ConditionClock clock = ConditionDanger.Clock(hediff);
        HediffStage? stage = hediff.CurStage;
        float vitalOffset = 0f;
        float vitalLevel = 1f;
        float capacityCap = 1f;
        float capacityOffset = 0f;
        List<PawnCapacityModifier>? mods = stage?.capMods;
        if (mods != null)
        {
            for (int i = 0; i < mods.Count; i++)
            {
                PawnCapacityModifier mod = mods[i];
                if (mod.capacity == null) continue;
                if (mod.offset < capacityOffset) capacityOffset = mod.offset;
                if (mod.SetMaxDefined && mod.setMax < capacityCap) capacityCap = mod.setMax;
                if (mod.offset < 0f && IsVital(mod.capacity))
                {
                    float level = pawn.health.capacities.GetLevel(mod.capacity);
                    if (mod.offset < vitalOffset)
                    {
                        vitalOffset = mod.offset;
                        vitalLevel = level;
                    }
                }
            }
        }
        bool clears = clock.SeverityPerDay < 0f || (hediff is HediffWithComps withComps && withComps.TryGetComp<HediffComp_Disappears>() != null);
        return new ConditionReading(clock.LifeThreateningNow, clock.DeathMtbDays, clock.LethalSeverity, clock.Severity, clock.SeverityPerDay,
            hediff.def.isBad, clears, vitalOffset, vitalLevel, capacityCap, capacityOffset, stage?.partEfficiencyOffset ?? 0f);
    }
}
