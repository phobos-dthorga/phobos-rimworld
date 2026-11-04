using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Rules;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Each outside body part's share of the skin, from the body Def's coverage tree (see <see cref="SkinShareRule"/>). The human
/// body gives torso 0.405, each leg 0.126, each arm 0.080, each shoulder 0.028, head 0.028 and so on, summing to 1. Computed
/// once per body. The engine's coverageAbs already leaves every child out, so it is not used here: subtracting the outside
/// children from it again zeroed the torso, neck, head and shoulders (the 0.1.0 fault).
/// </summary>
public static class SkinShares
{
    static readonly Dictionary<BodyDef, Dictionary<BodyPartRecord, float>> cache = new();

    /// <summary>The part's own skin, without its outside children.</summary>
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

    /// <summary>The part and everything under it: what a burned-off part takes with it.</summary>
    public static float WithChildren(BodyPartRecord part) => part.depth == BodyPartDepth.Outside ? part.coverageAbsWithChildren : 0f;

    static Dictionary<BodyPartRecord, float> Compute(BodyDef body)
    {
        var shares = new Dictionary<BodyPartRecord, float>();
        foreach (BodyPartRecord part in body.AllParts)
        {
            if (part.depth != BodyPartDepth.Outside) continue;
            float outsideChildren = 0f;
            foreach (BodyPartRecord child in part.parts)
            {
                if (child.depth == BodyPartDepth.Outside) outsideChildren += child.coverageAbsWithChildren;
            }
            shares[part] = SkinShareRule.OwnShare(part.coverageAbsWithChildren, outsideChildren);
        }
        return shares;
    }
}
