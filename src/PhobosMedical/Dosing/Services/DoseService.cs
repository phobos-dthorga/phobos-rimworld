using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Needs;
using Phobos.Dosing.Rules;
using Phobos.Framework.Health;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Services;

/// <summary>Why a pawn is not looking for a painkiller for themself.</summary>
public enum SelfBlock
{
    None,
    Disabled,
    NotEligible,
    NotEnoughPain,
    NotUpAndAbout,
    UnderMedicalRest,
    Teetotaler,
    /// <summary>Pain is no hardship to this pawn: the game's own pain thought is nullified for them (a masochist, an idealised-pain belief).</summary>
    PainWelcome,
}

/// <summary>One line of the decision: an indexed item, where it was found, and the rule's verdict.</summary>
public readonly struct DoseVerdict
{
    public readonly DoseOption Option;
    public readonly Thing? Item;
    /// <summary>Some of it exists on the map or on the doctor, reachable or not.</summary>
    public readonly bool Present;
    public readonly DoseRefusal Refusal;
    public readonly float Score;

    public DoseVerdict(DoseOption option, Thing? item, bool present, DoseRefusal refusal, float score)
    {
        Option = option;
        Item = item;
        Present = present;
        Refusal = refusal;
        Score = score;
    }
}

/// <summary>The decision for one patient: the chosen item, or every reason none was chosen.</summary>
public sealed class DoseDecision
{
    public DoseNeed Need;
    public DoseVerdict? Chosen;
    public readonly List<DoseVerdict> Verdicts = new();
}

/// <summary>
/// What a care need hands the decision besides the items (Medical 0.28.0): how serious it is, the conditions an item must remove or
/// stop to answer it (none for pain and immunity, where every indexed item answers), and whether it keeps the player's reserve.
/// </summary>
public readonly struct CareContext
{
    public readonly CareSeriousness Seriousness;
    public readonly List<HediffDef>? Targets;
    public readonly bool UsesReserve;

    public CareContext(CareSeriousness seriousness, List<HediffDef>? targets = null, bool usesReserve = false)
    {
        Seriousness = seriousness;
        Targets = targets;
        UsesReserve = usesReserve;
    }
}

/// <summary>
/// Reads the patient and the doctor, applies the game-free rules, and makes the job. Nothing here mutates game state. Since Medical
/// 0.28.0 the reasons for a dose are the rows of the care needs table (<see cref="PhobosCareNeedDef"/>): this class is the engine
/// each row's worker calls, with the one safety rule and the one choice for all of them.
/// </summary>
public static class DoseService
{
    static readonly List<DoseCandidate> candidates = new();

    /// <summary>Resting (downed or in bed) and in enough pain or losing to a disease, under the player's care at the setting's level or better.</summary>
    public static bool IsPatient(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!Patients.IsPlayersPerson(pawn) || !Patients.CareAllows(pawn, settings.minimumCare, true) || pawn.InAggroMentalState) return false;
        return NeedsDose(pawn, settings, rules);
    }

    /// <summary>Any reason a doctor would come with a dose: a care need that is switched on, given through the dosing job, and wanted now.</summary>
    public static bool NeedsDose(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        IReadOnlyList<PhobosCareNeedDef> needs = CareNeeds.Ordered;
        for (int i = 0; i < needs.Count; i++)
        {
            CareNeedWorker worker = needs[i].Worker;
            if (worker.DosedByDoctors && CareModeRule.DoctorsMay(worker.Mode) && worker.Wanted(pawn, settings, rules)) return true;
        }
        return false;
    }

    /// <summary>One of the player's people, under care at the setting's level or better, and not raging: who the dosing part looks after at all.</summary>
    public static bool IsEligible(Pawn pawn, DosingSettings settings) =>
        Patients.IsPlayersPerson(pawn) && Patients.CareAllows(pawn, settings.minimumCare, true) && !pawn.InAggroMentalState;

    /// <summary>
    /// A patient in bed with a disease that can kill and that is winning the race against their immunity, read from the game's own rates: every
    /// comp that moves the disease's severity, tending included, and the immunity record the game itself ticks. A dose against a disease works
    /// over days, so it waits for the bed (Medical 0.18.0, FieldCareRule): no doctor walks out to someone on the ground the carrier is about to lift.
    /// </summary>
    public static bool LosingToDisease(Pawn pawn, PhobosDosingRulesDef rules)
    {
        if (!FieldCareRule.MayActNow(CareUrgency.WaitsForBed, pawn.InBed())) return false;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is not HediffWithComps disease || !disease.Visible || disease.def.lethalSeverity <= 0f || !disease.def.PossibleToDevelopImmunityNaturally()) continue;
            HediffComp_Immunizable? immunizable = disease.TryGetComp<HediffComp_Immunizable>();
            if (immunizable == null || immunizable.FullyImmune) continue;
            float severityPerDay = ConditionDanger.SeverityPerDay(disease);
            float immunityPerDay = (pawn.health.immunity.GetImmunityRecord(disease.def)?.ImmunityChangePerTick(pawn, true, disease) ?? 0f) * GenDate.TicksPerDay;
            if (ImmunityRule.Losing(disease.Severity, disease.def.lethalSeverity, severityPerDay, immunizable.Immunity, immunityPerDay, rules.immunityRaceMarginDays)) return true;
        }
        return false;
    }

    /// <summary>
    /// The reason a patient who needs a dose is not one, for the doctor's right-click. Says nothing about a pawn in no need, or
    /// every right-click on anyone would carry a refusal.
    /// </summary>
    public static void ExplainNotPatient(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!Patients.IsPlayersPerson(pawn) || !NeedsDose(pawn, settings, rules)) return;
        if (!Patients.CareAllows(pawn, settings.minimumCare, true)) JobFailReason.Is("PhobosDosing_RefusalCare".Translate());
        else if (pawn.InAggroMentalState) JobFailReason.Is("PhobosDosing_RefusalMentalState".Translate());
    }

    public static DoseNeed NeedOf(Pawn pawn, PhobosDosingRulesDef rules) =>
        DoseNeedRule.Need(pawn.health.hediffSet.PainTotal, pawn.Downed || pawn.InBed(), rules.painThreshold, rules.sedationPainThreshold);

    /// <summary>
    /// The decision for a patient. With a doctor, items come from the doctor's inventory first, then the nearest reachable
    /// unforbidden stack; without one (the dev action), from the map around the patient. Reach is looked for only for the item the
    /// rule would choose, and the next best when that one is out of reach; <paramref name="everyItem"/> looks for all of them, so
    /// the dev action can say which are out of reach.
    /// </summary>
    public static DoseDecision Decide(Pawn? doctor, Pawn patient, bool everyItem = false)
    {
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        DosingSettings settings = DosingMod.Settings;
        IReadOnlyList<PhobosCareNeedDef> needs = CareNeeds.Ordered;
        DoseDecision? first = null;
        // The needs in the table's order; the first with something to give decides. One held back, or with nothing in reach, lets the
        // next speak (pain held back: the disease may still have a dose waiting), and the first to refuse gives the reason shown.
        for (int i = 0; i < needs.Count; i++)
        {
            CareNeedWorker worker = needs[i].Worker;
            if (!worker.DosedByDoctors || !CareModeRule.DoctorsMay(worker.Mode) || !worker.Wanted(patient, settings, rules)) continue;
            DoseDecision? decision = worker.Decide(doctor, patient, everyItem);
            if (decision == null) continue;
            if (decision.Chosen != null) return decision;
            // The reason shown is the first need's that has one to give: a need with nothing of its own on the map (a cure not in
            // stock) must not hide why the painkiller that is here was held back.
            if (first == null || (!AnyPresent(first) && AnyPresent(decision))) first = decision;
        }
        return first ?? new DoseDecision { Need = DoseNeed.None };
    }

    static bool AnyPresent(DoseDecision decision)
    {
        for (int i = 0; i < decision.Verdicts.Count; i++)
        {
            if (decision.Verdicts[i].Present) return true;
        }
        return false;
    }

    /// <summary>The decision for one need over one of the index's lists: what a need's worker calls.</summary>
    public static DoseDecision DecideCare(Pawn? doctor, Pawn patient, DoseNeed need, IReadOnlyList<DoseOption> options, in CareContext care, bool everyItem) =>
        DecideFor(doctor, patient, need, options, PhobosDosingRulesDef.Current, DosingMod.Settings, everyItem, care);

    /// <summary>Whether the rules would let anyone give this patient something for one need, wherever the items are.</summary>
    public static bool AnyCareAllowed(Pawn patient, DoseNeed need, IReadOnlyList<DoseOption> options, in CareContext care) =>
        AnyAllowed(patient, need, options, PhobosDosingRulesDef.Current, DosingMod.Settings, care);

    static DoseLimits DoctorLimits(PhobosDosingRulesDef rules, DosingSettings settings) =>
        new(rules.redoseBelowFraction, rules.overdoseLimit, rules.capacityFloor, settings.recreationalForPain, settings.ignoreAversionForMedical, rules.bleedHoldRate);

    /// <summary>
    /// Whether the rules would let anyone give this patient a dose right now, wherever the items are: the same safety and the same
    /// choice as <see cref="Decide"/>, without looking for anything. The patient list asks this once per rebuild, so a patient in
    /// pain whose last dose is still working costs the doctors nothing until it wears off.
    /// </summary>
    public static bool AnyDoseAllowed(Pawn patient, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        IReadOnlyList<PhobosCareNeedDef> needs = CareNeeds.Ordered;
        for (int i = 0; i < needs.Count; i++)
        {
            CareNeedWorker worker = needs[i].Worker;
            if (worker.DosedByDoctors && CareModeRule.DoctorsMay(worker.Mode) && worker.Wanted(patient, settings, rules) && worker.AnyAllowed(patient, settings, rules)) return true;
        }
        return false;
    }

    /// <summary>Whether an item answers the need for this patient: every indexed item does for pain and immunity; a cure or a preventive
    /// must name one of the conditions the need is about.</summary>
    static bool Answers(DoseOption option, DoseNeed need, List<HediffDef>? targets)
    {
        if (targets == null) return true;
        IReadOnlyList<HediffDef> named = need == DoseNeed.Cure ? option.Cures : option.Prevents;
        for (int i = 0; i < named.Count; i++)
        {
            if (targets.Contains(named[i])) return true;
        }
        return false;
    }

    /// <summary>
    /// A dose still working blocks the next of its kind: among painkillers and among the drugs for immunity, where relief and costs
    /// stack. A cure and a preventive are each for their own condition, so one never blocks another.
    /// </summary>
    static float ActiveOfKind(DoseNeed need, IReadOnlyList<DoseOption> options, HediffSet set) =>
        need == DoseNeed.Cure || need == DoseNeed.Prevention ? 0f : PainkillerActive(options, set, false);

    static bool AnyAllowed(Pawn patient, DoseNeed need, IReadOnlyList<DoseOption> options, PhobosDosingRulesDef rules, DosingSettings settings, in CareContext care)
    {
        HediffSet set = patient.health.hediffSet;
        DoseLimits limits = DoctorLimits(rules, settings);
        float painkillerActive = ActiveOfKind(need, options, set);
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            if (!Answers(options[i], need, care.Targets)) continue;
            candidates.Add(new DoseCandidate(options[i].Effect, Evaluate(options[i], patient, need, painkillerActive, limits, care) == DoseRefusal.None));
        }
        return DoseChoiceRule.Choose(candidates, need, set.BleedRateTotal > 0f, rules.Weights) >= 0;
    }

    static DoseDecision DecideFor(Pawn? doctor, Pawn patient, DoseNeed need, IReadOnlyList<DoseOption> options, PhobosDosingRulesDef rules, DosingSettings settings, bool everyItem,
        in CareContext care)
    {
        var decision = new DoseDecision { Need = need };
        bool bleeding = patient.health.hediffSet.BleedRateTotal > 0f;
        DoseLimits limits = DoctorLimits(rules, settings);
        // A medical painkiller still working blocks every other painkiller: relief and costs stack. A sedative may still
        // follow at extreme pain; it is checked against its own conditions only.
        float painkillerActive = ActiveOfKind(need, options, patient.health.hediffSet);
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            // The candidates and the verdicts stay one for one: an item that does not answer this need is on neither list.
            if (!Answers(option, need, care.Targets)) continue;
            bool present = IsPresent(doctor, patient, option.Def);
            DoseRefusal refusal = DoseRefusal.None;
            Thing? item = null;
            // The rules first and the search last: reach is the only costly check, and most options never need it.
            if (present) refusal = Evaluate(option, patient, decision.Need, painkillerActive, limits, care);
            bool allowed = present && refusal == DoseRefusal.None;
            if (allowed && everyItem)
            {
                item = FindItem(doctor, patient, option.Def);
                allowed = item != null;
            }
            candidates.Add(new DoseCandidate(option.Effect, allowed));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, need, bleeding, rules.Weights)));
        }
        ChooseInReach(decision, doctor, patient, false, bleeding, rules.Weights);
        return decision;
    }

    /// <summary>
    /// The rule's choice among the options the safety rule allows, looking for an item only once the rule has chosen it. An item out
    /// of reach is struck out and the rule asked again, so the answer is the one it would give with every item looked for first:
    /// the best allowed option that someone can actually fetch. Reads and rewrites the shared candidate list.
    /// </summary>
    static void ChooseInReach(DoseDecision decision, Pawn? doctor, Pawn patient, bool own, bool bleeding, DoseWeights weights)
    {
        while (true)
        {
            int chosen = DoseChoiceRule.Choose(candidates, decision.Need, bleeding, weights);
            if (chosen < 0) return;
            DoseVerdict verdict = decision.Verdicts[chosen];
            if (verdict.Item == null)
            {
                Thing? item = own ? FindOwnItem(patient, verdict.Option.Def) : FindItem(doctor, patient, verdict.Option.Def);
                verdict = new DoseVerdict(verdict.Option, item, verdict.Present, verdict.Refusal, verdict.Score);
                decision.Verdicts[chosen] = verdict;
            }
            if (verdict.Item != null)
            {
                decision.Chosen = verdict;
                return;
            }
            candidates[chosen] = new DoseCandidate(verdict.Option.Effect, false);
        }
    }

    /// <summary>
    /// The safety rule for one item and one patient, whoever is giving it: an earlier dose still working, a dangerous stage,
    /// the capacity floors, overdose and the patient's willingness. The one place both the doctor and the self path ask.
    /// </summary>
    static DoseRefusal Evaluate(DoseOption option, Pawn patient, DoseNeed need, float painkillerActive, DoseLimits limits, in CareContext care)
    {
        HediffSet set = patient.health.hediffSet;
        float activeFraction = ActiveFraction(option, set);
        if (!option.Effect.Sedative && painkillerActive > activeFraction) activeFraction = painkillerActive;
        bool reachesDanger = false;
        for (int j = 0; j < option.Hediffs.Count; j++)
        {
            DoseHediff dose = option.Hediffs[j];
            float existing = set.GetFirstHediffOfDef(dose.Def)?.Severity ?? 0f;
            if (existing + dose.Severity >= dose.DangerSeverity) reachesDanger = true;
        }
        var state = new PatientState(patient.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness),
            patient.health.capacities.GetLevel(PawnCapacityDefOf.Breathing),
            set.GetFirstHediffOfDef(HediffDefOf.DrugOverdose)?.Severity ?? 0f, WillTake(patient, option.Def), set.BleedRateTotal);
        DoseRefusal refusal = DoseSafetyRule.Check(option.Effect, need, activeFraction, reachesDanger, state, limits);
        if (refusal != DoseRefusal.None) return refusal;
        // What the care needs add (Medical 0.28.0), after the dose itself has passed: someone already dependent is not begun on anything.
        bool beginsLethalDependency = option.LethalDependency != null && !set.HasHediff(option.LethalDependency);
        // The rare mark is the player's (a setting since Medical 0.29.0), so it is asked here and not fixed when the index is built.
        bool rare = CareSafetyRule.Rare(option.MarketValue, DosingMod.Settings.rareItemMarketValue);
        refusal = CareSafetyRule.Check(SeriousnessFor(option, need, care), option.Effect.Addictiveness, beginsLethalDependency, rare);
        if (refusal != DoseRefusal.None) return refusal;
        if (care.UsesReserve && patient.Map != null && !StockReserveRule.MayTake(InStock(patient.Map, option.Def), DosingMod.Settings.preventionReserve))
            return DoseRefusal.ReserveKept;
        return DoseRefusal.None;
    }

    /// <summary>How serious the need is for this item: a cure's is by the conditions this item would remove, life-threatening when one can kill.</summary>
    static CareSeriousness SeriousnessFor(DoseOption option, DoseNeed need, in CareContext care)
    {
        if (need != DoseNeed.Cure || care.Targets == null) return care.Seriousness;
        for (int i = 0; i < option.Cures.Count; i++)
        {
            HediffDef condition = option.Cures[i];
            if (care.Targets.Contains(condition) && ConditionDanger.DangerSeverity(condition) < ConditionDangerRule.NoDanger) return CareSafetyRule.CureSeriousness(true);
        }
        return care.Seriousness;
    }

    /// <summary>How many of an item lie on the map where the colony may use them: what the reserve is counted against.</summary>
    public static int InStock(Map map, ThingDef def)
    {
        List<Thing> things = map.listerThings.ThingsOfDef(def);
        int count = 0;
        for (int i = 0; i < things.Count; i++)
        {
            if (things[i].Spawned && !things[i].IsForbidden(Faction.OfPlayer)) count += things[i].stackCount;
        }
        return count;
    }

    /// <summary>Whether any of an item is on the map at all, forbidden or not: what an unmet need looks for.</summary>
    public static bool OnMap(Map? map, ThingDef def) => map != null && map.listerThings.ThingsOfDef(def).Count > 0;

    /// <summary>The largest share of a dose still working among the painkillers; recreational ones count only when asked.</summary>
    static float PainkillerActive(IReadOnlyList<DoseOption> options, HediffSet set, bool includeRecreational)
    {
        float most = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            if (option.Effect.Sedative || (option.Effect.Recreational && !includeRecreational)) continue;
            float active = ActiveFraction(option, set);
            if (active > most) most = active;
        }
        return most;
    }

    /// <summary>Why a pawn is not looking for a painkiller right now, or None if they are. Cheapest tests first: this runs for every humanlike choosing a job.</summary>
    public static SelfBlock SelfBlockFor(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!settings.selfMedication) return SelfBlock.Disabled;
        if (!MaySelfMedicate(pawn, settings)) return SelfBlock.NotEligible;
        if (pawn.health?.hediffSet == null || !pawn.Spawned) return SelfBlock.NotEligible;
        float pain = pawn.health.hediffSet.PainTotal;
        if (pain < SelfDoseRule.Threshold(rules.selfPainThreshold, DrugDesire(pawn), rules.selfInterestOffset, rules.selfFascinationOffset)) return SelfBlock.NotEnoughPain;
        if (pawn.DevelopmentalStage != DevelopmentalStage.Adult) return SelfBlock.NotEligible;
        if (pawn.Downed || pawn.Drafted || pawn.InMentalState || !pawn.Awake()) return SelfBlock.NotUpAndAbout;
        if (pawn.InBed() || HealthAIUtility.ShouldSeekMedicalRest(pawn)) return SelfBlock.UnderMedicalRest;
        if (pawn.IsTeetotaler()) return SelfBlock.Teetotaler;
        if (PainIsWelcome(pawn)) return SelfBlock.PainWelcome;
        return SelfBlock.None;
    }

    /// <summary>
    /// Who may fetch their own painkillers: free colonists; the colony's secure slaves only by setting (the owner's card of 23 Nov
    /// 2026, off by default: the game counts a secure slave as a free colonist, so before 0.26.0 they did it unasked); guests only by
    /// their own setting. Prisoners never.
    /// </summary>
    public static bool MaySelfMedicate(Pawn pawn, DosingSettings settings)
    {
        if (pawn.IsFreeNonSlaveColonist) return true;
        if (pawn.IsFreeColonist) return settings.slavesSelfMedicate;
        return settings.guestsSelfMedicate && pawn.HostFaction == Faction.OfPlayer && !pawn.IsPrisoner && !pawn.IsSlave;
    }

    static ThoughtDef? painThought;
    static bool lookedForPainThought;

    /// <summary>
    /// Whether pain is no hardship to this pawn, by effect rather than by trait name (owner's rule): the game's own pain thought is
    /// nullified for them. In the base game that is the Masochist trait, and with Ideology a belief that idealises pain; a modded trait
    /// that nullifies the thought counts too. Such a pawn doesn't fetch their own painkillers (owner, 7 Nov 2026); doctors still treat
    /// them in bed.
    /// </summary>
    static bool PainIsWelcome(Pawn pawn)
    {
        if (!lookedForPainThought)
        {
            painThought = DefDatabase<ThoughtDef>.GetNamedSilentFail("Pain");
            lookedForPainThought = true;
        }
        return painThought != null && ThoughtUtility.ThoughtNullified(pawn, painThought);
    }

    static int DrugDesire(Pawn pawn) => pawn.story?.traits?.DegreeOfTrait(TraitDefOf.DrugDesire) ?? 0;

    /// <summary>
    /// What this pawn would take for themself, whatever their state: the same safety and the same choice as a doctor's, with the
    /// drug policy as the gate. Any painkiller still working blocks the next, recreational ones too, or highs would stack.
    /// </summary>
    public static DoseDecision SelfDecide(Pawn pawn, bool everyItem = false)
    {
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        int desire = DrugDesire(pawn);
        var decision = new DoseDecision { Need = SelfDoseRule.Need(pawn.health.hediffSet.PainTotal, SelfDoseRule.Threshold(rules.selfPainThreshold, desire, rules.selfInterestOffset, rules.selfFascinationOffset)) };
        if (decision.Need == DoseNeed.None) return decision;
        IReadOnlyList<DoseOption> options = DoseIndex.Options;
        HediffSet set = pawn.health.hediffSet;
        bool bleeding = set.BleedRateTotal > 0f;
        // The policy has already decided about recreational drugs here, and a pawn's own refusal is never overridden. A higher floor than
        // a doctor's: this pawn is on their feet, and a dose must not drop them where they stand.
        var limits = new DoseLimits(rules.redoseBelowFraction, rules.overdoseLimit, rules.selfCapacityFloor, true, false, rules.bleedHoldRate);
        float painkillerActive = PainkillerActive(options, set, true);
        DrugPolicy? policy = pawn.drugs?.CurrentPolicy;
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            bool present = IsPresent(pawn, pawn, option.Def);
            DoseRefusal refusal = DoseRefusal.None;
            Thing? item = null;
            if (present) refusal = PolicyPermits(pawn, policy, option, desire) ? Evaluate(option, pawn, decision.Need, painkillerActive, limits, SelfPainCare) : DoseRefusal.PolicyForbids;
            bool allowed = present && refusal == DoseRefusal.None;
            if (allowed && everyItem)
            {
                item = FindOwnItem(pawn, option.Def);
                allowed = item != null;
            }
            candidates.Add(new DoseCandidate(option.Effect, allowed));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, bleeding, rules.Weights)));
        }
        ChooseInReach(decision, pawn, pawn, true, bleeding, rules.Weights);
        return decision;
    }

    static readonly CareContext SelfPainCare = new(CareSeriousness.Serious);

    /// <summary>The game's own ingest job for what this pawn would take, or null: a painkiller first, then a preventive in an outbreak.</summary>
    public static Job? SelfJob(Pawn pawn)
    {
        if (SelfBlockFor(pawn, DosingMod.Settings, PhobosDosingRulesDef.Current) == SelfBlock.None && SelfDecide(pawn).Chosen is DoseVerdict chosen && chosen.Item != null)
        {
            if (pawn.IsSlave) Medical.Seen.Note("medical.slave-self-dose", pawn, chosen.Option.Def.label);
            return DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1);
        }
        return PreventionService.SelfJob(pawn);
    }

    /// <summary>
    /// Who may take a preventive on their own, whatever the outbreak: the people who may fetch their own painkillers, adult, flesh, up
    /// and about, and not resting for medical reasons (those are the doctors'). Cheapest tests first.
    /// </summary>
    public static bool MayTakeOwnPreventive(Pawn pawn, DosingSettings settings) =>
        MaySelfMedicate(pawn, settings) && pawn.Spawned && pawn.health?.hediffSet != null && pawn.RaceProps.IsFlesh
        && pawn.DevelopmentalStage == DevelopmentalStage.Adult && !pawn.Downed && !pawn.Drafted && !pawn.InMentalState && pawn.Awake()
        && !pawn.InBed() && !HealthAIUtility.ShouldSeekMedicalRest(pawn);

    /// <summary>
    /// What this pawn would take against the outbreak diseases they are unprotected from (Medical 0.28.0): the doctors' safety and
    /// choice, the higher floor of someone on their feet, the reserve, and the drug policy where it can speak about the item.
    /// </summary>
    public static DoseDecision SelfPreventionDecide(Pawn pawn, List<HediffDef> targets, bool everyItem = false)
    {
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        var decision = new DoseDecision { Need = targets.Count == 0 ? DoseNeed.None : DoseNeed.Prevention };
        if (decision.Need == DoseNeed.None) return decision;
        IReadOnlyList<DoseOption> options = DoseIndex.PreventionOptions;
        var limits = new DoseLimits(rules.redoseBelowFraction, rules.overdoseLimit, rules.selfCapacityFloor, false, false, rules.bleedHoldRate);
        var care = new CareContext(CareSeriousness.Minor, targets, true);
        DrugPolicy? policy = pawn.drugs?.CurrentPolicy;
        int desire = DrugDesire(pawn);
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            if (!Answers(option, DoseNeed.Prevention, targets)) continue;
            bool present = IsPresent(pawn, pawn, option.Def);
            DoseRefusal refusal = DoseRefusal.None;
            Thing? item = null;
            if (present) refusal = PreventionPolicyPermits(pawn, policy, option, desire) ? Evaluate(option, pawn, DoseNeed.Prevention, 0f, limits, care) : DoseRefusal.PolicyForbids;
            bool allowed = present && refusal == DoseRefusal.None;
            if (allowed && everyItem)
            {
                item = FindOwnItem(pawn, option.Def);
                allowed = item != null;
            }
            candidates.Add(new DoseCandidate(option.Effect, allowed));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, DoseNeed.Prevention, false, rules.Weights)));
        }
        ChooseInReach(decision, pawn, pawn, true, false, rules.Weights);
        return decision;
    }

    /// <summary>
    /// The drug policy's word on a preventive. It has a tick only for a drug taken for pleasure or one that addicts; for those it decides
    /// as it does for a painkiller. A plain medicine it cannot allow or forbid, so the need's own setting is the gate. Something that
    /// is not a drug at all has no policy line and is simply taken.
    /// </summary>
    static bool PreventionPolicyPermits(Pawn pawn, DrugPolicy? policy, DoseOption option, int desire)
    {
        ThingDef def = option.Def;
        if (!def.IsDrug) return true;
        bool canSpeak = def.IsPleasureDrug || def.IsAddictiveDrug;
        if (!canSpeak) return true;
        if (policy == null) return false;
        DrugPolicyEntry entry = policy[def];
        return SelfDoseRule.PreventionPolicyAllows(canSpeak, SelfDoseRule.PolicyAllows(entry.allowedForJoy, entry.allowScheduled, desire));
    }

    /// <summary>Only a drug has a policy line, and asking the policy about anything else throws. The game gives guests no policy.</summary>
    static bool PolicyPermits(Pawn pawn, DrugPolicy? policy, DoseOption option, int desire)
    {
        ThingDef def = option.Def;
        if (!def.IsDrug) return false;
        if (policy == null) return SelfDoseRule.NoPolicyAllows(pawn.HostFaction == Faction.OfPlayer && pawn.Faction != Faction.OfPlayer, option.Effect.Recreational);
        DrugPolicyEntry entry = policy[def];
        return SelfDoseRule.PolicyAllows(entry.allowedForJoy, entry.allowScheduled, desire);
    }

    /// <summary>The pawn's own inventory, then the nearest reachable stack by the game's own tests for a drug a pawn fetches.</summary>
    static Thing? FindOwnItem(Pawn pawn, ThingDef def)
    {
        ThingOwner<Thing>? inventory = pawn.inventory?.innerContainer;
        if (inventory != null)
        {
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].def == def) return inventory[i];
            }
        }
        if (pawn.Map == null) return null;
        return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f,
            thing => thing.IngestibleNow && !thing.IsForbidden(pawn) && !thing.IsBurning() && pawn.CanReserve(thing, 10, 1) && thing.IsSociallyProper(pawn));
    }

    /// <summary>The job for a doctor to give the chosen item, or null. With <paramref name="explain"/> (the doctor's right-click), the
    /// first refusal is set as the reason the menu shows; an ordinary look for work builds no text nobody will read.</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient, bool explain = false)
    {
        DoseDecision decision = Decide(doctor, patient);
        if (decision.Chosen is DoseVerdict chosen && chosen.Item != null)
        {
            Job job = JobMaker.MakeJob(DosingDefOf.PhobosDose, chosen.Item, patient);
            job.count = 1;
            Medical.Seen.Note("medical.dose-job-made", patient, chosen.Option.Def.label);
            if (decision.Need == DoseNeed.Cure && Medical.Seen.Wants("medical.cure-job-made", patient)) Medical.Seen.Note("medical.cure-job-made", patient, chosen.Option.Def.label);
            if (decision.Need == DoseNeed.Breathing && Medical.Seen.Wants("medical.breathing-job-made", patient))
                Medical.Seen.Note("medical.breathing-job-made", patient, chosen.Option.Def.label);
            if (decision.Need == DoseNeed.Prevention && Medical.Seen.Wants("medical.prevention-job-made", patient))
                Medical.Seen.Note("medical.prevention-job-made", patient, chosen.Option.Def.label);
            return job;
        }
        if (explain) JobFailReason.Is(ReasonText(decision));
        return null;
    }

    static string ReasonText(DoseDecision decision)
    {
        DoseRefusal worst = DoseRefusal.None;
        string label = "";
        for (int i = 0; i < decision.Verdicts.Count; i++)
        {
            DoseVerdict verdict = decision.Verdicts[i];
            if (verdict.Present && verdict.Refusal > worst)
            {
                worst = verdict.Refusal;
                label = verdict.Option.Def.label;
            }
        }
        if (worst == DoseRefusal.None) return "PhobosDosing_RefusalNothingInReach".Translate();
        return ("PhobosDosing_Refusal" + worst).Translate(label.Named("DRUG"));
    }

    /// <summary>The largest share of a dose of this item still in the patient, over the conditions it gives.</summary>
    static float ActiveFraction(DoseOption option, HediffSet set)
    {
        float fraction = 0f;
        for (int i = 0; i < option.Hediffs.Count; i++)
        {
            DoseHediff dose = option.Hediffs[i];
            float existing = set.GetFirstHediffOfDef(dose.Def)?.Severity ?? 0f;
            if (dose.Severity > 0f && existing / dose.Severity > fraction) fraction = existing / dose.Severity;
        }
        return fraction;
    }

    /// <summary>The game's drug check says no to anything that is not a drug, so an ingestible with no drug comp is simply taken.</summary>
    static bool WillTake(Pawn patient, ThingDef def) => def.GetCompProperties<CompProperties_Drug>() == null || patient.CanTakeDrug(def);

    /// <summary>A cheap test that some of the item exists for this doctor, so a refusal can be named without any path search.</summary>
    static bool IsPresent(Pawn? doctor, Pawn patient, ThingDef def)
    {
        if (doctor?.inventory != null)
        {
            ThingOwner<Thing> inventory = doctor.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].def == def) return true;
            }
        }
        return patient.Map != null && patient.Map.listerThings.ThingsOfDef(def).Count > 0;
    }

    static Thing? FindItem(Pawn? doctor, Pawn patient, ThingDef def)
    {
        if (doctor?.inventory != null)
        {
            ThingOwner<Thing> inventory = doctor.inventory.innerContainer;
            for (int i = 0; i < inventory.Count; i++)
            {
                if (inventory[i].def == def) return inventory[i];
            }
        }
        Map map = patient.Map;
        if (map == null) return null;
        Pawn seeker = doctor ?? patient;
        List<Thing> things = map.listerThings.ThingsOfDef(def);
        Thing? best = null;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < things.Count; i++)
        {
            Thing thing = things[i];
            if (!thing.Spawned) continue;
            // Distance first: the reservation and reach checks are the costly ones, and only a stack nearer than the best so far can win.
            float distance = (thing.Position - seeker.Position).LengthHorizontalSquared;
            if (distance >= bestDistance || thing.IsForbidden(seeker) || thing.IsBurning()) continue;
            if (doctor != null && (!doctor.CanReserve(thing, 10, 1) || !doctor.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some))) continue;
            bestDistance = distance;
            best = thing;
        }
        return best;
    }
}
