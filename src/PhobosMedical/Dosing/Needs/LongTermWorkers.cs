using System;
using System.Collections.Generic;
using System.Reflection;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Phobos.Dosing.Services;
using Phobos.Framework;
using Phobos.Framework.Health;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Needs;

/// <summary>
/// A long-term need (Medical 0.31.0; docs/development/medical-care-needs.md, slice E): a doctor gives the item to someone down or in
/// bed who has the need, and, with the need's setting at "doctors and self", people on their feet take their own. Each starts off (the
/// owner's card of 26 Nov 2026). A subclass says what the need reads and which items answer it; the choice and the safety rule are
/// <see cref="DoseService"/>'s, the same as for every need.
/// </summary>
public abstract class CareNeedWorker_LongTerm : CareNeedWorker
{
    protected abstract DoseNeed Need { get; }

    /// <summary>Every indexed item for this need.</summary>
    protected abstract IReadOnlyList<DoseOption> AllOptions { get; }

    /// <summary>Whether this person has the need now. Cheapest tests first: it runs for every patient at each rebuild of the patient list.</summary>
    public abstract bool Has(Pawn pawn, PhobosDosingRulesDef rules);

    /// <summary>The items that answer this person's need; every indexed one unless a need narrows it.</summary>
    protected virtual IReadOnlyList<DoseOption> OptionsFor(Pawn pawn) => AllOptions;

    CareContext Care => new(def.seriousness, null, def.usesReserve);

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        AllOptions.Count > 0 && Patients.IsLyingDown(patient) && Has(patient, rules);

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        Has(patient, PhobosDosingRulesDef.Current) ? DoseService.DecideCare(doctor, patient, Need, OptionsFor(patient), Care, everyItem) : null;

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        Has(patient, rules) && DoseService.AnyCareAllowed(patient, Need, OptionsFor(patient), Care);

    /// <summary>The ingest job for what this person on their feet would take for the need now, or null.</summary>
    public Job? SelfJob(Pawn pawn, PhobosDosingRulesDef rules)
    {
        if (AllOptions.Count == 0 || !Has(pawn, rules)) return null;
        if (DoseService.SelfCareDecide(pawn, Need, OptionsFor(pawn), Care, false).Chosen is not DoseVerdict chosen || chosen.Item == null) return null;
        string variant = Need.ToString();
        if (Medical.Seen.Wants("medical.longterm-self", pawn, variant)) Medical.Seen.Note("medical.longterm-self", pawn, chosen.Option.Def.label, variant);
        return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
    }
}

/// <summary>Mental strain (the owner's card: "close to a major break"): mood below the major break threshold, and not in a break already.</summary>
public class CareNeedWorker_MentalStrain : CareNeedWorker_LongTerm
{
    protected override DoseNeed Need => DoseNeed.MentalStrain;

    protected override IReadOnlyList<DoseOption> AllOptions => DoseIndex.StrainOptions;

    public override CareMode Mode => DosingMod.Settings.strainMode;

    public override bool Has(Pawn pawn, PhobosDosingRulesDef rules)
    {
        MentalBreaker? breaker = pawn.mindState?.mentalBreaker;
        return breaker != null && LongTermRule.Strained(breaker.CurMood, breaker.BreakThresholdMajor, pawn.InMentalState);
    }
}

/// <summary>
/// Addiction (the owner's card: "addicted to a drug their policy won't feed"): an addiction whose chemical no drug in the person's
/// policy is allowed to feed, and never one that kills when it goes unmet. Treated with something of another chemical.
/// </summary>
public class CareNeedWorker_Addiction : CareNeedWorker_LongTerm
{
    static readonly List<ChemicalDef> quitting = new();
    static readonly List<DoseOption> narrowed = new();
    static Dictionary<ChemicalDef, List<ThingDef>>? drugsByChemical;

    protected override DoseNeed Need => DoseNeed.Addiction;

    protected override IReadOnlyList<DoseOption> AllOptions => DoseIndex.AddictionOptions;

    public override CareMode Mode => DosingMod.Settings.addictionMode;

    public override bool Has(Pawn pawn, PhobosDosingRulesDef rules) => Quitting(pawn) > 0;

    /// <summary>The chemicals this person is being made to quit, into the shared list; how many.</summary>
    static int Quitting(Pawn pawn)
    {
        quitting.Clear();
        DrugPolicy? policy = pawn.drugs?.CurrentPolicy;
        if (policy == null) return 0;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is not Hediff_Addiction addiction || addiction.Chemical is not ChemicalDef chemical || quitting.Contains(chemical)) continue;
            bool lethal = ConditionDanger.DangerSeverity(addiction.def) < Phobos.Framework.Rules.ConditionDangerRule.NoDanger;
            if (LongTermRule.QuitWanted(PolicyFeeds(policy, chemical), lethal)) quitting.Add(chemical);
        }
        return quitting.Count;
    }

    /// <summary>Whether the policy lets any drug of this chemical satisfy the addiction.</summary>
    static bool PolicyFeeds(DrugPolicy policy, ChemicalDef chemical)
    {
        if (drugsByChemical == null)
        {
            drugsByChemical = new Dictionary<ChemicalDef, List<ThingDef>>();
            List<ThingDef> things = DefDatabase<ThingDef>.AllDefsListForReading;
            for (int i = 0; i < things.Count; i++)
            {
                if (!things[i].IsDrug || things[i].GetCompProperties<CompProperties_Drug>()?.chemical is not ChemicalDef owned) continue;
                if (!drugsByChemical.TryGetValue(owned, out List<ThingDef>? list)) drugsByChemical[owned] = list = new List<ThingDef>();
                list.Add(things[i]);
            }
        }
        if (!drugsByChemical.TryGetValue(chemical, out List<ThingDef>? drugs)) return false;
        for (int i = 0; i < drugs.Count; i++)
        {
            if (policy[drugs[i]].allowedForAddiction) return true;
        }
        return false;
    }

    /// <summary>Never a treatment of the same chemical as an addiction being quit: that would feed it.</summary>
    protected override IReadOnlyList<DoseOption> OptionsFor(Pawn pawn)
    {
        Quitting(pawn);
        narrowed.Clear();
        IReadOnlyList<DoseOption> all = AllOptions;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Chemical == null || !quitting.Contains(all[i].Chemical!)) narrowed.Add(all[i]);
        }
        return narrowed;
    }
}

/// <summary>
/// Old wounds (the owner's card: "someone with one that hurts or weakens"): a permanent injury or chronic condition that causes pain, or
/// takes from a capacity or from a part that serves one. A harmless scar alone is not reason enough.
/// </summary>
public class CareNeedWorker_OldWounds : CareNeedWorker_LongTerm
{
    protected override DoseNeed Need => DoseNeed.OldWounds;

    protected override IReadOnlyList<DoseOption> AllOptions => DoseIndex.OldWoundOptions;

    public override CareMode Mode => DosingMod.Settings.oldWoundsMode;

    public override bool Has(Pawn pawn, PhobosDosingRulesDef rules)
    {
        HediffSet set = pawn.health.hediffSet;
        List<Hediff> hediffs = set.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (!hediff.Visible || !(hediff.IsPermanent() || hediff.def.chronic)) continue;
            HediffStage? stage = hediff.CurStage;
            bool takesFromACapacity = stage != null && ((stage.capMods != null && stage.capMods.Count > 0) || stage.partEfficiencyOffset < 0f);
            // A scar's part counts only when the part serves a capacity (an eye, a hand, a lung), not skin over the torso.
            float partEfficiency = hediff.Part != null && hediff.Part.def.tags != null && hediff.Part.def.tags.Count > 0
                ? PawnCapacityUtility.CalculatePartEfficiency(set, hediff.Part)
                : 1f;
            if (LongTermRule.OldWoundMatters(hediff.PainOffset, takesFromACapacity, partEfficiency)) return true;
        }
        return false;
    }
}

/// <summary>A missing part (the owner's card): a natural body part missing and not replaced by an artificial one.</summary>
public class CareNeedWorker_MissingPart : CareNeedWorker_LongTerm
{
    protected override DoseNeed Need => DoseNeed.MissingPart;

    protected override IReadOnlyList<DoseOption> AllOptions => DoseIndex.RegrowthOptions;

    public override CareMode Mode => DosingMod.Settings.missingPartMode;

    public override bool Has(Pawn pawn, PhobosDosingRulesDef rules)
    {
        HediffSet set = pawn.health.hediffSet;
        List<Hediff> hediffs = set.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is Hediff_MissingPart missing && missing.Part != null && !set.PartOrAnyAncestorHasDirectlyAddedParts(missing.Part)) return true;
        }
        return false;
    }
}

/// <summary>
/// A reservoir an artificial part draws on (the owner's card: The Dead Man's Switch's biogel), once a whole dose fits: read through the
/// rules' reservoir readers, by name, never run.
/// </summary>
public class CareNeedWorker_Reservoir : CareNeedWorker_LongTerm
{
    static readonly Dictionary<Type, MemberInfo?> levels = new();
    static readonly List<DoseOption> narrowed = new();

    protected override DoseNeed Need => DoseNeed.Reservoir;

    protected override IReadOnlyList<DoseOption> AllOptions => DoseIndex.ReservoirOptions;

    public override CareMode Mode => DosingMod.Settings.reservoirMode;

    public override bool Has(Pawn pawn, PhobosDosingRulesDef rules) => Wanting(pawn) != null;

    /// <summary>The first reservoir on this person with room for the smallest dose that refills it, or null.</summary>
    static ReservoirReader? Wanting(Pawn pawn)
    {
        IReadOnlyList<DoseOption> options = DoseIndex.ReservoirOptions;
        if (options.Count == 0) return null;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Type type = hediffs[i].GetType();
            for (int j = 0; j < options.Count; j++)
            {
                ReservoirReader? reader = options[j].Reservoir;
                if (reader == null || type.FullName != reader.hediffClass) continue;
                if (Level(hediffs[i], reader) is float level && LongTermRule.ReservoirWantsRefill(level, reader.capacity, SmallestDose(reader))) return reader;
            }
        }
        return null;
    }

    static float SmallestDose(ReservoirReader reader)
    {
        float smallest = float.MaxValue;
        IReadOnlyList<DoseOption> options = DoseIndex.ReservoirOptions;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].Reservoir == reader && options[i].RefillAmount < smallest) smallest = options[i].RefillAmount;
        }
        return smallest == float.MaxValue ? 0f : smallest;
    }

    /// <summary>The reservoir's level, from the member the reader names (a property or a field, any number type).</summary>
    static float? Level(Hediff hediff, ReservoirReader reader)
    {
        Type type = hediff.GetType();
        if (!levels.TryGetValue(type, out MemberInfo? member))
        {
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            member = (MemberInfo?)type.GetProperty(reader.levelMember, any) ?? type.GetField(reader.levelMember, any);
            if (member == null) Log.WarningOnce(DosingMod.LogPrefix + " a reservoir reader names " + reader.hediffClass + "." + reader.levelMember + ", which that class does not have.", type.GetHashCode());
            levels[type] = member;
        }
        object? value = member switch
        {
            PropertyInfo property => property.GetValue(hediff),
            FieldInfo field => field.GetValue(hediff),
            _ => null,
        };
        return value switch
        {
            float f => f,
            double d => (float)d,
            int n => n,
            long l => l,
            _ => null,
        };
    }

    /// <summary>Only what refills the reservoir this person is short of.</summary>
    protected override IReadOnlyList<DoseOption> OptionsFor(Pawn pawn)
    {
        ReservoirReader? reader = Wanting(pawn);
        narrowed.Clear();
        IReadOnlyList<DoseOption> all = AllOptions;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Reservoir == reader) narrowed.Add(all[i]);
        }
        return narrowed;
    }
}

/// <summary>The long-term needs for someone on their feet, asked after a painkiller, a preventive and protection from toxins.</summary>
public static class LongTermCare
{
    /// <summary>The first long-term need this person takes their own for now, or null. Every switch starts off, so most games stop at the first test.</summary>
    public static Job? SelfJob(Pawn pawn)
    {
        DosingSettings s = DosingMod.Settings;
        if (!s.enabled || !(CareModeRule.SelfMay(s.strainMode) || CareModeRule.SelfMay(s.addictionMode) || CareModeRule.SelfMay(s.oldWoundsMode)
            || CareModeRule.SelfMay(s.missingPartMode) || CareModeRule.SelfMay(s.reservoirMode))) return null;
        if (!DoseService.MayTakeOwnPreventive(pawn, s) || !DoseService.IsEligible(pawn, s)) return null;
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        IReadOnlyList<PhobosCareNeedDef> needs = CareNeeds.Ordered;
        for (int i = 0; i < needs.Count; i++)
        {
            if (needs[i].Worker is not CareNeedWorker_LongTerm worker || !CareModeRule.SelfMay(worker.Mode)) continue;
            if (worker.SelfJob(pawn, rules) is Job job) return job;
        }
        return null;
    }
}
