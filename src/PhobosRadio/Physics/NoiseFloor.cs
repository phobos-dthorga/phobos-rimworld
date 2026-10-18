using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// The receiver's noise floor: thermal noise in the mode's bandwidth, plus the receiver's own noise
/// figure, plus whatever the colony is radiating nearby. Narrower modes hear deeper.
/// </summary>
public static class NoiseFloor
{
    /// <summary>kT at 290 K: −173.98 dBm per Hz, quoted as −174 (radio-reference-checks.md).</summary>
    public const double ThermalDbmPerHz = -174.0;

    /// <summary>Thermal noise in a bandwidth, after the receiver's noise figure, in dBm.</summary>
    public static double ThermalDbm(double bandwidthHz, double noiseFigureDb)
    {
        if (bandwidthHz <= 0) throw new ArgumentOutOfRangeException(nameof(bandwidthHz), "Bandwidth must be positive.");
        return ThermalDbmPerHz + 10.0 * Math.Log10(bandwidthHz) + noiseFigureDb;
    }

    /// <summary>
    /// The floor the receiver actually hears: thermal noise power-summed with local noise. Local noise
    /// of −∞ (nothing nearby) leaves the thermal floor unchanged.
    /// </summary>
    public static double TotalDbm(double bandwidthHz, double noiseFigureDb, double localNoiseDbm)
    {
        var thermal = ThermalDbm(bandwidthHz, noiseFigureDb);
        return double.IsNegativeInfinity(localNoiseDbm) ? thermal : Decibels.PowerSumDbm(thermal, localNoiseDbm);
    }

    /// <summary>
    /// A noise source's level at a distance, by the inverse-square law from its level at one metre.
    /// Distances under one metre count as one metre.
    /// </summary>
    public static double AtDistanceDbm(double levelAtOneMetreDbm, double metres)
    {
        var d = Math.Max(metres, 1.0);
        return levelAtOneMetreDbm - 20.0 * Math.Log10(d);
    }
}
