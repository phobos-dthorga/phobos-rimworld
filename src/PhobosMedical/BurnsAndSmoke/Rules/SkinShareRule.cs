namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// Each outside part's share of the skin, from the game's body coverage tree. The engine keeps two figures per part: its
/// coverage including every child (the root is 1), and its own coverage with all children taken out (the hit-chance weight
/// for that part alone). Skin is the first less the outside children's first: an inside organ's coverage stays with the skin
/// over it, so the outside parts sum to 1. Approximate: coverage is a hit chance, not skin area. Game-free.
/// </summary>
public static class SkinShareRule
{
    public static float OwnShare(float coverageWithChildren, float outsideChildrenCoverageWithChildren)
    {
        float own = coverageWithChildren - outsideChildrenCoverageWithChildren;
        return own < 0f ? 0f : own;
    }
}
