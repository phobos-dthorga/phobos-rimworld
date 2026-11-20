using System.Collections.Generic;
using Verse;

namespace Phobos.Farming;

/// <summary>
/// A class of crop by how it is harvested (owner, 20 Nov 2026: "maintain some semblance of realism"): cereals, pulses and oilseeds that a
/// grain front takes; row crops such as corn and sunflowers that need a row-crop header; and others for later harvesters. The game's
/// crop Defs hold nothing that tells grain from potatoes or berries, so a crop is placed in a class by XML (PhobosCropExtension), and
/// every harvesting front lists the classes it takes.
/// </summary>
public class PhobosCropClassDef : Def
{
    /// <summary>Whether the crop's harvest is its own seed (grain, pulses, oilseeds, corn kernels), so it can be kept back and sown again;
    /// false where it is not (hay, cotton lint), which then sows only from bought seed. Farming 0.6.0.</summary>
    public bool harvestIsSeed;
}

/// <summary>
/// The class of a crop, on its plant ThingDef (docs/development/farming-batch-one.md, "Fronts"). Add it to another mod's crop with one
/// patch, as D'thorga Farming's README shows; a crop with none is taken by no machine and stays a crop for hands.
/// </summary>
public class PhobosCropExtension : DefModExtension
{
    public PhobosCropClassDef? cropClass;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (cropClass == null) yield return "a crop extension names no crop class";
    }
}

/// <summary>Reads a crop's class.</summary>
public static class Crops
{
    /// <summary>The crop class of this plant, or null for a crop no machine takes.</summary>
    public static PhobosCropClassDef? ClassOf(ThingDef? plant) => plant?.GetModExtension<PhobosCropExtension>()?.cropClass;

    /// <summary>The crop's own harvest when it is also its seed (its class says so), or null: a crop in no class, or one whose harvest is
    /// not seed (hay, cotton lint), sows only from bought seed by machine and free by hand.</summary>
    public static ThingDef? OwnSeed(ThingDef? plant) => ClassOf(plant)?.harvestIsSeed == true ? plant!.plant?.harvestedThingDef : null;
}
