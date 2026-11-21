using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Cab bunks (Framework 0.214.0; docs/development/vehicle-accessories.md, B2): a heavy vehicle's fitted bunk is a caravan bed, so a rider rests
/// in it on the move as in a packed bed. The game gives caravan beds out from the packed beds in the cargo; Vehicle Framework gives them only to
/// the sick while a vehicle caravan moves. So the bunk is added to the caravan's beds, and while the caravan moves it goes to the most tired
/// passenger of its own vehicle. The game's own rest rule then rests whoever lies in it. Nothing here is saved: the bunk's sleeper is chosen
/// again each time the caravan works out its beds.
/// </summary>
public static class Berths
{
    /// <summary>Whether a person rides in one of this vehicle's crew places (driver, gunner) rather than a passenger seat.</summary>
    public static bool IsCrew(VehiclePawn vehicle, Pawn pawn)
    {
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role != null && handler.role.handlingTypes != HandlingType.None && handler.thingOwner.Contains(pawn)) return true;
        }
        return false;
    }

    /// <summary>Whether this vehicle's bunk takes a passenger seat's place: fitted, in a cab of more than two seats.</summary>
    public static bool BunkTakesSeat(VehiclePawn vehicle)
    {
        if (vehicle.GetComp<CompPhobosCamp>() is not { BunkFitted: true } || vehicle.handlers == null) return false;
        int seats = 0;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role != null) seats += handler.role.Slots;
        }
        return NightDriveRule.BunkTakesSeat(seats);
    }
}

/// <summary>Adds each vehicle's bunk to a vehicle caravan's beds, beside the packed beds in its cargo. Only adds; the game's own list is kept.</summary>
[HarmonyPatch(typeof(Caravan_BedsTracker), "GetUsableBeds")]
public static class BerthsUsableBeds
{
    public static void Postfix(Caravan_BedsTracker __instance, List<Building_Bed> outBeds)
    {
        if (__instance.caravan is not VehicleCaravan caravan) return;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosCamp>()?.Bunk is { } bunk && !outBeds.Contains(bunk)) outBeds.Add(bunk);
        }
    }
}

/// <summary>
/// While a vehicle caravan moves, gives each vehicle's free bunk to the most tired passenger of that vehicle. Runs after Vehicle Framework's own
/// bed pass (a prefix that replaces the game's for vehicle caravans and leaves healthy riders without a bed on the move); it only adds a
/// sleeper to a bunk nobody has.
/// </summary>
[HarmonyPatch(typeof(Caravan_BedsTracker), "RecalculateUsedBeds")]
public static class BerthsOnTheMove
{
    public static void Postfix(Caravan_BedsTracker __instance, Dictionary<Pawn, Building_Bed> ___usedBeds)
    {
        if (__instance.caravan is not VehicleCaravan caravan || !caravan.vehiclePather.MovingNow) return;
        float wantsBelow = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().bunkWantsBelowRest;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosCamp>()?.Bunk is not { } bunk || ___usedBeds.ContainsValue(bunk)) continue;
            List<Pawn> aboard = vehicle.AllPawnsAboard;
            var riders = new List<(double?, bool, bool)>(aboard.Count);
            foreach (Pawn pawn in aboard)
            {
                riders.Add((pawn.needs?.rest?.CurLevel, Berths.IsCrew(vehicle, pawn), ___usedBeds.ContainsKey(pawn)));
            }
            int index = NightDriveRule.BunkFor(riders, wantsBelow);
            if (index < 0) continue;
            Pawn sleeper = aboard[index];
            ___usedBeds[sleeper] = bunk;
            if (Seen.Wants("vehicles.bunk-on-the-move", vehicle)) Seen.Note("vehicles.bunk-on-the-move", vehicle, sleeper.LabelShort);
        }
    }
}
