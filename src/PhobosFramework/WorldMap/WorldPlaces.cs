using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.WorldMap;

/// <summary>
/// Places on the world map in the player's words, and how far they are in real terms (Framework 0.181.0; docs/development/vehicle-emergent-play.md):
/// for logbooks, causes and warnings that say where something happened and how far home is. Reads the game's own world objects and biomes,
/// so other mods' settlements and biomes count. A tile is about 30 km across (the owner's figure, <see cref="WorldScaleRule"/>).
/// </summary>
public static class WorldPlaces
{
    /// <summary>How near, in tiles, a settlement must be for a place to be named after it ("near Amber Anchorage"). Authored.</summary>
    public const float NearSettlementTiles = 2f;

    /// <summary>Where a thing is on the world: the tile of its map, or of the caravan, aircraft or anything else that holds it.</summary>
    public static PlanetTile TileOf(Thing? thing) => thing?.Tile ?? PlanetTile.Invalid;

    /// <summary>
    /// A place in words, never empty: a settlement's name at the tile, "near" one within <see cref="NearSettlementTiles"/>, else the
    /// tile's biome, else "somewhere" for a tile no longer known.
    /// </summary>
    public static string Describe(PlanetTile tile)
    {
        if (!tile.Valid || Find.World == null || Find.WorldGrid == null) return "PhobosPlace_Unknown".Translate();
        if (Find.WorldObjects.SettlementAt(tile) is { } at) return "PhobosPlace_At".Translate(at.LabelCap.Named("PLACE"));
        Settlement? nearest = null;
        float best = NearSettlementTiles;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Tile.Layer != tile.Layer) continue;
            float tiles = Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile);
            if (tiles > best) continue;
            best = tiles;
            nearest = settlement;
        }
        if (nearest != null) return "PhobosPlace_Near".Translate(nearest.LabelCap.Named("PLACE"));
        BiomeDef? biome = Find.WorldGrid[tile]?.PrimaryBiome;
        return biome != null ? "PhobosPlace_In".Translate(biome.label.Named("BIOME")) : "PhobosPlace_Unknown".Translate();
    }

    /// <summary>The nearest of the player's settlements on the same layer, and how many tiles away it is; null when the player has none.</summary>
    public static Settlement? NearestHome(PlanetTile tile, out float tiles)
    {
        tiles = 0f;
        if (!tile.Valid || Find.World == null) return null;
        Settlement? nearest = null;
        float best = float.MaxValue;
        foreach (Settlement settlement in Find.WorldObjects.Settlements)
        {
            if (settlement.Faction != Faction.OfPlayer || settlement.Tile.Layer != tile.Layer) continue;
            float distance = Find.WorldGrid.ApproxDistanceInTiles(tile, settlement.Tile);
            if (distance >= best) continue;
            best = distance;
            nearest = settlement;
        }
        if (nearest != null) tiles = best;
        return nearest;
    }

    /// <summary>A distance in tiles, in the player's words: whole kilometres (WorldScaleRule).</summary>
    public static string DistanceText(float tiles) => "PhobosPlace_Kilometres".Translate(WorldScaleRule.Kilometres(tiles).ToString("0").Named("KM"));
}
