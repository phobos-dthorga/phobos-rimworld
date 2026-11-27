using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Framework.Items;

/// <summary>
/// Apparel that stacks (Framework 0.370.0; the owner's report of 28 Nov 2026: hand-held radios one to a stack backed up the factory). The game
/// never splits a stack when someone puts apparel on: <see cref="Pawn_ApparelTracker.Wear"/> takes the whole thing it is given, so a colonist
/// putting on a stack of ten radios would wear all ten as one (read in the game's code, 28 Nov 2026; only the caravan gear tab splits one off
/// first). Any apparel whose Def stacks gets this, ours or another mod's, since the game's own apparel never stacks.
/// <para>The worn one stays the thing the caller gave, because the wear job, the outfit stand and dressing another pawn go on to mark that thing
/// as forced; the rest is split off and put back where the stack was: on the same cell (a shelf, a hopper), in the same container, or, for a
/// stack already taken out of everything, into the wearer's inventory or beside them. Its forbidden mark, quality and hit points go with it
/// (the game's SplitOff and its comps).</para>
/// <para>Cost: one stack-count check each time anyone puts anything on.</para>
/// </summary>
[HarmonyPatch(typeof(Pawn_ApparelTracker), nameof(Pawn_ApparelTracker.Wear))]
public static class StackedApparel
{
    public static void Prefix(Pawn_ApparelTracker __instance, Apparel newApparel)
    {
        if (newApparel == null || newApparel.stackCount <= 1) return;
        LeaveTheRest(__instance.pawn, newApparel);
    }

    /// <summary>Splits all but one off a stack about to be worn, and puts them back where the stack was.</summary>
    public static void LeaveTheRest(Pawn wearer, Apparel worn)
    {
        int restCount = worn.stackCount - 1;
        var holder = worn.holdingOwner;
        bool spawned = worn.Spawned;
        var map = worn.MapHeld;
        var cell = worn.PositionHeld;
        Thing rest = worn.SplitOff(restCount);
        if (spawned)
        {
            // The worn one leaves the cell first, so the rest takes its place there rather than merging back into it. The game's Wear would
            // take it off the map next anyway (DeSpawnOrDeselect), and does nothing more once it is gone.
            worn.DeSpawnOrDeselect();
            if (!GenPlace.TryPlaceThing(rest, cell, map, ThingPlaceMode.Direct) && !rest.Spawned && !rest.Destroyed)
                GenPlace.TryPlaceThing(rest, cell, map, ThingPlaceMode.Near);
        }
        else if (holder != null)
        {
            // Never merged back into the one about to be worn, which the game takes out of the container itself.
            holder.TryAdd(rest, canMergeWithExistingStacks: false);
        }
        else if (wearer.inventory == null || !wearer.inventory.innerContainer.TryAdd(rest))
        {
            if (wearer.MapHeld != null) GenPlace.TryPlaceThing(rest, wearer.PositionHeld, wearer.MapHeld, ThingPlaceMode.Near);
        }
        if (!rest.Spawned && rest.holdingOwner == null && !rest.Destroyed)
            FrameworkMod.Logger.Warn(wearer.LabelShort + " put on one " + worn.def.label + " from a stack of " + (restCount + 1) + ", and the other "
                + restCount + " could not be put anywhere.");
        else if (Seen.Wants("framework.stacked-apparel-worn", worn))
            Seen.Note("framework.stacked-apparel-worn", worn, restCount + " left " + (rest.Spawned ? "on the map" : "in a container"));
    }
}
