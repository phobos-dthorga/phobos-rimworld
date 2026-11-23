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
    public float TransmitPowerWatts { get; }
    public float NoiseFigureDb { get; }
    public IReadOnlyList<BandDef> Bands { get; }
    public ModeDef Mode { get; }
    /// <summary>What the sheet calls the antenna: the built-in whip, or a named antenna building.</summary>
    public string AntennaLabel { get; }

    public RadioEndpoint(Thing thing, IntVec3 cell, CompRadio radio, IReadOnlyList<BandDef> bands, float antennaHeightMetres,
        float antennaGainDbi, IReadOnlyList<FeedSegment> feedline, string antennaLabel)
    {
        Thing = thing;
        Cell = cell;
        AntennaHeightMetres = antennaHeightMetres;
        AntennaGainDbi = antennaGainDbi;
        Feedline = feedline;
        TransmitPowerWatts = radio.Props.transmitPowerWatts;
        // A station's receive preamplifier (Radio 0.4.0); a hand-held takes no upgrades and keeps its own.
        NoiseFigureDb = RadioUpgrades.NoiseFigureDb(radio.parent, radio.Props.noiseFigureDb);
        Bands = bands;
        Mode = radio.Mode;
        AntennaLabel = antennaLabel;
    }

    /// <summary>What this end brings to a link on a band.</summary>
    public EndSide Side(BandDef band) => new(Physics.Decibels.FromWatts(TransmitPowerWatts), AntennaGainDbi, FeedLossDb(band));

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
    public static RadioEndpoint ViaAntenna(CompRadio station, CompAntenna antenna, List<FeedSegment> feedline)
    {
        var bands = new List<BandDef>();
        foreach (var band in station.Props.bands) if (antenna.Props.bands.Contains(band)) bands.Add(band);
        // The antenna's own figures, or what its fitted upgrades set (Radio 0.4.0).
        return new RadioEndpoint(station.parent, antenna.parent.Position, station, bands,
            RadioUpgrades.Figure(antenna.parent, RadioUpgrades.HeightMetres, antenna.Props.heightMetres),
            RadioUpgrades.Figure(antenna.parent, RadioUpgrades.GainDbi, antenna.Props.gainDbi), feedline, antenna.parent.LabelCap);
    }
}
