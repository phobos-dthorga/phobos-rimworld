namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>What a chemical-deepening comp deepens.</summary>
public enum DeepeningTarget
{
    /// <summary>Nothing: a condition that is neither the acid on the skin nor an open burn on a body part.</summary>
    None,
    /// <summary>Every open marked burn on the person: only our "chemical on the skin" condition, which stands for the acid on all of them.</summary>
    MarkedBurns,
    /// <summary>The burn the comp sits on, and no other.</summary>
    Self,
}

/// <summary>
/// Acid left on the skin keeps burning until it is washed off, which is why chemical burns are flushed with water for a long time
/// (first aid everywhere says so). Game-free. The deepening rate is authored; the water is the ANSI/ISEA Z358.1 figures for emergency
/// showers and eyewashes (75.7 litres a minute and 1.5 litres a minute, each for 15 minutes), as reported by ISHN; approximate.
/// </summary>
public static class ChemicalRule
{
    /// <summary>Burn damage in <paramref name="hours"/> from acid still on the skin, falling as the acid is spent.</summary>
    public static float Deepening(float severity, float perHourAtFull, float hours)
    {
        if (severity <= 0f || perHourAtFull <= 0f || hours <= 0f) return 0f;
        return perHourAtFull * (severity > 1f ? 1f : severity) * hours;
    }

    /// <summary>
    /// What a chemical-deepening comp deepens (the owner's card of 28 Nov 2026, "only its own burn"): our acid on the skin deepens every
    /// open marked burn, as it always has; on any other condition the comp deepens only that condition, while it is an open, untended
    /// burn on a body part, and does nothing otherwise.
    /// </summary>
    public static DeepeningTarget Target(bool isAcidOnSkin, bool isInjuryOnPart, bool permanent, bool tended)
    {
        if (isAcidOnSkin) return DeepeningTarget.MarkedBurns;
        return isInjuryOnPart && !permanent && !tended ? DeepeningTarget.Self : DeepeningTarget.None;
    }

    /// <summary>
    /// How much a burn deepening itself grows (the owner's card of 28 Nov 2026, "let it take the part"): the damage, but no more than its
    /// part has left as the game counts it, so the part's health can reach 0, and the game then takes the part, without damage past that
    /// being counted on the wound.
    /// </summary>
    public static float OwnDeepening(float damage, float partHealthLeft)
    {
        if (damage <= 0f || partHealthLeft <= 0f) return 0f;
        return damage < partHealthLeft ? damage : partHealthLeft;
    }

    /// <summary>The water one rinse uses: an eyewash's when every acid burn is on the eyes, else a shower's.</summary>
    public static float RinseLitres(bool eyesOnly, float showerLitres, float eyewashLitres) => eyesOnly ? eyewashLitres : showerLitres;
}
