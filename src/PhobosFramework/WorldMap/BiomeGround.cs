using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

// Not "Phobos.Framework.World": a namespace named World would hide the game's Verse.World class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.WorldMap;

/// <summary>
/// What the ground of a world tile is like, read from the game's own Defs rather than biome names (so other mods' biomes count): the
/// biome's typical terrain (its terrain-by-fertility entry at a middling fertility), the biome's movement difficulty, and the game's
/// own winter offset for snow. For anything that moves across the world map and cares about the ground: vehicles, sledges, carts.
/// </summary>
public static class BiomeGround
{
    /// <summary>The fertility at which a biome's typical terrain is read (authored: the middle of the game's usual 0 to 1 range).</summary>
    public const float TypicalFertility = 0.5f;

    /// <summary>The path cost taken when a biome lists no terrain by fertility (some modded biomes): soil's (authored default).</summary>
    public const int UnknownTerrainPathCost = 2;

    static readonly Dictionary<BiomeDef, TerrainDef?> typical = new();

    /// <summary>The terrain a biome's maps are mostly made of (sand for deserts, soil for forests), or null when it lists none.</summary>
    public static TerrainDef? TypicalTerrain(BiomeDef biome)
    {
        if (biome == null) return null;
        if (!typical.TryGetValue(biome, out TerrainDef? terrain))
        {
            terrain = biome.terrainsByFertility is { Count: > 0 } list ? TerrainThreshold.TerrainAtValue(list, TypicalFertility) : null;
            typical[biome] = terrain;
        }
        return terrain;
    }

    /// <summary>The tile's ground, or false when the tile is invalid or has no biome.</summary>
    public static bool TryRead(PlanetTile tile, out TileGround ground)
    {
        ground = default;
        if (!tile.Valid || Find.WorldGrid == null) return false;
        Tile? data = Find.WorldGrid[tile];
        BiomeDef? biome = data?.PrimaryBiome;
        if (data == null || biome == null) return false;
        ground = new TileGround
        {
            Biome = biome,
            TerrainPathCost = TypicalTerrain(biome)?.pathCost ?? UnknownTerrainPathCost,
            BiomeDifficulty = biome.movementDifficulty,
            Hilliness = data.hilliness,
            Road = data is SurfaceTile surface && surface.Roads != null && surface.Roads.Count > 0,
            WinterOffset = WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(tile),
        };
        return true;
    }
}

/// <summary>A world tile's ground as <see cref="BiomeGround"/> reads it.</summary>
public struct TileGround
{
    public BiomeDef Biome;
    /// <summary>The path cost of the biome's typical terrain (soil 2, sand 4, soft sand and mud 14).</summary>
    public int TerrainPathCost;
    /// <summary>The biome's movement difficulty (1 for open country, 2 for jungle, 4 for swamp).</summary>
    public float BiomeDifficulty;
    public Hilliness Hilliness;
    public bool Road;
    /// <summary>The game's winter offset now: 0 without snow, up to 2 in deep winter.</summary>
    public float WinterOffset;
}
