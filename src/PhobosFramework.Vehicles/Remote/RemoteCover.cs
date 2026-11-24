using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Where on a map a vehicle driven from afar would keep its link, block by block (Framework 0.279.0; docs/development/autonomous-vehicles.md,
/// step 8): <see cref="RemoteCoverRule.Covered"/>, <see cref="RemoteCoverRule.Uncovered"/> or <see cref="RemoteCoverRule.NoSample"/>. A source
/// hands out a new one each time it works the map out again and never changes one it has handed out, so a reader knows a newer one by
/// its reference.
/// </summary>
public sealed class RemoteCover
{
    public RemoteCover(int blockCells, int width, int height, byte[] blocks)
    {
        BlockCells = blockCells;
        Width = width;
        Height = height;
        Blocks = blocks;
    }

    public int BlockCells { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>By row, <see cref="RemoteCoverRule.Blocks"/> of the width across. Never written after it is handed out.</summary>
    public byte[] Blocks { get; }
}

/// <summary>
/// A source of remote control that can say which ground it covers (Framework 0.279.0). Optional beside <see cref="IRemoteControlSource"/>:
/// a source with no notion of cover (a route, a mechanitor) simply does not take it, and its vehicles route as before.
/// </summary>
public interface IRemoteCoverSource
{
    /// <summary>The last whole cover map worked out for this map, or null while there is none yet. Read on the main thread, per path
    /// request; nothing is worked out in it.</summary>
    RemoteCover? CoverOf(Map map);
}

/// <summary>
/// Keeping to radio cover (Framework 0.279.0; docs/development/autonomous-vehicles.md, step 8): the extra cost of a cell to a vehicle driven
/// from afar where its link would fail, laid into Vehicle Framework's newer path finder through its own extension point, as the wider way
/// round for towing is (<see cref="TowClearanceCosts"/>). It applies only to a request from a vehicle the player has asked to be driven from
/// afar, whose source gives a cover map; every other vehicle's paths are untouched. Rewritten only when the source hands out a new map, and
/// then only the cells that changed. Derived, never saved; main thread only.
/// </summary>
public sealed class RemoteCoverCosts : VehicleCostOffsets
{
    readonly short[] written;
    RemoteCover? shown;
    int shownCost = -1;
    bool disposed;

    public RemoteCoverCosts(Map map, PathFinderManager manager) : base(map, manager)
    {
        written = new short[map.cellIndices.NumGridCells];
    }

    public override bool ShouldApplyFor(in PathSettings settings)
    {
        if (disposed || !VehicleSystemsMod.Settings.remoteKeepToCover || !RemoteControl.On) return false;
        if (settings.vehicle is not { Spawned: true } vehicle || vehicle.Map != map) return false;
        if (!RemoteControl.Wanted(vehicle) || CompPhobosFieldRun.DrivingARow(vehicle)) return false;
        if (RemoteControl.SourceFor(vehicle) is not IRemoteCoverSource source || source.CoverOf(map) is not { } cover) return false;
        if (cover.Width != map.Size.x || cover.Height != map.Size.z) return false;
        Show(cover);
        if (Seen.Wants("vehicles.remote-cover", vehicle))
        {
            Seen.Note("vehicles.remote-cover", vehicle, vehicle.LabelShort,
                values: new List<KeyValuePair<string, double>> { new("uncovered blocks", RemoteCoverRule.UncoveredCount(cover.Blocks)) });
        }
        return true;
    }

    /// <summary>Never asked for cell by cell: <see cref="Show"/> writes every cell that changed.</summary>
    public override void Update(int index)
    {
    }

    void Show(RemoteCover cover)
    {
        int cost = PhobosRemoteControlRulesDef.Current.uncoveredCellCost;
        if (ReferenceEquals(cover, shown) && cost == shownCost) return;
        shown = cover;
        shownCost = cost;
        short[] costs = RemoteCoverRule.Costs(cover.Blocks, cover.BlockCells, cover.Width, cover.Height, cost);
        // Only what changed is written: each write tells Vehicle Framework's path finders their copy of the grids is out of date.
        for (int i = 0; i < costs.Length && i < written.Length; i++)
        {
            if (costs[i] == written[i]) continue;
            written[i] = costs[i];
            this[i] = costs[i];
        }
    }

    public override void Dispose()
    {
        if (disposed) return;
        disposed = true;
        base.Dispose();
    }
}
