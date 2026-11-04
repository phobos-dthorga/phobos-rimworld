using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Services;

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

    /// <summary>Resting (downed or in bed) and in enough pain, under the player's care at the setting's level or better.</summary>
    public static bool IsPatient(Pawn pawn, DosingSettings settings, PhobosDosingRulesDef rules)
    {
        if (!IsPlayersPerson(pawn) || !CareAllows(pawn, settings) || pawn.InAggroMentalState) return false;
        return NeedOf(pawn, rules) != DoseNeed.None;
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
        if (!IsPlayersPerson(pawn) || NeedOf(pawn, rules) == DoseNeed.None) return;
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
        var decision = new DoseDecision { Need = NeedOf(patient, rules) };
        if (decision.Need == DoseNeed.None) return decision;
        IReadOnlyList<DoseOption> options = DoseIndex.Options;
        HediffSet set = patient.health.hediffSet;
        float consciousness = patient.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness);
        float breathing = patient.health.capacities.GetLevel(PawnCapacityDefOf.Breathing);
        float overdose = set.GetFirstHediffOfDef(HediffDefOf.DrugOverdose)?.Severity ?? 0f;
        bool bleeding = set.BleedRateTotal > 0f;
        var limits = new DoseLimits(rules.redoseBelowFraction, rules.overdoseLimit, rules.capacityFloor, settings.recreationalForPain, settings.ignoreAversionForMedical);
        // A medical painkiller still working blocks every other painkiller: relief and costs stack. A sedative may still
        // follow at extreme pain; it is checked against its own conditions only.
        float painkillerActive = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            if (option.Effect.Sedative || option.Effect.Recreational) continue;
            float active = ActiveFraction(option, set);
            if (active > painkillerActive) painkillerActive = active;
        }
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
                float activeFraction = ActiveFraction(option, set);
                if (!option.Effect.Sedative && painkillerActive > activeFraction) activeFraction = painkillerActive;
                bool reachesDanger = false;
                for (int j = 0; j < option.Hediffs.Count; j++)
                {
                    DoseHediff dose = option.Hediffs[j];
                    float existing = set.GetFirstHediffOfDef(dose.Def)?.Severity ?? 0f;
                    if (existing + dose.Severity >= dose.DangerSeverity) reachesDanger = true;
                }
                var state = new PatientState(consciousness, breathing, overdose, WillTake(patient, option.Def));
                refusal = DoseSafetyRule.Check(option.Effect, decision.Need, activeFraction, reachesDanger, state, limits);
                if (refusal == DoseRefusal.None) item = FindItem(doctor, patient, option.Def);
            }
            candidates.Add(new DoseCandidate(option.Effect, item != null));
            decision.Verdicts.Add(new DoseVerdict(option, item, present, refusal, DoseChoiceRule.Score(option.Effect, bleeding, rules.Weights)));
        }
        int chosen = DoseChoiceRule.Choose(candidates, decision.Need, bleeding, rules.Weights);
        if (chosen >= 0) decision.Chosen = decision.Verdicts[chosen];
        return decision;
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
        return ("PhobosDosing_Refusal" + worst).Translate(label);
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
