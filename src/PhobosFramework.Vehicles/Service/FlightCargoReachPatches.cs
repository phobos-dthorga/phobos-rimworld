using System;
using HarmonyLib;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Lets the player pick a destination that the aircraft's tank alone cannot reach, when its refuel-from-cargo switch is on and the fuel
/// aboard covers the rest (Framework 0.139.0; docs/development/vehicle-cargo-refuel.md). Vehicle Framework's destination picker
/// (FuelTargetUpdater, used for a launch from a map, from a caravan and in flight) refuses a journey that costs more than the tank holds,
/// and draws its line red. These two postfixes turn that refusal into an allowed journey only where CargoRefuel says the tank and the
/// fuel aboard get there, setting down on the way; every other refusal (too far for the aircraft, no target) stands. Postfixes only.
/// </summary>
public static class FlightCargoReach
{
    // Vehicle Framework keeps these private or protected; read by name, and the feature stands down if one has gone.
    static readonly Func<FuelTargetUpdater, float>? TotalFuelCost =
        AccessTools.PropertyGetter("Vehicles.World.FuelTargetUpdater:TotalFuelCost") is { } cost
            ? AccessTools.MethodDelegate<Func<FuelTargetUpdater, float>>(cost) : null;
    static readonly Func<FlightPathTargetUpdater, float>? TotalDistance =
        AccessTools.PropertyGetter("Vehicles.World.FlightPathTargetUpdater:TotalDistance") is { } distance
            ? AccessTools.MethodDelegate<Func<FlightPathTargetUpdater, float>>(distance) : null;
    static readonly Func<GlobalTargetInfo>? TargetUnderMouse =
        AccessTools.Method("Vehicles.World.FlightPathTargetUpdater:CurrentTargetUnderMouse") is { } mouse
            ? AccessTools.MethodDelegate<Func<GlobalTargetInfo>>(mouse) : null;

    /// <summary>How many times the aircraft sets down to reach the journey being picked, where the tank alone falls short and the fuel
    /// aboard makes it up; null where Vehicle Framework's own refusal should stand.</summary>
    public static int? Stops(FuelTargetUpdater updater, VehiclePawn? vehicle, out float cost)
    {
        cost = 0f;
        if (vehicle?.CompFueledTravel is not { } fuel || vehicle.CompVehicleLauncher is not { } launcher) return null;
        if (TotalFuelCost == null || TotalDistance == null) return null;
        cost = TotalFuelCost(updater);
        // The tank covers it (any refusal is not about fuel), or it is beyond the aircraft's own limit.
        if (cost <= fuel.Fuel || TotalDistance(updater) > launcher.MaxLaunchDistance) return null;
        return CargoRefuel.StopsForJourney(vehicle, cost);
    }

    /// <summary>Whether something is under the mouse to fly to (Vehicle Framework's own test).</summary>
    public static bool TargetValid => TargetUnderMouse != null && TargetUnderMouse().IsValid;

    /// <summary>The tile under the mouse in the destination picker, or null (Framework 0.140.0, the landing check's warning).</summary>
    public static PlanetTile? MouseTile => TargetUnderMouse != null && TargetUnderMouse() is { IsValid: true } target ? target.Tile : null;

    /// <summary>The fuel the journey being picked burns, or 0 where it cannot be read.</summary>
    public static float FuelCost(FuelTargetUpdater updater) => TotalFuelCost == null ? 0f : TotalFuelCost(updater);
}

/// <summary>Allows a journey the fuel aboard covers, saying how many stops it makes on the way (Framework 0.139.0).</summary>
[HarmonyPatch(typeof(FuelTargetUpdater), nameof(FuelTargetUpdater.ValidateTargets))]
public static class Patch_FuelTargetUpdater_ValidateTargets_CargoReach
{
    public static void Postfix(FuelTargetUpdater __instance, VehiclePawn ___vehicle, ref TargetValidation __result)
    {
        if (__result.isValid) return;
        if (FlightCargoReach.Stops(__instance, ___vehicle, out _) is not { } stops) return;
        __result = TargetValidation.Success with
        {
            Tooltip = "PhobosVehicleSystems_FlightRefuelStops".Translate(___vehicle.LabelShort.Named("VEHICLE"), stops.Named("STOPS")).Resolve()
        };
    }
}

/// <summary>Draws the line of such a journey white, or yellow where the fuel aboard would not bring it back (Framework 0.139.0), as Vehicle
/// Framework does for a journey the tank covers.</summary>
[HarmonyPatch(typeof(FuelTargetUpdater), "LaunchStatus")]
public static class Patch_FuelTargetUpdater_LaunchStatus_CargoReach
{
    public static void Postfix(FuelTargetUpdater __instance, VehiclePawn ___vehicle, ref ShuttleLaunchStatus __result)
    {
        if (__result != ShuttleLaunchStatus.Invalid || !FlightCargoReach.TargetValid) return;
        if (FlightCargoReach.Stops(__instance, ___vehicle, out float cost) == null) return;
        double supply = ___vehicle.CompFueledTravel.Fuel + CargoRefuel.CarriedAboard(___vehicle);
        __result = cost > supply - cost ? ShuttleLaunchStatus.NoReturnTrip : ShuttleLaunchStatus.Valid;
    }
}
