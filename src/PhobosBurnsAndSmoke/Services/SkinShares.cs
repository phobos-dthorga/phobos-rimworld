using System.Collections.Generic;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Each outside body part's share of the skin, from the body Def's coverage tree: a part's own share is its absolute coverage
/// less that of its outside children. The human body gives torso 0.405, each leg 0.126, each arm 0.08 and so on, summing to 1.
/// Approximate: the game's coverage is a hit chance, not skin area. Computed once per body.
/// </summary>
public static class SkinShares
{
    static readonly Dictionary<BodyDef, Dictionary<BodyPartRecord, float>> cache = new();

    public static float For(BodyPartRecord part)
    {
        if (part.body == null) return 0f;
        if (!cache.TryGetValue(part.body, out Dictionary<BodyPartRecord, float>? shares))
        {
            shares = Compute(part.body);
            cache[part.body] = shares;
        }
        return shares.TryGetValue(part, out float share) ? share : 0f;
    }

    static Dictionary<BodyPartRecord, float> Compute(BodyDef body)
    {
        var shares = new Dictionary<BodyPartRecord, float>();
        foreach (BodyPartRecord part in body.AllParts)
        {
            if (part.depth != BodyPartDepth.Outside) continue;
            float own = part.coverageAbs;
            foreach (BodyPartRecord child in part.parts)
            {
                if (child.depth == BodyPartDepth.Outside) own -= child.coverageAbs;
            }
            shares[part] = own < 0f ? 0f : own;
        }
        return shares;
    }
}
