using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The map's radio picture: every worn hand-held evaluated against every base station, recomputed on
/// a short interval and never saved. Gizmos, inspect strings and the thought read this cache; nothing
/// else computes a link.
/// </summary>
public sealed class RadioNet : MapComponent
{
    private static Map? lastMap;
    private static RadioNet? lastNet;

    private readonly NoiseService noise = new();
    private readonly List<LinkEvaluation> pairs = new();
    private readonly Dictionary<Pawn, LinkEvaluation> bestByPawn = new();
    private readonly Dictionary<Thing, int> inTouchByStation = new();
    private readonly Dictionary<Thing, string> antennaLabelByStation = new();
    private readonly List<CompRadio> stations = new();
    private readonly List<CompRadio> wornRadios = new();
    private int nextRecomputeTick = -1;
    private bool dirty = true;

    public RadioNet(Map map) : base(map) { }

    /// <summary>The net for a map, with a one-entry cache so callers never scan the component list each time.</summary>
    public static RadioNet For(Map map)
    {
        if (map != lastMap || lastNet == null)
        {
            lastNet = map.GetComponent<RadioNet>();
            lastMap = map;
        }
        return lastNet;
    }

    public override void MapComponentTick()
    {
        if (dirty || Find.TickManager.TicksGame >= nextRecomputeTick) Recompute();
    }

    public override void MapRemoved()
    {
        Clear();
        if (lastMap == map) { lastMap = null; lastNet = null; }
    }

    /// <summary>Whether a pawn is spawned, free and of the colony: the only pawns whose radios count.</summary>
    public static bool Counts(Pawn pawn) => pawn.Spawned && pawn.IsFreeNonSlaveColonist;

    public bool TryGetBest(Pawn pawn, out LinkEvaluation? link)
    {
        EnsureFresh();
        return bestByPawn.TryGetValue(pawn, out link);
    }

    public IReadOnlyList<LinkEvaluation> PairsFor(Thing station)
    {
        EnsureFresh();
        var result = new List<LinkEvaluation>();
        foreach (var pair in pairs) if (pair.B.Thing == station) result.Add(pair);
        return result;
    }

    public int InTouchCount(Thing station)
    {
        EnsureFresh();
        return inTouchByStation.TryGetValue(station, out var count) ? count : 0;
    }

    public string AntennaLabelFor(Thing station)
    {
        EnsureFresh();
        return antennaLabelByStation.TryGetValue(station, out var label) ? label : "PhobosRadio_AntennaBuiltIn".Translate();
    }

    public void MarkDirty() => dirty = true;

    /// <summary>Local noise at a cell from the last snapshot, in dBm, or −∞ when nothing reaches it.</summary>
    public double NoiseAt(IntVec3 cell)
    {
        EnsureFresh();
        return noise.LocalNoiseDbm(cell, RadioMod.Settings.metresPerCell);
    }

    private void EnsureFresh()
    {
        if (dirty) Recompute();
    }

    private void Clear()
    {
        pairs.Clear();
        bestByPawn.Clear();
        inTouchByStation.Clear();
        antennaLabelByStation.Clear();
        stations.Clear();
        wornRadios.Clear();
    }

    private void Recompute()
    {
        var rules = RadioRulesDef.Current;
        dirty = false;
        nextRecomputeTick = Find.TickManager.TicksGame + rules.recomputeIntervalTicks;
        Clear();
        noise.Snapshot(map, rules);
        CollectStations();
        if (stations.Count == 0) return;
        CollectWornRadios();
        if (wornRadios.Count == 0) return;

        var metresPerCell = RadioMod.Settings.metresPerCell;
        foreach (var station in stations)
        {
            var stationEnds = StationEndpoints(station, metresPerCell);
            string? antennaUsed = null;
            foreach (var handheld in wornRadios)
            {
                var wearer = handheld.Wearer!;
                var handheldEnd = RadioEndpoint.BuiltIn(handheld.parent, wearer.Position, handheld);
                LinkEvaluation? link = null;
                foreach (var stationEnd in stationEnds)
                {
                    var candidate = LinkBudgetService.Evaluate(map, handheldEnd, stationEnd, noise, rules, metresPerCell);
                    if (candidate != null && (link == null || candidate.MarginDb > link.MarginDb)) link = candidate;
                }
                if (link == null) continue;
                pairs.Add(link);
                antennaUsed ??= link.B.AntennaLabel;
                if (link.InTouch) inTouchByStation[station.parent] = InTouchCount(station.parent) + 1;
                if (!bestByPawn.TryGetValue(wearer, out var best) || link.MarginDb > best.MarginDb) bestByPawn[wearer] = link;
            }
            antennaLabelByStation[station.parent] = antennaUsed ?? stationEnds[0].AntennaLabel;
        }
    }

    private void CollectStations()
    {
        foreach (var building in map.listerBuildings.allBuildingsColonist)
        {
            var radio = building.TryGetComp<CompRadio>();
            if (radio != null && radio.Props.isStation) stations.Add(radio);
        }
    }

    private void CollectWornRadios()
    {
        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (!Counts(pawn) || pawn.apparel == null) continue;
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var radio = apparel.TryGetComp<CompRadio>();
                if (radio != null && !radio.Props.isStation) wornRadios.Add(radio);
            }
        }
    }

    /// <summary>
    /// Every antenna a station can use: its built-in whip, plus each antenna building on its coax that a
    /// cable path reaches. The link picks the best of them, so the best antenna for the band emerges.
    /// </summary>
    private static List<RadioEndpoint> StationEndpoints(CompRadio station, float metresPerCell)
    {
        var ends = new List<RadioEndpoint> { RadioEndpoint.BuiltIn(station.parent, station.parent.Position, station) };
        var port = station.parent.TryGetComp<CompFeedlinePort>();
        if (port?.PipeNet == null) return ends;
        foreach (var antenna in FeedlineService.AntennasOn(port))
        {
            if (!antenna.parent.Spawned) continue;
            var segments = FeedlineService.Segments(port.PipeNet, station.parent, antenna.parent, metresPerCell);
            if (segments == null) continue;
            var end = RadioEndpoint.ViaAntenna(station, antenna, segments);
            if (end.Bands.Count > 0) ends.Add(end);
        }
        return ends;
    }
}
