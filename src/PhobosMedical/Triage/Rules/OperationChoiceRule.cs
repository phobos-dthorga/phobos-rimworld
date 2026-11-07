using System.Collections.Generic;

namespace Phobos.Triage.Rules;

/// <summary>Why an operation cannot be queued, from the nearest-to-possible to the furthest. The order is what the message reports.</summary>
public enum Refusal
{
    None,
    /// <summary>An ingredient or tool is missing from the map.</summary>
    Item,
    /// <summary>Doctors exist but none meets the recipe's skill.</summary>
    Skill,
    /// <summary>No doctor could come at all.</summary>
    NoDoctor,
    /// <summary>The recipe's research is not done.</summary>
    Research,
    /// <summary>The chance of death is above what the player allows.</summary>
    TooRisky,
    /// <summary>The player's care level forbids the medicine it needs.</summary>
    CareLevel,
    /// <summary>It would be a violation on another faction's person.</summary>
    Violation,
    /// <summary>It needs a rare tool and the player keeps those for their own bills.</summary>
    RareTool,
    /// <summary>No recipe fixes the condition.</summary>
    NoRecipe,
}

/// <summary>One recipe that would fix a condition, as the game and the colony stand right now.</summary>
public readonly struct OperationCandidate
{
    public readonly bool ResearchDone;
    public readonly bool DoctorExists;
    /// <summary>The best surgery success chance among doctors meeting the skill; negative when none meets it.</summary>
    public readonly float BestDoctorSuccess;
    public readonly float SuccessFactor;
    public readonly float DeathOnFailure;
    public readonly bool IngredientsPresent;
    public readonly float IngredientCost;
    public readonly bool CareAllows;
    public readonly bool Violation;
    public readonly bool NeedsRareTool;

    public OperationCandidate(bool researchDone, bool doctorExists, float bestDoctorSuccess, float successFactor, float deathOnFailure,
        bool ingredientsPresent, float ingredientCost, bool careAllows, bool violation, bool needsRareTool)
    {
        ResearchDone = researchDone;
        DoctorExists = doctorExists;
        BestDoctorSuccess = bestDoctorSuccess;
        SuccessFactor = successFactor;
        DeathOnFailure = deathOnFailure;
        IngredientsPresent = ingredientsPresent;
        IngredientCost = ingredientCost;
        CareAllows = careAllows;
        Violation = violation;
        NeedsRareTool = needsRareTool;
    }
}

/// <summary>What the player allows the doctors to decide alone.</summary>
public readonly struct OperationLimits
{
    public readonly float MaxDeathChance;
    public readonly bool AllowViolations;
    public readonly bool AllowRareTools;

    public OperationLimits(float maxDeathChance, bool allowViolations, bool allowRareTools)
    {
        MaxDeathChance = maxDeathChance;
        AllowViolations = allowViolations;
        AllowRareTools = allowRareTools;
    }
}

/// <summary>
/// Picks the operation the doctors queue for a condition: the one most likely to succeed, then the cheapest; and when none
/// can be queued, the refusal nearest to being fixable, so the message says the one thing the player could change.
/// </summary>
public static class OperationChoiceRule
{
    /// <summary>The game clamps the outcome quality at this.</summary>
    public const float MaxQuality = 0.98f;

    /// <summary>The planning estimate of the game's outcome quality: the recipe's factor times the best doctor's chance, clamped.</summary>
    public static float ExpectedSuccess(in OperationCandidate c)
    {
        float quality = c.SuccessFactor * (c.BestDoctorSuccess > 0f ? c.BestDoctorSuccess : 0f);
        return quality > MaxQuality ? MaxQuality : quality < 0f ? 0f : quality;
    }

    /// <summary>The game rolls death only after a failed outcome: (1 - quality) times the recipe's death chance.</summary>
    public static float DeathChance(in OperationCandidate c) => (1f - ExpectedSuccess(c)) * (c.DeathOnFailure > 0f ? c.DeathOnFailure : 0f);

    public static Refusal RefusalOf(in OperationCandidate c, in OperationLimits limits)
    {
        if (!c.ResearchDone) return Refusal.Research;
        if (!c.DoctorExists) return Refusal.NoDoctor;
        if (c.BestDoctorSuccess < 0f) return Refusal.Skill;
        if (!c.CareAllows) return Refusal.CareLevel;
        if (c.Violation && !limits.AllowViolations) return Refusal.Violation;
        if (c.NeedsRareTool && !limits.AllowRareTools) return Refusal.RareTool;
        if (!c.IngredientsPresent) return Refusal.Item;
        if (DeathChance(c) > limits.MaxDeathChance) return Refusal.TooRisky;
        return Refusal.None;
    }

    /// <summary>
    /// The index of the candidate to queue, or -1. <paramref name="nearest"/> is None on success, else the refusal of the
    /// candidate that came closest (the lowest refusal in the enum's order).
    /// </summary>
    public static int Choose(IReadOnlyList<OperationCandidate> candidates, in OperationLimits limits, out Refusal nearest)
    {
        int best = -1;
        float bestSuccess = -1f;
        float bestCost = float.MaxValue;
        nearest = candidates.Count == 0 ? Refusal.NoRecipe : Refusal.NoRecipe;
        for (int i = 0; i < candidates.Count; i++)
        {
            OperationCandidate c = candidates[i];
            Refusal refusal = RefusalOf(c, limits);
            if (refusal != Refusal.None)
            {
                if (refusal < nearest) nearest = refusal;
                continue;
            }
            float success = ExpectedSuccess(c);
            if (success > bestSuccess || (success == bestSuccess && c.IngredientCost < bestCost))
            {
                best = i;
                bestSuccess = success;
                bestCost = c.IngredientCost;
            }
        }
        if (best >= 0) nearest = Refusal.None;
        return best;
    }
}
