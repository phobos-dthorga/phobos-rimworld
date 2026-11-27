using System;
using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Medical;
using Phobos.Radiation.Defs;
using Phobos.Radiation.Rules;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Services;

/// <summary>
/// The radiation model's work (D'thorga Medical 0.21.0; docs/development/radiation-model.md, RM2 to RM4, RM7): takes doses, works out
/// repair, runs the course from the highest effective dose reached, judges care, and says what a doctor can tell. The rule decides;
/// this applies it. Runs from <see cref="Givers.HediffGiver_PhobosRadiation"/> every few seconds of game time, only for someone who has
/// taken a dose, or carries one of Rimatomics' conditions to convert.
/// </summary>
public static class RadiationService
{
    static PhobosRadiationRulesDef Rules => PhobosRadiationRulesDef.Current;

    static int Today => Find.TickManager.TicksGame / GenDate.TicksPerDay;

    /// <summary>A dose in grays for this person: recorded, and the course started if it crosses the threshold.</summary>
    public static void Receive(Pawn pawn, float grays, string? source)
    {
        if (pawn == null || pawn.Dead || grays <= 0f || pawn.health?.hediffSet == null || !pawn.RaceProps.IsFlesh) return;
        Hediff_PhobosRadiationDose dose = DoseOf(pawn, create: true)!;
        dose.AddDose(Today, grays);
        dose.lastSource = source;
        LateEffects.OnDose(pawn, dose, grays);
        // The effective dose, the course and the late effects are worked out at the next step, a few seconds on, not at every dose
        // (fault-read 25 Nov 2026, F3): Rimatomics doses every 15 ticks for each radioactive thing near a pawn.
        if (Seen.Wants("radiation.dose-taken", pawn)) Seen.Note("radiation.dose-taken", pawn, (grays * 1000f).ToString("0") + " mGy from " + (source ?? "unknown"));
    }

    public static Hediff_PhobosRadiationDose? DoseOf(Pawn pawn, bool create)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(RadiationDefOf.PhobosRadiation_Dose) is Hediff_PhobosRadiationDose found) return found;
        if (!create) return null;
        var made = (Hediff_PhobosRadiationDose)HediffMaker.MakeHediff(RadiationDefOf.PhobosRadiation_Dose, pawn);
        made.Severity = 0.0001f;
        pawn.health.AddHediff(made);
        return made;
    }

    /// <summary>
    /// The radiation model turned off: anyone in the middle of a course is let out of it (fault-read 25 Nov 2026, F2), their early sickness
    /// and illness taken away, rather than left at the stage they had reached for good. Their dose is kept, for if it is turned on again.
    /// </summary>
    public static void StandDown(Pawn pawn)
    {
        HediffSet set = pawn.health.hediffSet;
        bool early = set.HasHediff(RadiationDefOf.PhobosRadiation_EarlySickness), ill = set.HasHediff(RadiationDefOf.PhobosRadiation_Illness);
        if (!early && !ill) return;
        SetPresent(pawn, RadiationDefOf.PhobosRadiation_EarlySickness, false, 0f);
        SetPresent(pawn, RadiationDefOf.PhobosRadiation_Illness, false, 0f);
        if (DoseOf(pawn, create: false) is { InCourse: true } dose) dose.EndCourse(Today);
    }

    /// <summary>The step: repair, the course, the illness and the blood count.</summary>
    public static void Step(Pawn pawn)
    {
        if (!RadiationPart.On) return;
        RimatomicsConversion.ConvertOnce(pawn);
        if (DoseOf(pawn, create: false) is not { } dose) return;
        Refresh(pawn, dose);
        if (dose.InCourse) RunCourse(pawn, dose);
        Contamination.Step(pawn, dose, Rules.intervalTicks / (double)GenDate.TicksPerDay);
        RadiationMedicine.NoteMarrowStimulant(pawn, dose);
        LateEffects.GiveDue(pawn, dose);
        LateEffects.ForgetEndedPregnancy(pawn, dose);
    }

    /// <summary>The effective dose after repair, old doses folded into the part beyond repair, and the course started at the threshold: a
    /// first course from the effective dose, a later one only from fresh doses since the last ended (<see cref="RadiationRule.StartsCourse"/>).</summary>
    static void Refresh(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        int today = Today;
        var doses = new List<(double, double)>(dose.doseDays.Count);
        for (int i = dose.doseDays.Count - 1; i >= 0; i--)
        {
            double daysAgo = today - dose.doseDays[i];
            if (daysAgo > RadiationRule.FullyRepairedAfterDays)
            {
                dose.beyondRepair += (float)(dose.doseGrays[i] * RadiationRule.NeverRepaired);
                dose.doseDays.RemoveAt(i);
                dose.doseGrays.RemoveAt(i);
                continue;
            }
            doses.Add((daysAgo, dose.doseGrays[i]));
        }
        float effective = (float)RadiationRule.Effective(doses, dose.beyondRepair);
        dose.Severity = Math.Max(0.0001f, effective);
        if (dose.InCourse)
        {
            if (effective > dose.peak) dose.peak = effective;
        }
        else if (effective >= RadiationRule.CourseFrom)
        {
            // After a course has ended, only fresh doses start another (Medical 0.26.1); its height still follows the whole dose.
            bool hadCourse = dose.HadCourse;
            double fresh = hadCourse ? RadiationRule.Fresh(doses, today - dose.courseEndDay, dose.courseEndDayGrays) : 0;
            if (RadiationRule.StartsCourse(effective, fresh, hadCourse))
            {
                dose.StartCourse(Find.TickManager.TicksGame, Sensitivity(pawn, 1) < RadiationRule.VomitShare(effective));
                if (Seen.Wants("radiation.course", pawn, "started")) Seen.Note("radiation.course", pawn, effective.ToString("0.0") + " Gy", "started");
                if (hadCourse && Seen.Wants("radiation.course", pawn, "again"))
                {
                    Seen.Note("radiation.course", pawn, "fresh " + fresh.ToString("0.00") + " Gy, peak " + effective.ToString("0.00") + " Gy", "again");
                }
            }
            else if (Seen.Wants("radiation.course", pawn, "held"))
            {
                Seen.Note("radiation.course", pawn, "effective " + effective.ToString("0.00") + " Gy, fresh " + fresh.ToString("0.00") + " Gy", "held");
            }
        }
        LateEffects.Judge(pawn, dose);
    }

    static void RunCourse(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        PhobosRadiationRulesDef rules = Rules;
        int now = Find.TickManager.TicksGame;
        double days = (now - dose.courseStart) / (double)GenDate.TicksPerDay;
        double hoursToVomit = RadiationRule.HoursToVomit(dose.peak);
        if (dose.willVomit && dose.firstVomit < 0 && days * 24 >= hoursToVomit) dose.firstVomit = now;
        bool vomited = dose.firstVomit >= 0;
        double daysToVomit = vomited ? (dose.firstVomit - dose.courseStart) / (double)GenDate.TicksPerDay : 0;
        RadiationPhase phase = RadiationRule.Phase(dose.peak, days, vomited, daysToVomit);

        // The anti-sickness tablets ease the early sickness: no vomiting, eating again (its first stage).
        SetPresent(pawn, RadiationDefOf.PhobosRadiation_EarlySickness, phase == RadiationPhase.Early, RadiationMedicine.OnAntiemetic(pawn) ? 0.5f : 1f);
        if (phase == RadiationPhase.Early && Seen.Wants("radiation.course", pawn, "early")) Seen.Note("radiation.course", pawn, "early sickness", "early");

        if (days >= 2 && dose.bloodCount < 0 && pawn.InBed() && pawn.CurrentBed() is { Medical: true }) dose.bloodCount = now;

        if (phase != RadiationPhase.Illness)
        {
            if (phase == RadiationPhase.None) dose.EndCourse(Today);
            return;
        }
        double earlyEnds = (vomited ? daysToVomit : 0) + RadiationRule.EarlyDays(dose.peak);
        double intoIllness = days - earlyEnds - RadiationRule.WellDays(dose.peak);
        double care = Care(pawn, rules);
        double lethal = RadiationRule.LethalDose(RadiationPart.Preset, care, Sensitivity(pawn, 2)) + RadiationMedicine.MarrowMoves(dose);
        float severity = (float)RadiationRule.Severity(dose.peak, lethal, intoIllness, rules.recoveryPerDay);
        bool rising = intoIllness <= RadiationRule.DaysToHeight(dose.peak);
        if (RadiationRule.IllnessOver(rising, severity, rules.courseEndsBelow))
        {
            SetPresent(pawn, RadiationDefOf.PhobosRadiation_Illness, false, 0f);
            dose.EndCourse(Today);
            if (Seen.Wants("radiation.course", pawn, "recovered")) Seen.Note("radiation.course", pawn, "recovered", "recovered");
            return;
        }
        if (rising) dose.illnessHeight = Math.Max(dose.illnessHeight, severity);
        else severity = Math.Min(severity, dose.illnessHeight);
        SetPresent(pawn, RadiationDefOf.PhobosRadiation_Illness, true, Math.Max(0.01f, severity));
        if (Seen.Wants("radiation.course", pawn, "illness"))
        {
            Seen.Note("radiation.course", pawn, "illness " + severity.ToStringPercent() + ", care " + care.ToString("0.00") + ", lethal at "
                + lethal.ToString("0.0") + " Gy", "illness");
        }
    }

    /// <summary>
    /// The care a sick person is getting, 0 to 1 (RM5): tended (by tend quality), in bed, in a clean room, on fluids, on a medicine that
    /// speeds immunity. Supportive care, after the sources: fluids, antibiotics, rest and a clean room against infection.
    /// </summary>
    public static double Care(Pawn pawn, PhobosRadiationRulesDef rules)
    {
        double care = 0;
        if (pawn.health.hediffSet.GetFirstHediffOfDef(RadiationDefOf.PhobosRadiation_Illness) is HediffWithComps illness
            && illness.TryGetComp<HediffComp_TendDuration>() is { IsTended: true } tend)
        {
            care += rules.careTended * tend.tendQuality;
        }
        if (pawn.InBed())
        {
            care += rules.careInBed;
            if (pawn.GetRoom() is { } room && room.GetStat(RoomStatDefOf.Cleanliness) >= rules.cleanRoomFrom) care += rules.careClean;
        }
        if (OnDrip != null && pawn.health.hediffSet.HasHediff(OnDrip)) care += rules.careFluids;
        if (pawn.GetStatValue(StatDefOf.ImmunityGainSpeed) > 1.05f) care += rules.careImmunity;
        return care > 1 ? 1 : care;
    }

    static HediffDef? onDrip;
    static bool dripLooked;
    static HediffDef? OnDrip
    {
        get
        {
            if (!dripLooked)
            {
                dripLooked = true;
                onDrip = DefDatabase<HediffDef>.GetNamedSilentFail("PhobosMedical_OnDrip");
            }
            return onDrip;
        }
    }

    /// <summary>A fixed number from 0 to 1 for this person and purpose, the same every time: who they are, never a roll at the time.</summary>
    public static double Sensitivity(Pawn pawn, int purpose)
    {
        unchecked
        {
            uint h = (uint)(pawn.thingIDNumber * 2654435761u) ^ (uint)(purpose * 40503u);
            h ^= h >> 16;
            h *= 0x7feb352d;
            h ^= h >> 15;
            h *= 0x846ca68b;
            h ^= h >> 16;
            return (h % 100000u + 0.5) / 100000.0;
        }
    }

    static void SetPresent(Pawn pawn, HediffDef def, bool present, float severity)
    {
        Hediff? found = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (!present)
        {
            if (found != null) pawn.health.RemoveHediff(found);
            return;
        }
        if (found == null)
        {
            found = HediffMaker.MakeHediff(def, pawn);
            found.Severity = severity;
            pawn.health.AddHediff(found);
        }
        else found.Severity = severity;
    }

    // ---- What is shown (RM7) ----

    /// <summary>The dose line in the condition's tooltip: exact once Rimatomics' Geiger research is done (or with the estimate off), else
    /// a doctor's estimate as a band.</summary>
    public static string Describe(Hediff_PhobosRadiationDose dose)
    {
        int category = RadiationRule.ExposureCategory(dose.lifetime);
        if (!RadiationPart.Settings.estimate || RimatomicsDose.GeigerKnown)
        {
            return "PhobosRadiation_TipExact".Translate(dose.Effective.ToString("0.00").Named("EFFECTIVE"), dose.lifetime.ToString("0.00").Named("LIFETIME"),
                category.Named("CATEGORY")).Resolve();
        }
        if (!dose.InCourse) return "PhobosRadiation_TipUnknown".Translate(category.Named("CATEGORY")).Resolve();
        int now = Find.TickManager.TicksGame;
        (double low, double high) band;
        if (dose.bloodCount >= 0) band = RadiationRule.EstimateFromBloodCount(dose.peak);
        else
        {
            double hoursSince = (now - dose.courseStart) / (double)GenDate.TicksPerHour;
            double hoursToVomit = dose.firstVomit >= 0 ? (dose.firstVomit - dose.courseStart) / (double)GenDate.TicksPerHour : 0;
            band = RadiationRule.EstimateFromVomiting(hoursToVomit, dose.firstVomit >= 0, hoursSince);
        }
        return "PhobosRadiation_TipEstimate".Translate(band.low.ToString("0.#").Named("LOW"), band.high.ToString("0.#").Named("HIGH"),
            category.Named("CATEGORY")).Resolve();
    }
}
