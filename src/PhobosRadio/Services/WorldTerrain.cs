using System.Collections.Generic;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Physics;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The high ground between two world tiles, as a link between them meets it (Radio 0.41.0; the owner's card of 27 Nov 2026, "Add terrain
/// blocking"; docs/development/radio-world-coverage.md). It walks the tiles of the straight way between the two (D'thorga Framework's
/// <see cref="TileAir.WalkLeg"/>, the walk a flight uses), reads each one's hills, and asks <see cref="TerrainRule"/> for the edge that
/// stands furthest into the line between the two aerials. The two ends' own tiles are left out: an aerial stands on its own hill.
/// <para>Cost: asked only when a link between tiles is worked out. One walk, a neighbour look a step (about seven small sums a tile of
/// distance), and one sum a tile on the way. Nothing is kept: another mod may change a tile's hills in play, and a link is asked rarely.</para>
/// </summary>
public static class WorldTerrain
{
    static readonly List<PlanetTile> walked = new();
    static readonly List<double> ground = new();

    /// <summary>What stands in the way: the edge's figure for every band, the tile it is on, and how far it stands over the line.</summary>
    public readonly record struct Blocking(double Figure, PlanetTile Tile, double OverLineMetres)
    {
        public static readonly Blocking None = new(double.NegativeInfinity, PlanetTile.Invalid, 0.0);
        public bool Any => !double.IsNegativeInfinity(Figure);

        /// <summary>What it costs on a band, in dB.</summary>
        public double LossDb(BandDef band) => TerrainRule.LossDb(Figure, band.centreMHz);
    }

    /// <summary>How high a tile's ground stands in the way of a link passing over it, in metres.</summary>
    public static double GroundOf(PlanetTile tile) => TerrainRule.GroundHeightMetres((int)Find.WorldGrid[tile].hilliness);

    /// <summary>What the game calls a tile's ground ("large hills", "mountainous"), for a line to the player.</summary>
    public static string GroundLabel(PlanetTile tile) => tile.Valid ? Find.WorldGrid[tile].hilliness.GetLabel() : string.Empty;

    /// <summary>
    /// The high ground that decides between two tiles, for aerials at these heights over their own tiles' plain (aerial and site together)
    /// and this far apart. <see cref="Blocking.None"/> with the rule switched off in the rules Def, for neighbouring tiles, for tiles on
    /// different layers of the planet, and where nothing stands high enough to matter.
    /// </summary>
    public static Blocking Between(PlanetTile a, PlanetTile b, double heightAMetres, double heightBMetres, double metres, RadioRulesDef rules)
    {
        if (!rules.terrainBlocksBetweenTiles || !Profile(a, b, ground, walked)) return Blocking.None;
        TerrainRule.Edge edge = TerrainRule.Worst(ground, metres, heightAMetres, heightBMetres);
        return edge.Any ? new Blocking(edge.Figure, walked[edge.Index + 1], edge.OverLineMetres) : Blocking.None;
    }

    /// <summary>
    /// The ground of the tiles on the way from one tile to another, in order, both ends' own tiles left out: what <see cref="Between"/>
    /// reads, as a list (Radio 0.48.0, for the side view of a path). <paramref name="tiles"/>, when given, is filled with the tiles walked,
    /// both ends included, so entry i of the ground is tile i + 1. False, and the ground empty, for the same tile, neighbouring tiles,
    /// tiles on different layers of the planet, or a walk that does not arrive.
    /// </summary>
    public static bool Profile(PlanetTile a, PlanetTile b, List<double> groundMetres, List<PlanetTile>? tiles = null)
    {
        groundMetres.Clear();
        List<PlanetTile> steps = tiles ?? walked;
        if (a == b || !TileAir.WalkLeg(a, b, steps) || steps.Count < 3) return false;
        for (int i = 1; i < steps.Count - 1; i++) groundMetres.Add(GroundOf(steps[i]));
        return true;
    }
}
