using System.Collections.Generic;
using DubsBadHygiene;
using Verse;

namespace Phobos.Factory.Hygiene;

/// <summary>
/// Put on a Pipe System ProcessDef: this bill needs piped water from Dubs Bad Hygiene. The machine that runs it
/// also needs <see cref="CompFactoryWater"/>. Other mods can add both in XML while Dubs Bad Hygiene is active.
/// </summary>
public class WaterUseExtension : DefModExtension
{
    /// <summary>Litres for a whole batch, in Dubs Bad Hygiene's own units.</summary>
    public float litresPerBatch;

    /// <summary>Chance a batch made entirely with untreated water carries food poisoning, 0 to 1.</summary>
    public float poisonChanceUntreated;

    /// <summary>Chance a batch made entirely with contaminated water carries food poisoning, 0 to 1.</summary>
    public float poisonChanceContaminated;

    public float PoisonChance(ContaminationLevel quality) => quality switch
    {
        ContaminationLevel.Contaminated => poisonChanceContaminated,
        ContaminationLevel.Untreated => poisonChanceUntreated,
        _ => 0f,
    };

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (litresPerBatch <= 0f) yield return "litresPerBatch must be above 0";
        if (poisonChanceUntreated is < 0f or > 1f) yield return "poisonChanceUntreated must be from 0 to 1";
        if (poisonChanceContaminated is < 0f or > 1f) yield return "poisonChanceContaminated must be from 0 to 1";
    }
}
