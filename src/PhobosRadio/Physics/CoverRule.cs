using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// The sums the radio's cover pictures share (Radio 0.42.0; docs/development/radio-world-coverage.md): where a signal to spare falls on
/// the colour ramp, and how far a picture on the world map has to look.
/// </summary>
public static class CoverRule
{
    /// <summary>The ramp's darkest end is kept for no contact, so the faintest contact starts a little above it (authored).</summary>
    public const double FaintestShade = 0.08;

    /// <summary>How the ramp bends, so the weak end, where a contact is decided, takes more of it (authored).</summary>
    public const double RampPower = 1.2;

    /// <summary>
    /// Where a signal to spare falls on the colour ramp, from <see cref="FaintestShade"/> at none to 1 at
    /// <paramref name="fullScaleDb"/> and beyond. The colony's overlay and the world map's picture both use it, so a shade means the same
    /// share of each picture's own full scale. A margin below nothing is no contact: the caller paints it apart.
    /// </summary>
    public static double Shade(double marginDb, double fullScaleDb)
    {
        double share = fullScaleDb <= 0 ? 1.0 : Math.Min(1.0, Math.Max(0.0, marginDb / fullScaleDb));
        return FaintestShade + (1.0 - FaintestShade) * Math.Pow(share, RampPower);
    }

    /// <summary>
    /// How many tiles out a world picture has to look: the radio horizon between the site's highest aerial and the far radio's aerial
    /// on the highest ground an aerial can stand on, in tiles, rounded up, never under 1 nor over <paramref name="capTiles"/>.
    /// </summary>
    public static int RadiusTiles(double siteHeightMetres, double farAerialMetres, double highestSiteLiftMetres, double metresPerTile, int capTiles)
    {
        if (metresPerTile <= 0) return Math.Max(1, capTiles);
        double tiles = Propagation.RadioHorizonMetres(siteHeightMetres, farAerialMetres + highestSiteLiftMetres) / metresPerTile;
        return (int)Math.Min(Math.Max(1.0, Math.Ceiling(tiles)), Math.Max(1, capTiles));
    }

    /// <summary>
    /// What a call has to spare when its first hop goes to a relay (Radio 0.43.0): the worse of that hop and what the relay has to spare
    /// onward, as a chain is as good as its worst hop. Straight home there is nothing onward to limit it: positive infinity leaves the
    /// hop as it is.
    /// </summary>
    public static double Through(double hopMarginDb, double onwardMarginDb) => Math.Min(hopMarginDb, onwardMarginDb);

    /// <summary>About how many tiles lie within a number of tiles of one: a hexagon's count, 3r² + 3r + 1, for a progress figure.</summary>
    public static int TilesWithin(int radiusTiles) => radiusTiles <= 0 ? 1 : 3 * radiusTiles * radiusTiles + 3 * radiusTiles + 1;
}
