using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Radio.Physics;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The coverage overlay (Radio 0.2.0, the radio visuals record's RV1): while a base station or an antenna is selected, every cell a
/// colonist could stand on is painted by the signal a hand-held there would have to spare with that station, through the selected
/// antenna or the best of the station's antennas.
/// <para>
/// <b>Performance is the mod's first concern</b> (owner, 23 Nov 2026), so this costs nothing unless a station or an antenna is
/// selected. While one is, it works cells out for at most <see cref="RadioRulesDef.coverageBudgetMsPerFrame"/> of real time a frame:
/// first a quick pass of one cell per block, then every cell. It works again only when a building or roof changes, the noise sources
/// change, or the cell scale does, and the picture is rebuilt only at the end of each pass. Nothing is saved.
/// </para>
/// <para>
/// Each cell goes through the same walk, inputs and arithmetic as a real contact (<see cref="ObstructionService.Walk"/>,
/// <see cref="LinkBudgetService.Inputs"/>, <see cref="LinkMath"/>), so the picture cannot disagree with the result. Two choices are
/// the agent's, for the owner to revise: every door counts as closed, so the picture does not redraw each time one swings; and each
/// cell shows the best of the hand-held kinds the game has (VHF or UHF), as if the colonist carried the right one.
/// </para>
/// </summary>
public sealed class RadioCoverage : MapComponent, ICellBoolGiver
{
    private static readonly ConditionalWeakTable<Map, RadioCoverage> byMap = new();
    private static List<Kind>? kinds;

    /// <summary>How often, in cells, the frame's time budget is checked: often enough to stop close to it.</summary>
    private const int CellsPerClockCheck = 16;

    private const int PassCoarse = 0;
    private const int PassFine = 1;
    private const int PassDone = 2;


    // What is asked for: the selected station or antenna, renewed every frame it stays selected.
    private Thing? asked;
    private int askedFrame = -10;

    // What the picture shows, and what it was worked out from.
    private Thing? subject;
    private int subjectChanges;
    private int subjectNoise;
    private float subjectMetresPerCell;
    private int changes;
    private bool listening;

    private float[] margins = System.Array.Empty<float>();
    private int pass = PassDone;
    private int cursor;
    private bool again;
    private bool showing;
    private CellBoolDrawer? drawer;
    private readonly Stopwatch watch = new();

    private readonly List<End> ends = new();
    private float leastBandFactor = 1f;
    private float fullScaleDb = 130f;

    public RadioCoverage(Map map) : base(map) { }

    public static RadioCoverage For(Map map)
    {
        if (byMap.TryGetValue(map, out var coverage)) return coverage;
        coverage = map.GetComponent<RadioCoverage>();
        if (coverage != null) byMap.AddOrUpdate(map, coverage);
        return coverage!;
    }

    /// <summary>Asks for the picture of a station or an antenna for this frame. Called while it is selected; nothing else starts work.</summary>
    public void Show(Thing thing)
    {
        if (thing.Map != map) return;
        asked = thing;
        askedFrame = RealTime.frameCount;
    }

    /// <summary>The selected thing's inspect line about the picture, or null when it is not the one shown.</summary>
    public string? InspectLine(Thing thing)
    {
        if (thing != subject || !showing) return null;
        if (ends.Count == 0) return "PhobosRadio_CoverageNoStation".Translate();
        if (pass == PassDone) return "PhobosRadio_CoverageShown".Translate();
        return "PhobosRadio_CoverageWorking".Translate(Progress().ToStringPercent().Named("PERCENT"));
    }

    public override void MapComponentUpdate()
    {
        showing = false;
        if (asked == null) return;
        if (RealTime.frameCount - askedFrame > 1 || !asked.Spawned || asked.Map != map || Find.CurrentMap != map)
        {
            asked = null;
            return;
        }
        showing = true;
        Listen();
        Refresh(asked);
        if (ends.Count == 0) return;
        Work(RadioRulesDef.Current.coverageBudgetMsPerFrame);
        Drawer.MarkForDraw();
        Drawer.CellBoolDrawerUpdate();
    }

    public override void MapRemoved()
    {
        if (listening)
        {
            map.events.BuildingSpawned -= OnBuilding;
            map.events.BuildingDespawned -= OnBuilding;
            map.events.RoofChanged -= OnRoof;
            listening = false;
        }
        byMap.Remove(map);
    }

    // ICellBoolGiver: what the game's cell drawer paints.
    public Color Color => Color.white;
    public bool GetCellBool(int index) => index < margins.Length && !float.IsNaN(margins[index]);
    public Color GetCellExtraColor(int index) => ColorFor(margins[index]);

    /// <summary>
    /// Works out a whole fine pass for a station or an antenna at once and says how long it took: the dev menu's measure of the
    /// overlay's cost on a real colony. It leaves the picture as it was.
    /// </summary>
    public (double milliseconds, int cells, int ends) TimeFullPass(Thing thing)
    {
        var saved = (float[])margins.Clone();
        subject = null;
        Refresh(thing);
        var clock = Stopwatch.StartNew();
        var cells = 0;
        var rules = RadioRulesDef.Current;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        var noise = RadioNet.For(map).Noise;
        for (var index = 0; index < margins.Length; index++)
        {
            var cell = map.cellIndices.IndexToCell(index);
            if (!Paintable(cell)) continue;
            Compute(cell, noise, rules, metresPerCell);
            cells++;
        }
        clock.Stop();
        margins = saved;
        subject = null;
        return (clock.Elapsed.TotalMilliseconds, cells, ends.Count);
    }

    private CellBoolDrawer Drawer => drawer ??= new CellBoolDrawer(this, map.Size.x, map.Size.z, RadioRulesDef.Current.coverageOpacity);

    private void Listen()
    {
        if (listening) return;
        map.events.BuildingSpawned += OnBuilding;
        map.events.BuildingDespawned += OnBuilding;
        map.events.RoofChanged += OnRoof;
        listening = true;
    }

    /// <summary>Only what a contact can see counts: a wall, a door, rock, or the radio's own coax and antennas.</summary>
    private void OnBuilding(Building building)
    {
        var def = building.def;
        if (def.IsEdifice() || def.HasModExtension<RadioCoaxExtension>() || def.HasComp(typeof(CompFeedlinePort))) changes++;
    }

    private void OnRoof(IntVec3 cell) => changes++;

    /// <summary>Something the picture is drawn from changed with no map event: an upgrade fitted or taken off (Radio 0.4.0).</summary>
    public void NotifyChanged() => changes++;

    /// <summary>
    /// How many times something a contact can see has changed on this map: a wall, a door, rock, coax or an antenna port built or gone, a
    /// roof, an upgrade (Radio 0.14.0). The cover map reads it, so it is worked out again on exactly the overlay's events; asking starts
    /// the counting.
    /// </summary>
    public int ChangeCount
    {
        get
        {
            Listen();
            return changes;
        }
    }

    /// <summary>Starts again for a new subject; works again over the old picture when what it was drawn from has changed.</summary>
    private void Refresh(Thing thing)
    {
        var metresPerCell = RadioMod.Settings.metresPerCell;
        var noiseSignature = RadioNet.For(map).Noise.Signature;
        if (thing == subject)
        {
            var changed = changes != subjectChanges || noiseSignature != subjectNoise || metresPerCell != subjectMetresPerCell;
            if (!changed) return;
            subjectChanges = changes;
            subjectNoise = noiseSignature;
            subjectMetresPerCell = metresPerCell;
            CollectEnds(thing, metresPerCell);
            if (pass == PassDone) { pass = PassFine; cursor = 0; }
            else again = true;
            return;
        }
        subject = thing;
        subjectChanges = changes;
        subjectNoise = noiseSignature;
        subjectMetresPerCell = metresPerCell;
        fullScaleDb = Mathf.Max(1f, RadioRulesDef.Current.coverageFullScaleDb);
        CollectEnds(thing, metresPerCell);
        var size = map.cellIndices.NumGridCells;
        if (margins.Length != size) margins = new float[size];
        for (var i = 0; i < size; i++) margins[i] = float.NaN;
        pass = PassCoarse;
        cursor = 0;
        again = false;
        Drawer.SetDirty();
    }

    private void Work(float budgetMs)
    {
        if (pass == PassDone) return;
        var rules = RadioRulesDef.Current;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        var noise = RadioNet.For(map).Noise;
        var block = Mathf.Max(1, rules.coverageCoarseBlockCells);
        var sizeX = map.Size.x;
        var sizeZ = map.Size.z;
        var blocksX = (sizeX + block - 1) / block;
        var blocksZ = (sizeZ + block - 1) / block;
        watch.Restart();
        var sinceCheck = 0;
        while (pass != PassDone)
        {
            if (pass == PassCoarse)
            {
                if (cursor >= blocksX * blocksZ) { EndPass(); continue; }
                CoarseBlock(cursor % blocksX * block, cursor / blocksX * block, block, noise, rules, metresPerCell);
                cursor++;
            }
            else
            {
                if (cursor >= margins.Length) { EndPass(); continue; }
                var cell = map.cellIndices.IndexToCell(cursor);
                margins[cursor] = Paintable(cell) ? (float)Compute(cell, noise, rules, metresPerCell) : float.NaN;
                cursor++;
            }
            if (++sinceCheck < CellsPerClockCheck) continue;
            sinceCheck = 0;
            if (watch.Elapsed.TotalMilliseconds >= budgetMs) break;
        }
    }

    private void EndPass()
    {
        Drawer.SetDirty();
        cursor = 0;
        if (pass == PassCoarse || again)
        {
            pass = PassFine;
            again = false;
        }
        else
        {
            pass = PassDone;
        }
    }

    /// <summary>The quick pass: one cell worked out per block, painted over the block's other standing cells.</summary>
    private void CoarseBlock(int x0, int z0, int block, NoiseService noise, RadioRulesDef rules, float metresPerCell)
    {
        float value = float.NaN;
        var found = false;
        for (var z = z0; z < z0 + block && z < map.Size.z; z++)
        {
            for (var x = x0; x < x0 + block && x < map.Size.x; x++)
            {
                var cell = new IntVec3(x, 0, z);
                if (!Paintable(cell)) continue;
                if (!found)
                {
                    value = (float)Compute(cell, noise, rules, metresPerCell);
                    found = true;
                }
                margins[map.cellIndices.CellToIndex(cell)] = value;
            }
        }
    }

    private bool Paintable(IntVec3 cell) => !cell.Fogged(map) && cell.Standable(map);

    private float Progress()
    {
        if (pass == PassDone) return 1f;
        if (pass == PassCoarse) return 0f;
        return margins.Length == 0 ? 1f : (float)cursor / margins.Length;
    }

    /// <summary>
    /// The best signal to spare a hand-held standing here would have, in dB, over the ends and hand-held kinds: exactly what
    /// <see cref="LinkBudgetService.Evaluate"/> would give, doors closed. Once the best it could do with nothing in the way is beaten, or
    /// is lost anyway, the walk along the line stops early.
    /// </summary>
    private double Compute(IntVec3 cell, NoiseService noise, RadioRulesDef rules, float metresPerCell)
    {
        var noiseAtCell = noise.LocalNoiseDbm(cell, metresPerCell);
        var best = double.NegativeInfinity;
        foreach (var end in ends)
        {
            var distanceMetres = cell.DistanceTo(end.Cell) * metresPerCell;
            // With nothing in the way: the most this end could give, which no wall can raise.
            var bestOpen = double.NegativeInfinity;
            foreach (var combo in end.Combos)
            {
                combo.PathLossDb = LinkBudgetService.PathLossDb(distanceMetres, combo.Band);
                combo.FloorAtCellDbm = NoiseFloor.TotalDbm(combo.Kind.Mode.bandwidthHz, combo.Kind.NoiseFigureDb, noiseAtCell);
                var open = Margin(combo, 0);
                if (open > bestOpen) bestOpen = open;
            }
            if (bestOpen <= best) continue;
            var walkedHeight = float.NaN;
            var crossings = default(Crossings);
            foreach (var combo in end.Combos)
            {
                if (combo.Kind.HeightMetres != walkedHeight)
                {
                    walkedHeight = combo.Kind.HeightMetres;
                    crossings = ObstructionService.Walk(map, cell, walkedHeight, end.Cell, end.HeightMetres, rules, doorsClosed: true,
                        stopAboveDb: (float)bestOpen, leastBandFactor: leastBandFactor);
                }
                var margin = Margin(combo, crossings.LossDb(combo.Band));
                if (margin > best) best = margin;
            }
        }
        return best;
    }

    /// <summary>The two-way margin, hand-held at the cell (A) and the station's end (B), as the link budget orders them.</summary>
    private static double Margin(Combo combo, double obstructionDb)
    {
        var mode = combo.Kind.Mode;
        var forward = LinkBudgetService.Inputs(combo.HandSide, combo.StationSide, combo.PathLossDb, obstructionDb, combo.FloorAtStationDbm, mode);
        var reverse = LinkBudgetService.Inputs(combo.StationSide, combo.HandSide, combo.PathLossDb, obstructionDb, combo.FloorAtCellDbm, mode);
        return LinkMath.TwoWayMarginDb(forward, reverse);
    }

    /// <summary>The station's ends this picture is for: all of a selected station's, or the one through a selected antenna.</summary>
    private void CollectEnds(Thing thing, float metresPerCell)
    {
        ends.Clear();
        leastBandFactor = 1f;
        var station = thing.TryGetComp<CompRadio>() is { Props.isStation: true } radio
            ? radio
            : thing.TryGetComp<CompFeedlinePort>() is { } port ? FeedlineService.StationOn(port) : null;
        if (station == null || !station.parent.Spawned) return;
        var noise = RadioNet.For(map).Noise;
        var first = true;
        foreach (var endpoint in RadioNet.StationEndpoints(station, metresPerCell))
        {
            if (thing != station.parent && endpoint.Cell != thing.Position) continue;
            var end = new End(endpoint.Cell, endpoint.AntennaHeightMetres);
            var noiseAtEnd = noise.LocalNoiseDbm(endpoint.Cell, metresPerCell);
            foreach (var kind in Kinds())
            {
                foreach (var band in kind.Bands)
                {
                    if (!endpoint.Bands.Contains(band)) continue;
                    end.Combos.Add(new Combo(kind, band,
                        handSide: new EndSide(kind.TransmitPowerDbm, kind.AntennaGainDbi, 0),
                        stationSide: endpoint.Side(band),
                        floorAtStationDbm: NoiseFloor.TotalDbm(kind.Mode.bandwidthHz, endpoint.NoiseFigureDb, noiseAtEnd)));
                    leastBandFactor = first ? band.obstructionFactor : Mathf.Min(leastBandFactor, band.obstructionFactor);
                    first = false;
                }
            }
            end.Combos.Sort((x, y) => x.Kind.HeightMetres.CompareTo(y.Kind.HeightMetres));
            if (end.Combos.Count > 0) ends.Add(end);
        }
    }

    /// <summary>Every hand-held the game has: worn radios that are not stations. Read once.</summary>
    private static List<Kind> Kinds()
    {
        if (kinds != null) return kinds;
        kinds = new List<Kind>();
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            var props = def.GetCompProperties<CompProperties_Radio>();
            if (props == null || props.isStation || props.mode == null || props.transmitPowerWatts <= 0) continue;
            kinds.Add(new Kind(props));
        }
        return kinds;
    }

    private Color ColorFor(float margin) =>
        margin < 0f ? UI.RadioPalette.NoContact : UI.RadioPalette.At(0.08f + 0.92f * Mathf.Pow(Mathf.Clamp01(margin / fullScaleDb), 1.2f));

    /// <summary>A kind of hand-held, read from its Def.</summary>
    private sealed class Kind
    {
        public readonly List<BandDef> Bands;
        public readonly ModeDef Mode;
        public readonly double TransmitPowerDbm;
        public readonly float AntennaGainDbi;
        public readonly float HeightMetres;
        public readonly float NoiseFigureDb;

        public Kind(CompProperties_Radio props)
        {
            Bands = props.bands;
            Mode = props.mode!;
            TransmitPowerDbm = Decibels.FromWatts(props.transmitPowerWatts);
            AntennaGainDbi = props.antennaGainDbi;
            HeightMetres = props.antennaHeightMetres;
            NoiseFigureDb = props.noiseFigureDb;
        }
    }

    /// <summary>One of the station's antennas, as the picture uses it.</summary>
    private sealed class End
    {
        public readonly IntVec3 Cell;
        public readonly float HeightMetres;
        public readonly List<Combo> Combos = new();

        public End(IntVec3 cell, float heightMetres)
        {
            Cell = cell;
            HeightMetres = heightMetres;
        }
    }

    /// <summary>A hand-held kind on one band through one end: what is fixed for the picture, and two figures reused for each cell.</summary>
    private sealed class Combo
    {
        public readonly Kind Kind;
        public readonly BandDef Band;
        public readonly EndSide HandSide;
        public readonly EndSide StationSide;
        public readonly double FloorAtStationDbm;
        public double PathLossDb;
        public double FloorAtCellDbm;

        public Combo(Kind kind, BandDef band, EndSide handSide, EndSide stationSide, double floorAtStationDbm)
        {
            Kind = kind;
            Band = band;
            HandSide = handSide;
            StationSide = stationSide;
            FloorAtStationDbm = floorAtStationDbm;
        }
    }
}
