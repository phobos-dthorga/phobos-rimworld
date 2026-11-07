using System.Collections.Generic;
using Phobos.Triage.Defs;
using RimWorld;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>What the index knows about one recipe, read once.</summary>
public sealed class OperationInfo
{
    public readonly RecipeDef Recipe;
    public readonly IReadOnlyList<HediffDef> Treats;
    public readonly bool NeedsMedicine;
    /// <summary>An ingredient that is not medicine and is worth at least the rules Def's rare-tool value.</summary>
    public readonly bool NeedsRareTool;
    /// <summary>A rough cost: base count times the cheapest allowed ingredient's market value, summed (for ordering only).</summary>
    public readonly float IngredientCost;

    public OperationInfo(RecipeDef recipe, IReadOnlyList<HediffDef> treats, bool needsMedicine, bool needsRareTool, float ingredientCost)
    {
        Recipe = recipe;
        Treats = treats;
        NeedsMedicine = needsMedicine;
        NeedsRareTool = needsRareTool;
        IngredientCost = ingredientCost;
    }
}

/// <summary>
/// Every surgery in the game that removes a condition, by the condition it removes: the recipe's own removesHediff (the game's,
/// More Injuries', ours) plus what a recipe's extension names, minus recipes that ask never to be queued. Built once on first
/// use after the Defs load; never by name.
/// </summary>
[StaticConstructorOnStartup]
public static class OperationIndex
{
    static Dictionary<HediffDef, List<OperationInfo>>? byCondition;
    static List<OperationInfo>? all;

    public static IReadOnlyList<OperationInfo> All
    {
        get
        {
            if (all == null) Build();
            return all!;
        }
    }

    public static IReadOnlyList<OperationInfo> For(HediffDef condition)
    {
        if (byCondition == null) Build();
        return byCondition!.TryGetValue(condition, out List<OperationInfo>? list) ? list : (IReadOnlyList<OperationInfo>)System.Array.Empty<OperationInfo>();
    }

    public static bool Treats(RecipeDef recipe, HediffDef condition)
    {
        if (recipe.removesHediff == condition) return true;
        PhobosTriageExtension? extension = recipe.GetModExtension<PhobosTriageExtension>();
        return extension != null && extension.treatsHediffs.Contains(condition);
    }

    static void Build()
    {
        var built = new Dictionary<HediffDef, List<OperationInfo>>();
        var list = new List<OperationInfo>();
        float rareValue = PhobosTriageRulesDef.Current.rareToolMarketValue;
        List<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefsListForReading;
        for (int i = 0; i < recipes.Count; i++)
        {
            RecipeDef recipe = recipes[i];
            if (recipe.Worker is not Recipe_Surgery) continue;
            PhobosTriageExtension? extension = recipe.GetModExtension<PhobosTriageExtension>();
            if (extension != null && extension.neverAutoQueue) continue;
            var treats = new List<HediffDef>();
            if (recipe.removesHediff != null) treats.Add(recipe.removesHediff);
            if (extension != null)
            {
                for (int t = 0; t < extension.treatsHediffs.Count; t++)
                {
                    if (!treats.Contains(extension.treatsHediffs[t])) treats.Add(extension.treatsHediffs[t]);
                }
            }
            if (treats.Count == 0) continue;
            ReadIngredients(recipe, rareValue, out bool needsMedicine, out bool needsRareTool, out float cost);
            var info = new OperationInfo(recipe, treats, needsMedicine, needsRareTool, cost);
            list.Add(info);
            for (int t = 0; t < treats.Count; t++)
            {
                if (!built.TryGetValue(treats[t], out List<OperationInfo>? forCondition))
                {
                    forCondition = new List<OperationInfo>();
                    built[treats[t]] = forCondition;
                }
                forCondition.Add(info);
            }
        }
        byCondition = built;
        all = list;
    }

    static void ReadIngredients(RecipeDef recipe, float rareValue, out bool needsMedicine, out bool needsRareTool, out float cost)
    {
        needsMedicine = false;
        needsRareTool = false;
        cost = 0f;
        if (recipe.ingredients == null) return;
        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            IngredientCount ingredient = recipe.ingredients[i];
            float cheapest = float.MaxValue;
            bool anyMedicine = false;
            bool allRare = true;
            bool any = false;
            foreach (ThingDef def in ingredient.filter.AllowedThingDefs)
            {
                any = true;
                if (def.IsMedicine) anyMedicine = true;
                if (def.IsMedicine || def.BaseMarketValue < rareValue) allRare = false;
                if (def.BaseMarketValue < cheapest) cheapest = def.BaseMarketValue;
            }
            if (!any) continue;
            if (anyMedicine) needsMedicine = true;
            if (allRare) needsRareTool = true;
            cost += ingredient.GetBaseCount() * (cheapest == float.MaxValue ? 0f : cheapest);
        }
    }
}
