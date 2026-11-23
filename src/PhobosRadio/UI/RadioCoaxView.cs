using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The coax glowing by loss (Radio 0.6.0, the radio visuals record's RV8): while a station or an antenna is selected, each cell of
/// the cable from the station to its antennas is tinted by how much of the sent signal is still there: bright at the station,
/// dimmer along the run, bright again past a powered amplifier. Worked out on the first band the station has, with the same coax
/// law as the link budget. Presentation only: it reads the route the feedline service already keeps.
/// <para><b>Cost:</b> only while one of them is selected; one quad per cable cell per frame, a few multiplications each.</para>
/// </summary>
public static class RadioCoaxView
{
    /// <summary>How far down from its strongest the dimmest cell is drawn, in dB; anything weaker is drawn at the dimmest.</summary>
    private const double RangeDb = 6.0;
    private const float CellSize = 0.36f;
    private static readonly Color Bright = new(0.44f, 0.83f, 0.88f, 0.85f);

    /// <summary>Draws the run to every antenna on a station's coax, or to the one antenna selected.</summary>
    public static void Draw(Thing selected)
    {
        if (!selected.Spawned) return;
        var station = selected.TryGetComp<CompRadio>() is { Props.isStation: true } radio
            ? radio
            : selected.TryGetComp<CompFeedlinePort>() is { } port ? FeedlineService.StationOn(port) : null;
        if (station?.parent.TryGetComp<CompFeedlinePort>() is not { PipeNet: { } net } stationPort) return;
        var band = station.Props.bands.Count > 0 ? station.Props.bands[0] : null;
        if (band == null) return;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        foreach (var antenna in FeedlineService.AntennasOn(stationPort))
        {
            if (selected != station.parent && antenna.parent != selected) continue;
            var path = FeedlineService.Path(net, station.parent, antenna.parent, metresPerCell);
            if (path != null) DrawRun(path, station, band, metresPerCell);
        }
    }

    private static void DrawRun(FeedPath path, CompRadio station, BandDef band, float metresPerCell)
    {
        var drive = station.Props.transmitPowerWatts;
        // The level along the run, relative to the station's own output, in dB; the brightest point sets the top of the range.
        double level = 0, peak = 0;
        foreach (var cell in path.Cells)
        {
            level = Step(level, cell, drive, band, metresPerCell);
            if (level > peak) peak = level;
        }
        level = 0;
        var altitude = AltitudeLayer.MetaOverlays.AltitudeFor();
        foreach (var cell in path.Cells)
        {
            level = Step(level, cell, drive, band, metresPerCell);
            // Rounded to eighths: the game keeps a material per colour asked for, so a smooth fade would make endless ones.
            var share = Mathf.Round(Mathf.Clamp01((float)((level - (peak - RangeDb)) / RangeDb)) * 8f) / 8f;
            var colour = new Color(Bright.r, Bright.g, Bright.b, 0.15f + 0.7f * share);
            var at = cell.Cell.ToVector3Shifted();
            at.y = altitude;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.identity, new Vector3(CellSize, 1f, CellSize)),
                MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, colour), 0);
        }
    }

    /// <summary>The level after one more cell: less its coax's loss, or raised by a powered amplifier (as the link budget raises it).</summary>
    private static double Step(double levelDb, FeedCell cell, float driveWatts, BandDef band, float metresPerCell)
    {
        if (cell.Amplifier is { } amplifier)
        {
            var props = amplifier.Props;
            if (!amplifier.Powered) return levelDb - props.lossDb;
            var outWatts = Amplifier.OutputWatts(driveWatts * Mathf.Pow(10f, (float)(levelDb / 10.0)), props.gainDb, props.ratedOutputWatts);
            return 10.0 * System.Math.Log10(System.Math.Max(outWatts, 1e-9) / driveWatts);
        }
        var coax = cell.Coax;
        return coax == null ? levelDb : levelDb - Coax.LossDb(metresPerCell, coax.lossDbPer100m, coax.referenceMHz, band.centreMHz);
    }
}
