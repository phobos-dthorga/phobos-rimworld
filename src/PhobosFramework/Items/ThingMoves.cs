using Verse;

namespace Phobos.Framework.Items;

/// <summary>
/// Moves things between containers and says truly how many moved (Framework 0.286.2; the owner's HEMTT of 25 Nov 2026). The game's
/// ThingOwner.TryTransferToContainer answers with the size of the stack that arrived as a thing of its own, so a stack that merged into one
/// already there counts as nought, or as only its unmerged rest. A loop that took that answer as the amount moved kept moving: the HEMTT's
/// work store took 975 steel out of its hold for fittings that needed about a hundred. Every move of ours that counts goes through here.
/// </summary>
public static class ThingMoves
{
    /// <summary>Moves up to so many of a thing from one container to another; returns how many left the first container.</summary>
    public static int Transfer(ThingOwner from, Thing thing, ThingOwner to, int count)
    {
        if (from == null || thing == null || to == null || count <= 0 || !from.Contains(thing)) return 0;
        int before = thing.stackCount;
        from.TryTransferToContainer(thing, to, count);
        // Whatever stayed behind is still this thing, in this container: a partial move splits off, a failed one is put back.
        int left = from.Contains(thing) && !thing.Destroyed ? thing.stackCount : 0;
        return System.Math.Max(0, before - left);
    }
}
