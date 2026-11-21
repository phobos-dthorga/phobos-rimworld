using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle's passenger seats and its walk-in cabin hold the same people (Framework 0.211.1; owner, 22 Nov 2026: one shared limit, 24 on
/// the Osprey). Vehicle Framework asks this getter before it seats anyone, and it has no extension point, so a postfix answers "full" for a
/// passenger seat once those seated and those in the cabin fill the seats between them, and, since Framework 0.214.0, one seat fewer where a cab
/// bunk takes its place. It only ever turns free into full, never skips
/// Vehicle Framework's own count, and leaves the driving and weapon seats alone. With nobody in a cabin it does nothing.
/// </summary>
[HarmonyPatch(typeof(VehicleRoleHandler), nameof(VehicleRoleHandler.AreSlotsAvailable), MethodType.Getter)]
public static class CabinSharedSeats
{
    public static void Postfix(VehicleRoleHandler __instance, ref bool __result)
    {
        if (!__result || __instance.role == null || __instance.role.handlingTypes != HandlingType.None || __instance.vehicle == null) return;
        IReadOnlyList<Pawn> inCabin = VehicleCabins.Occupants(__instance.vehicle);
        // A cab bunk takes a passenger seat's place in a cab of more than two seats (Framework 0.214.0, B2).
        int bunk = Berths.BunkTakesSeat(__instance.vehicle) ? 1 : 0;
        if (inCabin.Count == 0 && bunk == 0) return;
        VehicleCabins.PassengerSeats(__instance.vehicle, out int seats, out int seated);
        if (!CabinRoomRule.SeatFree(seats - bunk, seated, inCabin.Count)) __result = false;
    }
}
