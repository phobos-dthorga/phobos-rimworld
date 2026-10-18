using System.Collections.Generic;
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
}
