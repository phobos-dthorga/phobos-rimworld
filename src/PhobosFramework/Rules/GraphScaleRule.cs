using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The top of a graph whose reading's listed range dwarfs what was recorded, game-free (Framework 0.92.1): hours since a service may run
/// to 100,000, and a week of them drawn against that lies flat on the floor. When the recorded figures sit in the bottom tenth of the
/// listed range, the graph is scaled to them with a quarter's headroom, never to less than a floor span; otherwise the listed top stands.
/// </summary>
public static class GraphScaleRule
{
    /// <summary>The share of the listed range below which the data counts as squashed on the floor.</summary>
    public const double SquashedShare = 0.1;
    /// <summary>The headroom above the largest recorded figure.</summary>
    public const double Headroom = 1.25;

    public static double Top(double dataMax, double listedMin, double listedMax, double floorSpan)
    {
        double range = listedMax - listedMin;
        if (range <= 0 || double.IsNaN(dataMax) || dataMax - listedMin >= range * SquashedShare) return listedMax;
        double span = Math.Max(floorSpan, (dataMax - listedMin) * Headroom);
        return Math.Min(listedMax, listedMin + Math.Ceiling(span));
    }
}
