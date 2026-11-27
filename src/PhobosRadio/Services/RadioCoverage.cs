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
/// <b>Performance is the mod's first concern</b> (owner, 23 Nov 2026). While a station or an antenna is selected, cells are worked out for
/// at most <see cref="RadioRulesDef.coverageBudgetMsPerFrame"/> of real time a frame: first a quick pass of one cell per block, then
/// every cell. It works again only when a building or roof changes, the noise sources change, or the cell scale does, and the picture is
/// rebuilt only at the end of each pass. Nothing is saved.
/// </para>
/// <para>
/// <b>Resumable</b> (Radio 0.44.1; the owner's report of 27 Nov 2026: it "restarts everytime you click away"). Each station or antenna
/// keeps its own picture, the last <see cref="KeptPictures"/> of them, so selecting it again goes on where it was. The last one shown
/// finishes the pass under way after it is deselected, at the smaller <see cref="RadioRulesDef.coverageBackgroundBudgetMsPerFrame"/>,
/// and never starts another in the background. A change while a picture is shown works the picture out again over the old one, which
/// stays painted until the new pass ends and is said as "updating", never as starting over. Memory: one float a map cell for each picture
/// kept.
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

    /// <summary>How many stations' and antennas' pictures are kept, the most recently shown first (an agent default).</summary>
    public const int KeptPictures = 3;

    private const int PassCoarse = 0;
    private const int PassFine = 1;
    private const int PassDone = 2;

    // What is asked for: the selected station or antenna, renewed every frame it stays selected.
    private Thing? asked;
    private int askedFrame = -10;

    // What changes the picture: counted from the map's events.
    private int changes;
    private int fogChanges;
    private bool listening;

    /// <summary>The pictures kept, the most recently shown first. The first is the one drawn while shown, and finished in the background.</summary>
    private readonly List<Picture> pictures = new();
    private Picture? drawn;
    private bool showing;
    private CellBoolDrawer? drawer;
    private readonly Stopwatch watch = new();
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
        if (!showing || drawn == null || drawn.Subject != thing) return null;
        if (drawn.Ends.Count == 0) return "PhobosRadio_CoverageNoStation".Translate();
        if (drawn.Pass == PassDone) return "PhobosRadio_CoverageShown".Translate();
        // Radio 0.44.1: over a picture already finished once, a new pass updates it; it does not start again.
        return (drawn.Finished ? "PhobosRadio_CoverageUpdating" : "PhobosRadio_CoverageWorking").Translate(drawn.Progress.ToStringPercent().Named("PERCENT"));
    }

    public override void MapComponentUpdate()
    {
        showing = false;
        var rules = RadioRulesDef.Current;
        if (asked != null && (RealTime.frameCount - askedFrame > 1 || !asked.Spawned || asked.Map != map || Find.CurrentMap != map)) asked = null;
        if (asked != null)
        {
            showing = true;
            Listen();
            var picture = Take(asked);
            if (drawn != picture)
            {
                drawn = picture;
                Drawer.SetDirty();
            }
            Refresh(picture);
            if (picture.Ends.Count == 0) return;
            Work(picture, rules.coverageBudgetMsPerFrame, background: false);
            Drawer.MarkForDraw();
            Drawer.CellBoolDrawerUpdate();
            return;
        }
        // Radio 0.44.1: nothing selected that shows a picture: the last one shown finishes the pass under way, a little each frame, while
        // its map is the one looked at. Nothing new starts here.
        if (pictures.Count == 0 || Find.CurrentMap != map) return;
        var last = pictures[0];
        if (last.Pass == PassDone || last.Ends.Count == 0 || !last.Subject.Spawned || last.Subject.Map != map) return;
        Work(last, rules.coverageBackgroundBudgetMsPerFrame, background: true);
    }

    public override void MapRemoved()
    {
        if (listening)
        {
            map.events.BuildingSpawned -= OnBuilding;
            map.events.BuildingDespawned -= OnBuilding;
            map.events.RoofChanged -= OnRoof;
            map.events.CellFogChanged -= OnFog;
            listening = false;
        }
        pictures.Clear();
        drawn = null;
        byMap.Remove(map);
    }

    // ICellBoolGiver: what the game's cell drawer paints, from the picture shown.
    public Color Color => Color.white;
    public bool GetCellBool(int index) => drawn != null && index < drawn.Margins.Length && !float.IsNaN(drawn.Margins[index]);
    public Color GetCellExtraColor(int index) => ColorFor(drawn!.Margins[index]);

    /// <summary>
    /// Works out a whole fine pass for a station or an antenna at once and says how long it took: the dev menu's measure of the
    /// overlay's cost on a real colony. It leaves the pictures as they were.
    /// </summary>
    public (double milliseconds, int cells, int ends) TimeFullPass(Thing thing)
    {
        var picture = new Picture(thing, map.cellIndices.NumGridCells);
        CollectEnds(picture, RadioMod.Settings.metresPerCell);
        var clock = Stopwatch.StartNew();
        var cells = 0;
        var rules = RadioRulesDef.Current;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        var noise = RadioNet.For(map).Noise;
        for (var index = 0; index < picture.Margins.Length; index++)
        {
            var cell = map.cellIndices.IndexToCell(index);
            if (!Paintable(cell)) continue;
            Compute(picture, cell, noise, rules, metresPerCell);
            cells++;
        }
        clock.Stop();
        return (clock.Elapsed.TotalMilliseconds, cells, picture.Ends.Count);
    }

    private CellBoolDrawer Drawer => drawer ??= new CellBoolDrawer(this, map.Size.x, map.Size.z, RadioRulesDef.Current.coverageOpacity);

    private void Listen()
    {
        if (listening) return;
        map.events.BuildingSpawned += OnBuilding;
        map.events.BuildingDespawned += OnBuilding;
        map.events.RoofChanged += OnRoof;
        map.events.CellFogChanged += OnFog;
        listening = true;
    }

    /// <summary>Only what a contact can see counts: a wall, a door, rock, or the radio's own coax and antennas.</summary>
    private void OnBuilding(Building building)
    {
        var def = building.def;
        if (def.IsEdifice() || def.HasModExtension<RadioCoaxExtension>() || def.HasComp(typeof(CompFeedlinePort))) changes++;
    }

    private void OnRoof(IntVec3 cell) => changes++;

    /// <summary>Cells revealed from fog are painted at the next pass (fault-read 25 Nov 2026, F4); counted apart, as the cover map does not
    /// care what the player can see.</summary>
    private void OnFog(IntVec3 cell, bool fogged) => fogChanges++;

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

    /// <summary>This station's or antenna's picture, kept or new, moved to the front; the oldest beyond <see cref="KeptPictures"/> let go.</summary>
    private Picture Take(Thing thing)
    {
        pictures.RemoveAll(kept => !kept.Subject.Spawned || kept.Subject.Map != map);
        var index = pictures.FindIndex(kept => kept.Subject == thing);
        Picture picture;
        if (index >= 0)
        {
            picture = pictures[index];
            pictures.RemoveAt(index);
        }
        else
        {
            picture = new Picture(thing, map.cellIndices.NumGridCells);
        }
        pictures.Insert(0, picture);
        if (pictures.Count > KeptPictures) pictures.RemoveRange(KeptPictures, pictures.Count - KeptPictures);
        return picture;
    }

    /// <summary>Starts a new picture's first pass; works again over a kept one when what it was drawn from has changed.</summary>
    private void Refresh(Picture picture)
    {
        var metresPerCell = RadioMod.Settings.metresPerCell;
        var noiseSignature = RadioNet.For(map).Noise.Signature;
        fullScaleDb = Mathf.Max(1f, RadioRulesDef.Current.coverageFullScaleDb);
        if (picture.Margins.Length != map.cellIndices.NumGridCells) picture.Reset(map.cellIndices.NumGridCells);
        if (!picture.Started)
        {
            picture.Started = true;
            picture.Note(changes, fogChanges, noiseSignature, metresPerCell);
            CollectEnds(picture, metresPerCell);
            picture.Pass = PassCoarse;
            picture.Cursor = 0;
            Drawer.SetDirty();
            return;
        }
        var changed = picture.Changed(changes, fogChanges, noiseSignature, metresPerCell);
        if (!changed && !picture.Stale) return;
        picture.Note(changes, fogChanges, noiseSignature, metresPerCell);
        picture.Stale = false;
        CollectEnds(picture, metresPerCell);
        // Over the old picture, which stays painted: a fresh fine pass when it was done, else one more after the pass under way.
        if (picture.Pass == PassDone)
        {
            picture.Pass = PassFine;
            picture.Cursor = 0;
        }
        else
        {
            picture.Again = true;
        }
    }

    private void Work(Picture picture, float budgetMs, bool background)
    {
        if (picture.Pass == PassDone) return;
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
        while (picture.Pass != PassDone)
        {
            if (picture.Pass == PassCoarse)
            {
                if (picture.Cursor >= blocksX * blocksZ) { EndPass(picture, background); continue; }
                CoarseBlock(picture, picture.Cursor % blocksX * block, picture.Cursor / blocksX * block, block, noise, rules, metresPerCell);
                picture.Cursor++;
            }
            else
            {
                if (picture.Cursor >= picture.Margins.Length) { EndPass(picture, background); continue; }
                var cell = map.cellIndices.IndexToCell(picture.Cursor);
                picture.Margins[picture.Cursor] = Paintable(cell) ? (float)Compute(picture, cell, noise, rules, metresPerCell) : float.NaN;
                picture.Cursor++;
            }
            if (++sinceCheck < CellsPerClockCheck) continue;
            sinceCheck = 0;
            if (watch.Elapsed.TotalMilliseconds >= budgetMs) break;
        }
    }

    /// <summary>
    /// A pass has ended: the quick pass goes on to the fine one; a fine pass with a change behind it goes round again while shown, and in
    /// the background is left for when the picture is next shown (it is marked stale), so nothing new starts unseen.
    /// </summary>
    private void EndPass(Picture picture, bool background)
    {
        if (picture == drawn) Drawer.SetDirty();
        picture.Cursor = 0;
        if (picture.Pass == PassCoarse)
        {
            picture.Pass = PassFine;
            return;
        }
        picture.Finished = true;
        if (picture.Again && !background)
        {
            picture.Again = false;
            picture.Pass = PassFine;
            return;
        }
        picture.Stale = picture.Again;
        picture.Again = false;
        picture.Pass = PassDone;
        if (Seen.Wants("radio.coverage-finished", picture.Subject, background ? "background" : "shown"))
            Seen.Note("radio.coverage-finished", picture.Subject, background ? "finished while not selected" : "finished while shown",
                background ? "background" : "shown");
    }

    /// <summary>The quick pass: one cell worked out per block, painted over the block's other standing cells.</summary>
    private void CoarseBlock(Picture picture, int x0, int z0, int block, NoiseService noise, RadioRulesDef rules, float metresPerCell)
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
                    value = (float)Compute(picture, cell, noise, rules, metresPerCell);
                    found = true;
                }
                picture.Margins[map.cellIndices.CellToIndex(cell)] = value;
            }
        }
    }

    private bool Paintable(IntVec3 cell) => !cell.Fogged(map) && cell.Standable(map);

    /// <summary>
    /// The best signal to spare a hand-held standing here would have, in dB, over the ends and hand-held kinds: exactly what
    /// <see cref="LinkBudgetService.Evaluate"/> would give, doors closed. Once the best it could do with nothing in the way is beaten, or
    /// is lost anyway, the walk along the line stops early.
    /// </summary>
    private static double Compute(Picture picture, IntVec3 cell, NoiseService noise, RadioRulesDef rules, float metresPerCell)
    {
        var noiseAtCell = noise.LocalNoiseDbm(cell, metresPerCell);
        var best = double.NegativeInfinity;
        var map = picture.Subject.Map;
        foreach (var end in picture.Ends)
        {
            var distanceMetres = cell.DistanceTo(end.Cell) * metresPerCell;
            // With nothing in the way: the most this end could give, which no wall can raise.
            var bestOpen = double.NegativeInfinity;
            foreach (var combo in end.Combos)
            {
                combo.PathLossDb = LinkBudgetService.PathLossDb(distanceMetres, combo.Band);
                // Radio 0.45.0 (EW1): jamming at the cell on this combo's band, through the hand-held's antenna, as a link works it out.
                combo.FloorAtCellDbm = NoiseFloor.WithJammingDbm(NoiseFloor.TotalDbm(combo.Kind.Mode.bandwidthHz, combo.Kind.NoiseFigureDb, noiseAtCell),
                    LinkBudgetService.JammingAtInputDbm(noise.JammingDbm(cell, metresPerCell, combo.Kind.Mode.bandwidthHz, combo.Band), combo.HandSide));
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
                        stopAboveDb: (float)bestOpen, leastBandFactor: picture.LeastBandFactor);
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
    private void CollectEnds(Picture picture, float metresPerCell)
    {
        var thing = picture.Subject;
        var ends = picture.Ends;
        ends.Clear();
        picture.LeastBandFactor = 1f;
        var station = thing.TryGetComp<CompRadio>() is { Props.isStation: true } radio
            ? radio
            : thing.TryGetComp<CompFeedlinePort>() is { } port ? FeedlineService.StationOn(port) : null;
        if (station == null || !station.parent.Spawned) return;
        // An unpowered or switched-off desk reaches nobody, as the net counts it (fault-read 25 Nov 2026, F4): it paints nothing.
        if (station.parent.TryGetComp<CompPowerTrader>() is { PowerOn: false }) return;
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
                        // Radio 0.45.0 (EW1): jamming at the station's antenna on this band, through its gain and cable.
                        floorAtStationDbm: NoiseFloor.WithJammingDbm(NoiseFloor.TotalDbm(kind.Mode.bandwidthHz, endpoint.NoiseFigureDb, noiseAtEnd),
                            LinkBudgetService.JammingAtInputDbm(noise.JammingDbm(endpoint.Cell, metresPerCell, kind.Mode.bandwidthHz, band), endpoint.Side(band)))));
                    picture.LeastBandFactor = first ? band.obstructionFactor : Mathf.Min(picture.LeastBandFactor, band.obstructionFactor);
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
        var seen = new HashSet<(ModeDef, float, float, float, float)>();
        foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            // Hand-helds only (fault-read 25 Nov 2026, F1): a vehicle's radio is a ThingDef too (Vehicle Framework's VehicleDefs are), given
            // at start-up, and painted every cell about 13 dB too good. A pawn's own Def (a vehicle, an animal) is no hand-held.
            if (def.race != null) continue;
            var props = def.GetCompProperties<CompProperties_Radio>();
            if (props == null || props.isStation || props.mode == null || props.transmitPowerWatts <= 0) continue;
            // One kind for each set of figures, however many Defs share them: each kind costs a combination at every cell.
            if (!seen.Add((props.mode, props.transmitPowerWatts, props.antennaGainDbi, props.antennaHeightMetres, props.noiseFigureDb))) continue;
            kinds.Add(new Kind(props));
        }
        return kinds;
    }

    private Color ColorFor(float margin) =>
        margin < 0f ? UI.RadioPalette.NoContact : UI.RadioPalette.At((float)CoverRule.Shade(margin, fullScaleDb));

    /// <summary>
    /// One station's or antenna's picture (Radio 0.44.1): its cells, how far its pass has got, and what it was worked out from, so it can be
    /// kept, gone back to and finished while another is shown or none is.
    /// </summary>
    private sealed class Picture
    {
        public readonly Thing Subject;
        public float[] Margins = System.Array.Empty<float>();
        public readonly List<End> Ends = new();
        public float LeastBandFactor = 1f;
        public int Pass = PassDone;
        public int Cursor;
        /// <summary>Its first pass has been started.</summary>
        public bool Started;
        /// <summary>A fine pass has ended at least once: a new pass updates it.</summary>
        public bool Finished;
        /// <summary>A change came during the pass under way: one more pass follows, while shown.</summary>
        public bool Again;
        /// <summary>A change was left for when it is next shown: the background never starts a pass.</summary>
        public bool Stale;
        private int changes, fogChanges, noise;
        private float metresPerCell;

        public Picture(Thing subject, int cells)
        {
            Subject = subject;
            Reset(cells);
        }

        /// <summary>Every cell blank, and nothing worked out yet (a new map size).</summary>
        public void Reset(int cells)
        {
            Margins = new float[cells];
            for (var i = 0; i < cells; i++) Margins[i] = float.NaN;
            Started = Finished = Again = Stale = false;
            Pass = PassDone;
            Cursor = 0;
        }

        public float Progress => Pass == PassDone ? 1f : Pass == PassCoarse ? 0f : Margins.Length == 0 ? 1f : (float)Cursor / Margins.Length;

        public bool Changed(int nowChanges, int nowFog, int nowNoise, float nowMetres) =>
            nowChanges != changes || nowFog != fogChanges || nowNoise != noise || nowMetres != metresPerCell;

        public void Note(int nowChanges, int nowFog, int nowNoise, float nowMetres)
        {
            changes = nowChanges;
            fogChanges = nowFog;
            noise = nowNoise;
            metresPerCell = nowMetres;
        }
    }

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
