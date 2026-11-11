using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework.Rules;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// How far a vehicle caravan can drive on the fuel aboard (Framework 0.85.0; docs/development/vehicle-presentation-design.md, P7b), on
/// Vehicle Framework's own world costs: each step costs the ticks Vehicle Framework's caravan follower would spend moving into the next tile
/// (terrain, hills, winter and roads, for the caravan's slowest speed), and the fuel burns only while it moves, at Vehicle Framework's world
/// rate with our fittings' and tunes' factor. The vehicle that runs dry first sets the reach. Worked out when asked for, then kept until
/// the caravan's tile, its fuel (to a hundredth of the tank) or its speed changes; nothing per tick.
/// </summary>
public static class VehicleRange
{
    /// <summary>The most tiles one search looks at (authored: a full tank on open ground stays well inside it), so a huge budget cannot
    /// stall the game.</summary>
    public const int MaxTiles = 12000;

    /// <summary>How finely the fuel is followed before the reach is worked out again: a hundredth of the tank (authored).</summary>
    const float FuelSteps = 100f;

    /// <summary>How far above the globe the edge is drawn, as the game draws a caravan's path (WorldPath.DrawPath).</summary>
    const float Lift = 0.08f;

    public sealed class Reach
    {
        /// <summary>The ticks of driving the fuel aboard allows.</summary>
        public float BudgetTicks;
        /// <summary>The tiles reached.</summary>
        public int Tiles;
        /// <summary>The edge, as line segments on the globe.</summary>
        public readonly List<(Vector3 a, Vector3 b)> Edge = new();
        /// <summary>Whether the search hit its tile limit, so the edge drawn is short of the true reach.</summary>
        public bool Capped;
        internal int Tile = -1;
        internal int FuelKey = -1;
        internal int TicksPerMove = -1;
    }

    static readonly ConditionalWeakTable<VehicleCaravan, Reach> Cache = new();

    /// <summary>The ticks of driving the fuel aboard allows, the least of every vehicle that burns fuel, or null when none does.</summary>
    public static float? BudgetTicks(VehicleCaravan caravan, out int fuelKey)
    {
        float? budget = null;
        fuelKey = 0;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            CompFueledTravel? fuel = vehicle.CompFueledTravel;
            if (fuel == null) continue;
            float factor = vehicle.GetComp<CompPhobosVehicleSystems>()?.Fittings.TunedFuelFactor ?? 1f;
            float perTick = fuel.ConsumptionRateWorldPerTick * factor;
            if (perTick <= 0f) continue;
            float ticks = fuel.Fuel / perTick;
            if (budget == null || ticks < budget) budget = ticks;
            fuelKey = fuelKey * 31 + Mathf.RoundToInt(fuel.FuelPercent * FuelSteps);
        }
        return budget;
    }

    /// <summary>The caravan's reach, worked out again only when its tile, fuel or speed has changed; null when nothing in it burns fuel.</summary>
    public static Reach? For(VehicleCaravan caravan)
    {
        float? budget = BudgetTicks(caravan, out int fuelKey);
        if (budget == null || !caravan.Tile.Valid) return null;
        int ticksPerMove = caravan.TicksPerMove;
        if (Cache.TryGetValue(caravan, out Reach? cached) && cached.Tile == caravan.Tile.tileId && cached.FuelKey == fuelKey && cached.TicksPerMove == ticksPerMove)
        {
            return cached;
        }
        Reach reach = Work(caravan, budget.Value, ticksPerMove);
        reach.Tile = caravan.Tile.tileId;
        reach.FuelKey = fuelKey;
        reach.TicksPerMove = ticksPerMove;
        Cache.Remove(caravan);
        Cache.Add(caravan, reach);
        return reach;
    }

    static Reach Work(VehicleCaravan caravan, float budget, int ticksPerMove)
    {
        using Phobos.Framework.Debug.PhobosTiming.Scope timing = Phobos.Framework.Debug.PhobosTiming.Measure("vehicle range");
        PlanetLayer layer = caravan.Tile.Layer;
        WorldGrid grid = Find.WorldGrid;
        WorldVehiclePathGrid paths = WorldVehiclePathGrid.Instance;
        List<VehiclePawn> vehicles = caravan.VehiclesListForReading;
        var tiles = new List<PlanetTile>();

        void Neighbours(int tile, List<int> around)
        {
            tiles.Clear();
            grid.GetTileNeighbors(new PlanetTile(tile, layer), tiles);
            foreach (PlanetTile next in tiles) around.Add(next.tileId);
        }

        double Cost(int from, int to)
        {
            var end = new PlanetTile(to, layer);
            foreach (VehiclePawn vehicle in vehicles)
            {
                if (!paths.Passable(end, vehicle.VehicleDef)) return -1;
            }
            return VehicleCaravan_PathFollower.CostToMove(vehicles, ticksPerMove, new PlanetTile(from, layer), end);
        }

        Dictionary<int, double> reached = TravelReachRule.Within(caravan.Tile.tileId, budget, Neighbours, Cost, MaxTiles);
        var reach = new Reach { BudgetTicks = budget, Tiles = reached.Count, Capped = reached.Count >= MaxTiles };
        var inside = new List<Vector3>();
        var outside = new List<Vector3>();
        foreach ((int a, int b) in TravelReachRule.Boundary(reached, Neighbours))
        {
            // The shared side of two neighbouring tiles: the two corners they have in common.
            grid.GetTileVertices(new PlanetTile(a, layer), inside);
            grid.GetTileVertices(new PlanetTile(b, layer), outside);
            Vector3? first = null;
            foreach (Vector3 corner in inside)
            {
                bool shared = false;
                foreach (Vector3 other in outside)
                {
                    if ((corner - other).sqrMagnitude < 1e-4f) { shared = true; break; }
                }
                if (!shared) continue;
                Vector3 lifted = corner + corner.normalized * Lift;
                if (first == null) first = lifted;
                else { reach.Edge.Add((first.Value, lifted)); break; }
            }
        }
        return reach;
    }
}
