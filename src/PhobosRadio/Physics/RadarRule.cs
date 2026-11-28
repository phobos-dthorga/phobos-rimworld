using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// The two-way radar sum (Radio 0.74.0; docs/development/radar-and-air-defence.md, row RD1). A radar is a radio whose signal goes out and
/// comes back: the echo crosses the radio's one-way free-space loss twice, with the target's radar size in between as a "gain" of its own.
/// In the US Navy's Electronic Warfare and Radar Systems Engineering Handbook form (NAWCWD TP 8347, sections 4-3 and 4-4):
/// <c>Pr = Pt + Gt + Gr − 2α + Gσ</c>, with α the one-way loss (<see cref="Propagation.FreeSpacePathLossDb"/>) and
/// <c>Gσ = 10 log σ + 20 log f(MHz) − 38.54</c>. So the echo falls with the fourth power of distance: doubling the range costs 12 dB.
/// <para>Game-free arithmetic, so a next game could take it as it stands; it needs only its own targets' sizes and its own scale.</para>
/// </summary>
public static class RadarRule
{
    /// <summary>10 log10(4π / c²) for frequency in MHz and size in m²: −38.54 dB (NAWCWD TP 8347, section 4-4).</summary>
    public const double TargetGainConstantDb = -38.54;

    /// <summary>
    /// The echo needed above the noise for a 90 % chance of seeing a steady target on one look, with one false alarm in a million: 13.1 dB
    /// (MathWorks, "Swerling 1 target models", the non-fluctuating case).
    /// </summary>
    public const double SteadyTargetSnrDb = 13.1;

    /// <summary>
    /// The same for a target whose echo flickers from look to look, as a real aircraft's does (Swerling 1): 21.1 dB (MathWorks). This is
    /// the figure our radars use; the often-quoted 13 dB is for the steady case.
    /// </summary>
    public const double FluctuatingTargetSnrDb = 21.1;

    /// <summary>A target's radar size as a gain in dB: <c>10 log σ(m²) + 20 log f(MHz) − 38.54</c>.</summary>
    public static double TargetGainDb(double squareMetres, double megahertz)
    {
        if (squareMetres <= 0) throw new ArgumentOutOfRangeException(nameof(squareMetres), "A radar size must be positive.");
        if (megahertz <= 0) throw new ArgumentOutOfRangeException(nameof(megahertz), "Frequency must be positive.");
        return 10.0 * Math.Log10(squareMetres) + 20.0 * Math.Log10(megahertz) + TargetGainConstantDb;
    }

    /// <summary>
    /// The echo a radar hears from a target, in dBm: peak power, the antenna's gain out and back (one antenna sends and hears), the one-way
    /// loss twice, and the target's gain.
    /// </summary>
    public static double EchoDbm(double peakDbm, double gainDbi, double megahertz, double squareMetres, double metres) =>
        peakDbm + 2.0 * gainDbi - 2.0 * Propagation.FreeSpacePathLossDb(metres, megahertz) + TargetGainDb(squareMetres, megahertz);

    /// <summary>
    /// How far the sum sees a target, in metres: the range at which its echo stands <paramref name="neededSnrDb"/> above
    /// <paramref name="floorDbm"/>. The sum alone, before the horizon or the ground has a say (<see cref="SeenRangeMetres"/>).
    /// </summary>
    public static double DetectionRangeMetres(double peakDbm, double gainDbi, double megahertz, double squareMetres, double floorDbm, double neededSnrDb)
    {
        // peak + 2G + Gσ − 2(20 log km + 20 log f + 32.45) = floor + needed, solved for km.
        var budget = peakDbm + 2.0 * gainDbi + TargetGainDb(squareMetres, megahertz) - 2.0 * (20.0 * Math.Log10(megahertz) + Propagation.FreeSpaceConstantDb)
            - floorDbm - neededSnrDb;
        return 1000.0 * Math.Pow(10.0, budget / 40.0);
    }

    /// <summary>
    /// How far a radar actually sees a target, in metres: the sum's range, cut at the radio horizon between the radar's height and the
    /// target's (the radio's own four-thirds Earth rule, <see cref="Propagation.RadioHorizonMetres"/>).
    /// </summary>
    public static double SeenRangeMetres(double sumRangeMetres, double radarHeightMetres, double targetHeightMetres) =>
        Math.Min(sumRangeMetres, Propagation.RadioHorizonMetres(radarHeightMetres, targetHeightMetres));

    /// <summary>The needed echo above noise for a target: <see cref="FluctuatingTargetSnrDb"/> unless it is steady.</summary>
    public static double NeededSnrDb(bool fluctuating) => fluctuating ? FluctuatingTargetSnrDb : SteadyTargetSnrDb;
}
