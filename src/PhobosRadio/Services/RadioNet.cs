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
        CollectStations();
        if (stations.Count == 0) return;
        CollectWornRadios();
        if (wornRadios.Count == 0) return;

        var metresPerCell = RadioMod.Settings.metresPerCell;
        noise.Snapshot(map, rules);
        foreach (var station in stations)
        {
            var stationEnd = StationEndpoint(station);
            antennaLabelByStation[station.parent] = stationEnd.AntennaLabel;
            foreach (var handheld in wornRadios)
            {
                var wearer = handheld.Wearer!;
                var handheldEnd = RadioEndpoint.BuiltIn(handheld.parent, wearer.Position, handheld);
                var link = LinkBudgetService.Evaluate(map, handheldEnd, stationEnd, noise, rules, metresPerCell);
                if (link == null) continue;
                pairs.Add(link);
                if (link.InTouch) inTouchByStation[station.parent] = InTouchCount(station.parent) + 1;
                if (!bestByPawn.TryGetValue(wearer, out var best) || link.MarginDb > best.MarginDb) bestByPawn[wearer] = link;
            }
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

    /// <summary>The station's best antenna. Phase 1 commit 3: the built-in whip; feedline antennas follow.</summary>
    private RadioEndpoint StationEndpoint(CompRadio station) =>
        RadioEndpoint.BuiltIn(station.parent, station.parent.Position, station);
}
