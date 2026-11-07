using System.Collections.Generic;

namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>One burned body part, as the giver reads it from the pawn.</summary>
public readonly struct BurnedPart
{
    /// <summary>How much of the part's skin is burned, 0 to 1, from every burn on it (see <see cref="BurnLoadRule.AreaFraction"/>).</summary>
    public readonly float AreaFraction;
    /// <summary>The share of the body's skin this part is, 0 to 1, all parts summing to 1.</summary>
    public readonly float SkinShare;
    /// <summary>Whether every burn on the part has been tended.</summary>
    public readonly bool Tended;

    public BurnedPart(float areaFraction, float skinShare, bool tended)
    {
        AreaFraction = areaFraction < 0f ? 0f : areaFraction > 1f ? 1f : areaFraction;
        SkinShare = skinShare;
        Tended = tended;
    }

    /// <summary>A part burned off: its whole subtree's skin, scaled by how much of the stump is still raw.</summary>
    public static BurnedPart Lost(float subtreeSkinShare, float weight, bool tended) => new(weight, subtreeSkinShare, tended);
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
/// Burned body fraction. Area and depth are two things (owner's choice, 4 Nov 2026): the game's burn severity is depth, and it
/// already drives pain, healing time and whether the part is destroyed; area is derived from it per burn kind, so a wide
/// shallow burn from fire and a small deep one from an incendiary round come out differently. The part's burned area, capped
/// at the whole part, is weighted by the part's share of the skin (approximate: the game's coverage is a hit chance, not skin
/// area; the rule of nines gives head 9%, arms 9% each, legs 18% each, torso 36%). Game-free.
/// </summary>
public static class BurnLoadRule
{
    /// <summary>
    /// How much of a part one burn covers: the burn's damage times the kind's area per point, capped at the kind's largest
    /// possible area. Authored per kind: fire burns are wide and shallow, incendiary rounds small and deep.
    /// </summary>
    public static float AreaFraction(float severity, float areaPerDamage, float maxArea)
    {
        if (severity <= 0f || areaPerDamage <= 0f || maxArea <= 0f) return 0f;
        float area = severity * areaPerDamage;
        return area > maxArea ? maxArea : area;
    }

    /// <summary>
    /// How much of a burned-off part still counts as burned skin: all of it while the stump is fresh (untended), then fading
    /// to nothing over the heal time, by the stump's age. Authored; a late tend drops the weight at once, a simplification.
    /// </summary>
    public static float BurnedOffWeight(bool fresh, int ageTicks, int healTicks)
    {
        if (fresh) return 1f;
        if (healTicks <= 0) return 0f;
        float weight = 1f - (float)ageTicks / healTicks;
        return weight < 0f ? 0f : weight > 1f ? 1f : weight;
    }

    public static BurnLoad Compute(IReadOnlyList<BurnedPart> parts)
    {
        float burned = 0f;
        float tended = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            BurnedPart part = parts[i];
            if (part.AreaFraction <= 0f || part.SkinShare <= 0f) continue;
            float area = part.AreaFraction * part.SkinShare;
            burned += area;
            if (part.Tended) tended += area;
        }
        if (burned > 1f) burned = 1f;
        return new BurnLoad(burned, burned <= 0f ? 1f : tended / burned);
    }
}
