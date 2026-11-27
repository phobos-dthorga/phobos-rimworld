using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A chip detector: metal in an engine's or a gearbox's oil, shed by wear, lights a lamp before the wear breaks something (Framework
/// 0.365.0; docs/development/vehicle-chip-detector.md; the owner's answers of 28 Nov 2026: it reports only, and helicopter gearboxes have one
/// too). Game-free. Every figure is authored: the lamp lights after about a tenth of a part's health lost to wear, or about 50 hours run worn.
/// </summary>
public static class ChipDetectorRule
{
    /// <summary>Metal gained for each whole share of a part's health lost to wear: a tenth of its health lights the lamp.</summary>
    public const double MetalPerShare = 5;
    /// <summary>Metal gained each hour an engine runs past its service or with its hot section worn.</summary>
    public const double WornPerHour = 0.01;
    /// <summary>The lamp lights here.</summary>
    public const double LampFrom = 0.5;
    /// <summary>From here the lamp says there is much metal.</summary>
    public const double MuchFrom = 0.8;

    /// <summary>A part's metal after it lost <paramref name="shareLost"/> of its health to wear.</summary>
    public static double AfterWear(double metal, double shareLost) => Clamp01(metal + MetalPerShare * Math.Max(0, shareLost));

    /// <summary>A running engine's metal after <paramref name="hours"/> run, more for each way it is being run worn (past its service, its
    /// hot section worn).</summary>
    public static double AfterRunning(double metal, double hours, int wornWays) => Clamp01(metal + WornPerHour * Math.Max(0, hours) * Math.Max(0, wornWays));

    /// <summary>Whether the lamp is lit for this much metal.</summary>
    public static bool Lit(double metal) => metal >= LampFrom;

    /// <summary>Whether the lamp says there is much metal.</summary>
    public static bool Much(double metal) => metal >= MuchFrom;

    /// <summary>
    /// The likely cause a mechanic names in the diagnosis report (Framework 0.374.0; the record's CD5): among the ways the model sheds metal,
    /// the one that stands out in the part as it is now, most telling first. A guess from what can be seen, never a record of what shed it,
    /// so the report says "likely".
    /// </summary>
    public static ChipCause Cause(bool gearbox, bool hotSectionWorn, bool overdue, bool lowOilPressure) =>
        gearbox ? ChipCause.Gearbox
        : hotSectionWorn ? ChipCause.HotSection
        : lowOilPressure ? ChipCause.LowOil
        : overdue ? ChipCause.Overdue
        : ChipCause.Wear;

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}

/// <summary>What likely put metal in a part's oil (Framework 0.374.0, CD5).</summary>
public enum ChipCause
{
    /// <summary>A rotor gearbox: worked hard or past what it accepts.</summary>
    Gearbox,
    /// <summary>An engine whose hot section is past its life.</summary>
    HotSection,
    /// <summary>An engine short of oil pressure now.</summary>
    LowOil,
    /// <summary>An engine run past its service.</summary>
    Overdue,
    /// <summary>Wear inside it, with nothing else standing out.</summary>
    Wear,
}
