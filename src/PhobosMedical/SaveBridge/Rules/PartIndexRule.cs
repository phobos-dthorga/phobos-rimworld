using System.Collections.Generic;

namespace Phobos.MedicalSaveBridge.Rules;

/// <summary>
/// The rule that carries a saved body part across the insertion of new parts. Game-free, so the tests run without RimWorld.
///
/// RimWorld saves a body part as its position in the body's part list, which BodyDef.CacheDataRecursive fills depth-first from
/// the core part (read from RimWorld 1.7's code, 3 November 2026). A mod that adds parts with PatchOperationAdd appends children,
/// so every part the save already knew keeps its relative order, and the old numbering is today's with the new parts skipped. A
/// mod that moved an existing part would break that assumption; none of the mods this bridge was written for does.
/// </summary>
public static class PartIndexRule
{
    /// <summary>
    /// Builds the map from a saved (old) index to today's index. <paramref name="isNew"/> says, for each part in today's order,
    /// whether it comes from a mod the save never had. Returns null when no part is new, so callers can skip the body entirely.
    /// </summary>
    public static int[]? OldToNewIndices(IReadOnlyList<bool> isNew)
    {
        int oldCount = 0;
        for (int i = 0; i < isNew.Count; i++)
        {
            if (!isNew[i]) oldCount++;
        }
        if (oldCount == isNew.Count) return null;
        var map = new int[oldCount];
        int next = 0;
        for (int i = 0; i < isNew.Count; i++)
        {
            if (!isNew[i]) map[next++] = i;
        }
        return map;
    }

    /// <summary>
    /// Finds today's index for a saved one. False when the saved index was never valid for the old body (a corrupt or foreign
    /// reference); the caller then leaves the game's own answer alone.
    /// </summary>
    public static bool TryRemap(int[] map, int oldIndex, out int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= map.Length)
        {
            newIndex = oldIndex;
            return false;
        }
        newIndex = map[oldIndex];
        return true;
    }
}
