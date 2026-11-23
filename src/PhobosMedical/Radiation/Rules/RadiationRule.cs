using System;
using System.Collections.Generic;

namespace Phobos.Radiation.Rules;

/// <summary>The phase of radiation sickness a person is in.</summary>
public enum RadiationPhase
{
    /// <summary>No course under way: below the threshold, or over.</summary>
    None,
    /// <summary>The early sickness: nausea and vomiting, then malaise.</summary>
    Early,
    /// <summary>The deceptive well period.</summary>
    Well,
    /// <summary>The illness itself.</summary>
    Illness,
}

/// <summary>Which body system fails first at a dose.</summary>
public enum RadiationSyndrome
{
    None,
    BloodForming,
    Gut,
    Neurovascular,
}

/// <summary>A harshness preset's figures (docs/development/radiation-model.md, RM10). All authored.</summary>
public readonly struct RadiationPreset
{
    /// <summary>The dose, in grays, at which half die without care.</summary>
    public readonly double halfDieWithoutCare;
    /// <summary>How far full care moves that dose up.</summary>
    public readonly double careMoves;
    /// <summary>How strong late effects are, 0 to 1.</summary>
    public readonly double lateEffects;

    public RadiationPreset(double halfDieWithoutCare, double careMoves, double lateEffects)
    {
        this.halfDieWithoutCare = halfDieWithoutCare;
        this.careMoves = careMoves;
        this.lateEffects = lateEffects;
    }

    public static RadiationPreset Gentle => new(6, 3, 0);
    public static RadiationPreset Moderate => new(5, 2.5, 0.5);
    public static RadiationPreset Realistic => new(4, 2, 1);
    public static RadiationPreset Harsh => new(3.5, 1.5, 1);
}

/// <summary>
/// The radiation model's arithmetic (D'thorga Medical 0.21.0; docs/development/radiation-model.md, RM2 to RM4, RM7, RM10), game-free.
/// The figures come from the record's sources, simplified for play and labelled there: the equivalent residual dose of the US
/// Department of Agriculture's 1962 civil-defence handbook for repair; the Borden Institute's tables for time to vomiting; REMM and the
/// IAEA for the well period; ICRP 103 and the military medicine sources for the dose that kills half and how care moves it.
/// </summary>
public static class RadiationRule
{
    // ---- Repair (RM3) ----

    /// <summary>Days during which a dose counts in full before repair begins, the share never repaired, and the share of the rest
    /// repaired each day after (the 1962 handbook).</summary>
    public const double RepairStartsAfterDays = 4;
    public const double NeverRepaired = 0.1;
    public const double RepairedPerDay = 0.025;

    /// <summary>What one dose still counts for, <paramref name="daysAgo"/> after it was taken.</summary>
    public static double Remaining(double grays, double daysAgo)
    {
        if (grays <= 0) return 0;
        if (daysAgo <= RepairStartsAfterDays) return grays;
        return grays * (NeverRepaired + (1 - NeverRepaired) * Math.Pow(1 - RepairedPerDay, daysAgo - RepairStartsAfterDays));
    }

    /// <summary>The effective dose now: every dose's remainder, plus what is already beyond repair.</summary>
    public static double Effective(IEnumerable<(double daysAgo, double grays)> doses, double beyondRepair)
    {
        double total = beyondRepair;
        foreach ((double daysAgo, double grays) in doses) total += Remaining(grays, daysAgo);
        return total;
    }

    /// <summary>Days after which a dose is all but repaired, and its tenth can be counted as beyond repair.</summary>
    public const double FullyRepairedAfterDays = 150;

    // ---- The course (RM4) ----

    /// <summary>A course of sickness starts at this effective dose.</summary>
    public const double CourseFrom = 0.7;

    /// <summary>
    /// What the doses taken since the last course ended still count for, each with its repair (Medical 0.26.1; the owner's card of 24 Nov
    /// 2026, "Only from new exposure"). Doses are kept by game day, so the day the course ended is split: of that day's dose, only what
    /// was added after the end (its total less <paramref name="endDayGraysBefore"/>, the grays already recorded that day when it ended)
    /// is fresh. A day before it counts for nothing here, a day after it in full. Doses folded beyond repair (after
    /// <see cref="FullyRepairedAfterDays"/>) are no longer counted as fresh: by then a tenth of them is left.
    /// </summary>
    public static double Fresh(IEnumerable<(double daysAgo, double grays)> doses, double daysSinceCourseEnded, double endDayGraysBefore)
    {
        double total = 0;
        foreach ((double daysAgo, double grays) in doses)
        {
            if (daysAgo < daysSinceCourseEnded - 0.5) total += Remaining(grays, daysAgo);
            else if (daysAgo < daysSinceCourseEnded + 0.5) total += Remaining(grays - endDayGraysBefore, daysAgo);
        }
        return total;
    }

    /// <summary>
    /// Whether a course starts now (Medical 0.26.1). A first course starts when the effective dose reaches <see cref="CourseFrom"/>. After
    /// a course has ended, repair is too slow to bring the effective dose under it soon (3 Gy is still about 1.2 Gy seven weeks on; from 7
    /// Gy the tenth never repaired keeps it over for life), so the next course starts only when the fresh doses since, by themselves,
    /// reach it. The old damage still makes the new course worse: its height follows the whole effective dose.
    /// </summary>
    public static bool StartsCourse(double effective, double fresh, bool hadCourse) =>
        hadCourse ? fresh >= CourseFrom && effective >= CourseFrom : effective >= CourseFrom;

    /// <summary>Hours from the dose to the first vomiting (Blakely, Table 6-2): over 10 at 1 Gy, 4.6 at 2, 1.7 at 4, 1.0 at 6, 0.5 at 10.</summary>
    public static double HoursToVomit(double grays)
    {
        if (grays <= 0) return double.PositiveInfinity;
        double[] doses = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        double[] hours = { 10, 4.63, 2.62, 1.74, 1.27, 0.99, 0.79, 0.66, 0.56, 0.48 };
        if (grays <= doses[0]) return hours[0] * doses[0] / grays;
        for (int i = 1; i < doses.Length; i++)
        {
            if (grays <= doses[i]) return Lerp(hours[i - 1], hours[i], (grays - doses[i - 1]) / (doses[i] - doses[i - 1]));
        }
        return Math.Max(0.05, hours[hours.Length - 1] * doses[doses.Length - 1] / grays);
    }

    /// <summary>The share of people who vomit at a dose (Blakely, Table 6-2): 19 % at 1 Gy, 35 % at 2, 54 % at 3, 72 % at 4, 94 % at 6.</summary>
    public static double VomitShare(double grays)
    {
        double[] doses = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
        double[] shares = { 0, 0.19, 0.35, 0.54, 0.72, 0.86, 0.94, 0.98, 1 };
        if (grays <= 0) return 0;
        for (int i = 1; i < doses.Length; i++)
        {
            if (grays <= doses[i]) return Lerp(shares[i - 1], shares[i], (grays - doses[i - 1]) / (doses[i] - doses[i - 1]));
        }
        return 1;
    }

    /// <summary>How long the early sickness lasts after the first vomiting, in days: two at 1 Gy, a few hours above 8.</summary>
    public static double EarlyDays(double grays) => Clamp(2.2 - 0.25 * grays, 0.25, 2);

    /// <summary>The well period, in days (REMM after the IAEA: to day 18-28 at 2-4 Gy, a week at 6-8, none above 8).</summary>
    public static double WellDays(double grays) => Clamp(30 - 3.5 * grays, 0, 26);

    /// <summary>Which system fails first (Goans and Flynn: blood-forming about 1-8 Gy, gut from about 6, neurovascular from about 20).</summary>
    public static RadiationSyndrome Syndrome(double grays) =>
        grays >= 20 ? RadiationSyndrome.Neurovascular : grays >= 6 ? RadiationSyndrome.Gut : grays >= 1 ? RadiationSyndrome.BloodForming : RadiationSyndrome.None;

    /// <summary>Days the illness takes to reach its height: weeks for the blood, days for the gut, a day or two for the brain.</summary>
    public static double DaysToHeight(double grays) =>
        Syndrome(grays) switch
        {
            RadiationSyndrome.Neurovascular => 1.5,
            RadiationSyndrome.Gut => 6,
            _ => 14,
        };

    /// <summary>The phase at <paramref name="days"/> after the course began, for the highest effective dose reached.</summary>
    public static RadiationPhase Phase(double grays, double days, bool vomited, double daysToFirstVomit)
    {
        if (grays < CourseFrom || days < 0) return RadiationPhase.None;
        double earlyEnds = (vomited ? daysToFirstVomit : 0) + EarlyDays(grays);
        if (days < earlyEnds) return vomited ? RadiationPhase.Early : RadiationPhase.Well;
        return days < earlyEnds + WellDays(grays) ? RadiationPhase.Well : RadiationPhase.Illness;
    }

    // ---- Who lives (RM4, RM10) ----

    /// <summary>The spread of sensitivity between people, in grays (ICRP 103: the dose killing a tenth is about 2 Gy below the one
    /// killing half, so about 0.8 on a logistic curve).</summary>
    public const double Spread = 0.8;

    /// <summary>
    /// This person's own lethal dose: the preset's dose that kills half, moved up by the care they are getting (0 to 1), and moved by
    /// their own sensitivity, a fixed number from 0 to 1 drawn once from who they are, never rolled at the time.
    /// </summary>
    public static double LethalDose(RadiationPreset preset, double care, double sensitivity)
    {
        double u = Clamp(sensitivity, 0.001, 0.999);
        double personal = Spread * Math.Log(u / (1 - u));
        return Math.Max(1, preset.halfDieWithoutCare + preset.careMoves * Clamp(care, 0, 1) + personal);
    }

    /// <summary>How severe the illness becomes at its height: 1 is death; below it, the person recovers from the height.</summary>
    public static double Height(double grays, double lethalDose) => lethalDose <= 0 ? 1 : Clamp(grays / lethalDose, 0, 1);

    /// <summary>The illness's severity on its way to its height, then recovering from it.</summary>
    public static double Severity(double grays, double lethalDose, double daysIntoIllness, double recoveryPerDay)
    {
        if (daysIntoIllness <= 0) return 0;
        double height = Height(grays, lethalDose);
        double rise = DaysToHeight(grays);
        if (daysIntoIllness <= rise) return height * daysIntoIllness / rise;
        if (height >= 1) return 1;
        return Math.Max(0, height - recoveryPerDay * (daysIntoIllness - rise));
    }

    // ---- What a doctor can tell (RM7) ----

    /// <summary>A doctor's estimate from the hours to the first vomiting (Goans and Flynn; Blakely), as a band in grays.</summary>
    public static (double low, double high) EstimateFromVomiting(double hoursToVomit, bool vomited, double hoursSinceDose)
    {
        if (!vomited) return hoursSinceDose < 12 ? (0, 10) : (0, 2);
        if (hoursToVomit < 0.5) return (8, 30);
        if (hoursToVomit < 1) return (6, 8);
        if (hoursToVomit < 2) return (4, 6);
        if (hoursToVomit < 5) return (2, 4);
        return (1, 2);
    }

    /// <summary>A narrower band from a blood count, two days on: a quarter either way of the true dose.</summary>
    public static (double low, double high) EstimateFromBloodCount(double grays) => (grays * 0.75, grays * 1.25);

    /// <summary>The US Army's exposure category from the lifetime dose (FM 3-3-1): 0 none, 1 up to 0.7 Gy, 2 up to 1.5, 3 above.</summary>
    public static int ExposureCategory(double lifetimeGrays) => lifetimeGrays <= 0.005 ? 0 : lifetimeGrays <= 0.7 ? 1 : lifetimeGrays <= 1.5 ? 2 : 3;

    // ---- From Dubs Rimatomics (RM1) ----

    /// <summary>Rimatomics' sickness per unit of strength (its own figure), and the grays one unit of its sickness stands for (the
    /// record's conversion: its first visible stage, 0.04, is then about 0.36 Gy, and its fatal turn, 0.9, about 8 Gy).</summary>
    public const double RimatomicsSicknessPerStrength = 0.0028758333;
    public const double GraysPerRimatomicsSickness = 9;

    public static double GraysFromRimatomics(double strength) => strength <= 0 ? 0 : strength * RimatomicsSicknessPerStrength * GraysPerRimatomicsSickness;

    /// <summary>The fallout's dose rate by its age, by the 7:10 rule (Glasstone and Dolan, 9.16): full on its first day, then falling
    /// as the age to the power -1.2.</summary>
    public static double FalloutDecay(double daysOld) => daysOld <= 1 ? 1 : Math.Pow(daysOld, -1.2);

    static double Lerp(double a, double b, double t) => a + (b - a) * Clamp(t, 0, 1);

    static double Clamp(double value, double low, double high) => value < low ? low : value > high ? high : value;
}
