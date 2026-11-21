using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using UnityEngine;
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

/// <summary>What a player caravan's towed vehicles carry, for the game's food figures (Framework 0.228.0).</summary>
public static class TowedLarder
{
    /// <summary>The food in the cargo of everything this caravan tows; false when it tows none.</summary>
    public static bool Food(Caravan caravan, List<Thing> into)
    {
        into.Clear();
        if (caravan is not VehicleCaravan || caravan.Faction != Faction.OfPlayer) return false;
        foreach (VehiclePawn towed in Hitches.InTow(caravan))
        {
            if (towed.inventory?.innerContainer is not { } cargo) continue;
            for (int index = 0; index < cargo.Count; index++)
            {
                if (cargo[index].def.IsNutritionGivingIngestible) into.Add(cargo[index]);
            }
        }
        return into.Count > 0;
    }
}

/// <summary>
/// A caravan's "days of food" counts the food it tows (Framework 0.228.0; docs/development/vehicle-trailers.md, "Food in tow"). Its people
/// already eat from a towed trailer's cargo, but the figure on the caravan's card and its low-food warning still read only the caravan's own
/// packs, so a caravan with a full trailer was told it was starving. The game's own sum is run again with the towed food added, by the same
/// rules for who eats what; a caravan that tows no food keeps the game's figure untouched.
/// </summary>
[HarmonyPatch(typeof(DaysWorthOfFoodCalculator), nameof(DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood), new[] { typeof(Caravan) })]
public static class TowedLarderDays
{
    internal static readonly MethodInfo? Sum = AccessTools.Method(typeof(DaysWorthOfFoodCalculator), "ApproxDaysWorthOfFood", new[]
    {
        typeof(List<Pawn>), typeof(List<ThingDefCount>), typeof(PlanetTile), typeof(IgnorePawnsInventoryMode), typeof(Faction),
        typeof(WorldPath), typeof(float), typeof(int), typeof(bool),
    });

    static readonly List<Thing> Towed = new();
    static readonly List<ThingDefCount> Extra = new();

    public static void Postfix(Caravan caravan, ref float __result)
    {
        if (Sum == null || caravan == null || !TowedLarder.Food(caravan, Towed)) return;
        Extra.Clear();
        for (int index = 0; index < Towed.Count; index++) Extra.Add(new ThingDefCount(Towed[index].def, Towed[index].stackCount));
        Towed.Clear();
        // The game's own call for a caravan, with the towed food as its "extra food".
        object? days = Sum.Invoke(null, new object?[]
        {
            caravan.PawnsListForReading, Extra, caravan.Tile, IgnorePawnsInventoryMode.DontIgnore, caravan.Faction, caravan.pather.curPath,
            caravan.pather.nextTileCostLeft, caravan.TicksPerMove, caravan.pather.Moving && !caravan.pather.Paused,
        });
        Extra.Clear();
        if (days is not float counted) return;
        VehiclePawn? lead = ((VehicleCaravan)caravan).VehiclesListForReading.FirstOrDefault();
        if (Seen.Wants("trailers.days-of-food-in-tow", lead)) Seen.Note("trailers.days-of-food-in-tow", lead, __result.ToString("0.0") + " to " + counted.ToString("0.0") + " days");
        __result = counted;
    }
}

/// <summary>
/// And its "days until rot" (Framework 0.228.0): the figure beside the days of food looks at the towed food too, at the same outdoor
/// temperatures along the way as the caravan's own. Food in a towed trailer's cargo is kept at no other temperature, so that is fair.
/// </summary>
[HarmonyPatch(typeof(DaysUntilRotCalculator), nameof(DaysUntilRotCalculator.ApproxDaysUntilRot), new[] { typeof(Caravan) })]
public static class TowedLarderRot
{
    static readonly List<Thing> Towed = new();
    static readonly List<Thing> All = new();

    public static void Postfix(Caravan caravan, ref float __result)
    {
        if (caravan == null || !TowedLarder.Food(caravan, Towed)) return;
        All.Clear();
        All.AddRange(CaravanInventoryUtility.AllInventoryItems(caravan));
        All.AddRange(Towed);
        Towed.Clear();
        __result = DaysUntilRotCalculator.ApproxDaysUntilRot(All, caravan.Tile, caravan.pather.curPath, caravan.pather.nextTileCostLeft, caravan.TicksPerMove);
        All.Clear();
    }
}

/// <summary>
/// The window for forming a caravan counts the food in the trailers it takes (Framework 0.236.0; a limit the owner asked to have followed up,
/// 22 Nov 2026). Vehicle Framework works the window's days of food out from what is chosen to go; a trailer hitched behind a chosen vehicle
/// goes too, but its cargo was never in that sum. When the figure is worked out again, the game's own sum is run with and without the trailers'
/// food, and the difference is added to Vehicle Framework's figure, so its own route and speed stay in it. A trailer chosen to go by itself is
/// counted already and is left alone. Days until rot change only where there was no food before.
/// </summary>
[HarmonyPatch(typeof(Dialog_FormCaravan), "DaysWorthOfFood", MethodType.Getter)]
public static class TowedLarderForming
{
    static readonly List<Pawn> Pawns = new();
    static readonly List<ThingDefCount> Food = new();
    static readonly List<ThingDefCount> Towed = new();
    static readonly List<ThingCount> TowedThings = new();

    [HarmonyPriority(Priority.First)]
    public static void Prefix(bool ___daysWorthOfFoodDirty, out bool __state) => __state = ___daysWorthOfFoodDirty;

    public static void Postfix(Dialog_FormCaravan __instance, bool __state, List<TransferableOneWay> ___transferables, ref (float days, float tillRot) __result,
        ref (float days, float tillRot) ___cachedDaysWorthOfFood)
    {
        if (!__state || TowedLarderDays.Sum == null || ___transferables == null) return;
        Pawns.Clear();
        Food.Clear();
        Towed.Clear();
        TowedThings.Clear();
        var chosen = new HashSet<Thing>();
        foreach (TransferableOneWay row in ___transferables)
        {
            if (!row.HasAnyThing || row.CountToTransfer <= 0) continue;
            for (int i = 0; i < row.CountToTransfer && i < row.things.Count; i++) chosen.Add(row.things[i]);
            if (row.AnyThing is Pawn)
            {
                for (int i = 0; i < row.CountToTransfer && i < row.things.Count; i++)
                {
                    if (row.things[i] is Pawn { needs.food: not null } pawn && pawn.RaceProps.EatsFood) Pawns.Add(pawn);
                }
            }
            else if (row.AnyThing is not Corpse corpse || corpse.GetRotStage() == RotStage.Fresh)
            {
                Food.Add(new ThingDefCount(row.ThingDef, row.CountToTransfer));
            }
        }
        foreach (Thing thing in chosen)
        {
            if (thing is not VehiclePawn vehicle) continue;
            foreach (VehiclePawn trailer in CompPhobosHitch.Behind(vehicle))
            {
                if (chosen.Contains(trailer) || trailer.inventory?.innerContainer is not { } cargo) continue;
                foreach (Thing held in cargo)
                {
                    if (!held.def.IsNutritionGivingIngestible) continue;
                    Towed.Add(new ThingDefCount(held.def, held.stackCount));
                    TowedThings.Add(new ThingCount(held, held.stackCount));
                }
            }
        }
        if (Towed.Count == 0 || Pawns.Count == 0) return;
        Map? map = Traverse.Create(__instance).Field("map").GetValue<Map>();
        if (map == null) return;
        var mode = Traverse.Create(__instance).Property("IgnoreInventoryMode").GetValue<IgnorePawnsInventoryMode>();
        float without = Days(Food, map.Tile, mode);
        Food.AddRange(Towed);
        float with = Days(Food, map.Tile, mode);
        float added = Mathf.Max(0f, with - without);
        bool hadNone = __result.days <= 0.01f;
        float days = Mathf.Min(DaysWorthOfFoodCalculator.InfiniteDaysWorthOfFood, __result.days + added);
        float tillRot = hadNone ? DaysUntilRotCalculator.ApproxDaysUntilRot(TowedThings, map.Tile) : __result.tillRot;
        if (Seen.Wants("trailers.forming-days-of-food")) Seen.Note("trailers.forming-days-of-food", null, __result.days.ToString("0.0") + " to " + days.ToString("0.0") + " days");
        __result = (days, tillRot);
        ___cachedDaysWorthOfFood = __result;
        Pawns.Clear();
        Food.Clear();
        Towed.Clear();
        TowedThings.Clear();
    }

    static float Days(List<ThingDefCount> food, PlanetTile tile, IgnorePawnsInventoryMode mode) =>
        TowedLarderDays.Sum!.Invoke(null, new object?[] { Pawns, food, tile, mode, Faction.OfPlayer, null, 0f, 3300, false }) is float days ? days : 0f;
}
