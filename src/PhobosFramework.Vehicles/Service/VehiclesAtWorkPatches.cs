using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Vehicles at Work (shengchipangxieyaotuke.vehiclesatwork) sends colonists' vehicles on trips by giving them a Goto job and starting
/// the path itself, so it never asks the comps' CanMove that our refusals to move answer (only the float menu does). It asks Vehicle
/// Framework's CanMove through its own static helpers instead, so these postfixes add our refusals there: a vehicle with worn brakes,
/// a set-up job holding it or a boat on the bottom is not picked for a trip, and one that stops mid-trip says why. A tie-in by name
/// (no reference to its assembly); each patch stands down when the mod is absent or its helper is renamed. Our fault to fix, not theirs:
/// the refusal is ours (soft-dependency-crosslinks.md, SD1; owner, 13 Nov 2026: "Fix it now"). Postfixes only.
/// </summary>
static class VehiclesAtWork
{
    public const string PackageId = "shengchipangxieyaotuke.vehiclesatwork";

    /// <summary>
    /// Whether its helpers may be looked up. Each patch names its helper in full ("WOR.VF:Healthy"), so scripts/check-targets.py can
    /// check the name and the patch's parameters against the installed mod.
    /// </summary>
    public static bool Active => OptionalMods.IsActive(PackageId);

    /// <summary>Our refusal for a vehicle Vehicles at Work asks about, or null.</summary>
    public static string? Refusal(Pawn pawn) => pawn is VehiclePawn vehicle ? MoveRefusals.For(vehicle) : null;
}

/// <summary>Its "healthy" test (Vehicle Framework's CanMove) decides which vehicles may be picked for a trip: ours say no while refusing.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkHealthyPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.VF:Healthy") : null;

    public static void Postfix(Pawn v, ref bool __result)
    {
        if (__result && VehiclesAtWork.Refusal(v) != null) __result = false;
    }
}

/// <summary>Its "can move" test (Vehicle Framework's CanMoveFinal) is asked before it drives off: ours say no while refusing.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkCanMovePatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.VF:CanMove") : null;

    public static void Postfix(Pawn v, ref bool __result)
    {
        if (__result && VehiclesAtWork.Refusal(v) != null) __result = false;
    }
}

/// <summary>
/// Its message when a trip is called off ("{vehicle} {why}, so {driver} walks") would otherwise blame damage for our refusal. Where the
/// vehicle has fuel and Vehicle Framework itself would let it move, the reason is ours, so ours is given.
/// </summary>
[HarmonyPatch]
public static class VehiclesAtWorkWhyStuckPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.VF:WhyStuck") : null;

    public static void Postfix(Pawn v, ref string __result)
    {
        if (v is not VehiclePawn vehicle || !vehicle.CanMove || vehicle.CompFueledTravel is { EmptyTank: true }) return;
        string? refusal = MoveRefusals.For(vehicle);
        if (refusal == null) return;
        __result = "PhobosVehicleSystems_WorkTripRefused".Translate(refusal.TrimEnd('.').Named("REASON")).Resolve();
    }
}

/// <summary>
/// A depot bay is the parking place of the vehicle assigned to it (Framework 0.176.0; docs/development/vehicles-at-work-tie-ins.md, VW2;
/// owner, 19 Nov 2026: "Yes, next"). Vehicles at Work parks a vehicle "home" in two places: its Drive work returning a vehicle left idle,
/// and a commuter bringing one back (<c>WOR.Lots.Spot</c> with <c>home</c> set, from WorkGiver_ParkVehicle and Commute.BringBack). For a
/// vehicle with a bay of ours on its map, home is the bay's parking cell; it decides only which cell, and its own trip drives there. Its
/// other parking (beside a job, by storage for a haul) is left alone. A vehicle is only ever parked when the player has switched on its
/// trips or hauling there, so this never sends a vehicle the player did not hand over; our own "Go to bay" stays an order (owner, 9 Nov 2026).
/// </summary>
static class VehiclesAtWorkBays
{
    /// <summary>The bay of a vehicle Vehicles at Work asks about, or null: a vehicle of ours with a bay of the player's on its map.</summary>
    public static CompPhobosDepotBay? BayOf(Pawn pawn) => pawn is VehiclePawn vehicle ? DepotBays.Of(vehicle) : null;

    /// <summary>
    /// The bay's parking cell when the vehicle can drive there: no other vehicle stands on the pad, and Vehicle Framework finds a way (as the
    /// bay's own Go to bay asks). Invalid otherwise, and its own lots then decide.
    /// </summary>
    public static IntVec3 ParkingCell(VehiclePawn vehicle, CompPhobosDepotBay bay)
    {
        IntVec3 cell = bay.ParkingCell(vehicle);
        Map map = vehicle.Map;
        if (!cell.InBounds(map)) return IntVec3.Invalid;
        CellRect pad = bay.Pad;
        foreach (Pawn other in map.mapPawns.AllPawnsSpawned)
        {
            if (other != vehicle && other is VehiclePawn && pad.Contains(other.Position)) return IntVec3.Invalid;
        }
        return vehicle.CanReachVehicle(cell, Verse.AI.PathEndMode.OnCell, Danger.Deadly) ? cell : IntVec3.Invalid;
    }

    /// <summary>Whether any vehicle on this map has a bay of the player's, for its Drive work to look for one to bring home.</summary>
    public static bool AnyAssigned(Map map)
    {
        foreach (CompPhobosDepotBay bay in DepotBays.On(map))
        {
            if (bay.Assigned != null) return true;
        }
        return false;
    }
}

/// <summary>Where it parks a vehicle "home": the vehicle's bay, when it has one and can get there.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkHomeSpotPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.Lots:Spot") : null;

    public static void Postfix(Pawn v, bool home, ref IntVec3 __result)
    {
        if (!home || v is not VehiclePawn vehicle || VehiclesAtWorkBays.BayOf(v) is not { } bay) return;
        IntVec3 cell = VehiclesAtWorkBays.ParkingCell(vehicle, bay);
        if (!cell.IsValid) return;
        __result = cell;
        if (Seen.Wants("vehicles.work-park-bay", vehicle)) Seen.Note("vehicles.work-park-bay", vehicle, bay.parent.LabelShort);
    }
}

/// <summary>Whether a vehicle is parked where it belongs: for one with a bay, only on that bay's pad.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkParkedRightPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.Lots:ParkedRight") : null;

    public static void Postfix(Pawn v, ref bool __result)
    {
        if (v is VehiclePawn vehicle && VehiclesAtWorkBays.BayOf(v) is { } bay) __result = bay.Holds(vehicle);
    }
}

/// <summary>
/// Its test of which vehicles are free for a trip (Framework 0.177.0, VW3): one waiting for a driver to run an errand of ours is not. Its
/// commuters would otherwise drive off in a truck a colonist is on the way to take for a delivery.
/// </summary>
[HarmonyPatch]
public static class VehiclesAtWorkReadyPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.Wheels:Ready") : null;

    public static void Postfix(Pawn v, ref bool __result)
    {
        if (__result && v is VehiclePawn vehicle && DriverCalls.Waits(vehicle)) __result = false;
    }
}

/// <summary>Its Drive work looks for vehicles to park only on a map with a parking lot: a bay with a vehicle counts as one.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkAnyLotPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Active ? AccessTools.Method("WOR.Lots:Any") : null;

    public static void Postfix(Map map, ref bool __result)
    {
        if (!__result && map != null && VehiclesAtWorkBays.AnyAssigned(map)) __result = true;
    }
}
