using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// Path loss models. Phase 1 has only free space, which every on-map band uses; later bands
/// (skywave, bounce, through-earth) swap in their own term, as the design says.
/// </summary>
public static class Propagation
{
    /// <summary>
    /// 20 log10(4π × 10⁹ / c) for distance in km and frequency in MHz: 32.45 dB. The design quotes 32.44;
    /// both are in common use (radio-reference-checks.md).
    /// </summary>
    public const double FreeSpaceConstantDb = 32.45;

    /// <summary>
    /// Below this distance the far-field formula is meaningless; the loss is clamped to the 1 m value so
    /// two radios on neighbouring cells never show a negative path loss.
    /// </summary>
    public const double MinimumDistanceMetres = 1.0;

    /// <summary>Free-space path loss in dB: 20 log10 d(km) + 20 log10 f(MHz) + 32.45.</summary>
    public static double FreeSpacePathLossDb(double metres, double megahertz)
    {
        if (megahertz <= 0) throw new ArgumentOutOfRangeException(nameof(megahertz), "Frequency must be positive.");
        var km = Math.Max(metres, MinimumDistanceMetres) / 1000.0;
        return 20.0 * Math.Log10(km) + 20.0 * Math.Log10(megahertz) + FreeSpaceConstantDb;
    }

    /// <summary>
    /// Ray clearance: the straight line between two antennas, at the given fraction of the way from the
    /// first, is below the wall height, so a wall there blocks it. A mast sees over walls; only the walls
    /// near a low hand-held bite. Heights in metres.
    /// </summary>
    public static bool RayBlockedByWall(double fraction, double heightFromMetres, double heightToMetres, double wallHeightMetres)
    {
        var rayHeight = heightFromMetres + (heightToMetres - heightFromMetres) * Math.Min(Math.Max(fraction, 0.0), 1.0);
        return rayHeight < wallHeightMetres;
    }
}
