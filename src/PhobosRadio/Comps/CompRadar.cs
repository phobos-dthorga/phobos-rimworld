using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio;

/// <summary>
/// A radar station (Radio 0.74.0; docs/development/radar-and-air-defence.md, row RD6). Its figures are the radar sum's
/// (<see cref="RadarRule"/>): a frequency, a peak power, one antenna that sends and hears, a receiver's bandwidth and noise figure, and the
/// height of its mast. XML first, so another modder adds a radar with a Def. Every figure on the shipped radars is authored and says so.
/// </summary>
public class CompProperties_Radar : CompProperties
{
    public float megahertz = 3000f;
    public float peakPowerWatts = 100000f;
    public float antennaGainDbi = 33f;
    public float bandwidthHz = 1e6f;
    public float noiseFigureDb = 5f;
    /// <summary>The antenna's height above the ground, in metres, before any taller tower is fitted.</summary>
    public float mastMetres = 15f;
    /// <summary>The turning antenna's picture, drawn over the station while it has power; none draws nothing.</summary>
    public string? antennaTexPath;
    public Vector2 antennaDrawSize = new(3f, 3f);
    /// <summary>Where the antenna turns, from the station's centre, in cells.</summary>
    public Vector3 antennaOffset = Vector3.zero;
    /// <summary>How fast it turns, in degrees a game tick (6 = one turn in a game minute's 60 ticks; authored, for looks).</summary>
    public float turnDegreesPerTick = 6f;
    /// <summary>A short word for the band, for the inspect line ("S band").</summary>
    public string bandLabel = "";

    public CompProperties_Radar() => compClass = typeof(CompRadar);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef)) yield return error;
        if (megahertz <= 0) yield return $"{parentDef.defName}: CompProperties_Radar needs a positive megahertz";
        if (peakPowerWatts <= 0) yield return $"{parentDef.defName}: CompProperties_Radar needs a positive peakPowerWatts";
        if (bandwidthHz <= 0) yield return $"{parentDef.defName}: CompProperties_Radar needs a positive bandwidthHz";
    }
}

/// <summary>
/// A radar station on the map: it works while it has power and stands under open sky (RD3: any roof over it blinds it), and answers how far
/// it sees a target of a given size and height. The colony's radar picture is <see cref="MapComponent_PhobosRadar"/>'s.
/// <para>Cost: a roof look over its footprint once a rare tick; the antenna is one mesh drawn a frame while on screen; the sums are asked by
/// the radar picture only.</para>
/// </summary>
public class CompRadar : ThingComp
{
    /// <summary>The taller tower's figure, the antenna's height in metres (RadioUpgrades.HeightMetres).</summary>
    public const string TowerFigure = RadioUpgrades.HeightMetres;
    /// <summary>The automatic plot extractor's figure: 1 when fitted.</summary>
    public const string PlotExtractorFigure = "plotExtractor";
    /// <summary>The data link's figure: 1 when fitted.</summary>
    public const string DataLinkFigure = "dataLink";

    private CompPowerTrader? power;
    private CompFlickable? flick;
    private bool roofed;
    private Material? antennaMat;

    public CompProperties_Radar Props => (CompProperties_Radar)props;

    /// <summary>Under a roof somewhere over its footprint, at its last look.</summary>
    public bool Roofed => roofed;

    public bool Powered => (power == null || power.PowerOn) && (flick == null || flick.SwitchIsOn);

    /// <summary>It has power and open sky.</summary>
    public bool Working => parent.Spawned && Powered && !roofed && !parent.IsBrokenDown();

    public bool HasPlotExtractor => RadioUpgrades.Figure(parent, PlotExtractorFigure, 0f) >= 1f;

    public bool HasDataLink => RadioUpgrades.Figure(parent, DataLinkFigure, 0f) >= 1f;

    /// <summary>The antenna's height over the plain: its mast (a taller tower if fitted) and its tile's hills.</summary>
    public double HeightMetres => RadioUpgrades.Figure(parent, TowerFigure, Props.mastMetres)
        + (parent.Spawned ? RelayNode.SiteOf(parent.Map.Tile) : 0.0);

    /// <summary>The noise its receiver hears, in dBm.</summary>
    public double FloorDbm => NoiseFloor.ThermalDbm(Props.bandwidthHz, Props.noiseFigureDb);

    /// <summary>How far it sees a target of this size at this height, in metres: the sum, cut at the horizon.</summary>
    public double SeesMetres(double squareMetres, double targetMetres) =>
        RadarRule.SeenRangeMetres(
            RadarRule.DetectionRangeMetres(Decibels.FromWatts(Props.peakPowerWatts), Props.antennaGainDbi, Props.megahertz, squareMetres, FloorDbm,
                RadarRule.FluctuatingTargetSnrDb),
            HeightMetres, targetMetres);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
        flick = parent.GetComp<CompFlickable>();
        LookAtRoof();
        MapComponent_PhobosRadar.For(parent.Map).Add(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_PhobosRadar.For(map).Remove(this);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        LookAtRoof();
    }

    public override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        RadioUpgrades.Notice(parent, signal);
        if (parent.Spawned) MapComponent_PhobosRadar.For(parent.Map).MarkDirty();
    }

    private void LookAtRoof()
    {
        if (!parent.Spawned) return;
        bool was = roofed;
        roofed = false;
        foreach (IntVec3 cell in parent.OccupiedRect())
        {
            if (parent.Map.roofGrid.Roofed(cell))
            {
                roofed = true;
                break;
            }
        }
        if (was != roofed) MapComponent_PhobosRadar.For(parent.Map).MarkDirty();
    }

    /// <summary>The turning antenna, drawn while it works; still, at its last bearing, while it does not.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (Props.antennaTexPath.NullOrEmpty()) return;
        antennaMat ??= MaterialPool.MatFrom(Props.antennaTexPath, ShaderDatabase.Cutout);
        float angle = Working ? (Find.TickManager.TicksGame * Props.turnDegreesPerTick + parent.thingIDNumber * 37f) % 360f : parent.thingIDNumber * 37f % 360f;
        Vector3 pos = parent.DrawPos + Props.antennaOffset;
        pos.y = AltitudeLayer.BuildingOnTop.AltitudeFor();
        var matrix = Matrix4x4.TRS(pos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(Props.antennaDrawSize.x, 1f, Props.antennaDrawSize.y));
        Graphics.DrawMesh(MeshPool.plane10, matrix, antennaMat, 0);
    }

    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned) return string.Empty;
        if (roofed) return "PhobosRadar_Roofed".Translate();
        if (!Working) return "PhobosRadar_Off".Translate();
        var rules = RadioRulesDef.Current;
        double fighter = SeesMetres(5.0, 3000.0) / 1000.0;
        double drop = SeesMetres(10 * rules.radarDropPodSquareMetres, rules.radarDropCarrierMetres) / 1000.0;
        return "PhobosRadar_Inspect".Translate(Props.bandLabel.Named("BAND"), fighter.ToString("0").Named("FIGHTER"), drop.ToString("0").Named("DROP"),
            HeightMetres.ToString("0").Named("HEIGHT"));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer) yield break;
        yield return new Command_Action
        {
            defaultLabel = "PhobosRadar_ScopeView".Translate(),
            defaultDesc = "PhobosRadar_ScopeViewDesc".Translate(),
            icon = RadarSymbols.ScopeIcon,
            action = () => Dialog_RadarScope.Open(parent.Map),
        };
    }
}

/// <summary>
/// The radar console (Radio 0.74.0; the owner's word of 28 Nov 2026: "involve a console for the scope itself that operators will work from,
/// custom paid art and all"): a desk with screens where colonists keep watch, and where the scope opens. It shows every radar on its map and
/// every radar linked to it by data link. Without a console the radars still warn, on power alone (the owner's card of the same day).
/// </summary>
public class CompProperties_RadarConsole : CompProperties
{
    public CompProperties_RadarConsole() => compClass = typeof(CompRadarConsole);
}

public class CompRadarConsole : ThingComp
{
    private CompPowerTrader? power;

    public bool Working => parent.Spawned && (power == null || power.PowerOn) && !parent.IsBrokenDown();

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
        MapComponent_PhobosRadar.For(parent.Map).Add(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapComponent_PhobosRadar.For(map).Remove(this);
    }

    public override string CompInspectStringExtra()
    {
        if (!parent.Spawned) return string.Empty;
        var picture = MapComponent_PhobosRadar.For(parent.Map);
        return "PhobosRadar_ConsoleInspect".Translate(picture.WorkingRadarCount.Named("RADARS"), picture.LinkedRadarCount.Named("LINKED"));
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer) yield break;
        var command = new Command_Action
        {
            defaultLabel = "PhobosRadar_Scope".Translate(),
            defaultDesc = "PhobosRadar_ScopeDesc".Translate(),
            icon = RadarSymbols.ScopeIcon,
            action = () => Dialog_RadarScope.Open(parent.Map),
        };
        if (!Working) command.Disable("PhobosRadar_ConsoleOff".Translate());
        yield return command;
    }
}
