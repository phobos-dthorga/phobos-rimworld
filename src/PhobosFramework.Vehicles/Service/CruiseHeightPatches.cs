using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Targeting;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The leg's speed (Framework 0.141.0, the cruising height between tiles): Vehicle Framework reads an aircraft's flight speed once as each leg
/// begins (AerialVehicleInFlight.SetSpeed), so the leg's factor sets the whole leg. Postfix; 1 on the ground, so the range Vehicle Framework
/// draws and prices from the same figure is left to the destination picker's own pricing below.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.FlightSpeed), MethodType.Getter)]
public static class Patch_FlightSpeed_Cruise
{
    public static void Postfix(CompVehicleLauncher __instance, ref float __result)
    {
        try
        {
            __result *= CruiseHeight.SpeedFactor(__instance.Vehicle);
        }
        catch (Exception e)
        {
            VehicleSystemsMod.Logger.ErrorOnce("Cruising height speed failed; the aircraft flies at its author's speed: " + e);
        }
    }
}

/// <summary>The leg's fuel (Framework 0.141.0): Vehicle Framework spends the world-flight multiplier every tick in flight. Postfix.</summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.FuelConsumptionWorldMultiplier), MethodType.Getter)]
public static class Patch_FlightFuel_Cruise
{
    public static void Postfix(CompVehicleLauncher __instance, ref float __result)
    {
        try
        {
            __result *= CruiseHeight.FuelFactor(__instance.Vehicle);
        }
        catch (Exception e)
        {
            VehicleSystemsMod.Logger.ErrorOnce("Cruising height fuel failed; the aircraft burns its author's rate: " + e);
        }
    }
}

/// <summary>
/// The destination picker's journey, priced leg by leg (Framework 0.141.0). Vehicle Framework prices a whole journey at one rate
/// (FuelTargetUpdater.TargeterUpdate sets its private TotalFuelCost from the total distance); each leg here costs what its own height asks,
/// so the fuel shown, the "not enough fuel" refusal and the line's colour all follow the legs. The legs are kept for the target check.
/// </summary>
public static class CruisePicker
{
    static readonly Action<FuelTargetUpdater, float>? SetTotalFuelCost =
        AccessTools.PropertySetter("Vehicles.World.FuelTargetUpdater:TotalFuelCost") is { } setter
            ? AccessTools.MethodDelegate<Action<FuelTargetUpdater, float>>(setter) : null;

    /// <summary>The legs of the journey being picked, from the picker's last update; the last is the one to the mouse.</summary>
    public static readonly List<CruiseLeg> Legs = new();
    static FuelTargetUpdater? pricedBy;

    /// <summary>The tile a journey starts from: the map's, the flight's or the caravan's.</summary>
    static PlanetTile StartTile(VehiclePawn vehicle)
    {
        if (vehicle.Spawned) return vehicle.Map.Tile;
        if (vehicle.ParentHolder is AerialVehicleInFlight aerial) return aerial.Tile;
        return vehicle.GetCaravan()?.Tile ?? PlanetTile.Invalid;
    }

    public static void Price(FuelTargetUpdater updater, VehiclePawn vehicle, List<GlobalTargetInfo> targets)
    {
        Legs.Clear();
        pricedBy = updater;
        if (!CruiseHeight.Governs(vehicle) || vehicle.CompVehicleLauncher is not { } launcher) return;
        PlanetTile from = StartTile(vehicle);
        if (!from.Valid) return;
        var stops = new List<PlanetTile>(targets.Count + 1);
        foreach (GlobalTargetInfo target in targets) stops.Add(target.Tile);
        if (FlightCargoReach.MouseTile is { Valid: true } mouse && targets.Count < launcher.launchProtocol.MaxFlightNodes) stops.Add(mouse);
        if (stops.Count == 0) return;
        float total = 0f;
        float burned = 0f;
        bool priced = true;
        CruiseHeight.Suspended = true;
        try
        {
            foreach (PlanetTile to in stops)
            {
                if (to == from) continue;
                // The leg's weight is the aircraft's less the fuel the legs before it burn.
                CruiseLeg? leg = CruiseHeight.Leg(vehicle, from, to, burned);
                float distance = Ext_Math.SphericalDistance(WorldHelper.GetTilePos(from), WorldHelper.GetTilePos(to));
                float cost = launcher.FuelNeededToLaunchAtDist(distance);
                if (leg is { } known)
                {
                    Legs.Add(known);
                    cost *= (float)known.FuelPerDistance;
                }
                else
                {
                    priced = false;
                }
                total += cost;
                burned += cost;
                from = to;
            }
        }
        finally
        {
            CruiseHeight.Suspended = false;
        }
        // A leg that could not be walked (a tile in orbit) leaves Vehicle Framework's own price standing.
        if (priced && SetTotalFuelCost != null && Legs.Count > 0) SetTotalFuelCost(updater, total);
    }

    /// <summary>The legs priced by this picker, or none.</summary>
    public static IReadOnlyList<CruiseLeg> For(FuelTargetUpdater updater) => pricedBy == updater ? Legs : Array.Empty<CruiseLeg>();
}

/// <summary>Prices the journey leg by leg after Vehicle Framework's own update (Framework 0.141.0). Postfix; it never skips the original.</summary>
[HarmonyPatch(typeof(FuelTargetUpdater), nameof(FuelTargetUpdater.TargeterUpdate))]
public static class Patch_FuelTargetUpdater_TargeterUpdate_Cruise
{
    public static void Postfix(FuelTargetUpdater __instance, VehiclePawn ___vehicle, ref TargetData<GlobalTargetInfo> targetData)
    {
        try
        {
            if (___vehicle == null) return;
            CruisePicker.Price(__instance, ___vehicle, targetData.targets);
        }
        catch (Exception e)
        {
            CruiseHeight.Suspended = false;
            VehicleSystemsMod.Logger.ErrorOnce("Cruising height pricing failed; the picker shows Vehicle Framework's own price: " + e);
        }
    }
}

/// <summary>
/// Refuses a journey with a leg over ground higher than the engines can climb, saying how much lighter it must be or that it must route round;
/// otherwise says, for the leg to the mouse, what height it is flown at and why, where that is not the height it was built for
/// (Framework 0.141.0). Runs after the cargo-reach and landing postfixes, adding to what they left.
/// </summary>
[HarmonyPatch(typeof(FuelTargetUpdater), nameof(FuelTargetUpdater.ValidateTargets))]
[HarmonyPriority(Priority.VeryLow)]
public static class Patch_FuelTargetUpdater_ValidateTargets_Cruise
{
    public static void Postfix(FuelTargetUpdater __instance, VehiclePawn ___vehicle, ref TargetValidation __result)
    {
        if (!__result.isValid || ___vehicle == null) return;
        try
        {
            IReadOnlyList<CruiseLeg> legs = CruisePicker.For(__instance);
            if (legs.Count == 0) return;
            foreach (CruiseLeg leg in legs)
            {
                if (leg.Flyable) continue;
                string? refusal = CruiseHeight.Describe(___vehicle, leg);
                if (VehicleSystemsMod.Settings.showOnly)
                {
                    Add(ref __result, refusal);
                    return;
                }
                __result = TargetValidation.Failed with { Tooltip = refusal };
                return;
            }
            Add(ref __result, CruiseHeight.Describe(___vehicle, legs[legs.Count - 1]));
        }
        catch (Exception e)
        {
            VehicleSystemsMod.Logger.ErrorOnce("Cruising height check failed; the journey is judged as Vehicle Framework judges it: " + e);
        }
    }

    static void Add(ref TargetValidation result, string? line)
    {
        if (line.NullOrEmpty()) return;
        string? before = result.Tooltip;
        result = result with { Tooltip = before.NullOrEmpty() ? line : before + "\n" + line };
    }
}
