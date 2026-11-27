using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Upgrades;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The radio's building upgrades (Radio 0.4.0, the owner's word of 24 Nov 2026: "use the code we created earlier to allow for
/// upgradeable buildings. Experimental, but I wish to explore it further"), through D'thorga Framework's building upgrades. Each upgrade
/// is a <see cref="PhobosBuildingUpgradeDef"/> in XML that sets one of the figures named here; the radio reads them when it works its
/// links out (about once a second), never per frame. Every figure is "larger is better", as the framework keeps the largest any fitted
/// upgrade sets.
/// </summary>
public static class RadioUpgrades
{
    /// <summary>An antenna's height above the ground, in metres: a whip's taller pole, a mast's extension.</summary>
    public const string HeightMetres = "heightMetres";
    /// <summary>An antenna's gain, in dBi: a longer collinear.</summary>
    public const string GainDbi = "gainDbi";
    /// <summary>How many 25 kHz channels a station has (Radio 0.7.0): a second transceiver.</summary>
    public const string Channels = "channels";
    /// <summary>How much quieter a station's receiver is, in dB, taken off its noise figure: a receive preamplifier.</summary>
    public const string NoiseFigureReductionDb = "noiseFigureReductionDb";
    /// <summary>Whether a station passes calls on between world tiles, 1 for yes (Radio 0.24.0): the repeater.</summary>
    public const string Repeater = "repeater";
    /// <summary>The station has a Morse key (Radio 0.40.0): 1 with the upgrade.</summary>
    public const string MorseKey = "morseKey";

    /// <summary>Every band an upgrade can give (Radio 0.49.0): those whose Def names an upgrade figure. Read once.</summary>
    private static List<BandDef>? upgradeBands;

    private static List<BandDef> UpgradeBands => upgradeBands ??=
        DefDatabase<BandDef>.AllDefsListForReading.Where(band => !string.IsNullOrEmpty(band.upgradeFigure)).ToList();

    /// <summary>
    /// A building's own bands with those its fitted upgrades add (Radio 0.49.0), into <paramref name="into"/>: the aviation transceiver on a
    /// station, the aviation antennas on a mast. Called when the net works its links out, about once a second; a building with no
    /// upgrade comp adds nothing.
    /// </summary>
    public static void BandsOf(Thing building, List<BandDef> own, List<BandDef> into)
    {
        into.Clear();
        into.AddRange(own);
        if (UpgradeBands.Count == 0 || building.TryGetComp<CompPhobosBuildingUpgrades>() is not { } upgrades) return;
        foreach (var band in UpgradeBands)
        {
            if (!into.Contains(band) && upgrades.Figure(band.upgradeFigure!, 0f) >= 1f) into.Add(band);
        }
    }

    /// <summary>A figure on a building: what its fitted upgrades set, else its own.</summary>
    public static float Figure(Thing thing, string name, float own) =>
        thing.TryGetComp<CompPhobosBuildingUpgrades>()?.Figure(name, own) ?? own;

    /// <summary>A station's noise figure once its upgrades are fitted, never below 0 dB.</summary>
    public static float NoiseFigureDb(Thing station, float own) =>
        System.Math.Max(0f, own - Figure(station, NoiseFigureReductionDb, 0f));

    /// <summary>
    /// Hears that an upgrade was fitted or taken off (the framework's signal on the building), and tells the net and the coverage
    /// picture, which would otherwise wait for their next look. Called from the radio's comps.
    /// </summary>
    public static void Notice(ThingWithComps building, string signal)
    {
        if (signal != CompPhobosBuildingUpgrades.ChangedSignal || !building.Spawned) return;
        RadioNet.For(building.Map).MarkDirty();
        RadioCoverage.For(building.Map).NotifyChanged();
    }
}
