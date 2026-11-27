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
    /// The bandwidth local noise levels are quoted in: a source's <c>noiseDbmAt1m</c> is its noise in 12.5 kHz, one voice channel
    /// (Radio 0.45.0; docs/development/electronic-warfare.md, EW2).
    /// </summary>
    public const double ReferenceBandwidthHz = 12500.0;

    /// <summary>
    /// What a receiver of this bandwidth hears of noise quoted in the reference bandwidth, in dB to add: 10 log10(B / 12.5 kHz). Morse's
    /// 500 Hz hears 14 dB less of it than voice, a 50 kHz picture 6 dB more. <b>An approximation</b>: it takes local noise as spread evenly
    /// across the band, as electrical noise and barrage jamming are; a narrow whistle would not scale so.
    /// </summary>
    public static double BandwidthScaleDb(double bandwidthHz)
    {
        if (bandwidthHz <= 0) throw new ArgumentOutOfRangeException(nameof(bandwidthHz), "Bandwidth must be positive.");
        return 10.0 * Math.Log10(bandwidthHz / ReferenceBandwidthHz);
    }

    /// <summary>
    /// The floor the receiver actually hears: thermal noise power-summed with local noise. Local noise is quoted in the reference
    /// bandwidth and scaled to the receiver's own (since Radio 0.45.0, EW2; before, it was taken as it was, whatever the mode). Local noise
    /// of −∞ (nothing nearby) leaves the thermal floor unchanged.
    /// </summary>
    public static double TotalDbm(double bandwidthHz, double noiseFigureDb, double localNoiseDbm)
    {
        var thermal = ThermalDbm(bandwidthHz, noiseFigureDb);
        return double.IsNegativeInfinity(localNoiseDbm) ? thermal : Decibels.PowerSumDbm(thermal, localNoiseDbm + BandwidthScaleDb(bandwidthHz));
    }

    /// <summary>
    /// A floor with jamming added: the jamming reaching the receiver's input (after its antenna's gain and its cable's loss),
    /// power-summed with the floor it hears anyway. Jamming of −∞ (none) leaves the floor as it was.
    /// </summary>
    public static double WithJammingDbm(double floorDbm, double jammingAtInputDbm) =>
        double.IsNegativeInfinity(jammingAtInputDbm) ? floorDbm : Decibels.PowerSumDbm(floorDbm, jammingAtInputDbm);

    /// <summary>
    /// A noise source's level at a distance, by the inverse-square law from its level at one metre.
    /// Distances under one metre count as one metre.
    /// </summary>
    public static double AtDistanceDbm(double levelAtOneMetreDbm, double metres)
    {
        var d = Math.Max(metres, 1.0);
        return levelAtOneMetreDbm - 20.0 * Math.Log10(d);
    }

    /// <summary>
    /// How much a source's noise is faded at a distance, in dB to add: 0 inside the fade, then a smooth fall to nothing (−∞) at the
    /// source's radius, so its edge is a soft glow rather than a hard ring (the owner's choice, 23 Nov 2026). The fade is a power
    /// weight of smoothstep(1 − (d − start) / (radius − start)) beginning at <paramref name="fadeStartFraction"/> of the radius.
    /// <b>An authored simplification</b>, not physics: real noise has no radius. It keeps the radius that makes the sum cheap, with
    /// no visible edge. A fraction of 1 or more gives the old hard edge.
    /// </summary>
    public static double FadeDb(double distanceCells, double radiusCells, double fadeStartFraction)
    {
        if (radiusCells <= 0 || distanceCells >= radiusCells) return double.NegativeInfinity;
        var start = radiusCells * Math.Clamp(fadeStartFraction, 0.0, 1.0);
        if (distanceCells <= start) return 0.0;
        var t = (distanceCells - start) / (radiusCells - start);
        var weight = 1.0 - t * t * (3.0 - 2.0 * t);
        return weight <= 0 ? double.NegativeInfinity : 10.0 * Math.Log10(weight);
    }
}
