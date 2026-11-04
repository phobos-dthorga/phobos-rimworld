using System.Collections.Generic;

namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>One burned body part, as the giver reads it from the pawn.</summary>
public readonly struct BurnedPart
{
    /// <summary>Total burn severity on the part, whatever class of burn.</summary>
    public readonly float Severity;
    /// <summary>The part's full health for this pawn; a burn past it counts as the whole part.</summary>
    public readonly float MaxHealth;
    /// <summary>The share of the body's skin this part is, 0 to 1, all parts summing to 1.</summary>
    public readonly float SkinShare;
    /// <summary>Whether every burn on the part has been tended.</summary>
    public readonly bool Tended;

    public BurnedPart(float severity, float maxHealth, float skinShare, bool tended)
    {
        Severity = severity;
        MaxHealth = maxHealth;
        SkinShare = skinShare;
        Tended = tended;
    }
}

/// <summary>The result: how much of the body is burned, and how much of that burned area has been tended.</summary>
public readonly struct BurnLoad
{
    /// <summary>Burned fraction of the body's skin, 0 to 1 (the game's version of total body surface area).</summary>
    public readonly float Tbsa;
    /// <summary>The tended share of the burned area, 0 to 1 (1 when nothing is burned).</summary>
    public readonly float TendedFraction;

    public BurnLoad(float tbsa, float tendedFraction)
    {
        Tbsa = tbsa;
        TendedFraction = tendedFraction;
    }
}

/// <summary>
/// Burned body fraction from the burns on each part: a part counts as burned in proportion to burn severity over its health,
/// capped at the whole part, weighted by the part's share of the skin. The share comes from the game's body coverage figures
/// (approximate: they are hit chances, not skin area; the rule of nines gives head 9%, arms 9% each, legs 18% each, torso 36%).
/// Game-free.
/// </summary>
public static class BurnLoadRule
{
    public static BurnLoad Compute(IReadOnlyList<BurnedPart> parts)
    {
        float burned = 0f;
        float tended = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            BurnedPart part = parts[i];
            if (part.Severity <= 0f || part.MaxHealth <= 0f || part.SkinShare <= 0f) continue;
            float fraction = part.Severity / part.MaxHealth;
            if (fraction > 1f) fraction = 1f;
            float area = fraction * part.SkinShare;
            burned += area;
            if (part.Tended) tended += area;
        }
        if (burned > 1f) burned = 1f;
        return new BurnLoad(burned, burned <= 0f ? 1f : tended / burned);
    }
}
