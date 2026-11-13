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
    const string Helpers = "WOR.VF";

    public static MethodBase? Helper(string name) => OptionalMods.IsActive(PackageId) ? AccessTools.Method(Helpers + ":" + name) : null;

    /// <summary>Our refusal for a vehicle Vehicles at Work asks about, or null.</summary>
    public static string? Refusal(Pawn pawn) => pawn is VehiclePawn vehicle ? MoveRefusals.For(vehicle) : null;
}

/// <summary>Its "healthy" test (Vehicle Framework's CanMove) decides which vehicles may be picked for a trip: ours say no while refusing.</summary>
[HarmonyPatch]
public static class VehiclesAtWorkHealthyPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => VehiclesAtWork.Helper("Healthy");

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

    static MethodBase? TargetMethod() => VehiclesAtWork.Helper("CanMove");

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

    static MethodBase? TargetMethod() => VehiclesAtWork.Helper("WhyStuck");

    public static void Postfix(Pawn v, ref string __result)
    {
        if (v is not VehiclePawn vehicle || !vehicle.CanMove || vehicle.CompFueledTravel is { EmptyTank: true }) return;
        string? refusal = MoveRefusals.For(vehicle);
        if (refusal == null) return;
        __result = "PhobosVehicleSystems_WorkTripRefused".Translate(refusal.TrimEnd('.').Named("REASON")).Resolve();
    }
}
