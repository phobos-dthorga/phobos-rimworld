using Phobos.Medical.Services;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>
/// Water for a building that uses it now and then (a safety shower), from whatever supplies it. A plumbing mod's tie-in adds a comp
/// with this; without one, water is assumed. The reason is player text, already translated.
/// </summary>
public interface IPhobosWaterSource
{
    bool TryDraw(float litres, out string reason);
    /// <summary>The last draw failed recently, so nobody should run here for water.</summary>
    bool LooksDry { get; }
}

/// <summary>
/// A safety shower and eyewash: acid on the skin is rinsed off here. The water is the ANSI/ISEA Z358.1 figures as reported by ISHN
/// (approximate): a drench shower 75.7 litres a minute and an eyewash 1.5 litres a minute, each for 15 minutes, which is 625 ticks.
/// Other mods can add stations with this in XML.
/// </summary>
public class CompProperties_PhobosRinseStation : CompProperties
{
    public float showerLitres = 1136f;
    public float eyewashLitres = 22.7f;
    public int rinseTicks = 625;
    /// <summary>How long a doctor takes to draw water here for a patient who can't walk (authored).</summary>
    public int fillTicks = 120;
    /// <summary>Loops while someone rinses under it; null for silence.</summary>
    public SoundDef? showerSound;
    /// <summary>Plays when a doctor rinses a patient with water drawn here; null for silence.</summary>
    public SoundDef? pourSound;

    public CompProperties_PhobosRinseStation() => compClass = typeof(CompPhobosRinseStation);
}

public class CompPhobosRinseStation : ThingComp
{
    public CompProperties_PhobosRinseStation Props => (CompProperties_PhobosRinseStation)props;

    public override string CompInspectStringExtra() =>
        (RinseService.WaterSource(parent)?.LooksDry == true ? "PhobosMedical_RinseStationDry" : "PhobosMedical_RinseStationReady").Translate().Resolve();
}
