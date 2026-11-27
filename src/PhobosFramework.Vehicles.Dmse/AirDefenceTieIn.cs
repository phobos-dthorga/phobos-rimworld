using System;
using System.Linq;
using System.Reflection;
using Fortified;
using HarmonyLib;
using Phobos.Shared;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.AirDefence;

/// <summary>
/// Starts the air-defence tie-in (Framework 0.119.0): the launch check for a closed sky (AD7) and the launch warning prompt, both through
/// Harmony only where Vehicle Framework has no extension point. The tracks and the warning receiver come in through DMS Expedition's own
/// AirDefenseExtensionDef, in XML, and need no code here.
/// </summary>
[StaticConstructorOnStartup]
public static class AirDefenceTieIn
{
    const string LogPrefix = "[D'thorga Framework]";

    static AirDefenceTieIn()
    {
        var harmony = new Harmony(VehicleSystemsMod.HarmonyId + ".Dmse");
        HarmonyPatchApplier.ApplyEach(harmony, typeof(AirDefenceTieIn).Assembly, LogPrefix);
        ClosedSky.Find();
        LaunchChecks.Register(ClosedSky.Refusal);
        VehicleSystemsMod.Logger.Info("air defence tie-in with The Dead Man's Switch - Expedition: aircraft are tracks, warning receivers, "
            + "countermeasures, launch warnings" + (ClosedSky.Readable ? ", closed skies" : " (closed skies not readable: the alert counter was not found)"));
    }
}

/// <summary>
/// AD7 (Framework 0.119.0; owner, 17 Nov 2026): a base whose alarm has gone off closes the airspace over its map for a time, as DMS's own
/// shuttles are grounded there (Fortified Features Framework's alert counter, MapComponent_AlertCounter.IsAirspaceBlocked, read by name: it is
/// in another assembly, which The Dead Man's Switch requires). Our aircraft on that map are refused take-off, with the reason.
/// </summary>
public static class ClosedSky
{
    static Type? counter;
    static PropertyInfo? blocked;

    public static bool Readable => counter != null && blocked != null;

    public static void Find()
    {
        counter = AccessTools.TypeByName("Fortified.MapComponent_AlertCounter");
        blocked = counter == null ? null : AccessTools.Property(counter, "IsAirspaceBlocked");
    }

    public static string? Refusal(VehiclePawn vehicle)
    {
        if (!Readable || !vehicle.Spawned || vehicle.Map is not { } map) return null;
        MapComponent? component = map.components.FirstOrDefault(c => c.GetType() == counter);
        if (component == null || blocked!.GetValue(component) is not true) return null;
        Seen.Note("aircraft.closed-sky", vehicle);
        return "PhobosAirCombat_SkyClosed".Translate(vehicle.LabelShort.Named("VEHICLE"));
    }
}

/// <summary>
/// The launch warning (Framework 0.119.0; owner, 17 Nov 2026): before the player's aircraft takes off towards or across an air defence that would
/// engage it, DMS Expedition's own prompt names the danger and asks to confirm, as it does for shuttles (InterceptRiskUtility.ConfirmOrDefer, its
/// public method). The prefix skips the launch while the prompt is open (recorded reason: the launch must wait for the player's answer); a yes
/// launches again through the same method, which DMS then lets through.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.Launch))]
public static class LaunchPromptFromMap
{
    public static bool Prefix(CompVehicleLauncher __instance, TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
    {
        VehiclePawn vehicle = __instance.Vehicle;
        if (vehicle?.Faction != Faction.OfPlayer || !vehicle.Spawned || !VehicleSystemsMod.Settings.airDefenceSeesAircraft) return true;
        PlanetTile to = LaunchPrompt.Last(targetData);
        if (!to.Valid) return true;
        return LaunchPrompt.Confirm(vehicle.Map.Tile, to, () => __instance.Launch(targetData, arrivalAction));
    }
}

/// <summary>The same warning when the player sends an aircraft already in flight somewhere new.</summary>
[HarmonyPatch(typeof(AerialVehicleInFlight), nameof(AerialVehicleInFlight.Launch))]
public static class LaunchPromptInFlight
{
    public static bool Prefix(AerialVehicleInFlight __instance, TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
    {
        if (__instance.Faction != Faction.OfPlayer || !VehicleSystemsMod.Settings.airDefenceSeesAircraft) return true;
        PlanetTile to = LaunchPrompt.Last(targetData);
        if (!to.Valid) return true;
        // From where it is now (Framework 0.120.5, fault-read A11): the flight's Tile is the last waypoint it reached. Read only on the order.
        int here = WorldHelper.GetNearestTile(__instance.DrawPos);
        PlanetTile from = here >= 0 ? (PlanetTile)here : __instance.Tile;
        return LaunchPrompt.Confirm(from, to, () => __instance.Launch(targetData, arrivalAction));
    }
}

/// <summary>The same warning when an aircraft takes off from a caravan on the world map (fault-read A11): Vehicle Framework's caravan launch
/// goes straight to the flight's order, past both launches above.</summary>
[HarmonyPatch(typeof(VehicleCaravan), nameof(VehicleCaravan.Launch))]
public static class LaunchPromptFromCaravan
{
    public static bool Prefix(VehicleCaravan __instance, TargetData<GlobalTargetInfo> targetData, IArrivalAction arrivalAction)
    {
        if (__instance.Faction != Faction.OfPlayer || !VehicleSystemsMod.Settings.airDefenceSeesAircraft) return true;
        PlanetTile to = LaunchPrompt.Last(targetData);
        if (!to.Valid) return true;
        return LaunchPrompt.Confirm(__instance.Tile, to, () => __instance.Launch(targetData, arrivalAction));
    }
}

static class LaunchPrompt
{
    public static PlanetTile Last(TargetData<GlobalTargetInfo> targetData) =>
        targetData.targets is { Count: > 0 } targets ? targets[targets.Count - 1].Tile : PlanetTile.Invalid;

    public static bool Confirm(PlanetTile from, PlanetTile to, Action relaunch)
    {
        bool go = InterceptRiskUtility.ConfirmOrDefer(from, to, Faction.OfPlayer, checkDestination: true, relaunch);
        if (!go && Seen.Wants("aircraft.launch-warning")) Seen.Note("aircraft.launch-warning", null, from + " to " + to);
        return go;
    }
}
