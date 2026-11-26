using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Phobos.Framework;
using Phobos.Framework.Health;
using Phobos.Framework.Rules;
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

/// <summary>An ingestible on one of the index's lists, and what one dose of it does.</summary>
public sealed class DoseOption
{
    static readonly HediffDef[] None = Array.Empty<HediffDef>();

    public readonly ThingDef Def;
    public readonly IReadOnlyList<DoseHediff> Hediffs;
    public readonly DoseEffect Effect;
    /// <summary>Conditions one dose removes outright (Medical 0.28.0): read from an outcome class a reader knows, or declared on the item.</summary>
    public readonly IReadOnlyList<HediffDef> Cures;
    /// <summary>Conditions one dose makes the taker immune to (Medical 0.28.0): the game's makeImmuneTo on the stage it gives, or declared.</summary>
    public readonly IReadOnlyList<HediffDef> Prevents;
    /// <summary>The addiction this item can begin when that addiction can kill, or null: never begun by rule.</summary>
    public readonly HediffDef? LethalDependency;
    /// <summary>What one is worth, by the game's own market value: at the player's rare mark or over, it is kept for saving a life.</summary>
    public readonly float MarketValue;

    public DoseOption(ThingDef def, IReadOnlyList<DoseHediff> hediffs, DoseEffect effect, IReadOnlyList<HediffDef>? cures = null, IReadOnlyList<HediffDef>? prevents = null,
        HediffDef? lethalDependency = null, float marketValue = 0f)
    {
        Def = def;
        Hediffs = hediffs;
        Effect = effect;
        Cures = cures ?? None;
        Prevents = prevents ?? None;
        LethalDependency = lethalDependency;
        MarketValue = marketValue;
    }
}

/// <summary>
/// Every ingestible in the game that answers a care need, found by what its Defs say it does, never by name (owner's rule, 5 Nov
/// 2026): each outcome that gives a condition is read at the dose's severity for pain, consciousness, breathing, bleeding, vomiting,
/// immunity gain, breathing gained and the conditions it makes the taker immune to, and the drug comp for addictiveness and category.
/// Five lists: painkillers (items whose relief is too small to matter or whose score cannot pass zero are left out), the medical drugs
/// that speed up immunity, cures and preventives (Medical 0.28.0), and helps to breathing (0.29.0). A cure is read from an outcome class the rules' readers know, or declared on
/// the item (<see cref="PhobosDosingExtension"/>). Built once, on first use after the Defs load. An effect a mod applies from its own
/// code rather than its XML stays invisible here unless it is declared; that is recorded in the design record as a limit. An item
/// that gives a disease itself is never a drug for immunity, a cure or a preventive, whatever else it does.
/// </summary>
[StaticConstructorOnStartup]
public static class DoseIndex
{
    /// <summary>Vanilla Expanded Framework's comp for a condition that spreads between people: such a condition is an outbreak.</summary>
    const string SpreadableComp = "VEF.Hediffs.HediffCompProperties_Spreadable";

    static List<DoseOption>? options;
    static List<DoseOption>? immunityOptions;
    static List<DoseOption>? cureOptions;
    static List<DoseOption>? preventionOptions;
    static List<DoseOption>? breathingOptions;
    static HashSet<HediffDef>? curable;
    static HashSet<HediffDef>? preventableOutbreaks;

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

    /// <summary>Items that remove a condition outright.</summary>
    public static IReadOnlyList<DoseOption> CureOptions
    {
        get
        {
            if (cureOptions == null) Build();
            return cureOptions!;
        }
    }

    /// <summary>Items that make the taker immune to an outbreak disease.</summary>
    public static IReadOnlyList<DoseOption> PreventionOptions
    {
        get
        {
            if (preventionOptions == null) Build();
            return preventionOptions!;
        }
    }

    /// <summary>Items that add to breathing: a bridge for a patient who can barely breathe.</summary>
    public static IReadOnlyList<DoseOption> BreathingOptions
    {
        get
        {
            if (breathingOptions == null) Build();
            return breathingOptions!;
        }
    }

    /// <summary>Every condition some indexed cure removes: the cheap first test of whether a patient could be cured.</summary>
    public static HashSet<HediffDef> Curable
    {
        get
        {
            if (curable == null) Build();
            return curable!;
        }
    }

    /// <summary>Every outbreak disease some indexed preventive protects against.</summary>
    public static HashSet<HediffDef> PreventableOutbreaks
    {
        get
        {
            if (preventableOutbreaks == null) Build();
            return preventableOutbreaks!;
        }
    }

    /// <summary>Whether any list holds anything: the work giver's first test.</summary>
    public static bool Any => Options.Count > 0 || ImmunityOptions.Count > 0 || CureOptions.Count > 0 || PreventionOptions.Count > 0 || BreathingOptions.Count > 0;

    static DoseIndex() => _ = Options;

    static void Build()
    {
        var built = new List<DoseOption>();
        var builtImmunity = new List<DoseOption>();
        var builtCures = new List<DoseOption>();
        var builtPrevention = new List<DoseOption>();
        var builtBreathing = new List<DoseOption>();
        var builtCurable = new HashSet<HediffDef>();
        var builtPreventable = new HashSet<HediffDef>();
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        HashSet<HediffDef> outbreaks = OutbreakKinds(rules);
        Dictionary<Type, FieldInfo> readers = RemovalReaders(rules);
        var stages = new List<StageEffect>();
        var hediffs = new List<DoseHediff>();
        var cures = new List<HediffDef>();
        var prevents = new List<HediffDef>();
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            List<IngestionOutcomeDoer>? doers = def.ingestible?.outcomeDoers;
            if (doers == null || def.category != ThingCategory.Item) continue;
            PhobosDosingExtension? extension = def.GetModExtension<PhobosDosingExtension>();
            if (extension?.neverAutoDose == true) continue;
            stages.Clear();
            hediffs.Clear();
            cures.Clear();
            prevents.Clear();
            bool givesDisease = false;
            for (int j = 0; j < doers.Count; j++)
            {
                if (doers[j] is not IngestionOutcomeDoer_GiveHediff give)
                {
                    if (doers[j] != null && readers.TryGetValue(doers[j].GetType(), out FieldInfo? field)) AddConditions(cures, field.GetValue(doers[j]));
                    continue;
                }
                if (give.hediffDef?.stages == null || give.hediffDef.stages.Count == 0) continue;
                float severity = give.severity > 0f ? give.severity : give.hediffDef.initialSeverity;
                if (give.hediffDef.PossibleToDevelopImmunityNaturally()) givesDisease = true;
                HediffStage stage = give.hediffDef.stages[give.hediffDef.StageAtSeverity(severity)];
                stages.Add(Read(stage));
                AddConditions(prevents, stage.makeImmuneTo);
                hediffs.Add(new DoseHediff(give.hediffDef, severity, ConditionDanger.DangerSeverity(give.hediffDef)));
            }
            if (extension != null)
            {
                AddConditions(cures, extension.cures);
                AddConditions(prevents, extension.prevents);
            }
            if (stages.Count == 0 && cures.Count == 0 && prevents.Count == 0) continue;
            CompProperties_Drug? drug = def.GetCompProperties<CompProperties_Drug>();
            DoseEffect effect = DoseEffectRule.Combine(stages, drug?.addictiveness ?? 0f, def.IsNonMedicalDrug, rules.sedativeConsciousnessCeiling);
            HediffDef? dependency = LethalDependency(drug);
            float value = def.BaseMarketValue;
            if (stages.Count > 0 && DoseChoiceRule.Worthwhile(effect, rules.minPainRelief, rules.Weights))
                built.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value));
            if (stages.Count > 0 && def.IsDrug && !givesDisease && DoseChoiceRule.WorthwhileForImmunity(effect, rules.minImmunityGain, rules.Weights))
                builtImmunity.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value));
            if (givesDisease) continue;
            if (stages.Count > 0 && def.IsDrug && DoseChoiceRule.WorthwhileForBreathing(effect, rules.minBreathingGain, rules.Weights))
                builtBreathing.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value));
            if (cures.Count > 0 && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.Cure, rules.Weights))
            {
                builtCures.Add(new DoseOption(def, hediffs.ToArray(), effect, cures: cures.ToArray(), lethalDependency: dependency, marketValue: value));
                builtCurable.UnionWith(cures);
            }
            // A preventive is listed for the outbreak diseases it stops, and for nothing else it happens to make immune to.
            prevents.RemoveAll(condition => !outbreaks.Contains(condition));
            if (prevents.Count > 0 && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.Prevention, rules.Weights))
            {
                builtPrevention.Add(new DoseOption(def, hediffs.ToArray(), effect, prevents: prevents.ToArray(), lethalDependency: dependency, marketValue: value));
                builtPreventable.UnionWith(prevents);
            }
        }
        options = built;
        immunityOptions = builtImmunity;
        cureOptions = builtCures;
        preventionOptions = builtPrevention;
        breathingOptions = builtBreathing;
        curable = builtCurable;
        preventableOutbreaks = builtPreventable;
    }

    /// <summary>Adds the conditions a field or a Def list holds: one condition, or any list of them; anything else adds nothing.</summary>
    static void AddConditions(List<HediffDef> into, object? value)
    {
        if (value is HediffDef one)
        {
            if (!into.Contains(one)) into.Add(one);
        }
        else if (value is IEnumerable many)
        {
            foreach (object? item in many)
            {
                if (item is HediffDef condition && !into.Contains(condition)) into.Add(condition);
            }
        }
    }

    /// <summary>
    /// The conditions that count as an outbreak, by what they are: a disease one of the game's events brings to a group of people at
    /// once, a condition that spreads between people (Vanilla Expanded Framework's comp), and any the rules name.
    /// </summary>
    static HashSet<HediffDef> OutbreakKinds(PhobosDosingRulesDef rules)
    {
        var kinds = new HashSet<HediffDef>();
        List<IncidentDef> incidents = DefDatabase<IncidentDef>.AllDefsListForReading;
        for (int i = 0; i < incidents.Count; i++)
        {
            if (incidents[i].diseaseIncident != null) kinds.Add(incidents[i].diseaseIncident);
        }
        Type? spreadable = OptionalMods.TypeNamed(SpreadableComp);
        if (spreadable != null)
        {
            List<HediffDef> conditions = DefDatabase<HediffDef>.AllDefsListForReading;
            for (int i = 0; i < conditions.Count; i++)
            {
                List<HediffCompProperties>? comps = conditions[i].comps;
                if (comps == null) continue;
                for (int j = 0; j < comps.Count; j++)
                {
                    if (spreadable.IsInstanceOfType(comps[j])) kinds.Add(conditions[i]);
                }
            }
        }
        foreach (HediffDef named in rules.outbreakDiseases)
        {
            if (named != null) kinds.Add(named);
        }
        return kinds;
    }

    /// <summary>
    /// The rules' readers whose class a loaded mod has, each with the field it names. A reader naming a field its class does not have
    /// is said once in the log: the other mod changed, and its cures would silently stop being found.
    /// </summary>
    static Dictionary<Type, FieldInfo> RemovalReaders(PhobosDosingRulesDef rules)
    {
        var readers = new Dictionary<Type, FieldInfo>();
        foreach (RemovalReader reader in rules.removalReaders)
        {
            if (reader == null || reader.doerClass.NullOrEmpty()) continue;
            Type? type = OptionalMods.TypeNamed(reader.doerClass);
            if (type == null) continue;
            FieldInfo? field = type.GetField(reader.field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Log.Warning(DosingMod.LogPrefix + " a removal reader names " + reader.doerClass + "." + reader.field + ", which that class does not have: its items are not read as cures.");
                continue;
            }
            readers[type] = field;
        }
        return readers;
    }

    /// <summary>The addiction a drug can begin, when that addiction's own stages can kill (the game's luciferium need is the model).</summary>
    static HediffDef? LethalDependency(CompProperties_Drug? drug)
    {
        HediffDef? addiction = drug != null && drug.addictiveness > 0f ? drug.chemical?.addictionHediff : null;
        return addiction != null && ConditionDanger.DangerSeverity(addiction) < ConditionDangerRule.NoDanger ? addiction : null;
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
        return new StageEffect(stage.painFactor, stage.painOffset, consciousnessOffset, consciousnessSetMax, breathingOffset, stage.totalBleedFactor, immunityGain, stage.vomitMtbDays);
    }
}
