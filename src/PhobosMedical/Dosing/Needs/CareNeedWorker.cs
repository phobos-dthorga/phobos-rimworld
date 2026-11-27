using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using Phobos.Dosing.Services;
using Phobos.Framework.Health;
using Phobos.Framework.Rules;
using Phobos.Radiation;
using Phobos.Radiation.Services;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Needs;

/// <summary>
/// The reading side of one care need (Medical 0.28.0; docs/development/medical-care-needs.md): who wants it from a doctor now, what
/// the rules would give, and what is missing when nothing in stock answers it. A worker reads and decides nothing about the game's
/// state; the choice and the safety rule are <see cref="DoseService"/>'s, the same for every need. The base class is a need nobody
/// has, so a Def with a broken worker class does nothing rather than throwing.
/// </summary>
public class CareNeedWorker
{
    public PhobosCareNeedDef def = null!;

    /// <summary>The player's switch for this need; the Def's default where the player has no setting for it.</summary>
    public virtual CareMode Mode => def.defaultMode;

    /// <summary>Whether doctors give it through the dosing job. False for a need with a job of its own (the radiation medicines).</summary>
    public virtual bool DosedByDoctors => true;

    /// <summary>Whether this patient wants it from a doctor now. Cheap: it runs for every patient at each rebuild of the patient list.</summary>
    public virtual bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => false;

    /// <summary>The decision for a patient who wants it, or null when there is nothing to decide.</summary>
    public virtual DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) => null;

    /// <summary>Whether the rules would let anyone give this patient something for it now, wherever the items are.</summary>
    public virtual bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => false;

    /// <summary>
    /// What this person lacks, in the game world's words, when they have the need and nothing on the map answers it although the
    /// colony knows how to make something that would; null otherwise. For the alert.
    /// </summary>
    public virtual string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => null;

    /// <summary>Whether none of these items is on the map while the colony could make at least one of them.</summary>
    protected static bool NoneInStockButKnown(Map? map, IReadOnlyList<DoseOption> options, List<HediffDef>? targets, DoseNeed need, bool recreationalToo = true)
    {
        bool known = false;
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            if (!recreationalToo && option.Effect.Recreational) continue;
            if (targets != null && !Names(need == DoseNeed.Cure ? option.Cures : option.Prevents, targets)) continue;
            if (DoseService.OnMap(map, option.Def)) return false;
            known |= CareStock.Known(option.Def);
        }
        return known;
    }

    static bool Names(IReadOnlyList<HediffDef> named, List<HediffDef> targets)
    {
        for (int i = 0; i < named.Count; i++)
        {
            if (targets.Contains(named[i])) return true;
        }
        return false;
    }
}

/// <summary>
/// A breathing rescue (Medical 0.29.0; the owner's card of 26 Nov 2026): a patient down or in bed whose breathing is at the rules' level
/// or under, and not held there by a ceiling, is given something that adds to breathing: a bridge until oxygen or an operation, never
/// a cure. Life-threatening, so it is looked at first. Nobody is woken with it: consciousness is not read.
/// </summary>
public class CareNeedWorker_Breathing : CareNeedWorker
{
    public override CareMode Mode => DosingMod.Settings.breathingMode;

    CareContext Care => new(def.seriousness, null, def.usesReserve);

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseIndex.BreathingOptions.Count > 0 && Patients.IsLyingDown(patient) && Struggling(patient, rules);

    /// <summary>Breathing at the level or under, on a flesh body, with no condition capping it there.</summary>
    public static bool Struggling(Pawn patient, PhobosDosingRulesDef rules)
    {
        if (!patient.RaceProps.IsFlesh) return false;
        float breathing = patient.health.capacities.GetLevel(PawnCapacityDefOf.Breathing);
        if (breathing > rules.breathingRescueLevel) return false;
        float ceiling = 1f;
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            List<PawnCapacityModifier>? mods = hediffs[i].CurStage?.capMods;
            if (mods == null) continue;
            for (int j = 0; j < mods.Count; j++)
            {
                if (mods[j].capacity == PawnCapacityDefOf.Breathing && mods[j].setMax < ceiling) ceiling = mods[j].setMax;
            }
        }
        return BreathingRescueRule.NeedsRescue(breathing, rules.breathingRescueLevel, ceiling);
    }

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        DoseService.DecideCare(doctor, patient, DoseNeed.Breathing, DoseIndex.BreathingOptions, Care, everyItem);

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseService.AnyCareAllowed(patient, DoseNeed.Breathing, DoseIndex.BreathingOptions, Care);

    public override string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        Wanted(patient, settings, rules) && NoneInStockButKnown(patient.Map, DoseIndex.BreathingOptions, null, DoseNeed.Breathing)
            ? "PhobosDosing_UnmetBreathing".Translate().Resolve()
            : null;
}

/// <summary>
/// Toxic exposure under way (Medical 0.30.0; the owner's card of 26 Nov 2026): a patient down or in bed whose toxic buildup has begun
/// and who is still in its source is given something that keeps toxins out. People on their feet take their own
/// (<see cref="ExposureService"/>). A minor need: nothing addictive, nothing rare, and never below the player's reserve.
/// </summary>
public class CareNeedWorker_Toxic : CareNeedWorker
{
    public override CareMode Mode => DosingMod.Settings.toxicMode;

    CareContext Care => new(def.seriousness, null, def.usesReserve);

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseIndex.ToxicOptions.Count > 0 && Patients.IsLyingDown(patient) && ExposureService.ToxicNow(patient, rules);

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        DoseService.DecideCare(doctor, patient, DoseNeed.Toxic, DoseIndex.ToxicOptions, Care, everyItem);

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseService.AnyCareAllowed(patient, DoseNeed.Toxic, DoseIndex.ToxicOptions, Care);
}

/// <summary>
/// Caught in vacuum (Medical 0.30.0; the owner's card of 26 Nov 2026): the person takes a vacuum-resisting dose they carry, on the spot.
/// No doctor goes into vacuum for it, so it is never a doctor's work; the giver is <see cref="Jobs.JobGiver_PhobosVacuumDose"/>, ahead of
/// the game's own search for air. On the table for its switch and the load line.
/// </summary>
public class CareNeedWorker_Vacuum : CareNeedWorker
{
    public override CareMode Mode => DosingMod.Settings.vacuumRescue ? CareMode.DoctorsAndSelf : CareMode.Off;

    public override bool DosedByDoctors => false;
}

/// <summary>Pain, and at extreme pain a sedative: the dosing part's first need, as it has worked since Emergency Dosing.</summary>
public class CareNeedWorker_Pain : CareNeedWorker
{
    static readonly CareContext Care = new(CareSeriousness.Serious);

    public override CareMode Mode => DosingMod.Settings.selfMedication ? CareMode.DoctorsAndSelf : CareMode.DoctorsOnly;

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => DoseService.NeedOf(patient, rules) != DoseNeed.None;

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem)
    {
        DoseNeed need = DoseService.NeedOf(patient, PhobosDosingRulesDef.Current);
        return need == DoseNeed.None ? null : DoseService.DecideCare(doctor, patient, need, DoseIndex.Options, Care, everyItem);
    }

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        DoseNeed need = DoseService.NeedOf(patient, rules);
        return need != DoseNeed.None && DoseService.AnyCareAllowed(patient, need, DoseIndex.Options, Care);
    }

    public override string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        Wanted(patient, settings, rules) && NoneInStockButKnown(patient.Map, DoseIndex.Options, null, DoseNeed.PainRelief, settings.recreationalForPain)
            ? "PhobosDosing_UnmetPain".Translate().Resolve()
            : null;
}

/// <summary>A disease that is winning the race against the patient's immunity: a drug that speeds immunity up.</summary>
public class CareNeedWorker_DiseaseDrug : CareNeedWorker
{
    static readonly CareContext Care = new(CareSeriousness.LifeThreatening);

    public override CareMode Mode => DosingMod.Settings.immunityDosing ? CareMode.DoctorsOnly : CareMode.Off;

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => DoseService.LosingToDisease(patient, rules);

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        DoseService.DecideCare(doctor, patient, DoseNeed.Immunity, DoseIndex.ImmunityOptions, CareFor(patient), everyItem);

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseService.AnyCareAllowed(patient, DoseNeed.Immunity, DoseIndex.ImmunityOptions, CareFor(patient));

    /// <summary>The diseases winning, so a drug is matched to their class (Medical 0.33.0). Asked only for a patient already wanted.</summary>
    static CareContext CareFor(Pawn patient)
    {
        var losing = new List<HediffDef>();
        DoseService.LosingDiseases(patient, PhobosDosingRulesDef.Current, losing);
        return new CareContext(Care.Seriousness, losing);
    }

    public override string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseIndex.ImmunityOptions.Count > 0 && Wanted(patient, settings, rules) && NoneInStockButKnown(patient.Map, DoseIndex.ImmunityOptions, null, DoseNeed.Immunity)
            ? "PhobosDosing_UnmetDisease".Translate().Resolve()
            : null;
}

/// <summary>
/// A cure (Medical 0.28.0): the patient, down or in bed, has a condition an item removes outright. A cure works at once, so it is given
/// where the patient lies. How serious it is goes by the condition: life-threatening when it can kill.
/// </summary>
public class CareNeedWorker_Cure : CareNeedWorker
{
    static readonly List<HediffDef> targets = new();

    public override CareMode Mode => DosingMod.Settings.cureMode;

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseIndex.CureOptions.Count > 0 && Patients.IsLyingDown(patient) && Collect(patient) > 0;

    /// <summary>The patient's visible conditions that some indexed item removes, into the shared list.</summary>
    static int Collect(Pawn patient)
    {
        targets.Clear();
        HashSet<HediffDef> curable = DoseIndex.Curable;
        List<Hediff> hediffs = patient.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            HediffDef condition = hediffs[i].def;
            if (hediffs[i].Visible && condition.isBad && curable.Contains(condition) && !targets.Contains(condition)) targets.Add(condition);
        }
        return targets.Count;
    }

    CareContext Care => new(def.seriousness, targets, def.usesReserve);

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        Collect(patient) == 0 ? null : DoseService.DecideCare(doctor, patient, DoseNeed.Cure, DoseIndex.CureOptions, Care, everyItem);

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        Collect(patient) > 0 && DoseService.AnyCareAllowed(patient, DoseNeed.Cure, DoseIndex.CureOptions, Care);

    public override string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        // Someone on their feet with a curable condition is shown too: the cure waits for them to lie down, the shortage does not.
        if (DoseIndex.CureOptions.Count == 0 || Collect(patient) == 0 || !NoneInStockButKnown(patient.Map, DoseIndex.CureOptions, targets, DoseNeed.Cure)) return null;
        return "PhobosDosing_UnmetCure".Translate(targets[0].label.Named("CONDITION")).Resolve();
    }
}

/// <summary>
/// Prevention in an outbreak (Medical 0.28.0): a disease that strikes groups is on the map, and a patient down or in bed is
/// unprotected against it. People on their feet take their own (<see cref="PreventionService"/>). A minor need: nothing addictive,
/// nothing rare, and never below the player's reserve.
/// </summary>
public class CareNeedWorker_Prevention : CareNeedWorker
{
    static readonly List<HediffDef> targets = new();

    public override CareMode Mode => DosingMod.Settings.preventionMode;

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        DoseIndex.PreventionOptions.Count > 0 && Patients.IsLyingDown(patient) && PreventionService.Collect(patient, rules, targets) > 0;

    CareContext Care => new(def.seriousness, targets, def.usesReserve);

    public override DoseDecision? Decide(Pawn? doctor, Pawn patient, bool everyItem) =>
        PreventionService.Collect(patient, PhobosDosingRulesDef.Current, targets) == 0
            ? null
            : DoseService.DecideCare(doctor, patient, DoseNeed.Prevention, DoseIndex.PreventionOptions, Care, everyItem);

    public override bool AnyAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        PreventionService.Collect(patient, rules, targets) > 0 && DoseService.AnyCareAllowed(patient, DoseNeed.Prevention, DoseIndex.PreventionOptions, Care);
}

/// <summary>
/// The radiation medicines, on the table for its switch and its alert (Medical 0.28.0). Which medicine, when, and the doctor's job stay
/// with <see cref="RadiationMedicine"/> and its own work giver, unchanged: its choice is by dose and stage, not by an item's effect.
/// </summary>
public class CareNeedWorker_Radiation : CareNeedWorker
{
    public override CareMode Mode => RadiationPart.On ? CareMode.DoctorsOnly : CareMode.Off;

    public override bool DosedByDoctors => false;

    public override bool Wanted(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) => RadiationMedicine.Needed(patient) != null;

    public override string? Unmet(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules) =>
        RadiationMedicine.Needed(patient) is { } medicine && !DoseService.OnMap(patient.Map, medicine)
            ? "PhobosDosing_UnmetMedicine".Translate(medicine.label.Named("MEDICINE")).Resolve()
            : null;
}
