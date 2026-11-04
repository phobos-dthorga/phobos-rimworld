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
    public readonly DoseRefusal Refusal;
    public readonly float Score;

    public DoseVerdict(DoseOption option, Thing? item, DoseRefusal refusal, float score)
    {
        Option = option;
        Item = item;
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
        if (!pawn.Spawned || pawn.Dead || !pawn.RaceProps.Humanlike || pawn.health?.hediffSet == null) return false;
        if (pawn.Faction != Faction.OfPlayer && pawn.HostFaction != Faction.OfPlayer) return false;
        MedicalCareCategory care = pawn.playerSettings?.medCare ?? MedicalCareCategory.NoCare;
        if (care == MedicalCareCategory.NoCare || care == MedicalCareCategory.NoMeds || care < settings.minimumCare) return false;
        if (pawn.InAggroMentalState) return false;
        return NeedOf(pawn, rules) != DoseNeed.None;
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
        candidates.Clear();
        for (int i = 0; i < options.Count; i++)
        {
            DoseOption option = options[i];
            Thing? item = FindItem(doctor, patient, option.Def);
            DoseRefusal refusal;
            if (item == null)
            {
                refusal = DoseRefusal.None;
            }
            else
            {
                float activeFraction = 0f;
                bool reachesDanger = false;
                for (int j = 0; j < option.Hediffs.Count; j++)
                {
                    DoseHediff dose = option.Hediffs[j];
                    float existing = set.GetFirstHediffOfDef(dose.Def)?.Severity ?? 0f;
                    if (dose.Severity > 0f && existing / dose.Severity > activeFraction) activeFraction = existing / dose.Severity;
                    if (existing + dose.Severity >= dose.DangerSeverity) reachesDanger = true;
                }
                var state = new PatientState(consciousness, breathing, overdose, patient.CanTakeDrug(option.Def));
                refusal = DoseSafetyRule.Check(option.Effect, decision.Need, activeFraction, reachesDanger, state, limits);
            }
            bool allowed = item != null && refusal == DoseRefusal.None;
            candidates.Add(new DoseCandidate(option.Effect, allowed));
            decision.Verdicts.Add(new DoseVerdict(option, item, refusal, DoseChoiceRule.Score(option.Effect, bleeding, rules.Weights)));
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
        bool anyItem = false;
        DoseRefusal worst = DoseRefusal.None;
        string label = "";
        for (int i = 0; i < decision.Verdicts.Count; i++)
        {
            DoseVerdict verdict = decision.Verdicts[i];
            if (verdict.Item == null) continue;
            anyItem = true;
            if (verdict.Refusal > worst)
            {
                worst = verdict.Refusal;
                label = verdict.Option.Def.label;
            }
        }
        if (!anyItem) return "PhobosDosing_RefusalNothingInReach".Translate();
        return ("PhobosDosing_Refusal" + worst).Translate(label);
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
