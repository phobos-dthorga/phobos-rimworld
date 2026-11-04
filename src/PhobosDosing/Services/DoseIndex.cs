using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>One condition a dose gives, with the severity past which its stages turn dangerous.</summary>
public readonly struct DoseHediff
{
    public readonly HediffDef Def;
    public readonly float Severity;
    /// <summary>The lowest severity at which a stage is life-threatening or carries a death clock, or the lethal severity; huge when none.</summary>
    public readonly float DangerSeverity;

    public DoseHediff(HediffDef def, float severity, float dangerSeverity)
    {
        Def = def;
        Severity = severity;
        DangerSeverity = dangerSeverity;
    }
}

/// <summary>An ingestible that relieves pain, and what one dose of it does.</summary>
public sealed class DoseOption
{
    public readonly ThingDef Def;
    public readonly IReadOnlyList<DoseHediff> Hediffs;
    public readonly DoseEffect Effect;

    public DoseOption(ThingDef def, IReadOnlyList<DoseHediff> hediffs, DoseEffect effect)
    {
        Def = def;
        Hediffs = hediffs;
        Effect = effect;
    }
}

/// <summary>
/// Every ingestible in the game that relieves pain, found by what its Defs say it does, never by name (owner's rule, 5 Nov
/// 2026): each outcome that gives a condition is read at the dose's severity for pain, consciousness, breathing and bleeding,
/// and the drug comp for addictiveness and category; items whose relief is too small to matter or whose score cannot pass zero
/// are left out. Built once, on first use after the Defs load. An effect a mod applies from its own code rather than its XML
/// stays invisible here; that is recorded in the design record as a limit. A second list holds the medical drugs that speed up immunity,
/// read the same way. An item that gives a disease itself is never on it, whatever else it does.
/// </summary>
[StaticConstructorOnStartup]
public static class DoseIndex
{
    static List<DoseOption>? options;
    static List<DoseOption>? immunityOptions;

    public static IReadOnlyList<DoseOption> Options
    {
        get
        {
            if (options == null) Build();
            return options!;
        }
    }

    public static IReadOnlyList<DoseOption> ImmunityOptions
    {
        get
        {
            if (immunityOptions == null) Build();
            return immunityOptions!;
        }
    }

    static DoseIndex() => _ = Options;

    static void Build()
    {
        var built = new List<DoseOption>();
        var builtImmunity = new List<DoseOption>();
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        var stages = new List<StageEffect>();
        var hediffs = new List<DoseHediff>();
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            List<IngestionOutcomeDoer>? doers = def.ingestible?.outcomeDoers;
            if (doers == null || def.category != ThingCategory.Item) continue;
            if (def.GetModExtension<PhobosDosingExtension>()?.neverAutoDose == true) continue;
            stages.Clear();
            hediffs.Clear();
            bool givesDisease = false;
            for (int j = 0; j < doers.Count; j++)
            {
                if (doers[j] is not IngestionOutcomeDoer_GiveHediff give || give.hediffDef?.stages == null || give.hediffDef.stages.Count == 0) continue;
                float severity = give.severity > 0f ? give.severity : give.hediffDef.initialSeverity;
                if (give.hediffDef.PossibleToDevelopImmunityNaturally()) givesDisease = true;
                HediffStage stage = give.hediffDef.stages[give.hediffDef.StageAtSeverity(severity)];
                stages.Add(Read(stage));
                hediffs.Add(new DoseHediff(give.hediffDef, severity, DangerSeverity(give.hediffDef)));
            }
            if (stages.Count == 0) continue;
            CompProperties_Drug? drug = def.GetCompProperties<CompProperties_Drug>();
            DoseEffect effect = DoseEffectRule.Combine(stages, drug?.addictiveness ?? 0f, def.IsNonMedicalDrug, rules.sedativeConsciousnessCeiling);
            if (DoseChoiceRule.Worthwhile(effect, rules.minPainRelief, rules.Weights)) built.Add(new DoseOption(def, hediffs.ToArray(), effect));
            if (def.IsDrug && !givesDisease && DoseChoiceRule.WorthwhileForImmunity(effect, rules.minImmunityGain, rules.Weights)) builtImmunity.Add(new DoseOption(def, hediffs.ToArray(), effect));
        }
        options = built;
        immunityOptions = builtImmunity;
    }

    static StageEffect Read(HediffStage stage)
    {
        float consciousnessOffset = 0f;
        float consciousnessSetMax = 1f;
        float breathingOffset = 0f;
        if (stage.capMods != null)
        {
            for (int i = 0; i < stage.capMods.Count; i++)
            {
                PawnCapacityModifier mod = stage.capMods[i];
                if (mod.capacity == PawnCapacityDefOf.Consciousness)
                {
                    consciousnessOffset += mod.offset;
                    if (mod.setMax < consciousnessSetMax) consciousnessSetMax = mod.setMax;
                }
                else if (mod.capacity == PawnCapacityDefOf.Breathing)
                {
                    breathingOffset += mod.offset;
                }
            }
        }
        float immunityGain = 0f;
        if (stage.statOffsets != null) immunityGain += stage.statOffsets.GetStatOffsetFromList(StatDefOf.ImmunityGainSpeed);
        if (stage.statFactors != null) immunityGain += stage.statFactors.GetStatFactorFromList(StatDefOf.ImmunityGainSpeed) - 1f;
        return new StageEffect(stage.painFactor, stage.painOffset, consciousnessOffset, consciousnessSetMax, breathingOffset, stage.totalBleedFactor, immunityGain);
    }

    static float DangerSeverity(HediffDef def)
    {
        float danger = def.lethalSeverity > 0f ? def.lethalSeverity : float.MaxValue;
        for (int i = 0; i < def.stages.Count; i++)
        {
            HediffStage stage = def.stages[i];
            if ((stage.lifeThreatening || stage.deathMtbDays > 0f) && stage.minSeverity < danger) danger = stage.minSeverity;
        }
        return danger;
    }
}
