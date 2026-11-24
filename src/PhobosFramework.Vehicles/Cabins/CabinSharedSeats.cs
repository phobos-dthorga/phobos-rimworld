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
    /// <summary>The bunk's sleeper is climbing in now, so the bunk's place is theirs to take. Main thread only.</summary>
    static bool boarding;

    /// <summary>Marks the bunk's sleeper as climbing in until the returned scope is disposed.</summary>
    internal static Boarding BunkSleeperBoarding()
    {
        bool before = boarding;
        boarding = true;
        return new Boarding(before);
    }

    internal readonly struct Boarding : System.IDisposable
    {
        readonly bool before;
        public Boarding(bool before) => this.before = before;
        public void Dispose() => boarding = before;
    }

    public static void Postfix(VehicleRoleHandler __instance, ref bool __result)
    {
        if (!__result || __instance.role == null || __instance.role.handlingTypes != HandlingType.None || __instance.vehicle == null) return;
        IReadOnlyList<Pawn> inCabin = VehicleCabins.Occupants(__instance.vehicle);
        // People in a tent on the deck take no seat in the cab (Framework 0.217.0).
        if (inCabin.Count > 0 && VehicleCabins.Of(__instance.vehicle)?.Props.deckTent == true) inCabin = System.Array.Empty<Pawn>();
        // A cab bunk takes a passenger seat's place in a cab of more than two seats (Framework 0.214.0, B2). Its sleeper fills that place:
        // someone in the bunk already sits in the seated count, and the sleeper climbing in may take it (Framework 0.292.0; before, the
        // place was taken twice, and a cab with only it left refused its own sleeper).
        int bunk = Berths.BunkTakesSeat(__instance.vehicle) && !boarding
            && __instance.vehicle.GetComp<CompPhobosCamp>() is not { BunkInUse: true } ? 1 : 0;
        if (inCabin.Count == 0 && bunk == 0) return;
        VehicleCabins.PassengerSeats(__instance.vehicle, out int seats, out int seated);
        if (!CabinRoomRule.SeatFree(seats - bunk, seated, inCabin.Count)) __result = false;
    }
}
