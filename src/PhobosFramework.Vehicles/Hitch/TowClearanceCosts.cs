using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The wider way round (Framework 0.277.0; docs/development/systems/hitches-and-trailers.md): the extra cost of each cell to a vehicle that
/// drives a tow, laid into Vehicle Framework's path finder through its own extension point. Vehicle Framework finds every
/// <see cref="VehicleCostOffsets"/> by type and makes one per map; this one applies only to a request from a vehicle with something on its
/// hitch, so nothing changes for any other vehicle. Two layers: the clearance costs near ground a trailer cannot stand on
/// (<see cref="TowRouteRule.ClearanceCosts"/>), worked out again from the whole map whenever a towing vehicle asks for a path after any cell's
/// path cost has changed; and snags, the costs laid for a while around a spot where a checked route would have run a trailer into something
/// (<see cref="CompPhobosHitch"/>'s route check). Derived, never saved. Main thread only: Vehicle Framework gathers the grids for a request
/// before handing it to its path-finding thread, and reads a copy.
/// </summary>
public sealed class TowClearanceCosts : VehicleCostOffsets
{
    static readonly Dictionary<Map, TowClearanceCosts> byMap = new();

    readonly short[] written;
    readonly Dictionary<int, (int Cost, int Until)> snags = new();
    int nextExpiry = int.MaxValue;
    bool stale = true;
    bool disposed;

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public TowClearanceCosts(Map map, PathFinderManager manager) : base(map, manager)
    {
        written = new short[map.cellIndices.NumGridCells];
        // Any change to a cell's path cost (a wall built or mined, a fence, a door, deep water) may move the clearance: worked out again at the
        // next towing vehicle's request, so a busy map pays only a flag.
        map.events.PathCostRecalculate += OnPathCost;
        lock (byMap) byMap[map] = this;
    }

    /// <summary>This map's grid, or null while Vehicle Framework's newer path finder is not the one in use.</summary>
    public static TowClearanceCosts? On(Map map)
    {
        lock (byMap) return byMap.TryGetValue(map, out TowClearanceCosts? grid) ? grid : null;
    }

    void OnPathCost(IntVec3 _) => stale = true;

    public override bool ShouldApplyFor(in PathSettings settings)
    {
        if (disposed || !VehicleSystemsMod.Settings.hitchWiderWay) return false;
        if (settings.vehicle is not { Spawned: true } vehicle || vehicle.Map != map || !CompPhobosHitch.TakesWiderWay(vehicle)) return false;
        Refresh();
        return true;
    }

    /// <summary>Never asked for cell by cell: <see cref="Refresh"/> writes every cell that changed.</summary>
    public override void Update(int index)
    {
    }

    /// <summary>Lays a snag's cost around these cells, for the rules' ticks: the next path a towing vehicle asks for keeps further off them.</summary>
    public void Snag(IEnumerable<IntVec3> cells)
    {
        PhobosVehicleRulesDef rules = Rules;
        int until = Find.TickManager.TicksGame + rules.towSnagTicks;
        int reach = rules.towSnagReachCells;
        foreach (IntVec3 at in cells)
        {
            foreach (IntVec3 c in CellRect.CenteredOn(at, reach))
            {
                if (!c.InBounds(map)) continue;
                int index = map.cellIndices.CellToIndex(c);
                snags[index] = (rules.towSnagCost, until);
            }
        }
        if (until < nextExpiry) nextExpiry = until;
        stale = true;
    }

    void Refresh()
    {
        int now = Find.TickManager.TicksGame;
        if (now >= nextExpiry) Expire(now);
        if (!stale) return;
        stale = false;
        int width = map.Size.x, height = map.Size.z;
        var blocked = new bool[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++) blocked[x + z * width] = !new IntVec3(x, 0, z).Walkable(map);
        }
        short[] costs = TowRouteRule.ClearanceCosts(blocked, width, height, Rules.towClearanceCosts);
        foreach (KeyValuePair<int, (int Cost, int Until)> snag in snags)
        {
            int cell = snag.Key;
            costs[cell] = (short)System.Math.Min(short.MaxValue, costs[cell] + snag.Value.Cost);
        }
        // Only what changed is written: each write tells Vehicle Framework's path finders their copy of the grids is out of date.
        for (int i = 0; i < costs.Length; i++)
        {
            if (costs[i] == written[i]) continue;
            written[i] = costs[i];
            this[i] = costs[i];
        }
    }

    void Expire(int now)
    {
        nextExpiry = int.MaxValue;
        var gone = new List<int>();
        foreach (KeyValuePair<int, (int Cost, int Until)> snag in snags)
        {
            if (snag.Value.Until <= now) gone.Add(snag.Key);
            else if (snag.Value.Until < nextExpiry) nextExpiry = snag.Value.Until;
        }
        foreach (int cell in gone) snags.Remove(cell);
        if (gone.Count > 0) stale = true;
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;
        map.events.PathCostRecalculate -= OnPathCost;
        lock (byMap)
        {
            if (byMap.TryGetValue(map, out TowClearanceCosts? grid) && grid == this) byMap.Remove(map);
        }
        base.Dispose();
    }
}
