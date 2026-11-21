using System;

namespace Phobos.Radiation.Rules;

/// <summary>The stages of a pregnancy that radiation harms differently (CDC, after NCRP Report 174, Table 1.1), by weeks since conception.</summary>
public enum PrenatalWindow
{
    /// <summary>Up to 2 weeks: the embryo dies or is unharmed ("all or nothing").</summary>
    Implantation,
    /// <summary>Weeks 3 to 7: malformation, such as neurological and motor deficiencies.</summary>
    Organs,
    /// <summary>Weeks 8 to 15: the most vulnerable time for the developing brain.</summary>
    BrainEarly,
    /// <summary>Weeks 16 to 25: the brain still, less so.</summary>
    BrainLate,
    /// <summary>Week 26 to birth: no malformation; loss only at high doses.</summary>
    Late,
}

/// <summary>
/// Radiation's late effects (D'thorga Medical 0.23.0; docs/development/radiation-model.md, RM8; the owner's choices of 22 Nov 2026: all
/// four, each with its own switch, sooner than real), game-free. Chance effects (cancer) rise with the lifetime dose; tissue reactions
/// (cataract, sterility) begin at a threshold. Whether a person is affected comes from fixed numbers drawn from who they are (0 to 1),
/// never a roll at the time. The figures are from the record's sources where it says so, and authored where it says so.
/// </summary>
public static class RadiationLateRule
{
    /// <summary>Late effects come at a quarter of the real time (the owner's choice, 22 Nov 2026: "sooner", with no setting).</summary>
    public const double TimeScale = 0.25;
    /// <summary>The game's year is 60 days; real years are counted as game years, then scaled.</summary>
    public const double DaysPerYear = 60;

    /// <summary>Game days for a span of real years.</summary>
    public static double DaysFor(double realYears) => realYears * DaysPerYear * TimeScale;

    /// <summary>Whether a person who would be affected is, under a preset's late-effects strength (0 to 1): Moderate's half spares half.</summary>
    public static bool Spared(double strength, double draw) => draw >= Clamp(strength, 0, 1);

    // ---- Cancer ----

    /// <summary>Cancer per gray over a lifetime: ICRP 103's 5.5 % per sievert for a whole population. The survivors' figures at high
    /// doses are higher, so this errs gentle (agent's choice).</summary>
    public const double CancerPerGray = 0.055;
    /// <summary>The share of radiation's cancers that are leukaemia: about a sixth (agent's figure, unverified).</summary>
    public const double LeukaemiaShare = 1.0 / 6;

    public static double LeukaemiaChance(double lifetimeGrays) => Clamp(lifetimeGrays * CancerPerGray * LeukaemiaShare, 0, 0.95);

    public static double SolidCancerChance(double lifetimeGrays) => Clamp(lifetimeGrays * CancerPerGray * (1 - LeukaemiaShare), 0, 0.95);

    /// <summary>Real years until a leukaemia shows: from about 2, most by 6 to 8 (RERF), here 2 to 8 by the person's draw.</summary>
    public static double LeukaemiaYears(double draw) => 2 + 6 * Clamp(draw, 0, 1);

    /// <summary>Real years until a solid cancer shows: from about 10 (RERF), here 10 to 30 by the person's draw.</summary>
    public static double SolidCancerYears(double draw) => 10 + 20 * Clamp(draw, 0, 1);

    // ---- Cataract ----

    /// <summary>The lens: clouding from 0.5 Gy (ICRP's 2011 statement), a cataract that impairs sight certain by 5 Gy (ICRP 41, in ICRP 103);
    /// a straight line between (agent's simplification).</summary>
    public const double CataractFrom = 0.5;
    public const double CataractCertainAt = 5;

    public static double CataractChance(double lensGrays) => lensGrays <= CataractFrom ? 0 : Clamp((lensGrays - CataractFrom) / (CataractCertainAt - CataractFrom), 0, 1);

    /// <summary>Real years until the cataract shows: sooner the larger the dose, 8 years at the threshold down to 1 at 5 Gy (authored).</summary>
    public static double CataractYears(double lensGrays) => Lerp(8, 1, (lensGrays - CataractFrom) / (CataractCertainAt - CataractFrom));

    // ---- Sterility (ICRP 103, from ICRP 41, a single brief dose) ----

    /// <summary>Temporary sterility in men from 0.15 Gy.</summary>
    public const double TemporaryMaleFrom = 0.15;

    /// <summary>The dose that makes this man permanently sterile: 3.5 to 6 Gy, by his draw.</summary>
    public static double PermanentMaleAt(double draw) => 3.5 + 2.5 * Clamp(draw, 0, 1);

    /// <summary>The dose that makes this woman permanently sterile: 2.5 to 6 Gy, lower the older she is (ICRP gives the range; the age
    /// slope is authored: the lower of her draw's figure and one falling from 6 Gy at 20 to 2.5 Gy at 45).</summary>
    public static double PermanentFemaleAt(double draw, double ageYears)
    {
        double byDraw = 6 - 3.5 * Clamp(draw, 0, 1);
        double byAge = Lerp(6, 2.5, (ageYears - 20) / 25);
        return Math.Min(byDraw, byAge);
    }

    /// <summary>Real years a man's temporary sterility lasts: longer the larger the dose, a quarter-year at the threshold up to five
    /// (authored, after the sources' "months to years").</summary>
    public static double TemporarySterileYears(double grays) => Clamp(grays * 1.5, 0.25, 5);

    // ---- Pregnancy (CDC, after NCRP Report 174; the fetus takes the mother's dose, a simplification: the CDC says it "tends to be lower") ----

    /// <summary>A Biotech pregnancy's progress (0 to 1) as weeks since conception, of 38.</summary>
    public static double WeeksSinceConception(double gestationProgress) => Clamp(gestationProgress, 0, 1) * 38;

    public static PrenatalWindow Window(double weeks) =>
        weeks <= 2 ? PrenatalWindow.Implantation
        : weeks <= 7 ? PrenatalWindow.Organs
        : weeks <= 15 ? PrenatalWindow.BrainEarly
        : weeks <= 25 ? PrenatalWindow.BrainLate
        : PrenatalWindow.Late;

    /// <summary>Below this, nothing but cancer is detectable (CDC: "not detectable for fetal doses below about 0.10 Gy").</summary>
    public const double PrenatalFrom = 0.1;

    /// <summary>
    /// The chance this pregnancy is lost, from the dose taken in each window. In the first two weeks, from 0.1 Gy: "may increase
    /// slightly" up to 0.5 Gy, then "likely high" (authored: a fifth at 0.5 Gy, nine in ten by 1.5). Later, above 0.5 Gy, "may increase,
    /// depending on dose" (authored: none at 0.5, half by 3 Gy). The larger of the two.
    /// </summary>
    public static double LossChance(double implantationGrays, double laterGrays)
    {
        double early = implantationGrays < PrenatalFrom ? 0
            : implantationGrays <= 0.5 ? Lerp(0.02, 0.2, (implantationGrays - PrenatalFrom) / 0.4)
            : Lerp(0.2, 0.9, (implantationGrays - 0.5) / 1.0);
        double later = laterGrays <= 0.5 ? 0 : Lerp(0, 0.5, (laterGrays - 0.5) / 2.5);
        return Math.Max(early, later);
    }

    /// <summary>The chance of a malformation (neurological and motor deficiencies) from the dose in weeks 3 to 7: above 0.5 Gy the
    /// "probability ... increases" (authored: none at 0.5, three in five by 2 Gy).</summary>
    public static double MalformationChance(double organGrays) => organGrays <= 0.5 ? 0 : Lerp(0, 0.6, (organGrays - 0.5) / 1.5);

    /// <summary>
    /// The chance of intellectual disability: 40 % per gray in weeks 8 to 15 and 15 % per gray in weeks 16 to 25 (the CDC's figures at
    /// 1 Gy, taken as a straight line), from ICRP 103's threshold of 0.3 Gy for the first window and the CDC's 0.5 Gy for the second.
    /// </summary>
    public static double BrainInjuryChance(double brainEarlyGrays, double brainLateGrays)
    {
        double early = brainEarlyGrays < 0.3 ? 0 : 0.4 * brainEarlyGrays;
        double late = brainLateGrays < 0.5 ? 0 : 0.15 * brainLateGrays;
        return Clamp(early + late, 0, 0.95);
    }

    static double Lerp(double a, double b, double t) => a + (b - a) * Clamp(t, 0, 1);

    static double Clamp(double value, double low, double high) => value < low ? low : value > high ? high : value;
}
