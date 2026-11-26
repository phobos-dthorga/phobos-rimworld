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
    /// The radio horizon between two antennas over smooth ground, in metres: 4.12 (sqrt h1 + sqrt h2) km with the heights in metres, the
    /// line of sight under the usual four-thirds Earth radius for VHF and UHF (the design's VHF/UHF row; ITU-R P.526 treats the same smooth
    /// Earth). Past it a VHF link fails (Radio 0.21.0): diffraction beyond the horizon is left out, a labelled simplification.
    /// </summary>
    public static double RadioHorizonMetres(double heightAMetres, double heightBMetres) =>
        4120.0 * (Math.Sqrt(Math.Max(0.0, heightAMetres)) + Math.Sqrt(Math.Max(0.0, heightBMetres)));

    /// <summary>
    /// Plane-earth path loss in dB: 40 log10 d(m) - 20 log10(h1 h2), heights in metres (Radio 0.22.0; the owner's card of 26 Nov 2026,
    /// "Plane-earth fading", from radio-autonomy's relay design). The two-ray model over flat ground, which falls off with the fourth power
    /// of distance past its break point; between tiles the path loss is the greater of this and free space, still cut at the horizon.
    /// Heights under 0.1 m count as 0.1 m, distances under 1 m as 1 m.
    /// </summary>
    public static double PlaneEarthPathLossDb(double metres, double heightAMetres, double heightBMetres) =>
        40.0 * Math.Log10(Math.Max(metres, MinimumDistanceMetres)) - 20.0 * Math.Log10(Math.Max(heightAMetres, 0.1) * Math.Max(heightBMetres, 0.1));

    /// <summary>
    /// The height a tile's ground lifts an aerial by, in metres, by the game's hilliness from 1 (flat) to 4 (mountainous) (Radio 0.22.0; the
    /// owner's card of 26 Nov 2026, "By hilliness"; radio-autonomy's figures, authored): flat 0, small hills 30, large hills 100, mountains
    /// 300. Anything else adds nothing.
    /// </summary>
    public static double SiteHeightMetres(int hilliness) => hilliness switch
    {
        2 => 30.0,
        3 => 100.0,
        4 => 300.0,
        _ => 0.0,
    };

    /// <summary>
    /// What obstructions cost on a band. The band's factor applies to walls and built roofs, which a shorter wavelength gets through by way
    /// of openings wide enough for it; rock and natural roofs have no openings and absorb, so they count in full (owner, 9 Nov 2026). A
    /// labelled simplification: real rock absorbs somewhat more at higher frequencies, and this leaves the two bands equal there.
    /// </summary>
    public static double ObstructionDb(double builtDb, double naturalDb, double bandFactor) => builtDb * bandFactor + naturalDb;

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
