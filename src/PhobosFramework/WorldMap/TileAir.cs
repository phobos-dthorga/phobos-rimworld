using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.WorldMap;

/// <summary>
/// How high a world tile stands, for anything the air's thinness matters to (D'thorga Framework 0.132.0; the rules are AirRule's). The
/// game gives every surface tile an elevation in metres above the sea (docs/development/world-elevation-and-descents.md); a local map
/// has no height of its own, so a map is as high as its tile. Only the planet's surface counts: in orbit a tile's "elevation" is the
/// layer's radius, and water reads as the sea.
/// </summary>
public static class TileAir
{
    /// <summary>The tile's height above the sea in metres: 0 for water, for a tile off the planet's surface and for no tile at all.</summary>
    public static float ElevationMetres(PlanetTile tile)
    {
        if (!tile.Valid || Find.WorldGrid == null || tile.LayerDef == null || tile.LayerDef.isSpace) return 0f;
        return Find.WorldGrid[tile] is SurfaceTile surface && surface.elevation > 0f ? surface.elevation : 0f;
    }

    /// <summary>The height of the tile a map stands on (a cave or other pocket map reads its parent's).</summary>
    public static float ElevationMetres(Map? map) => map == null ? 0f : ElevationMetres(map.Tile);
}
