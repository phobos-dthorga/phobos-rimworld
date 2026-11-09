using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>
/// A factory bill saved before its recipe changed. Pipe System builds a bill's ingredient owners once, when the bill is added
/// (Process.Setup), and saves them; it never compares them with the recipe again. After a recipe change the bill keeps asking for
/// the old ingredients, and the hopper scan matches cell items against the new recipe but puts them into the old owners: an item
/// the old bill never wanted finds no owner and throws, which ends the scan before the items behind it on the cell are read (read in
/// Pipe System 1.7, 7 Nov 2026; the owner's save of 7 Nov held bills from two recipe changes). This compares the two lists and
/// rebuilds the owners from the recipe, setting any counted ingredients down beside the machine, as deleting the bill would.
/// </summary>
public static class BillRepair
{
    static readonly FieldInfo? TargetCount = AccessTools.Field(typeof(Process), "targetCount");
    static readonly FieldInfo? IngredientResources = AccessTools.Field(typeof(Process), "ingredientsCompResources");

    /// <summary>Whether the bill's saved owners no longer match its recipe, by ingredient, category, pipe and count.</summary>
    public static bool IsStale(Process process)
    {
        var owners = process.IngredientsOwners;
        var ingredients = process.Def?.ingredients;
        if (owners == null || ingredients == null) return false;
        if (owners.Count != ingredients.Count) return true;
        for (int i = 0; i < owners.Count; i++)
        {
            ThingAndResourceOwner owner = owners[i];
            ProcessDef.Ingredient ingredient = ingredients[i];
            if (owner.ThingDef != ingredient.thing || owner.ThingCategoryDef != ingredient.thingCategory || owner.PipeNetDef != ingredient.pipeNet) return true;
            if (!ingredient.nutritionGetter && owner.Count + owner.Required != (int)ingredient.countNeeded) return true;
        }
        return false;
    }

    /// <summary>
    /// Rebuilds the owners from the recipe. Counted ingredients go back beside the machine (Pipe System's own reset), the player's
    /// target count is kept (Setup resets it to 1), and the bill then fills from its hoppers as a new one would.
    /// </summary>
    public static void Rebuild(Process process)
    {
        object? target = TargetCount?.GetValue(process);
        PadResources(process);
        process.ResetOwners(false);
        process.Setup();
        if (target != null) TargetCount!.SetValue(process, target);
    }

    /// <summary>Pipe System's reset reads this list by the owners' positions, and builds it from the recipe: a recipe that lost an ingredient
    /// has more saved owners than entries, and the reset would throw. An empty entry means no pipe, so the ingredient is set down by the machine.</summary>
    static void PadResources(Process process)
    {
        if (IngredientResources?.GetValue(process) is not List<CompResource> resources) return;
        int owners = process.IngredientsOwners?.Count ?? 0;
        while (resources.Count < owners) resources.Add(null!);
    }

    public static string LabelOf(Process process) => process.Def?.label ?? process.Def?.defName ?? "bill";
}
