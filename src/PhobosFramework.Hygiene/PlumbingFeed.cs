using System.Collections.Generic;
using DubsBadHygiene;
using Verse;

namespace Phobos.Framework.Hygiene;

/// <summary>What a building found when it tried to draw water.</summary>
public enum WaterDraw
{
    Flowing,
    /// <summary>No plumbing with a water tower under it or against its side.</summary>
    NoPlumbing,
    /// <summary>Plumbing with a tower, but too little water in it.</summary>
    TowersEmpty,
}

/// <summary>
/// A building's water supply from Dubs Bad Hygiene's plumbing, from any pipe under it or against its side. It only reads the plumbing:
/// it is deliberately not one of Dubs' own pipe comps, which would delete the plumbing under the building (on load too) and leave gaps
/// in the player's pipe runs if a Phobos mod were removed. One per building, held by the building's comp; derived, so never saved.
/// Moved here from D'thorga Factory Additions (7 Nov 2026) so the factory machines and the oxygen electrolyser share one copy.
/// </summary>
public sealed class PlumbingFeed
{
    CompPipe? feed;

    /// <summary>Dubs Bad Hygiene's lite mode has no plumbing, so nothing could ever supply water.</summary>
    public static bool LiteMode => DubsBadHygiene.Settings.LiteMode;

    /// <summary>Draws the water from plumbing under or beside the building, or says why it couldn't. Quality is the water's, for
    /// buildings that care about it.</summary>
    public WaterDraw TryPull(Thing building, float litres, out ContaminationLevel quality) =>
        TryPullFrom(FindNet(building, litres), litres, out quality);

    /// <summary>What a draw of these litres would find now, without drawing anything (a vehicle's service checks before work starts).</summary>
    public WaterDraw Check(Thing building, float litres)
    {
        PlumbingNet? net = FindNet(building, litres);
        if (net == null || net.WaterTowers.Count == 0) return WaterDraw.NoPlumbing;
        return net.WaterStorage >= litres ? WaterDraw.Flowing : WaterDraw.TowersEmpty;
    }

    /// <summary>
    /// Draws the water from a net the caller already knows, such as a fixture's own pipe (D'thorga Water Tap's taps, which can adopt
    /// this when next touched), with the same three answers: no net or no tower is no plumbing, too little water is empty towers.
    /// </summary>
    public static WaterDraw TryPullFrom(PlumbingNet? net, float litres, out ContaminationLevel quality)
    {
        quality = default;
        if (net == null || net.WaterTowers.Count == 0) return WaterDraw.NoPlumbing;
        return net.PullWater(litres, out quality) ? WaterDraw.Flowing : WaterDraw.TowersEmpty;
    }

    /// <summary>
    /// The plumbing to draw from: the last one used while it still has enough, else any within reach that has enough, else one with a
    /// water tower so the building can say the tower is empty. Null if none has a tower. Looking again costs little and happens only
    /// while the building is short of water.
    /// </summary>
    PlumbingNet? FindNet(Thing building, float litres)
    {
        if (feed != null && feed.parent.Spawned && !feed.closed && feed.pipeNet is { } current && current.WaterStorage >= litres)
        {
            return current;
        }
        feed = null;
        PlumbingNet? withEmptyTower = null;
        Map map = building.Map;
        foreach (IntVec3 cell in FeedCells(building))
        {
            if (!cell.InBounds(map)) continue;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                CompPipe? pipe = things[i].TryGetComp<CompPipe>();
                // Dubs' water and sewage share one plumbing net, whose pipes are all in Sewage mode.
                if (pipe == null || pipe.mode != PipeType.Sewage || pipe.closed || pipe.pipeNet is not { } net) continue;
                if (net.WaterStorage >= litres)
                {
                    feed = pipe;
                    return net;
                }
                if (net.WaterTowers.Count > 0) withEmptyTower = net;
            }
        }
        return withEmptyTower;
    }

    static IEnumerable<IntVec3> FeedCells(Thing building)
    {
        foreach (IntVec3 cell in building.OccupiedRect()) yield return cell;
        foreach (IntVec3 cell in GenAdj.CellsAdjacentCardinal(building)) yield return cell;
    }
}
