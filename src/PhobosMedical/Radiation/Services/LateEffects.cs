using System.Collections.Generic;
using System.Linq;
using Phobos.Medical;
using Phobos.Radiation.Defs;
using Phobos.Radiation.Rules;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Services;

/// <summary>
/// Radiation's late effects (D'thorga Medical 0.23.0; docs/development/radiation-model.md, RM8; the owner's choices of 22 Nov 2026): cancer,
/// cataract, sterility and harm in pregnancy, each behind its own switch, their strength from the harshness preset, arriving at a quarter
/// of the real time. <see cref="RadiationLateRule"/> decides; this applies it. Who is affected comes from fixed numbers drawn from who the
/// person is (<see cref="RadiationService.Sensitivity"/>), never a roll at the time, so saving and loading changes nothing.
/// </summary>
public static class LateEffects
{
    // The purposes of each person's fixed draws. Never change one: a saved colony's fates hang on them.
    const int LeukaemiaChance = 3, LeukaemiaWhen = 4, CancerChance = 5, CancerWhen = 6, CancerWhere = 13, CataractChance = 7, SterileAt = 8,
        SterileSpared = 9, Malformation = 11, BrainInjury = 12, PregnancyLossBase = 1000;

    static int Today => Find.TickManager.TicksGame / GenDate.TicksPerDay;

    static RadiationSettings Settings => RadiationPart.Settings;

    static double Strength => RadiationPart.Preset.lateEffects;

    /// <summary>From the refresh: the highest dose reached, what is now coming and when, and sterility, which follows the dose at once.</summary>
    public static void Judge(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        if (dose.Effective > dose.highest) dose.highest = dose.Effective;
        double strength = Strength;
        if (strength <= 0) return;
        int today = Today;
        if (Settings.cancer)
        {
            if (dose.leukaemiaDay == Hediff_PhobosRadiationDose.LateNone
                && RadiationService.Sensitivity(pawn, LeukaemiaChance) < RadiationLateRule.LeukaemiaChance(dose.lifetime) * strength)
            {
                dose.leukaemiaDay = today + Days(RadiationLateRule.LeukaemiaYears(RadiationService.Sensitivity(pawn, LeukaemiaWhen)));
            }
            if (dose.cancerDay == Hediff_PhobosRadiationDose.LateNone
                && RadiationService.Sensitivity(pawn, CancerChance) < RadiationLateRule.SolidCancerChance(dose.lifetime) * strength)
            {
                dose.cancerDay = today + Days(RadiationLateRule.SolidCancerYears(RadiationService.Sensitivity(pawn, CancerWhen)));
            }
        }
        if (Settings.cataract && dose.cataractDay == Hediff_PhobosRadiationDose.LateNone
            && RadiationService.Sensitivity(pawn, CataractChance) < RadiationLateRule.CataractChance(dose.lifetime) * strength)
        {
            dose.cataractDay = today + Days(RadiationLateRule.CataractYears(dose.lifetime));
        }
        if (Settings.sterility) JudgeSterility(pawn, dose, strength);
    }

    static int Days(double realYears) => (int)System.Math.Ceiling(RadiationLateRule.DaysFor(realYears));

    static void JudgeSterility(Pawn pawn, Hediff_PhobosRadiationDose dose, double strength)
    {
        if (dose.sterilePermanent || pawn.gender == Gender.None || dose.highest < RadiationLateRule.TemporaryMaleFrom) return;
        if (RadiationLateRule.Spared(strength, RadiationService.Sensitivity(pawn, SterileSpared))) return;
        double draw = RadiationService.Sensitivity(pawn, SterileAt);
        double permanentAt = pawn.gender == Gender.Male
            ? RadiationLateRule.PermanentMaleAt(draw)
            : RadiationLateRule.PermanentFemaleAt(draw, pawn.ageTracker?.AgeBiologicalYearsFloat ?? 30f);
        HediffSet set = pawn.health.hediffSet;
        if (dose.highest >= permanentAt)
        {
            dose.sterilePermanent = true;
            if (set.GetFirstHediffOfDef(RadiationDefOf.PhobosRadiation_TemporarySterility) is { } temporary) pawn.health.RemoveHediff(temporary);
            if (!set.HasHediff(RadiationDefOf.Sterilized)) pawn.health.AddHediff(RadiationDefOf.Sterilized);
            if (Seen.Wants("radiation.late", pawn, "sterile-permanent")) Seen.Note("radiation.late", pawn, dose.highest.ToString("0.0") + " Gy", "sterile-permanent");
            return;
        }
        if (pawn.gender != Gender.Male || dose.highest <= dose.sterileTemporaryFor * 1.2f) return;
        dose.sterileTemporaryFor = dose.highest;
        if (set.GetFirstHediffOfDef(RadiationDefOf.PhobosRadiation_TemporarySterility) is not HediffWithComps sterile)
        {
            sterile = (HediffWithComps)HediffMaker.MakeHediff(RadiationDefOf.PhobosRadiation_TemporarySterility, pawn);
            pawn.health.AddHediff(sterile);
        }
        if (sterile.TryGetComp<HediffComp_Disappears>() is { } disappears)
        {
            disappears.ticksToDisappear = Days(RadiationLateRule.TemporarySterileYears(dose.highest)) * GenDate.TicksPerDay;
        }
        if (Seen.Wants("radiation.late", pawn, "sterile-temporary")) Seen.Note("radiation.late", pawn, dose.highest.ToString("0.00") + " Gy", "sterile-temporary");
    }

    /// <summary>From the step: gives what has come due, while its switch is on.</summary>
    public static void GiveDue(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        int today = Today;
        if (Settings.cancer && Due(dose.leukaemiaDay, today))
        {
            dose.leukaemiaDay = Hediff_PhobosRadiationDose.LateGiven;
            if (!pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_Leukaemia))
            {
                Hediff leukaemia = HediffMaker.MakeHediff(RadiationDefOf.PhobosRadiation_Leukaemia, pawn);
                leukaemia.Severity = PhobosRadiationRulesDef.Current.cancerStartSeverity;
                pawn.health.AddHediff(leukaemia);
                Tell(pawn, RadiationDefOf.PhobosRadiation_Leukaemia);
            }
            if (Seen.Wants("radiation.late", pawn, "leukaemia")) Seen.Note("radiation.late", pawn, dose.lifetime.ToString("0.0") + " Gy lifetime", "leukaemia");
        }
        if (Settings.cancer && Due(dose.cancerDay, today))
        {
            dose.cancerDay = Hediff_PhobosRadiationDose.LateGiven;
            if (CancerPart(pawn) is { } part)
            {
                Hediff carcinoma = HediffMaker.MakeHediff(RadiationDefOf.Carcinoma, pawn, part);
                carcinoma.Severity = PhobosRadiationRulesDef.Current.cancerStartSeverity;
                pawn.health.AddHediff(carcinoma, part);
                Tell(pawn, RadiationDefOf.Carcinoma);
                if (Seen.Wants("radiation.late", pawn, "cancer")) Seen.Note("radiation.late", pawn, part.def.defName, "cancer");
            }
        }
        if (Settings.cataract && Due(dose.cataractDay, today))
        {
            dose.cataractDay = Hediff_PhobosRadiationDose.LateGiven;
            int eyes = 0;
            foreach (BodyPartRecord eye in pawn.health.hediffSet.GetNotMissingParts().Where(p => p.def.tags.Contains(BodyPartTagDefOf.SightSource)).ToList())
            {
                if (pawn.health.hediffSet.hediffs.Any(h => h.def == RadiationDefOf.Cataract && h.Part == eye)) continue;
                pawn.health.AddHediff(RadiationDefOf.Cataract, eye);
                eyes++;
            }
            if (eyes > 0) Tell(pawn, RadiationDefOf.Cataract);
            if (Seen.Wants("radiation.late", pawn, "cataract")) Seen.Note("radiation.late", pawn, eyes + " eyes", "cataract");
        }
    }

    static bool Due(int day, int today) => day >= 0 && today >= day;

    /// <summary>Where a solid cancer forms: from a place in the rules' list fixed by who the person is, the first such part they still have.</summary>
    static BodyPartRecord? CancerPart(Pawn pawn)
    {
        List<BodyPartDef> places = PhobosRadiationRulesDef.Current.solidCancerParts;
        if (places.Count == 0) return null;
        List<BodyPartRecord> parts = pawn.health.hediffSet.GetNotMissingParts().ToList();
        int start = (int)(RadiationService.Sensitivity(pawn, CancerWhere) * places.Count);
        for (int i = 0; i < places.Count; i++)
        {
            BodyPartDef place = places[(start + i) % places.Count];
            if (parts.FirstOrDefault(p => p.def == place) is { } part) return part;
        }
        return null;
    }

    static void Tell(Pawn pawn, HediffDef condition)
    {
        if (pawn.Faction != Faction.OfPlayer || !PawnUtility.ShouldSendNotificationAbout(pawn)) return;
        Messages.Message("PhobosRadiation_MessageLate".Translate(pawn.Named("PAWN"), condition.label.Named("CONDITION")), pawn, MessageTypeDefOf.NegativeHealthEvent);
    }

    // ---- Pregnancy ----

    /// <summary>From each dose taken: the dose a pregnancy took in its window, and whether it is lost.</summary>
    public static void OnDose(Pawn pawn, Hediff_PhobosRadiationDose dose, float grays)
    {
        if (!ModsConfig.BiotechActive || !(Settings.pregnancyLoss || Settings.pregnancyChild || Settings.pregnancyCancer)) return;
        if (PregnancyUtility.GetPregnancyHediff(pawn) is not Hediff_Pregnant pregnancy) return;
        if (dose.pregnancyId != pregnancy.loadID || dose.prenatal.Count != 5)
        {
            dose.pregnancyId = pregnancy.loadID;
            dose.prenatal = new List<float> { 0, 0, 0, 0, 0 };
        }
        PrenatalWindow window = RadiationLateRule.Window(RadiationLateRule.WeeksSinceConception(pregnancy.GestationProgress));
        dose.prenatal[(int)window] += grays;
        if (!Settings.pregnancyLoss || Strength <= 0) return;
        float later = dose.prenatal[1] + dose.prenatal[2] + dose.prenatal[3] + dose.prenatal[4];
        double loss = RadiationLateRule.LossChance(dose.prenatal[0], later) * Strength;
        if (RadiationService.Sensitivity(pawn, PregnancyLossBase + (pregnancy.loadID & 0xffff)) >= loss) return;
        ClearPrenatal(dose);
        pregnancy.Miscarry();
        if (pawn.Faction == Faction.OfPlayer)
        {
            Find.LetterStack.ReceiveLetter("PhobosRadiation_LetterLossLabel".Translate(pawn.Named("PAWN")), "PhobosRadiation_LetterLossText".Translate(pawn.Named("PAWN")),
                LetterDefOf.NegativeEvent, pawn);
        }
        if (Seen.Wants("radiation.prenatal", pawn, "loss")) Seen.Note("radiation.prenatal", pawn, window.ToString(), "loss");
    }

    /// <summary>From the step: forgets a pregnancy's doses once it has ended some other way (a termination), so a later one starts clean.</summary>
    public static void ForgetEndedPregnancy(Pawn pawn, Hediff_PhobosRadiationDose dose)
    {
        if (dose.pregnancyId < 0 || !ModsConfig.BiotechActive) return;
        if (PregnancyUtility.GetPregnancyHediff(pawn) != null || InLabour(pawn)) return;
        ClearPrenatal(dose);
    }

    static HediffDef? labour, pushing;
    static bool labourLooked;

    static bool InLabour(Pawn pawn)
    {
        if (!labourLooked)
        {
            labourLooked = true;
            labour = DefDatabase<HediffDef>.GetNamedSilentFail("PregnancyLabor");
            pushing = DefDatabase<HediffDef>.GetNamedSilentFail("PregnancyLaborPushing");
        }
        HediffSet set = pawn.health.hediffSet;
        return (labour != null && set.HasHediff(labour)) || (pushing != null && set.HasHediff(pushing));
    }

    static void ClearPrenatal(Hediff_PhobosRadiationDose dose)
    {
        dose.pregnancyId = -1;
        dose.prenatal.Clear();
    }

    /// <summary>From a birth: what the child took before birth, and its harm. The mother's record of it is then cleared.</summary>
    public static void Born(Pawn mother, Pawn baby)
    {
        if (!RadiationPart.On || baby.Dead || RadiationService.DoseOf(mother, create: false) is not { } dose || dose.prenatal.Count != 5) return;
        var taken = new List<float>(dose.prenatal);
        ClearPrenatal(dose);
        float total = taken.Sum();
        if (total <= 0f) return;
        double strength = Strength;
        if (Settings.pregnancyChild && strength > 0)
        {
            if (RadiationService.Sensitivity(baby, Malformation) < RadiationLateRule.MalformationChance(taken[1]) * strength)
            {
                baby.health.AddHediff(RadiationDefOf.PhobosRadiation_PrenatalMalformation);
                if (Seen.Wants("radiation.prenatal", baby, "malformation")) Seen.Note("radiation.prenatal", baby, taken[1].ToString("0.00") + " Gy", "malformation");
            }
            if (RadiationService.Sensitivity(baby, BrainInjury) < RadiationLateRule.BrainInjuryChance(taken[2], taken[3]) * strength)
            {
                baby.health.AddHediff(RadiationDefOf.PhobosRadiation_PrenatalBrainInjury);
                if (Seen.Wants("radiation.prenatal", baby, "brain")) Seen.Note("radiation.prenatal", baby, (taken[2] + taken[3]).ToString("0.00") + " Gy", "brain");
            }
        }
        if (Settings.pregnancyCancer && baby.health?.hediffSet != null && baby.RaceProps.IsFlesh)
        {
            Hediff_PhobosRadiationDose carried = RadiationService.DoseOf(baby, create: true)!;
            carried.lifetime += total;
            carried.lastSource = "before birth";
            if (Seen.Wants("radiation.prenatal", baby, "carried")) Seen.Note("radiation.prenatal", baby, total.ToString("0.00") + " Gy", "carried");
        }
    }
}
