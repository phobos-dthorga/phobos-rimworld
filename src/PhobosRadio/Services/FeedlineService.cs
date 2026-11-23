using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PipeSystem;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>One run of one coax type: how many metres of it, and its loss law.</summary>
public readonly record struct FeedSegment(float Metres, RadioCoaxExtension Coax)
{
    public float LossDb(BandDef band) => (float)Physics.Coax.LossDb(Metres, Coax.lossDbPer100m, Coax.referenceMHz, band.centreMHz);
}

/// <summary>
/// The cable between a station and one antenna: its coax by type, and the amplifiers the path passes through on the way (Radio 0.5.0).
/// An amplifier on a branch of the net, off the path, is not in it.
/// </summary>
public sealed class FeedPath
{
    public static readonly FeedPath None = new(new List<FeedSegment>(), new List<CompAmplifier>());

    public List<FeedSegment> Segments { get; }
    public List<CompAmplifier> Amplifiers { get; }

    public FeedPath(List<FeedSegment> segments, List<CompAmplifier> amplifiers)
    {
        Segments = segments;
        Amplifiers = amplifiers;
    }
}

/// <summary>
/// Walks a Vanilla Expanded Framework pipe net as a cable run. VEF keeps a net when a connector joins it and rebuilds it when
/// one leaves, so a cached route is keyed on the net, its size and the metres a cell stands for.
/// </summary>
public static class FeedlineService
{
    private static readonly RadioCoaxExtension DefaultCoax = new();
    // Weak on the net: a net holds its whole map, and neither a load nor a rebuilt net may keep one alive here.
    private static readonly ConditionalWeakTable<PipeNet, Dictionary<(Thing from, Thing to, float metresPerCell, int connectors), FeedPath?>> cache = new();
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
    /// The cable between two things on the same net, or null when no cable path joins them: its coax as segments and the
    /// amplifiers on the way. Breadth-first over the net's cells, cardinal steps; endpoint footprints cost nothing. Kept per net
    /// until a connector joins or leaves it; whether an amplifier is powered is read by its caller, never kept here.
    /// </summary>
    public static FeedPath? Path(PipeNet net, Thing from, Thing to, float metresPerCell)
    {
        var routes = cache.GetOrCreateValue(net);
        var key = (from, to, metresPerCell, net.connectors.Count);
        if (routes.TryGetValue(key, out var cached)) return cached;
        var path = Trace(net, from, to, metresPerCell);
        routes[key] = path;
        if (routes.Count > 256) routes.Clear();
        return path;
    }

    public static float LossDb(List<FeedSegment>? segments, BandDef band)
    {
        if (segments == null) return 0f;
        var total = 0f;
        foreach (var segment in segments) total += segment.LossDb(band);
        return total;
    }

    private static FeedPath? Trace(PipeNet net, Thing from, Thing to, float metresPerCell)
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

        // Walk back, counting coax cells by type and noting each amplifier the path passes through; endpoint cells are not coax.
        var metresByCoax = new Dictionary<RadioCoaxExtension, float>();
        var amplifiers = new List<CompAmplifier>();
        var current = reached.Value;
        while (parentOf.TryGetValue(current, out var previous))
        {
            if (!fromCells.Contains(current) && !toCells.Contains(current))
            {
                var amplifier = AmplifierAt(map, current);
                if (amplifier != null)
                {
                    if (!amplifiers.Contains(amplifier)) amplifiers.Add(amplifier);
                }
                else
                {
                    var coax = CoaxAt(map, current, net.def) ?? DefaultCoax;
                    metresByCoax[coax] = (metresByCoax.TryGetValue(coax, out var m) ? m : 0f) + metresPerCell;
                }
            }
            current = previous;
        }
        var segments = new List<FeedSegment>();
        foreach (var pair in metresByCoax) segments.Add(new FeedSegment(pair.Value, pair.Key));
        return new FeedPath(segments, amplifiers);
    }

    private static CompAmplifier? AmplifierAt(Map map, IntVec3 cell)
    {
        foreach (var thing in cell.GetThingList(map))
        {
            if (thing.TryGetComp<CompAmplifier>() is { } amplifier) return amplifier;
        }
        return null;
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
