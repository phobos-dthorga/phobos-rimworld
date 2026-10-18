using System.Collections.Generic;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// One end of a link, as the budget sees it: where the antenna is, what it is, what feeds it, and what
/// the radio behind it can do. Built by <see cref="RadioNet"/> from comps and the map.
/// </summary>
public sealed class RadioEndpoint
{
    public Thing Thing { get; }
    public IntVec3 Cell { get; }
    public float AntennaHeightMetres { get; }
    public float AntennaGainDbi { get; }
    public float FeedLossDb { get; }
    public float TransmitPowerWatts { get; }
    public float NoiseFigureDb { get; }
    public IReadOnlyList<BandDef> Bands { get; }
    public ModeDef Mode { get; }
    /// <summary>What the sheet calls the antenna: the built-in whip, or a named antenna building.</summary>
    public string AntennaLabel { get; }

    public RadioEndpoint(Thing thing, IntVec3 cell, CompRadio radio, float antennaHeightMetres, float antennaGainDbi,
        float feedLossDb, string antennaLabel)
    {
        Thing = thing;
        Cell = cell;
        AntennaHeightMetres = antennaHeightMetres;
        AntennaGainDbi = antennaGainDbi;
        FeedLossDb = feedLossDb;
        TransmitPowerWatts = radio.Props.transmitPowerWatts;
        NoiseFigureDb = radio.Props.noiseFigureDb;
        Bands = radio.Props.bands;
        Mode = radio.Mode;
        AntennaLabel = antennaLabel;
    }

    /// <summary>An endpoint using the radio's own built-in antenna at the radio's cell.</summary>
    public static RadioEndpoint BuiltIn(Thing thing, IntVec3 cell, CompRadio radio) =>
        new(thing, cell, radio, radio.Props.antennaHeightMetres, radio.Props.antennaGainDbi, 0f,
            "PhobosRadio_AntennaBuiltIn".Translate());
}
