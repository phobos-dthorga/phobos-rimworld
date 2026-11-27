using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>
/// The operations that give an item which mends the worst health condition (Medical 0.33.0; docs/development/medical-care-round-two.md):
/// the game's "administer healer mech serum", found by what its item does (a use effect of the game's
/// <c>CompUseEffect_FixWorstHealthCondition</c>), never by name. Triage queues one only for a patient it tags expectant, and only while
/// the game's own choice of what the item mends is the condition killing them, so a costly item is never spent on something else.
/// </summary>
[StaticConstructorOnStartup]
public static class HealerSerum
{
    static readonly List<RecipeDef> recipes = new();

    static HealerSerum()
    {
        foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
        {
            if (recipe.workerClass == null || !typeof(Recipe_AdministerUsableItem).IsAssignableFrom(recipe.workerClass)) continue;
            if (recipe.ingredients == null || recipe.ingredients.Count != 1 || !recipe.ingredients[0].IsFixedIngredient) continue;
            ThingDef item = recipe.ingredients[0].FixedIngredient;
            if (Mends(item)) recipes.Add(recipe);
        }
    }

    /// <summary>The operations found: none without the item's mod.</summary>
    public static IReadOnlyList<RecipeDef> Recipes => recipes;

    public static bool IsSerum(RecipeDef recipe) => recipes.Contains(recipe);

    static bool Mends(ThingDef item)
    {
        if (item.comps == null) return false;
        for (int i = 0; i < item.comps.Count; i++)
        {
            if (item.comps[i]?.compClass != null && typeof(CompUseEffect_FixWorstHealthCondition).IsAssignableFrom(item.comps[i].compClass)) return true;
        }
        return false;
    }

    /// <summary>Whether the item would mend this very condition: the game's own choice, read without changing anything.</summary>
    public static bool WouldMend(Pawn pawn, Hediff condition) =>
        HealthUtility.TryGetWorstHealthCondition(pawn, out Hediff worst, out _) && worst == condition;

    /// <summary>A serum operation already on the patient's bills, ours or the player's.</summary>
    public static Bill_Medical? OnBills(Pawn pawn)
    {
        List<Bill> bills = pawn.BillStack.Bills;
        for (int i = 0; i < bills.Count; i++)
        {
            if (bills[i] is Bill_Medical medical && IsSerum(medical.recipe)) return medical;
        }
        return null;
    }
}
