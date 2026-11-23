using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>
/// Decibel arithmetic. Everything in the link budget is a power level in dBm or a gain or loss in dB,
/// so the whole budget is addition; these helpers convert at the edges and sum noise powers.
/// No game types: this file is compiled into the unit-test project too.
/// </summary>
public static class Decibels
{
    /// <summary>Doubling a distance adds 20 log10 2 = 6.02 dB of free-space loss: the design's 6 dB rule.</summary>
    public const double DoublingDistanceLossDb = 6.0206;

    public static double FromWatts(double watts)
    {
        if (watts <= 0) throw new ArgumentOutOfRangeException(nameof(watts), "Power must be positive.");
        return 10.0 * Math.Log10(watts * 1000.0);
    }

    public static double ToWatts(double dBm) => Math.Pow(10.0, dBm / 10.0) / 1000.0;

    public static double ToMilliwatts(double dBm) => Math.Pow(10.0, dBm / 10.0);

    public static double FromMilliwatts(double milliwatts)
    {
        if (milliwatts <= 0) throw new ArgumentOutOfRangeException(nameof(milliwatts), "Power must be positive.");
        return 10.0 * Math.Log10(milliwatts);
    }

    /// <summary>Sums independent noise powers given in dBm; returns the combined level in dBm.</summary>
    public static double PowerSumDbm(IEnumerable<double> levelsDbm)
    {
        double milliwatts = 0;
        foreach (var level in levelsDbm) milliwatts += ToMilliwatts(level);
        return MilliwattsToDbm(milliwatts);
    }

    /// <summary>
    /// Two levels summed, with no allocation: the coverage overlay calls this for every cell it paints (Radio 0.2.0). Either level may
    /// be −∞ (nothing there).
    /// </summary>
    public static double PowerSumDbm(double aDbm, double bDbm) => MilliwattsToDbm(ToMilliwatts(aDbm) + ToMilliwatts(bDbm));

    /// <summary>A total in milliwatts back to dBm; nothing at all is −∞.</summary>
    public static double MilliwattsToDbm(double milliwatts) => milliwatts <= 0 ? double.NegativeInfinity : FromMilliwatts(milliwatts);
}
