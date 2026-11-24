using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Visitors make room (Framework 0.292.0; docs/development/vehicle-parked-use.md, the seat fix; the owner's pick of 25 Nov 2026). A visitor sits in
/// a free passenger seat, so before this a vehicle full of visitors refused a boarding order, and a caravan being formed counted their seats as
/// taken. Now a visitor's seat counts as free to anyone else boarding, and the visitor climbs out (with a message) as the other is seated.
/// Vehicle Framework has no extension point for who holds a seat, so three small patches: each acts only for the player's vehicle with a
/// visitor aboard, and returns at once otherwise.
/// </summary>
public static class VisitSeats
{
    /// <summary>Our own visit is seating its visitor now: they never make room for themselves.</summary>
    internal static bool seatingVisitor;

    /// <summary>A passenger seat of this vehicle holding a visitor, or null.</summary>
    internal static VehicleRoleHandler? SeatWithVisitor(VehiclePawn vehicle)
    {
        if (vehicle.Faction != Faction.OfPlayer || !VehicleVisits.AnyVisitorIn(vehicle) || vehicle.handlers == null) return null;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role == null || handler.role.HandlingTypes != HandlingType.None || handler.thingOwner == null) continue;
            for (int i = 0; i < handler.thingOwner.Count; i++)
            {
                if (VehicleVisits.VisitOf(handler.thingOwner[i]) is { } visit && visit.vehicle == vehicle) return handler;
            }
        }
        return null;
    }

    /// <summary>The number of visitors sitting in this vehicle's passenger seats.</summary>
    internal static int VisitorsSeated(VehiclePawn vehicle)
    {
        if (vehicle.Faction != Faction.OfPlayer || !VehicleVisits.AnyVisitorIn(vehicle) || vehicle.handlers == null) return 0;
        int count = 0;
        foreach (VehicleRoleHandler handler in vehicle.handlers)
        {
            if (handler.role == null || handler.role.HandlingTypes != HandlingType.None || handler.thingOwner == null) continue;
            for (int i = 0; i < handler.thingOwner.Count; i++)
            {
                if (VehicleVisits.VisitOf(handler.thingOwner[i]) is { } visit && visit.vehicle == vehicle) count++;
            }
        }
        return count;
    }
}

/// <summary>A boarding order finds no seat: a visitor's passenger seat is offered instead (Vehicle Framework's CanBoardVehicle asks this).</summary>
[HarmonyPatch(typeof(RoleHelper), nameof(RoleHelper.GetHandlerFor))]
public static class VisitSeats_HandlerFor
{
    public static void Postfix(VehiclePawn vehicle, Pawn pawn, ref VehicleRoleHandler? __result)
    {
        if (__result != null || VisitSeats.seatingVisitor || VehicleVisits.VisitOf(pawn) != null) return;
        if (VisitSeats.SeatWithVisitor(vehicle) is { } handler && handler.CanOperateRole(pawn)) __result = handler;
    }
}

/// <summary>Someone else is seated where a visitor sits, the seat full: the visitor climbs out first, and the player is told.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.TryAddPawn), typeof(Pawn), typeof(VehicleRoleHandler))]
public static class VisitSeats_MakeRoom
{
    public static void Prefix(VehiclePawn __instance, Pawn pawn, VehicleRoleHandler handler)
    {
        if (VisitSeats.seatingVisitor || handler?.role == null || handler.role.HandlingTypes != HandlingType.None || handler.AreSlotsAvailable) return;
        if (__instance.Faction != Faction.OfPlayer || !VehicleVisits.AnyVisitorIn(__instance) || VehicleVisits.VisitOf(pawn) != null) return;
        VehicleVisits.MakeRoom(__instance, handler);
    }
}

/// <summary>A caravan being formed counts a visitor's seat as free: they climb out as it sets off anyway.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.SeatsAvailable), MethodType.Getter)]
public static class VisitSeats_Count
{
    public static void Postfix(VehiclePawn __instance, ref int __result)
    {
        int visitors = VisitSeats.VisitorsSeated(__instance);
        if (visitors > 0) __result += visitors;
    }
}
