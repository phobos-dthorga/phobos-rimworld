using System.Collections.Generic;
using Phobos.Radio.Services;
using Verse;

namespace Phobos.Radio;

/// <summary>An antenna building: parameters, not a simulation, as the design says.</summary>
public class CompProperties_Antenna : CompProperties
{
    public List<BandDef> bands = new();
    public float gainDbi;
    public float heightMetres = 3f;
    /// <summary>The antenna lies flat, as a dipole (Radio 0.62.0): HF's ground wave from it is horizontally polarised and dies within a few
    /// kilometres; its strength is the sky. Whips and masts stand upright (the default).</summary>
    public bool horizontal;

    public CompProperties_Antenna() => compClass = typeof(CompAntenna);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef)) yield return error;
        if (bands.Count == 0) yield return $"{parentDef.defName}: CompProperties_Antenna needs at least one band";
        if (heightMetres <= 0) yield return $"{parentDef.defName}: heightMetres must be positive";
    }
}

public class CompAntenna : ThingComp
{
    public CompProperties_Antenna Props => (CompProperties_Antenna)props;

    private readonly List<BandDef> bands = new();

    /// <summary>The bands this antenna works: its own, and any its fitted upgrades add (Radio 0.49.0: the aviation antennas on a mast).</summary>
    public List<BandDef> Bands
    {
        get
        {
            RadioUpgrades.BandsOf(parent, Props.bands, bands);
            if (bands.Count > Props.bands.Count && Seen.Wants("radio.aviation-bands", parent, "antenna"))
                Seen.Note("radio.aviation-bands", parent, bands.Count + " bands", "antenna");
            return bands;
        }
    }

    /// <summary>While selected, the map shows this antenna's coverage (Radio 0.2.0); the picture's own service does the work.</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        if (!parent.Spawned || parent.Faction != RimWorld.Faction.OfPlayer) return;
        RadioCoverage.For(parent.Map).Show(parent);
        UI.RadioCoaxView.Draw(parent);
    }

    /// <summary>An upgrade fitted or taken off: the net and the picture work it in at once (Radio 0.4.0).</summary>
    public override void ReceiveCompSignal(string signal)
    {
        RadioUpgrades.Notice(parent, signal);
        // Radio 0.64.0: a dipole draws the mast its upgrade names, so its picture is printed again.
        if (parent is Building_PhobosDipole && parent.Spawned) parent.DirtyMapMesh(parent.Map);
    }

    public override string CompInspectStringExtra() =>
        parent.Spawned ? RadioCoverage.For(parent.Map).InspectLine(parent) ?? string.Empty : string.Empty;
}
