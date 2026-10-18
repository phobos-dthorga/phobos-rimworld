using System.Collections.Generic;
using PipeSystem;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>One run of one coax type: how many metres of it, and its loss law.</summary>
public readonly record struct FeedSegment(float Metres, RadioCoaxExtension Coax)
{
    public float LossDb(BandDef band) => (float)Physics.Coax.LossDb(Metres, Coax.lossDbPer100m, Coax.referenceMHz, band.centreMHz);
}

/// <summary>
/// Walks a Vanilla Expanded Framework pipe net as a cable run. The net is rebuilt by VEF whenever a
/// connector spawns or despawns, so a cache keyed on the net instance is fresh by construction.
/// </summary>
public static class FeedlineService
{
    private static readonly RadioCoaxExtension DefaultCoax = new();
    private static readonly Dictionary<(PipeNet net, Thing from, Thing to), List<FeedSegment>?> cache = new();
    private static readonly Queue<IntVec3> queue = new();
    private static readonly Dictionary<IntVec3, int> seen = new();

    public static List<CompAntenna> AntennasOn(CompResource port)
    {
        var result = new List<CompAntenna>();
        if (port.PipeNet == null) return result;
        foreach (var connector in port.PipeNet.connectors)
        {
            var antenna = connector.parent.TryGetComp<CompAntenna>();
            if (antenna != null) result.Add(antenna);
        }
        return result;
    }

    public static CompRadio? StationOn(CompResource port)
    {
        if (port.PipeNet == null) return null;
        foreach (var connector in port.PipeNet.connectors)
        {
            var radio = connector.parent.TryGetComp<CompRadio>();
            if (radio != null && radio.Props.isStation) return radio;
        }
        return null;
    }

    /// <summary>
    /// The coax between two things on the same net as segments, or null when no cable path joins them.
    /// Breadth-first over the net's cells, cardinal steps; endpoint footprints cost nothing.
    /// </summary>
    public static List<FeedSegment>? Segments(PipeNet net, Thing from, Thing to, float metresPerCell)
    {
        var key = (net, from, to);
        if (cache.TryGetValue(key, out var cached)) return cached;
        var segments = Trace(net, from, to, metresPerCell);
        cache[key] = segments;
        if (cache.Count > 256) cache.Clear();
        return segments;
    }

    public static float LossDb(List<FeedSegment>? segments, BandDef band)
    {
        if (segments == null) return 0f;
        var total = 0f;
        foreach (var segment in segments) total += segment.LossDb(band);
        return total;
    }

    private static List<FeedSegment>? Trace(PipeNet net, Thing from, Thing to, float metresPerCell)
    {
        var map = net.map;
        var grid = net.networkGrid;
        var fromCells = from.OccupiedRect();
        var toCells = to.OccupiedRect();
        queue.Clear();
        seen.Clear();
        foreach (var cell in fromCells)
        {
            queue.Enqueue(cell);
            seen[cell] = -1;
        }
        var parentOf = new Dictionary<IntVec3, IntVec3>();
        IntVec3? reached = null;
        while (queue.Count > 0 && reached == null)
        {
            var cell = queue.Dequeue();
            foreach (var step in GenAdj.CardinalDirections)
            {
                var next = cell + step;
                if (seen.ContainsKey(next) || !next.InBounds(map) || !grid[next]) continue;
                seen[next] = 0;
                parentOf[next] = cell;
                if (toCells.Contains(next)) { reached = next; break; }
                queue.Enqueue(next);
            }
        }
        if (reached == null) return null;

        // Walk back, counting coax cells by type; endpoint cells are not coax.
        var metresByCoax = new Dictionary<RadioCoaxExtension, float>();
        var current = reached.Value;
        while (parentOf.TryGetValue(current, out var previous))
        {
            if (!fromCells.Contains(current) && !toCells.Contains(current))
            {
                var coax = CoaxAt(map, current, net.def) ?? DefaultCoax;
                metresByCoax[coax] = (metresByCoax.TryGetValue(coax, out var m) ? m : 0f) + metresPerCell;
            }
            current = previous;
        }
        var segments = new List<FeedSegment>();
        foreach (var pair in metresByCoax) segments.Add(new FeedSegment(pair.Value, pair.Key));
        return segments;
    }

    private static RadioCoaxExtension? CoaxAt(Map map, IntVec3 cell, PipeNetDef net)
    {
        foreach (var thing in cell.GetThingList(map))
        {
            if (net.pipeDefs != null && net.pipeDefs.Contains(thing.def))
            {
                return thing.def.GetModExtension<RadioCoaxExtension>();
            }
        }
        return null;
    }
}
