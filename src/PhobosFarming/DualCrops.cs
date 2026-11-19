using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Farming;

/// <summary>
/// The second product of a dual crop (Vanilla Expanded Framework's DualCropExtension: barley's hay, oats' raw oats), worked out for a
/// machine the way that framework's own harvest patch works it out for a hand, because its patch passes over a vehicle (its public
/// source, Source/VEF/Plants/Harmony/Plant_PlantCollected.cs, read 20 Nov 2026; the rule written afresh in HarvestRule.SecondProduct, none of
/// its code, as its licence asks). Read by reflection from the extension's fields, by its full name, only when the framework is loaded: no
/// reference to its assembly. A crop without the extension gives nothing.
/// </summary>
public static class DualCrops
{
    const string ExtensionTypeName = "VEF.Plants.DualCropExtension";

    static bool looked;
    static System.Type? extensionType;
    static FieldInfo? secondaryOutput, outPutAmount, randomOutput, randomSecondaryOutput;

    static bool Ready()
    {
        if (looked) return extensionType != null;
        looked = true;
        extensionType = AccessTools.TypeByName(ExtensionTypeName);
        if (extensionType == null) return false;
        secondaryOutput = AccessTools.Field(extensionType, "secondaryOutput");
        outPutAmount = AccessTools.Field(extensionType, "outPutAmount");
        randomOutput = AccessTools.Field(extensionType, "randomOutput");
        randomSecondaryOutput = AccessTools.Field(extensionType, "randomSecondaryOutput");
        if (secondaryOutput == null || outPutAmount == null)
        {
            FarmingMod.Logger.Warn("Vanilla Expanded Framework's dual crop extension has changed shape; second products from machines are off.");
            extensionType = null;
        }
        return extensionType != null;
    }

    /// <summary>Adds a dual crop's second product to the tally, as a hand's harvest would give it. Call before the plant's harvest hook.</summary>
    public static void Second(Plant plant, float yieldStat, Dictionary<ThingDef, int> tally)
    {
        if (plant.def.modExtensions == null || !Ready() || !plant.CanYieldNow()) return;
        object? extension = null;
        foreach (DefModExtension e in plant.def.modExtensions)
        {
            if (extensionType!.IsInstanceOfType(e))
            {
                extension = e;
                break;
            }
        }
        if (extension == null) return;
        int amount = (int)(outPutAmount!.GetValue(extension) ?? 0);
        int count = HarvestRule.SecondProduct(plant.Blighted, amount, plant.Growth, yieldStat, Rand.Value, Rand.Value);
        if (count <= 0) return;
        ThingDef? output = null;
        if (randomOutput?.GetValue(extension) is true && randomSecondaryOutput?.GetValue(extension) is IList list && list.Count > 0)
        {
            output = list[Rand.Range(0, list.Count)] as ThingDef;
        }
        output ??= secondaryOutput!.GetValue(extension) as ThingDef;
        if (output == null) return;
        tally.TryGetValue(output, out int had);
        tally[output] = had + count;
    }
}
