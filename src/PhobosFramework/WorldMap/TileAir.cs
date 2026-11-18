using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
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

    /// <summary>The most tiles a leg's walk takes before it gives up: further than any flight in the game (authored).</summary>
    public const int MaxWalkTiles = 2000;

    static readonly List<PlanetTile> neighbours = new();

    /// <summary>
    /// The highest ground on a straight leg between two surface tiles (D'thorga Framework 0.141.0, the cruising height between tiles): its
    /// highest tile of any kind and its highest mountainous one (NaN for none), both ends included. The walk steps each time to the neighbour
    /// whose centre is nearest the far end, which follows the great circle a flight draws. False, and nothing read, for a tile off the
    /// planet's surface or a walk that does not arrive.
    /// </summary>
    public static bool HighestOnLeg(PlanetTile from, PlanetTile to, out float highestMetres, out float highestMountainousMetres)
    {
        highestMetres = 0f;
        highestMountainousMetres = float.NaN;
        WorldGrid? grid = Find.WorldGrid;
        if (grid == null || !OnSurface(from) || !OnSurface(to) || from.Layer != to.Layer) return false;
        Vector3 target = grid.GetTileCenter(to);
        PlanetTile here = from;
        for (int step = 0; step <= MaxWalkTiles; step++)
        {
            Note(here, ref highestMetres, ref highestMountainousMetres);
            if (here == to) return true;
            grid.GetTileNeighbors(here, neighbours);
            PlanetTile next = here;
            float best = (grid.GetTileCenter(here) - target).sqrMagnitude;
            foreach (PlanetTile neighbour in neighbours)
            {
                float distance = (grid.GetTileCenter(neighbour) - target).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    next = neighbour;
                }
            }
            if (next == here) return false;
            here = next;
        }
        return false;
    }

    static bool OnSurface(PlanetTile tile) => tile.Valid && tile.LayerDef != null && !tile.LayerDef.isSpace;

    static void Note(PlanetTile tile, ref float highest, ref float highestMountainous)
    {
        if (Find.WorldGrid[tile] is not SurfaceTile surface) return;
        float metres = surface.elevation > 0f ? surface.elevation : 0f;
        if (metres > highest) highest = metres;
        if (surface.hilliness >= Hilliness.Mountainous && (float.IsNaN(highestMountainous) || metres > highestMountainous)) highestMountainous = metres;
    }
}
