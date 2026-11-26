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
    /// <summary>Whether it counts for the redose check: false for a harm the item brings alongside its work (Medical 0.32.0).</summary>
    public readonly bool ForRedose;

    public DoseHediff(HediffDef def, float severity, float dangerSeverity, bool forRedose = true)
    {
        Def = def;
        Severity = severity;
        DangerSeverity = dangerSeverity;
        ForRedose = forRedose;
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
    /// <summary>The drug's chemical, or null: an addiction is never treated with a drug of its own chemical (Medical 0.31.0).</summary>
    public readonly ChemicalDef? Chemical;
    /// <summary>The reservoir one dose refills, and by how much (Medical 0.31.0); null and 0 for anything else.</summary>
    public readonly ReservoirReader? Reservoir;
    public readonly float RefillAmount;

    public DoseOption(ThingDef def, IReadOnlyList<DoseHediff> hediffs, DoseEffect effect, IReadOnlyList<HediffDef>? cures = null, IReadOnlyList<HediffDef>? prevents = null,
        HediffDef? lethalDependency = null, float marketValue = 0f, ChemicalDef? chemical = null, ReservoirReader? reservoir = null, float refillAmount = 0f)
    {
        Chemical = chemical;
        Reservoir = reservoir;
        RefillAmount = refillAmount;
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
    static List<DoseOption>? toxicOptions;
    static List<DoseOption>? vacuumOptions;
    static List<DoseOption>? strainOptions;
    static List<DoseOption>? addictionOptions;
    static List<DoseOption>? oldWoundOptions;
    static List<DoseOption>? regrowthOptions;
    static List<DoseOption>? reservoirOptions;

    /// <summary>Items that steady someone close to a major break (Medical 0.31.0).</summary>
    public static IReadOnlyList<DoseOption> StrainOptions => Built(ref strainOptions);
    /// <summary>Items that ease an addiction (Medical 0.31.0).</summary>
    public static IReadOnlyList<DoseOption> AddictionOptions => Built(ref addictionOptions);
    /// <summary>Items that heal old wounds and chronic conditions (Medical 0.31.0).</summary>
    public static IReadOnlyList<DoseOption> OldWoundOptions => Built(ref oldWoundOptions);
    /// <summary>Items that regrow missing parts (Medical 0.31.0).</summary>
    public static IReadOnlyList<DoseOption> RegrowthOptions => Built(ref regrowthOptions);
    /// <summary>Items that refill a reservoir an artificial part draws on (Medical 0.31.0).</summary>
    public static IReadOnlyList<DoseOption> ReservoirOptions => Built(ref reservoirOptions);

    static IReadOnlyList<DoseOption> Built(ref List<DoseOption>? list)
    {
        if (list == null) Build();
        return list!;
    }
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

    /// <summary>Items that keep out toxic buildup (Medical 0.30.0).</summary>
    public static IReadOnlyList<DoseOption> ToxicOptions
    {
        get
        {
            if (toxicOptions == null) Build();
            return toxicOptions!;
        }
    }

    /// <summary>Items that raise vacuum resistance (Medical 0.30.0).</summary>
    public static IReadOnlyList<DoseOption> VacuumOptions
    {
        get
        {
            if (vacuumOptions == null) Build();
            return vacuumOptions!;
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
    public static bool Any => Options.Count > 0 || ImmunityOptions.Count > 0 || CureOptions.Count > 0 || PreventionOptions.Count > 0 || BreathingOptions.Count > 0 || ToxicOptions.Count > 0
        || StrainOptions.Count > 0 || AddictionOptions.Count > 0 || OldWoundOptions.Count > 0 || RegrowthOptions.Count > 0 || ReservoirOptions.Count > 0;

    static DoseIndex() => _ = Options;

    static void Build()
    {
        var built = new List<DoseOption>();
        var builtImmunity = new List<DoseOption>();
        var builtCures = new List<DoseOption>();
        var builtPrevention = new List<DoseOption>();
        var builtBreathing = new List<DoseOption>();
        var builtToxic = new List<DoseOption>();
        var builtVacuum = new List<DoseOption>();
        var builtStrain = new List<DoseOption>();
        var builtAddiction = new List<DoseOption>();
        var builtOldWounds = new List<DoseOption>();
        var builtRegrowth = new List<DoseOption>();
        var builtReservoir = new List<DoseOption>();
        var builtCurable = new HashSet<HediffDef>();
        var builtPreventable = new HashSet<HediffDef>();
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        HashSet<HediffDef> outbreaks = OutbreakKinds(rules);
        Dictionary<Type, FieldInfo> readers = RemovalReaders(rules);
        Dictionary<Type, GivenReader> givers = GivenReaders(rules);
        var addictionClasses = new HashSet<string>(rules.addictionTreatments);
        var regrowthClasses = new HashSet<string>(rules.regrowthClasses);
        Dictionary<Type, (ReservoirReader reader, FieldInfo amount)> refills = RefillReaders(rules);
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
            float breakDrop = 0f;
            bool treatsAddiction = false, healsOldWounds = false, regrowsParts = false;
            ReservoirReader? reservoir = null;
            float refillAmount = 0f;

            // One condition the item gives, at the severity given (zero or less: the condition's own starting severity). Fed by the game's
            // outcome class, by another mod's class through the rules' readers, and by use effects on an item swallowed (Medical 0.32.0).
            void Give(HediffDef condition, float given)
            {
                float severity = given > 0f ? given : condition.initialSeverity;
                // What a condition does in its comps or its class (Medical 0.31.0): read whether it has stages or not.
                ReadTreatments(condition, addictionClasses, regrowthClasses, ref treatsAddiction, ref healsOldWounds, ref regrowsParts);
                if (condition.stages == null || condition.stages.Count == 0)
                {
                    // A condition with no stages (Hydra's) still counts for the redose check: one working dose is not doubled.
                    if (treatsAddiction || healsOldWounds || regrowsParts)
                        hediffs.Add(new DoseHediff(condition, severity, ConditionDanger.DangerSeverity(condition)));
                    return;
                }
                if (condition.PossibleToDevelopImmunityNaturally()) givesDisease = true;
                HediffStage stage = condition.stages[condition.StageAtSeverity(severity)];
                stages.Add(Read(stage));
                AddConditions(prevents, stage.makeImmuneTo);
                if (stage.statOffsets != null) breakDrop -= stage.statOffsets.GetStatOffsetFromList(StatDefOf.MentalBreakThreshold);
                hediffs.Add(new DoseHediff(condition, severity, ConditionDanger.DangerSeverity(condition)));
            }

            void GiveRead(GivenReader reader, object source)
            {
                if (reader.SkipWhenSet != null && reader.SkipWhenSet.GetValue(source) != null) return;
                if (reader.Condition.GetValue(source) is not HediffDef condition) return;
                float given = reader.Severity != null && ToFloat(reader.Severity.GetValue(source)) is float severity ? severity : 0f;
                if (given < 0f && reader.Source.negativeTakesAway) return;
                Give(condition, given);
            }

            for (int j = 0; j < doers.Count; j++)
            {
                IngestionOutcomeDoer doer = doers[j];
                if (doer == null) continue;
                if (doer is IngestionOutcomeDoer_GiveHediff give)
                {
                    if (give.hediffDef != null) Give(give.hediffDef, give.severity);
                    continue;
                }
                if (doer is IngestionOutcomeDoer_UseThing)
                {
                    // Swallowing applies the item's use effects (the game's own outcome for it): read the ones that give a condition.
                    List<CompProperties>? comps = def.comps;
                    for (int k = 0; comps != null && k < comps.Count; k++)
                    {
                        if (comps[k] is CompProperties_UseEffectAddHediff add)
                        {
                            if (add.hediffDef != null) Give(add.hediffDef, 0f);
                        }
                        else if (comps[k] != null && givers.TryGetValue(comps[k].GetType(), out GivenReader? useReader)) GiveRead(useReader, comps[k]);
                    }
                    continue;
                }
                Type doerType = doer.GetType();
                if (givers.TryGetValue(doerType, out GivenReader? reader)) GiveRead(reader, doer);
                if (readers.TryGetValue(doerType, out FieldInfo? field)) AddConditions(cures, field.GetValue(doer));
                if (refills.TryGetValue(doerType, out var refill) && ToFloat(refill.amount.GetValue(doer)) is float amount && amount > 0f)
                {
                    reservoir = refill.reader;
                    refillAmount = amount;
                }
            }
            MarkSideHarms(hediffs);
            if (extension != null)
            {
                AddConditions(cures, extension.cures);
                AddConditions(prevents, extension.prevents);
                treatsAddiction |= extension.treatsAddiction;
                healsOldWounds |= extension.healsOldWounds;
                regrowsParts |= extension.regrowsParts;
            }
            bool longTerm = treatsAddiction || healsOldWounds || regrowsParts || refillAmount > 0f;
            if (stages.Count == 0 && cures.Count == 0 && prevents.Count == 0 && !longTerm) continue;
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
            if (stages.Count > 0 && def.IsDrug && DoseChoiceRule.WorthwhileForExposure(effect, DoseNeed.Toxic, rules.minToxicCut, rules.Weights))
                builtToxic.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value));
            if (stages.Count > 0 && def.IsDrug && DoseChoiceRule.WorthwhileForExposure(effect, DoseNeed.Vacuum, rules.minVacuumGain, rules.Weights))
                builtVacuum.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value));
            // The long-term needs (0.31.0): medical items only, as for every care need of 0.28.0 and later.
            if (def.IsDrug)
            {
                ChemicalDef? chemical = drug?.chemical;
                if (stages.Count > 0 && breakDrop >= rules.minBreakThresholdDrop && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.MentalStrain, rules.Weights))
                    builtStrain.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value, chemical: chemical));
                if (treatsAddiction && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.Addiction, rules.Weights))
                    builtAddiction.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value, chemical: chemical));
                if (healsOldWounds && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.OldWounds, rules.Weights))
                    builtOldWounds.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value, chemical: chemical));
                if (regrowsParts && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.MissingPart, rules.Weights))
                    builtRegrowth.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value, chemical: chemical));
                if (refillAmount > 0f && DoseChoiceRule.WorthwhileForCare(effect, DoseNeed.Reservoir, rules.Weights))
                    builtReservoir.Add(new DoseOption(def, hediffs.ToArray(), effect, lethalDependency: dependency, marketValue: value, chemical: chemical,
                        reservoir: reservoir, refillAmount: refillAmount));
            }
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
        toxicOptions = builtToxic;
        vacuumOptions = builtVacuum;
        strainOptions = builtStrain;
        addictionOptions = builtAddiction;
        oldWoundOptions = builtOldWounds;
        regrowthOptions = builtRegrowth;
        reservoirOptions = builtReservoir;
        curable = builtCurable;
        preventableOutbreaks = builtPreventable;
    }

    /// <summary>
    /// What a condition does to old wounds, addiction and missing parts, from its class and its comps (Medical 0.31.0): the game's own
    /// healing of permanent wounds by type, the others by the full names the rules list. Read, never run.
    /// </summary>
    static void ReadTreatments(HediffDef def, HashSet<string> addictionClasses, HashSet<string> regrowthClasses, ref bool treatsAddiction, ref bool healsOldWounds,
        ref bool regrowsParts)
    {
        if (def.hediffClass != null && addictionClasses.Contains(def.hediffClass.FullName ?? "")) treatsAddiction = true;
        if (def.comps == null) return;
        for (int i = 0; i < def.comps.Count; i++)
        {
            HediffCompProperties comp = def.comps[i];
            if (comp == null) continue;
            if (comp is HediffCompProperties_HealPermanentWounds) healsOldWounds = true;
            string name = comp.GetType().FullName ?? "";
            if (addictionClasses.Contains(name)) treatsAddiction = true;
            if (regrowthClasses.Contains(name)) regrowsParts = true;
        }
    }

    /// <summary>The refilling outcomes of the rules' reservoirs whose classes a loaded mod has, each with its amount field.</summary>
    static Dictionary<Type, (ReservoirReader, FieldInfo)> RefillReaders(PhobosDosingRulesDef rules)
    {
        var found = new Dictionary<Type, (ReservoirReader, FieldInfo)>();
        foreach (ReservoirReader reader in rules.reservoirs)
        {
            if (reader == null || reader.refillDoerClass.NullOrEmpty()) continue;
            Type? type = OptionalMods.TypeNamed(reader.refillDoerClass);
            if (type == null) continue;
            FieldInfo? field = type.GetField(reader.amountField, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                Log.Warning(DosingMod.LogPrefix + " a reservoir reader names " + reader.refillDoerClass + "." + reader.amountField + ", which that class does not have: its items are not read.");
                continue;
            }
            found[type] = (reader, field);
        }
        return found;
    }

    static float? ToFloat(object? value) => value switch
    {
        int i => i,
        float f => f,
        double d => (float)d,
        long l => l,
        _ => null,
    };

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

    /// <summary>A given-condition reader whose class a loaded mod has, with the fields it names (Medical 0.32.0).</summary>
    sealed class GivenReader
    {
        public readonly GivenConditionReader Source;
        public readonly FieldInfo Condition;
        public readonly FieldInfo? Severity;
        public readonly FieldInfo? SkipWhenSet;

        public GivenReader(GivenConditionReader source, FieldInfo condition, FieldInfo? severity, FieldInfo? skipWhenSet)
        {
            Source = source;
            Condition = condition;
            Severity = severity;
            SkipWhenSet = skipWhenSet;
        }
    }

    /// <summary>
    /// The rules' given-condition readers whose class a loaded mod has. A reader naming a field its class does not have is said once in
    /// the log, as the removal readers are: the other mod changed, and its items would silently stop being read.
    /// </summary>
    static Dictionary<Type, GivenReader> GivenReaders(PhobosDosingRulesDef rules)
    {
        const BindingFlags Fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var found = new Dictionary<Type, GivenReader>();
        foreach (GivenConditionReader reader in rules.givenConditionReaders)
        {
            if (reader == null || reader.className.NullOrEmpty()) continue;
            Type? type = OptionalMods.TypeNamed(reader.className);
            if (type == null) continue;
            FieldInfo? condition = type.GetField(reader.conditionField, Fields);
            FieldInfo? severity = reader.severityField.NullOrEmpty() ? null : type.GetField(reader.severityField, Fields);
            FieldInfo? skip = reader.skipWhenSetField.NullOrEmpty() ? null : type.GetField(reader.skipWhenSetField, Fields);
            if (condition == null || (severity == null && !reader.severityField.NullOrEmpty()) || (skip == null && !reader.skipWhenSetField.NullOrEmpty()))
            {
                Log.Warning(DosingMod.LogPrefix + " a given-condition reader names a field " + reader.className + " does not have: its items are not read.");
                continue;
            }
            found[type] = new GivenReader(reader, condition, severity, skip);
        }
        return found;
    }

    /// <summary>Marks a harm the item brings alongside its work, so it never holds back the next dose (Medical 0.32.0).</summary>
    static void MarkSideHarms(List<DoseHediff> hediffs)
    {
        bool givesHarmless = false;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (!hediffs[i].Def.isBad) givesHarmless = true;
        }
        for (int i = 0; i < hediffs.Count; i++)
        {
            DoseHediff dose = hediffs[i];
            if (!DoseSafetyRule.CountsForRedose(dose.Def.isBad, givesHarmless))
                hediffs[i] = new DoseHediff(dose.Def, dose.Severity, dose.DangerSeverity, forRedose: false);
        }
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
        float toxic = 0f, toxicEnvironment = 0f, vacuum = 0f;
        if (stage.statOffsets != null)
        {
            toxic = stage.statOffsets.GetStatOffsetFromList(StatDefOf.ToxicResistance);
            toxicEnvironment = stage.statOffsets.GetStatOffsetFromList(StatDefOf.ToxicEnvironmentResistance);
            // Odyssey's stat: null without it.
            if (StatDefOf.VacuumResistance != null) vacuum = stage.statOffsets.GetStatOffsetFromList(StatDefOf.VacuumResistance);
        }
        return new StageEffect(stage.painFactor, stage.painOffset, consciousnessOffset, consciousnessSetMax, breathingOffset, stage.totalBleedFactor, immunityGain, stage.vomitMtbDays,
            toxic, toxicEnvironment, vacuum);
    }
}
