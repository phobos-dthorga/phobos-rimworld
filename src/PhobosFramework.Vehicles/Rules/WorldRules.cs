using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How the load-dependent fuel setting corrects Vehicle Framework's own fuel use (docs/development/vehicle-world-map-design.md). Game-free.</summary>
public static class FuelRules
{
    /// <summary>
    /// Fuel to burn (positive) or give back (negative) per hour beyond what Vehicle Framework burns on its own: VF's hourly rate
    /// times the share it burns now (1 moving, 0.5 idling on a map, 0 standing on the world map) against the share the engine model
    /// says it should burn, normalised so a nominal cruise matches the vehicle author's rate.
    /// </summary>
    public static double ExtraPerHour(double vfPerHour, double vfShare, double modelShare) => vfPerHour * (modelShare - vfShare);

    /// <summary>Vehicle Framework's world-map fuel rate per hour from its per-tick figure.</summary>
    public static double WorldPerHour(double perTick, int ticksPerHour) => perTick * ticksPerHour;
}

/// <summary>How a caravan's vehicles' speed caps combine (the drivetrain's power and traction limits, on the world map). Game-free.</summary>
public static class CaravanRules
{
    /// <summary>The caravan moves as its slowest vehicle: the smallest cap, 1 when no vehicle is capped, never below the crawl floor.</summary>
    public static double CombineCaps(IEnumerable<double> caps)
    {
        double worst = 1;
        foreach (double cap in caps)
        {
            if (cap < worst) worst = cap;
        }
        return Math.Max(DrivetrainModel.CrawlFloor, worst);
    }

    /// <summary>Ticks per move after the cap: more ticks for a slower caravan (a cap of 0.5 doubles them).</summary>
    public static int TicksPerMove(int ticks, double cap) => cap >= 1 ? ticks : (int)Math.Ceiling(ticks / Math.Max(DrivetrainModel.CrawlFloor, cap));
}

/// <summary>
/// The ground a caravan drives over, as a path cost the drivetrain's ground curves already read (soil 2, sand 4, soft sand and mud
/// 14, marsh 30), from what the Framework's BiomeGround reads off the tile. Game-free; every figure is authored
/// (docs/development/vehicle-crew-and-ground-design.md).
/// </summary>
public static class WorldGroundRules
{
    /// <summary>Path cost added per point of the biome's movement difficulty above 1: a swamp (4) adds 9, about soft mud; a jungle
    /// (2) adds 3, undergrowth on soil (authored).</summary>
    public const double PathCostPerBiomeDifficulty = 3;

    /// <summary>Path cost added per point of the game's winter offset (0 to 2): deep winter adds 8, the game's own figure for medium
    /// snow on a map (authored mapping).</summary>
    public const double PathCostPerWinterOffset = 4;

    /// <summary>
    /// The tile's path cost for a vehicle: on a road, the road (0) plus the snow on it; off it, the biome's typical terrain, the
    /// biome's difficulty above open country, and the snow. Running gear with its own surface (rails) never asks.
    /// </summary>
    public static double PathCost(double terrainPathCost, double biomeDifficulty, bool road, double winterOffset)
    {
        double snow = Math.Max(0, winterOffset) * PathCostPerWinterOffset;
        if (road) return snow;
        return Math.Max(0, terrainPathCost) + Math.Max(0, biomeDifficulty - 1) * PathCostPerBiomeDifficulty + snow;
    }

    /// <summary>The grade a caravan climbs, by the tile's hilliness (authored: flat 0%, small hills 2%, large hills 5%, mountainous 8%;
    /// the numbers are the enum's order in the game, 1 flat to 4 mountainous).</summary>
    public static double Grade(int hilliness) => hilliness switch
    {
        2 => 0.02,
        3 => 0.05,
        4 => 0.08,
        _ => 0,
    };
}
