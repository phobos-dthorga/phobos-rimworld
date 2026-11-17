using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Puts what a vehicle's work makes into storage beside it (Framework 0.126.0; docs/development/universal-hoppers.md, UH3 and UH5): the
/// first cell, in the order given, of a storage building of the vehicle's own side that would take the thing and has room by the game's own
/// storage rule. What no storage takes, or what does not fit, is set down nearby as before. Other sides' storage is never filled.
/// </summary>
public static class StorageNearby
{
    /// <summary>The storage building and cell that would take this thing now, or null.</summary>
    public static Building_Storage? CellFor(Thing thing, Map map, IEnumerable<IntVec3> cells, Faction? side, out IntVec3 cell)
    {
        cell = IntVec3.Invalid;
        if (side == null) return null;
        foreach (IntVec3 c in cells)
        {
            if (!c.InBounds(map)) continue;
            if (c.GetEdifice(map) is not Building_Storage store || store.Faction != side || !store.Accepts(thing)) continue;
            if (!c.IsValidStorageFor(map, thing)) continue;
            cell = c;
            return store;
        }
        return null;
    }

    /// <summary>
    /// Places a thing into storage among the cells if one takes it, and whatever is left near the fallback cell, as the work did before.
    /// True when all of it is placed; <paramref name="into"/> is the storage that took some, for the caller's own sighting note.
    /// </summary>
    public static bool Place(Thing thing, Map map, IEnumerable<IntVec3> cells, Faction? side, IntVec3 fallback, Predicate<IntVec3>? fallbackValidator, out Building_Storage? into)
    {
        into = CellFor(thing, map, cells, side, out IntVec3 cell);
        if (into != null)
        {
            bool all = GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Direct);
            if (all || thing.Destroyed || thing.stackCount <= 0) return true;
        }
        return GenPlace.TryPlaceThing(thing, fallback, map, ThingPlaceMode.Near, null, fallbackValidator);
    }
}
