using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// One end of a link, as the budget sees it: where the antenna is, what it is, what feeds it, and what
/// the radio behind it can do. Built by <see cref="RadioNet"/> from comps, the feedline and the map.
/// </summary>
public sealed class RadioEndpoint
{
    private static readonly List<FeedSegment> NoFeedline = new();

    public Thing Thing { get; }
    public IntVec3 Cell { get; }
    public float AntennaHeightMetres { get; }
    public float AntennaGainDbi { get; }
    public IReadOnlyList<FeedSegment> Feedline { get; }
    /// <summary>What the station sends into its cable, after any amplifier in its run (Radio 0.5.0).</summary>
    public float TransmitPowerWatts { get; }
    /// <summary>What the amplifiers in its run cost on receive, switched out of the line, in dB (Radio 0.5.0).</summary>
    public float ReceiveThroughAmplifiersDb { get; }
    public float NoiseFigureDb { get; }
    public IReadOnlyList<BandDef> Bands { get; }
    public ModeDef Mode { get; }
    /// <summary>What the sheet calls the antenna: the built-in whip, or a named antenna building.</summary>
    public string AntennaLabel { get; }
    /// <summary>Whether its antenna lies flat, as a dipole does (Radio 0.62.0): HF's ground wave on such a link is horizontally polarised,
    /// and dies within a few kilometres. Whips and masts stand upright.</summary>
    public bool Horizontal { get; init; }

    public RadioEndpoint(Thing thing, IntVec3 cell, CompRadio radio, IReadOnlyList<BandDef> bands, float antennaHeightMetres,
        float antennaGainDbi, IReadOnlyList<FeedSegment> feedline, string antennaLabel, IReadOnlyList<CompAmplifier>? amplifiers = null)
    {
        Thing = thing;
        Cell = cell;
        AntennaHeightMetres = antennaHeightMetres;
        AntennaGainDbi = antennaGainDbi;
        Feedline = feedline;
        TransmitPowerWatts = ThroughAmplifiers(radio.Props.transmitPowerWatts, amplifiers, out var receiveLossDb);
        ReceiveThroughAmplifiersDb = receiveLossDb;
        // A station's receive preamplifier (Radio 0.4.0); a hand-held takes no upgrades and keeps its own.
        NoiseFigureDb = RadioUpgrades.NoiseFigureDb(radio.parent, radio.Props.noiseFigureDb);
        Bands = bands;
        Mode = radio.Mode;
        AntennaLabel = antennaLabel;
    }

    /// <summary>What this end brings to a link on a band.</summary>
    public EndSide Side(BandDef band) =>
        new(Physics.Decibels.FromWatts(TransmitPowerWatts), AntennaGainDbi, FeedLossDb(band), ReceiveThroughAmplifiersDb);

    /// <summary>
    /// The power a station sends through the amplifiers in its cable run, read as they are now (powered or not), and what they cost on
    /// receive: each one's loss, as every amplifier is switched out of the line while the station listens.
    /// </summary>
    private static float ThroughAmplifiers(float driveWatts, IReadOnlyList<CompAmplifier>? amplifiers, out float receiveLossDb)
    {
        receiveLossDb = 0f;
        if (amplifiers == null || amplifiers.Count == 0) return driveWatts;
        Span<(double, double, double, bool)> figures = stackalloc (double, double, double, bool)[amplifiers.Count];
        for (var i = 0; i < amplifiers.Count; i++)
        {
            var props = amplifiers[i].Props;
            figures[i] = (props.gainDb, props.ratedOutputWatts, props.lossDb, amplifiers[i].Powered);
            receiveLossDb += props.lossDb;
        }
        return (float)Physics.Amplifier.ThroughWatts(driveWatts, figures);
    }

    public float FeedLossDb(BandDef band)
    {
        var total = 0f;
        foreach (var segment in Feedline) total += segment.LossDb(band);
        return total;
    }

    public float FeedlineMetres
    {
        get
        {
            var total = 0f;
            foreach (var segment in Feedline) total += segment.Metres;
            return total;
        }
    }

    /// <summary>An endpoint using the radio's own built-in antenna at the radio's cell.</summary>
    public static RadioEndpoint BuiltIn(Thing thing, IntVec3 cell, CompRadio radio) =>
        new(thing, cell, radio, radio.Props.bands, radio.Props.antennaHeightMetres, radio.Props.antennaGainDbi, NoFeedline,
            "PhobosRadio_AntennaBuiltIn".Translate());

    /// <summary>A station heard through an antenna building on its coax; the bands are those both support.</summary>
    public static RadioEndpoint ViaAntenna(CompRadio station, CompAntenna antenna, FeedPath path)
    {
        var feedline = path.Segments;
        var bands = new List<BandDef>();
        // Since Radio 0.49.0 each with the bands its upgrades add (the aviation transceiver and antennas).
        var antennaBands = antenna.Bands;
        foreach (var band in station.Bands) if (antennaBands.Contains(band)) bands.Add(band);
        // The antenna's own figures, or what its fitted upgrades set (Radio 0.4.0).
        return new RadioEndpoint(station.parent, antenna.parent.Position, station, bands,
            RadioUpgrades.Figure(antenna.parent, RadioUpgrades.HeightMetres, antenna.Props.heightMetres),
            RadioUpgrades.Figure(antenna.parent, RadioUpgrades.GainDbi, antenna.Props.gainDbi), feedline, antenna.parent.LabelCap,
            path.Amplifiers) { Horizontal = antenna.Props.horizontal };
    }
}
