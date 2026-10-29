using System.Collections.Generic;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// Turns every item made at the named benches into a bill on one factory machine. The bills are generated
/// at load from each item's own recipe, after every mod's patches, so they follow the item's costs, work
/// and research. Another mod automates another bench by adding one of these in XML.
/// </summary>
public class BenchBillTemplateDef : Def
{
    /// <summary>The factory building that gets the bills. It needs Pipe System's advanced resource processor.</summary>
    public ThingDef? machine;
    /// <summary>Benches whose items (ThingDefs with a recipeMaker naming one of them) get a bill.</summary>
    public List<ThingDef> sourceBenches = new();
    /// <summary>Items this template never automates.</summary>
    public List<ThingDef> excludedThings = new();
    /// <summary>Research every bill needs on top of its item's own: the bench's, so bills wait for it.</summary>
    public List<ResearchProjectDef> researchPrerequisites = new();
    /// <summary>Machine ticks per unit of bench work. VFE Factory's machining bills use 4.</summary>
    public float workMultiplier = 4f;
    /// <summary>How many of a stacking item may wait in the output cell; lowered per item to a safe limit.</summary>
    public float maxOutputCount = 75f;
    /// <summary>The bill's name; {0} is the item's label.</summary>
    public string billLabel = "Make {0}";
    /// <summary>The bill's description; {0} is the item's label.</summary>
    public string billDescription = "";
    /// <summary>Where the generated bills start in the machine's Add process list (they are sorted by kind, then name).</summary>
    public int firstPriorityInBillList = 10;
    /// <summary>Input cells, relative to the machine and turned with it.</summary>
    public List<IntVec3> autoInputSlots = new();
    public bool spawnOnInteractionCell = true;
    public SoundDef? sustainerDef;
    public EffecterDef? effecterDef;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors()) yield return error;
        if (machine == null) yield return "machine is not set";
        else if (machine.GetCompProperties<CompProperties_AdvancedResourceProcessor>() == null)
            yield return $"machine {machine.defName} has no PipeSystem.CompProperties_AdvancedResourceProcessor";
        if (sourceBenches.NullOrEmpty()) yield return "sourceBenches is empty";
        if (autoInputSlots.NullOrEmpty()) yield return "autoInputSlots is empty (the bills would never take ingredients)";
        if (workMultiplier <= 0f) yield return "workMultiplier must be positive";
        if (maxOutputCount <= 0f) yield return "maxOutputCount must be positive";
        if (!billLabel.Contains("{0}")) yield return "billLabel must contain {0} for the item's label";
    }
}
