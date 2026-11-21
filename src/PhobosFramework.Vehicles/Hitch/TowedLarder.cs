using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A caravan's people eat from what it tows (Framework 0.219.0; docs/development/vehicle-trailers.md, "Food in tow"). A towed vehicle rides
/// in its hitch and is no member of the caravan, so the game's caravan feeding never saw food in a trailer's cargo, and its people went
/// hungry beside a full one. Only when the caravan's own packs have nothing a member can eat now is the towed cargo searched, by the game's
/// own rules for what a member may eat and how much they like it, so a caravan behaves exactly as before while its packs hold food. The
/// towed vehicle is named as the food's owner, so the game takes an eaten stack out of that trailer's cargo itself.
/// </summary>
[HarmonyPatch(typeof(CaravanInventoryUtility), nameof(CaravanInventoryUtility.TryGetBestFood),
    new[] { typeof(Caravan), typeof(Pawn), typeof(Thing), typeof(Pawn) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out })]
public static class TowedLarderFood
{
    public static void Postfix(Caravan caravan, Pawn forPawn, ref Thing food, ref Pawn owner, ref bool __result)
    {
        if (__result || caravan is not VehicleCaravan || caravan.Faction != Faction.OfPlayer || forPawn == null) return;
        Thing? best = null;
        VehiclePawn? from = null;
        float bestScore = 0f;
        foreach (VehiclePawn towed in Hitches.InTow(caravan))
        {
            if (towed.inventory?.innerContainer is not { } cargo) continue;
            for (int index = 0; index < cargo.Count; index++)
            {
                Thing thing = cargo[index];
                if (!CaravanPawnsNeedsUtility.CanEatForNutritionNow(thing, forPawn)) continue;
                float score = CaravanPawnsNeedsUtility.GetFoodScore(thing, forPawn);
                if (best != null && score <= bestScore) continue;
                best = thing;
                bestScore = score;
                from = towed;
            }
        }
        if (best == null || from == null) return;
        food = best;
        owner = from;
        __result = true;
        if (Seen.Wants("trailers.caravan-ate-from-tow", from)) Seen.Note("trailers.caravan-ate-from-tow", from, forPawn.LabelShort + ": " + best.LabelShort);
    }
}
