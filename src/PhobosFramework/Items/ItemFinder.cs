using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Items;

/// <summary>
/// Finding a tool a job needs: the nearest one a pawn can reach and reserve on its map, or one aboard a caravan (in a pawn's
/// inventory or a vehicle's cargo). For the kits Phobos mods ask for (a jump-start kit, a medical kit) when the player's setting
/// says they must be crafted rather than assumed.
/// </summary>
public static class ItemFinder
{
    /// <summary>The nearest unforbidden, reservable thing of the Def the pawn can reach (and the validator accepts), or null.</summary>
    public static Thing? FindNearest(Pawn pawn, ThingDef def, float maxDistance = 9999f, Predicate<Thing>? validator = null)
    {
        if (pawn?.Map == null || def == null) return null;
        return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch,
            TraverseParms.For(pawn), maxDistance, thing => !thing.IsForbidden(pawn) && pawn.CanReserve(thing) && (validator == null || validator(thing)));
    }

    /// <summary>A thing of the Def aboard the caravan: a pawn's inventory or, with Vehicle Framework, a vehicle's cargo (both are
    /// inventories of the caravan's pawns, which that framework lists through the caravan). Null when none.</summary>
    public static Thing? FindInCaravan(Caravan caravan, ThingDef def, Predicate<Thing>? validator = null)
    {
        if (caravan == null || def == null) return null;
        bool Wanted(Thing thing) => thing.def == def && (validator == null || validator(thing));
        return CaravanInventoryUtility.AllInventoryItems(caravan).FirstOrDefault(Wanted)
            ?? caravan.PawnsListForReading.SelectMany(p => p.inventory?.innerContainer ?? Enumerable.Empty<Thing>()).FirstOrDefault(Wanted);
    }

    /// <summary>Every thing of the Def aboard the caravan.</summary>
    public static IEnumerable<Thing> AllInCaravan(Caravan caravan, ThingDef def) =>
        caravan == null || def == null ? Enumerable.Empty<Thing>() : CaravanInventoryUtility.AllInventoryItems(caravan).Where(t => t.def == def);
}
