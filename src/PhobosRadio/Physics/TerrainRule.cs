using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>
/// High ground between two tiles (Radio 0.41.0; the owner's card of 27 Nov 2026, "Add terrain blocking"; docs/development/radio-world-coverage.md).
/// The hills of each tile on the way stand as high as they lift an aerial (<see cref="GroundHeightMetres"/>). Where the highest of them
/// reaches the straight line between the two aerials, the wave has to bend over it, and that costs signal: the single knife-edge of
/// ITU-R P.526, on the one edge that stands furthest into the line. A labelled simplification: one edge, never several, and a ridge as
/// sharp as a knife. Flat ground costs nothing here: it is plane earth's and the horizon's already.
/// </summary>
public static class TerrainRule
{
    /// <summary>
    /// The planet's radius as a radio wave sees it, in metres: four-thirds of 6,371 km, the usual allowance for the air bending the wave.
    /// The same figure the radio horizon's 4.12 comes from: the square root of twice this is 4,122.
    /// </summary>
    public const double EffectiveEarthRadiusMetres = 8_495_000.0;

    /// <summary>Below this the edge is far enough under the line to cost nothing (ITU-R P.526: its formula holds above -0.78).</summary>
    public const double ClearBelow = -0.78;

    /// <summary>The speed of light in metres a microsecond, so a frequency in MHz divides into it for a wavelength in metres.</summary>
    const double LightMetresPerMicrosecond = 299.792458;

    /// <summary>
    /// How high a tile's ground stands in the way, in metres, by the game's hilliness from 1 (flat) to 5 (impassable): what it lifts an
    /// aerial by (<see cref="Propagation.SiteHeightMetres"/>), and 500 for impassable mountains, which nothing can stand on (authored).
    /// </summary>
    public static double GroundHeightMetres(int hilliness) => hilliness == 5 ? 500.0 : Propagation.SiteHeightMetres(hilliness);

    /// <summary>
    /// How high the straight line between two aerials passes over smooth ground, in metres, at a point <paramref name="fromAMetres"/> from
    /// the first and <paramref name="fromBMetres"/> from the second: the line itself, less the planet's bulge between them.
    /// </summary>
    public static double LineHeightMetres(double heightAMetres, double heightBMetres, double fromAMetres, double fromBMetres)
    {
        double total = fromAMetres + fromBMetres;
        if (total <= 0) return Math.Min(heightAMetres, heightBMetres);
        return heightAMetres + (heightBMetres - heightAMetres) * fromAMetres / total - BulgeMetres(fromAMetres, fromBMetres);
    }

    /// <summary>
    /// How far the planet bulges up into a straight line between two points on it, in metres, at a point this far from each (Radio
    /// 0.48.0, for the side view of a path between tiles): d1 d2 / (2 R), with the radius a radio wave sees. A side view draws the ground
    /// raised by it, so the line between the aerials can be drawn straight.
    /// </summary>
    public static double BulgeMetres(double fromAMetres, double fromBMetres) =>
        fromAMetres <= 0 || fromBMetres <= 0 ? 0.0 : fromAMetres * fromBMetres / (2.0 * EffectiveEarthRadiusMetres);

    /// <summary>
    /// How far an edge stands into the line, as a figure that holds for every band: its height over the line times the square root of
    /// 2 (d1 + d2) / (d1 d2). Divided by the square root of the wavelength it is the knife-edge's own parameter (ITU-R P.526's v).
    /// </summary>
    public static double EdgeFigure(double overLineMetres, double fromAMetres, double fromBMetres) =>
        fromAMetres <= 0 || fromBMetres <= 0 ? double.NegativeInfinity
            : overLineMetres * Math.Sqrt(2.0 * (fromAMetres + fromBMetres) / (fromAMetres * fromBMetres));

    /// <summary>What a knife edge costs, in dB, by its parameter v: nothing well under the line, 6 dB grazing it, more the deeper the shadow.</summary>
    public static double KnifeEdgeLossDb(double v) =>
        !(v > ClearBelow) ? 0.0 : Math.Max(0.0, 6.9 + 20.0 * Math.Log10(Math.Sqrt((v - 0.1) * (v - 0.1) + 1.0) + v - 0.1));

    /// <summary>What an edge costs on a band, in dB, from its figure: a shorter wave bends less, so it loses more.</summary>
    public static double LossDb(double edgeFigure, double megahertz) =>
        megahertz <= 0 || double.IsNegativeInfinity(edgeFigure) || double.IsNaN(edgeFigure) ? 0.0
            : KnifeEdgeLossDb(edgeFigure / Math.Sqrt(LightMetresPerMicrosecond / megahertz));

    /// <summary>The edge that decides: which tile on the way, its figure, and how far it stands over the line.</summary>
    public readonly record struct Edge(int Index, double Figure, double OverLineMetres)
    {
        public static readonly Edge None = new(-1, double.NegativeInfinity, 0.0);
        public bool Any => Index >= 0;
    }

    /// <summary>
    /// The edge standing furthest into the line between two aerials, over the ground of the tiles between them in order, taken as evenly
    /// spaced along <paramref name="totalMetres"/>. Flat ground (0 or less) is passed over. <see cref="Edge.None"/> when nothing stands
    /// high enough to matter on any band.
    /// </summary>
    public static Edge Worst(IReadOnlyList<double> groundMetres, double totalMetres, double heightAMetres, double heightBMetres)
    {
        Edge worst = Edge.None;
        int count = groundMetres.Count;
        if (count == 0 || totalMetres <= 0) return worst;
        double step = totalMetres / (count + 1);
        for (int i = 0; i < count; i++)
        {
            double ground = groundMetres[i];
            if (ground <= 0) continue;
            double fromA = step * (i + 1);
            double fromB = totalMetres - fromA;
            double over = ground - LineHeightMetres(heightAMetres, heightBMetres, fromA, fromB);
            double figure = EdgeFigure(over, fromA, fromB);
            if (figure > worst.Figure) worst = new Edge(i, figure, over);
        }
        return worst;
    }
}
