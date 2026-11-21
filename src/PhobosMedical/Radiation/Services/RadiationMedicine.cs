using System.Collections.Generic;
using Phobos.Framework.Health;
using Phobos.Medical;
using Phobos.Radiation.Defs;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radiation.Services;

/// <summary>
/// The three radiation medicines (D'thorga Medical 0.24.0; docs/development/radiation-model.md, RM5; the owner's choices of 22 Nov 2026:
/// all three, two research projects, Prussian blue from steel and neutroamine): what each does, and which a patient needs, so doctors
/// give them by themselves once a dose is known or estimated. The anti-sickness tablets ease the early sickness (no vomiting, eating
/// again); the marrow stimulant, started within a day of the course, raises the dose a person survives (after filgrastim: the Neupogen
/// label, for doses "greater than 2 gray"); Prussian blue speeds caesium out of the body about threefold (Radiogardase label: about 80
/// days down to 26 at Goiania).
/// </summary>
public static class RadiationMedicine
{
    static PhobosRadiationRulesDef Rules => PhobosRadiationRulesDef.Current;

    public static bool OnAntiemetic(Pawn pawn) => pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_OnAntiemetic);

    /// <summary>From the start: Prussian blue shortens the body's half-life for what is inside.</summary>
    internal static void Start() =>
        Contamination.InsideHalfLife = pawn => Rules.insideHalfLifeDays
            * (pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_OnPrussianBlue) ? Rules.prussianBlueHalfLifeFactor : 1f);

    /// <summary>From the step: the tick a marrow stimulant was first in the body during this course.</summary>
    public static void NoteMarrowStimulant(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        if (dose.InCourse && dose.marrowStart < 0 && pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_OnMarrowStimulant))
        {
            dose.marrowStart = Find.TickManager.TicksGame;
            if (Seen.Wants("radiation.medicine", pawn, "marrow")) Seen.Note("radiation.medicine", pawn, MarrowInTime(dose) ? "in time" : "too late", "marrow");
        }
    }

    /// <summary>Whether the marrow stimulant was started within the rules' day of the course beginning.</summary>
    public static bool MarrowInTime(Hediff_PhobosRadiationDose dose) =>
        dose.marrowStart >= 0 && dose.courseStart >= 0 && dose.marrowStart - dose.courseStart <= Rules.marrowStimulantWithinDays * GenDate.TicksPerDay;

    /// <summary>How far the marrow stimulant moves this person's lethal dose, in grays.</summary>
    public static double MarrowMoves(Hediff_PhobosRadiationDose dose) => MarrowInTime(dose) ? Rules.marrowStimulantMoves : 0;

    /// <summary>The medicine this patient needs now, or null: the first of anti-sickness tablets, the marrow stimulant, Prussian blue.</summary>
    public static ThingDef? Needed(Pawn patient)
    {
        if (!RadiationPart.On || patient.Dead || RadiationService.DoseOf(patient, create: false) is not { } dose) return null;
        HediffSet set = patient.health.hediffSet;
        if (set.HasHediff(RadiationDefOf.PhobosRadiation_EarlySickness) && !OnAntiemetic(patient) && Researched(RadiationDefOf.PhobosRadiation_Antiemetic))
        {
            return RadiationDefOf.PhobosRadiation_Antiemetic;
        }
        if (dose.InCourse && dose.marrowStart < 0 && KnownAbove(dose, Rules.marrowStimulantFromGrays)
            && Find.TickManager.TicksGame - dose.courseStart <= Rules.marrowStimulantWithinDays * GenDate.TicksPerDay
            && !set.HasHediff(RadiationDefOf.PhobosRadiation_OnMarrowStimulant) && Researched(RadiationDefOf.PhobosRadiation_MarrowStimulant))
        {
            return RadiationDefOf.PhobosRadiation_MarrowStimulant;
        }
        if (RadiationPart.Settings.contamination && dose.inside >= Rules.prussianBlueFromInside
            && !set.HasHediff(RadiationDefOf.PhobosRadiation_OnPrussianBlue) && Researched(RadiationDefOf.PhobosRadiation_PrussianBlue))
        {
            return RadiationDefOf.PhobosRadiation_PrussianBlue;
        }
        return null;
    }

    static bool Researched(ThingDef medicine) => medicine.recipeMaker?.researchPrerequisite is not { } project || project.IsFinished;

    /// <summary>Whether a doctor can tell the dose is above a figure: exactly once measured, else from vomiting within the hours the
    /// estimate gives for it (a first vomit within 5 hours means 2 Gy or more).</summary>
    static bool KnownAbove(Hediff_PhobosRadiationDose dose, float grays)
    {
        if (!RadiationPart.Settings.estimate || RimatomicsDose.GeigerKnown) return dose.peak >= grays;
        if (dose.firstVomit < 0) return false;
        double hours = (dose.firstVomit - dose.courseStart) / (double)GenDate.TicksPerHour;
        return Phobos.Radiation.Rules.RadiationRule.EstimateFromVomiting(hours, true, hours).low >= grays;
    }

    /// <summary>The player's people with a radiation medicine waiting, on a map.</summary>
    public static IEnumerable<Pawn> Patients(Map map)
    {
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (Phobos.Framework.Health.Patients.IsPlayersPerson(pawn) && pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_Dose) && Needed(pawn) != null)
            {
                yield return pawn;
            }
        }
    }

    /// <summary>A doctor's job to give this patient what they need, or null; with <paramref name="explain"/>, says why not.</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient, bool explain)
    {
        if (patient == doctor || Needed(patient) is not { } medicine) return null;
        if (!Phobos.Framework.Health.Patients.CareAllows(patient, MedicalCareCategory.HerbalOrWorse, requireMedicine: true))
        {
            if (explain) JobFailReason.Is("PhobosRadiation_RefusalCare".Translate());
            return null;
        }
        if (!FieldCare.CanReserve(doctor, patient, explain)) return null;
        Thing? item = GenClosest.ClosestThingReachable(doctor.Position, doctor.Map, ThingRequest.ForDef(medicine), PathEndMode.ClosestTouch,
            TraverseParms.For(doctor), 9999f, thing => !thing.IsForbidden(doctor) && doctor.CanReserve(thing));
        if (item == null)
        {
            if (explain) JobFailReason.Is("PhobosRadiation_NoMedicine".Translate(medicine.label.Named("MEDICINE")));
            return null;
        }
        Job job = JobMaker.MakeJob(RadiationDefOf.PhobosRadiation_GiveMedicine, item, patient);
        job.count = 1;
        if (Seen.Wants("radiation.medicine", patient, "given")) Seen.Note("radiation.medicine", patient, medicine.defName, "given");
        return job;
    }
}
