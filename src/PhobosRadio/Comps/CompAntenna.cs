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

    /// <summary>While selected, the map shows this antenna's coverage (Radio 0.2.0); the picture's own service does the work.</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        if (!parent.Spawned || parent.Faction != RimWorld.Faction.OfPlayer) return;
        RadioCoverage.For(parent.Map).Show(parent);
        UI.RadioCoaxView.Draw(parent);
    }

    /// <summary>An upgrade fitted or taken off: the net and the picture work it in at once (Radio 0.4.0).</summary>
    public override void ReceiveCompSignal(string signal) => RadioUpgrades.Notice(parent, signal);

    public override string CompInspectStringExtra() =>
        parent.Spawned ? RadioCoverage.For(parent.Map).InspectLine(parent) ?? string.Empty : string.Empty;
}
