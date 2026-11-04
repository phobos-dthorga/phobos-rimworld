using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
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
/// Reads the patient and the doctor, applies the game-free rules, and makes the job. Nothing here mutates game state.
/// </summary>
public static class DoseService
{
    static readonly List<DoseCandidate> candidates = new();

    /// <summary>Resting (downed or in bed) and in enough pain or losing to a disease, under the player's care at the setting's level or better.</summary>
    public static bool IsPatient(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!IsPlayersPerson(pawn) || !CareAllows(pawn, settings) || pawn.InAggroMentalState) return false;
        return NeedsDose(pawn, settings, rules);
    }

    /// <summary>Either reason a doctor would come: pain, or a disease that is winning.</summary>
    public static bool NeedsDose(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules) =>
        NeedOf(pawn, rules) != DoseNeed.None || (settings.immunityDosing && LosingToDisease(pawn, rules));

    /// <summary>
    /// A resting patient with a disease that can kill and that is winning the race against their immunity, read from the game's own rates: every
    /// comp that moves the disease's severity, tending included, and the immunity record the game itself ticks.
    /// </summary>
    public static bool LosingToDisease(Pawn pawn, PhobosDosingRulesDef rules)
    {
        if (!pawn.Downed && !pawn.InBed()) return false;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is not HediffWithComps disease || !disease.Visible || disease.def.lethalSeverity <= 0f || !disease.def.PossibleToDevelopImmunityNaturally()) continue;
            HediffComp_Immunizable? immunizable = disease.TryGetComp<HediffComp_Immunizable>();
            if (immunizable == null || immunizable.FullyImmune) continue;
            float severityPerDay = 0f;
            for (int c = 0; c < disease.comps.Count; c++)
            {
                if (disease.comps[c] is HediffComp_SeverityModifierBase modifier) severityPerDay += modifier.SeverityChangePerDay();
            }
            float immunityPerDay = (pawn.health.immunity.GetImmunityRecord(disease.def)?.ImmunityChangePerTick(pawn, true, disease) ?? 0f) * GenDate.TicksPerDay;
            if (ImmunityRule.Losing(disease.Severity, disease.def.lethalSeverity, severityPerDay, immunizable.Immunity, immunityPerDay, rules.immunityRaceMarginDays)) return true;
        }
        return false;
    }

    /// <summary>Spawned, alive, humanlike, past babyhood, and the player's own or the player's guest or prisoner.</summary>
    static bool IsPlayersPerson(Pawn pawn) =>
        pawn.Spawned && !pawn.Dead && pawn.RaceProps.Humanlike && pawn.health?.hediffSet != null
        && !DevelopmentalStageExtensions.Baby(pawn.DevelopmentalStage)
        && (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer);

    static bool CareAllows(Pawn pawn, DosingSettings settings)
    {
        MedicalCareCategory care = pawn.playerSettings?.medCare ?? MedicalCareCategory.NoCare;
        return care != MedicalCareCategory.NoCare && care != MedicalCareCategory.NoMeds && care >= settings.minimumCare;
    }

    /// <summary>
    /// The reason a patient who needs a dose is not one, for the doctor's right-click. Says nothing about a pawn in no need, or
    /// every right-click on anyone would carry a refusal.
    /// </summary>
    public static void ExplainNotPatient(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!IsPlayersPerson(pawn) || !NeedsDose(pawn, settings, rules)) return;
        if (!CareAllows(pawn, settings)) JobFailReason.Is("PhobosDosing_RefusalCare".Translate());
        else if (pawn.InAggroMentalState) JobFailReason.Is("PhobosDosing_RefusalMentalState".Translate());
    }

    public static DoseNeed NeedOf(Pawn pawn, PhobosDosingRulesDef rules) =>
        DoseNeedRule.Need(pawn.health.hediffSet.PainTotal, pawn.Downed || pawn.InBed(), rules.painThreshold, rules.sedationPainThreshold);

    /// <summary>
    /// The decision for a patient. With a doctor, items come from the doctor's inventory first, then the nearest reachable
    /// unforbidden stack; without one (the dev action), from the map around the patient.
    /// </summary>
    public static DoseDecision Decide(Pawn? doctor, Pawn patient)
    {
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        DosingSettings settings = DosingMod.Settings;
        DoseNeed painNeed = NeedOf(patient, rules);
        DoseDecision? pain = painNeed == DoseNeed.None ? null : DecideFor(doctor, patient, painNeed, DoseIndex.Options, rules, settings);
        if (pain?.Chosen != null) return pain;
        // Held back from pain relief, or nothing in reach for it: the disease may still have a dose waiting.
        if (settings.immunityDosing && LosingToDisease(patient, rules))
        {
            DoseDecision immunity = DecideFor(doctor, patient, DoseNeed.Immunity, DoseIndex.ImmunityOptions, rules, settings);
            if (immunity.Chosen != null || pain == null) return immunity;
        }
        return pain ?? new DoseDecision { Need = DoseNeed.None };
    }

    static DoseDecision DecideFor(Pawn? doctor, Pawn patient, DoseNeed need, IReadOnlyList<DoseOption> options, PhobosDosingRulesDef rules, DosingSettings settings)
    {
        var decision = new DoseDecision { Need = need };
        bool bleeding = patient.health.hediffSet.BleedRateTotal > 0f;
        var limits = new DoseLimits(rules.redoseBelowFraction, rules.overdoseLimit, rules.capacityFloor, settings.recreationalForPain, settings.ignoreAversionForMedical, rules.bleedHoldRate);
        // A medical painkiller still working blocks every other painkiller: relief and costs stack. A sedative may still
        // follow at extreme pain; it is checked against its own conditions only.
        float painkillerActive = PainkillerActive(options, patient.health.hediffSet, false);
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            bool present = IsPresent(doctor, patient, option.Def);
            DoseRefusal refusal = DoseRefusal.None;
            Thing? item = null;
            if (present)
            {
                // The rules first and the path search last: reach is the only costly check, and most options never need it.
                refusal = Evaluate(option, patient, decision.Need, painkillerActive, limits);
                if (refusal == DoseRefusal.None) item = FindItem(doctor, patient, option.Def);
            }
            candidates.Add(new DoseCandidate(option.Effect, item != null));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, need, bleeding, rules.Weights)));
        }
        int chosen = DoseChoiceRule.Choose(candidates, need, bleeding, rules.Weights);
        if (chosen >= 0) decision.Chosen = decision.Verdicts[chosen];
        return decision;
    }

    /// <summary>
    /// The safety rule for one item and one patient, whoever is giving it: an earlier dose still working, a dangerous stage,
    /// the capacity floors, overdose and the patient's willingness. The one place both the doctor and the self path ask.
    /// </summary>
    static DoseRefusal Evaluate(DoseOption option, Pawn patient, DoseNeed need, float painkillerActive, DoseLimits limits)
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
        return DoseSafetyRule.Check(option.Effect, need, activeFraction, reachesDanger, state, limits);
    }

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
        if (!pawn.IsFreeColonist && !(settings.guestsSelfMedicate && pawn.HostFaction == Faction.OfPlayer && !pawn.IsPrisoner && !pawn.IsSlave)) return SelfBlock.NotEligible;
        if (pawn.health?.hediffSet == null || !pawn.Spawned) return SelfBlock.NotEligible;
        float pain = pawn.health.hediffSet.PainTotal;
        if (pain < SelfDoseRule.Threshold(rules.selfPainThreshold, DrugDesire(pawn), rules.selfInterestOffset, rules.selfFascinationOffset)) return SelfBlock.NotEnoughPain;
        if (pawn.DevelopmentalStage != DevelopmentalStage.Adult) return SelfBlock.NotEligible;
        if (pawn.Downed || pawn.Drafted || pawn.InMentalState || !pawn.Awake()) return SelfBlock.NotUpAndAbout;
        if (pawn.InBed() || HealthAIUtility.ShouldSeekMedicalRest(pawn)) return SelfBlock.UnderMedicalRest;
        if (pawn.IsTeetotaler()) return SelfBlock.Teetotaler;
        return SelfBlock.None;
    }

    static int DrugDesire(Pawn pawn) => pawn.story?.traits?.DegreeOfTrait(TraitDefOf.DrugDesire) ?? 0;

    /// <summary>
    /// What this pawn would take for themself, whatever their state: the same safety and the same choice as a doctor's, with the
    /// drug policy as the gate. Any painkiller still working blocks the next, recreational ones too, or highs would stack.
    /// </summary>
    public static DoseDecision SelfDecide(Pawn pawn)
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
            if (present)
            {
                refusal = PolicyPermits(pawn, policy, option, desire) ? Evaluate(option, pawn, decision.Need, painkillerActive, limits) : DoseRefusal.PolicyForbids;
                if (refusal == DoseRefusal.None) item = FindOwnItem(pawn, option.Def);
            }
            candidates.Add(new DoseCandidate(option.Effect, item != null));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, bleeding, rules.Weights)));
        }
        int chosen = DoseChoiceRule.Choose(candidates, decision.Need, bleeding, rules.Weights);
        if (chosen >= 0) decision.Chosen = decision.Verdicts[chosen];
        return decision;
    }

    /// <summary>The game's own ingest job for what this pawn would take, or null.</summary>
    public static Job? SelfJob(Pawn pawn)
    {
        if (SelfBlockFor(pawn, DosingMod.Settings, PhobosDosingRulesDef.Current) != SelfBlock.None) return null;
        return SelfDecide(pawn).Chosen is DoseVerdict chosen && chosen.Item != null ? DrugAIUtility.IngestAndTakeToInventoryJob(chosen.Item, pawn, 1) : null;
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

    /// <summary>The job for a doctor to give the chosen item, or null with the first refusal set as the job fail reason.</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient)
    {
        DoseDecision decision = Decide(doctor, patient);
        if (decision.Chosen is DoseVerdict chosen && chosen.Item != null)
        {
            Job job = JobMaker.MakeJob(DosingDefOf.PhobosDose, chosen.Item, patient);
            job.count = 1;
            return job;
        }
        JobFailReason.Is(ReasonText(decision));
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
            if (!thing.Spawned || thing.IsForbidden(seeker) || thing.IsBurning()) continue;
            if (doctor != null && (!doctor.CanReserve(thing, 10, 1) || !doctor.CanReach(thing, PathEndMode.ClosestTouch, Danger.Some))) continue;
            float distance = (thing.Position - seeker.Position).LengthHorizontalSquared;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = thing;
            }
        }
        return best;
    }
}
