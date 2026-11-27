using System.Collections.Generic;
using Phobos.Radio.Physics;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The colony's own electrical noise. A snapshot is taken once per recompute by scanning the colonist
/// buildings whose Defs carry <see cref="RadioNoiseExtension"/>; endpoints then sum the few sources in
/// range. Nothing here walks cells, and asking about a cell allocates nothing (the coverage overlay asks for every cell it paints).
/// <para>Since Radio 0.45.0 it holds the map's jammers too (<see cref="RadioJamming"/>; docs/development/electronic-warfare.md, EW1), while the
/// setting "Jamming reaches radios" is on. A jammer has no radius: its noise falls off by free space across the whole map, as a signal does.
/// Cost: one more source per jammer, summed at each end of each link and each painted cell; jammers are few (none to three on a map).</para>
/// </summary>
public sealed class NoiseService
{
    private readonly struct Source
    {
        public readonly IntVec3 Cell;
        public readonly float LevelDbmAt1m;
        public readonly int RadiusCells;

        public Source(IntVec3 cell, float levelDbmAt1m, int radiusCells)
        {
            Cell = cell;
            LevelDbmAt1m = levelDbmAt1m;
            RadiusCells = radiusCells;
        }
    }

    /// <summary>A jammer as the snapshot holds it: its power, the width it spreads that over, and the bands it covers (null: all).</summary>
    private readonly struct Jam
    {
        public readonly IntVec3 Cell;
        public readonly double RadiatedDbm;
        public readonly double SpanHz;
        public readonly HashSet<BandDef>? Bands;

        public Jam(IntVec3 cell, double radiatedDbm, double spanHz, HashSet<BandDef>? bands)
        {
            Cell = cell;
            RadiatedDbm = radiatedDbm;
            SpanHz = spanHz;
            Bands = bands;
        }

        public bool Covers(BandDef band) => Bands == null || Bands.Contains(band);
    }

    private static Dictionary<ThingDef, RadioNoiseExtension>? noisyDefs;
    private static double allBandsSpanHz = -1;
    private readonly List<Source> sources = new();
    private readonly List<Jam> jams = new();
    private readonly List<RadioJammer> collected = new();
    private float fadeStartFraction = 1f;

    private static Dictionary<ThingDef, RadioNoiseExtension> NoisyDefs
    {
        get
        {
            if (noisyDefs == null)
            {
                noisyDefs = new Dictionary<ThingDef, RadioNoiseExtension>();
                foreach (var def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    var extension = def.GetModExtension<RadioNoiseExtension>();
                    if (extension != null) noisyDefs[def] = extension;
                }
            }
            return noisyDefs;
        }
    }

    public int SourceCount => sources.Count;

    /// <summary>How many jammers the last snapshot heard.</summary>
    public int JammerCount => jams.Count;

    /// <summary>The width of every band the radio knows, in Hz: what a jammer naming no bands spreads its power over.</summary>
    private static double AllBandsSpanHz
    {
        get
        {
            if (allBandsSpanHz < 0)
            {
                allBandsSpanHz = 0;
                foreach (var band in DefDatabase<BandDef>.AllDefsListForReading) allBandsSpanHz += SpanHz(band);
            }
            return allBandsSpanHz;
        }
    }

    private static double SpanHz(BandDef band) => (band.highMHz - band.lowMHz) * 1e6;

    /// <summary>
    /// Changes whenever the snapshot holds different sources (one switched on or off, built, moved or gone), so a picture drawn from
    /// an earlier snapshot knows to redraw. Equal snapshots give equal signatures.
    /// </summary>
    public int Signature { get; private set; }

    public void Snapshot(Map map, RadioRulesDef rules)
    {
        sources.Clear();
        fadeStartFraction = rules.noiseFadeStartFraction;
        var signature = 17;
        if (NoisyDefs.Count > 0)
        {
            foreach (var building in map.listerBuildings.allBuildingsColonist)
            {
                if (!NoisyDefs.TryGetValue(building.def, out var extension) || !IsActive(building, extension)) continue;
                var radius = extension.radiusCells > 0 ? extension.radiusCells : rules.noiseRadiusDefaultCells;
                sources.Add(new Source(building.Position, extension.noiseDbmAt1m, radius));
                signature = unchecked(signature * 31 + Gen.HashCombineInt(building.Position.GetHashCode(), radius)
                                      + extension.noiseDbmAt1m.GetHashCode());
            }
        }
        signature = SnapshotJammers(map, signature);
        Signature = unchecked(signature * 31 + sources.Count * 7 + jams.Count);
    }

    /// <summary>The map's jammers, while the setting lets them reach radios (Radio 0.45.0, EW1).</summary>
    private int SnapshotJammers(Map map, int signature)
    {
        jams.Clear();
        if (!RadioMod.Settings.jammingReachesRadios || !RadioJamming.Any) return signature;
        collected.Clear();
        RadioJamming.Collect(map, collected);
        foreach (var jammer in collected)
        {
            HashSet<BandDef>? bands = null;
            double span;
            if (jammer.Bands == null) span = AllBandsSpanHz;
            else
            {
                bands = new HashSet<BandDef>(jammer.Bands);
                span = 0;
                foreach (var band in bands) span += SpanHz(band);
            }
            if (span <= 0) continue;
            jams.Add(new Jam(jammer.Cell, Decibels.FromWatts(jammer.RadiatedWatts), span, bands));
            signature = unchecked(signature * 31 + Gen.HashCombineInt(jammer.Cell.GetHashCode(), (int)jammer.RadiatedWatts) + (bands?.Count ?? -1));
            // Seen in play (Radio 0.45.0, row 707): a jammer heard by the radio, and whose.
            var whose = jammer.Source?.Faction == Faction.OfPlayer ? "ours" : "theirs";
            if (jammer.Source != null && Seen.Wants("radio.jamming", jammer.Source, whose))
                Seen.Note("radio.jamming", jammer.Source, jammer.RadiatedWatts.ToString("0") + " W sent out", whose);
        }
        collected.Clear();
        return signature;
    }

    private static bool IsActive(Building building, RadioNoiseExtension extension)
    {
        if (extension.computerNoise && !RadioMod.Settings.terminalNoise) return false;
        switch (extension.noiseWhen)
        {
            case NoiseWhen.Always:
                return true;
            case NoiseWhen.Connected:
                return building.TryGetComp<CompPower>()?.PowerNet != null;
            default:
                var trader = building.TryGetComp<CompPowerTrader>();
                if (trader == null || !trader.PowerOn) return false;
                // A generator out of fuel stays "on" and simply produces nothing.
                return trader is not CompPowerPlant plant || plant.PowerOutput > 0f;
        }
    }

    /// <summary>
    /// Local noise at a cell in dBm in the reference bandwidth (12.5 kHz; <see cref="NoiseFloor.TotalDbm"/> scales it to a mode's), or −∞
    /// when no source reaches it: each source by the inverse-square law, faded to nothing towards its radius
    /// (<see cref="NoiseFloor.FadeDb"/>), power-summed. Jammers are not in it: they are asked for by band, <see cref="JammingDbm"/>.
    /// </summary>
    public double LocalNoiseDbm(IntVec3 cell, float metresPerCell)
    {
        double milliwatts = 0;
        foreach (var source in sources)
        {
            var distanceCells = source.Cell.DistanceTo(cell);
            var fade = NoiseFloor.FadeDb(distanceCells, source.RadiusCells, fadeStartFraction);
            if (double.IsNegativeInfinity(fade)) continue;
            milliwatts += Decibels.ToMilliwatts(NoiseFloor.AtDistanceDbm(source.LevelDbmAt1m, distanceCells * metresPerCell) + fade);
        }
        return Decibels.MilliwattsToDbm(milliwatts);
    }

    /// <summary>
    /// Jamming reaching a 0 dBi antenna at a cell, in dBm within a receiver's bandwidth on one band, or −∞ when no jammer covers that
    /// band (Radio 0.45.0, EW1): each jammer by free space (<see cref="Jamming.AtAntennaDbm"/>), power-summed. Allocates nothing.
    /// </summary>
    public double JammingDbm(IntVec3 cell, float metresPerCell, double bandwidthHz, BandDef band)
    {
        if (jams.Count == 0) return double.NegativeInfinity;
        double milliwatts = 0;
        foreach (var jam in jams)
        {
            if (!jam.Covers(band)) continue;
            var metres = jam.Cell.DistanceTo(cell) * metresPerCell;
            milliwatts += Decibels.ToMilliwatts(Jamming.AtAntennaDbm(jam.RadiatedDbm, jam.SpanHz, bandwidthHz, metres, band.centreMHz));
        }
        return Decibels.MilliwattsToDbm(milliwatts);
    }
}
