using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Items;

/// <summary>
/// What a pawn has on them: worn as apparel, or carried in their inventory (Framework 0.381.0, first for the radio's hand-helds).
/// Combat Extended's loadouts, and the game's own "take to inventory", put kit in the inventory and never wear it, so a mod that
/// asks only about worn apparel misses what a pawn plainly has with them. Neither thing is spawned while a pawn has it.
/// </summary>
public static class PawnGear
{
    /// <summary>The pawn carrying this thing in their inventory, or null when nobody is (worn, held, on the ground or in storage).</summary>
    public static Pawn? CarrierOf(Thing thing) => thing.ParentHolder is Pawn_InventoryTracker inventory ? inventory.pawn : null;

    /// <summary>
    /// Adds each comp of the kind on the pawn's worn apparel, then on the things in their inventory, to the list: worn first, so a
    /// caller that takes the first prefers what is worn. Allocates nothing; the caller owns the list and clears it.
    /// </summary>
    public static void CompsOn<T>(Pawn pawn, List<T> into) where T : ThingComp
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn != null)
        {
            for (int i = 0; i < worn.Count; i++)
            {
                if (worn[i].TryGetComp<T>() is { } comp) into.Add(comp);
            }
        }
        ThingOwner<Thing>? carried = pawn.inventory?.innerContainer;
        if (carried == null) return;
        for (int i = 0; i < carried.Count; i++)
        {
            if (carried[i].TryGetComp<T>() is { } comp) into.Add(comp);
        }
    }
}
