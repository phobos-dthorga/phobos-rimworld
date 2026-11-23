using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// On a recipe whose ingredients are meals (Framework 0.273.0; docs/development/vehicle-nutrient-paste.md, NP5: the paste bakes): what those
/// meals were made from passes into the product, as well as the meals themselves. The game records only an ingredient's own name, so paste
/// made from human meat would otherwise come out of the oven clean. Marks only the recipes that carry it; every other recipe is untouched.
/// </summary>
public class PhobosKeepsIngredientsExtension : DefModExtension
{
}

/// <summary>
/// Passes the ingredients' own ingredients into a marked recipe's products (NP5). A postfix that wraps the game's product list, after the
/// game has registered its own ingredients; it never skips the original, and costs one extension check for any other recipe.
/// </summary>
[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
public static class PasteBakeIngredients
{
    public static void Postfix(RecipeDef recipeDef, List<Thing> ingredients, ref IEnumerable<Thing> __result)
    {
        if (recipeDef?.HasModExtension<PhobosKeepsIngredientsExtension>() != true || ingredients == null) return;
        __result = Carry(__result, ingredients);
    }

    static IEnumerable<Thing> Carry(IEnumerable<Thing> products, List<Thing> ingredients)
    {
        foreach (Thing product in products)
        {
            if (product.TryGetComp<CompIngredients>() is { } made)
            {
                for (int i = 0; i < ingredients.Count; i++)
                {
                    if (ingredients[i]?.TryGetComp<CompIngredients>() is not { } inner) continue;
                    for (int j = 0; j < inner.ingredients.Count; j++) made.RegisterIngredient(inner.ingredients[j]);
                }
                if (Seen.Wants("vehicles.paste-bake", product)) Seen.Note("vehicles.paste-bake", product, made.ingredients.Count + " ingredient(s)");
            }
            yield return product;
        }
    }
}
