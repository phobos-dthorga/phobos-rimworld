using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A comp that can move its vehicle on a battery with the engine off (Framework 0.104.0, the generator set's hybrid; owner, 12 Nov 2026;
/// docs/development/vehicle-generator-set.md, "The hybrid"). While <see cref="OnBattery"/> holds, the vehicle's systems count the engine as
/// off (no engine heat, sound or load), Vehicle Framework burns no fuel, and the drive keeps <see cref="SpeedFactor"/> of its speed. With an
/// empty tank it may still draft and move while <see cref="CanCreep"/> holds.
/// </summary>
public interface IVehicleBatteryDrive
{
    /// <summary>The vehicle is running on its battery now.</summary>
    bool OnBattery { get; }

    /// <summary>The battery holds enough to move at all.</summary>
    bool CanCreep { get; }

    /// <summary>The share of its speed the vehicle keeps on the battery.</summary>
    float SpeedFactor { get; }
}

/// <summary>
/// Vehicle Framework burns fuel while a vehicle is drafted, through its fuel comp's private ShouldConsumeNow. On the battery the engine is off,
/// so no fuel is burned: the getter answers false while the battery drives it. No extension point exists; postfix only, and only for a vehicle
/// whose battery drive is running. When the getter is false, the same comp's tick also skips its "out of fuel, so undraft" line, which is
/// what lets a set with an empty tank keep creeping.
/// </summary>
[HarmonyPatch(typeof(CompFueledTravel), "ShouldConsumeNow", MethodType.Getter)]
public static class BatteryDriveFuelPatch
{
    public static void Postfix(CompFueledTravel __instance, ref bool __result)
    {
        if (!__result || __instance.Vehicle is not { } vehicle) return;
        if (CompPhobosVehicleSystems.Of(vehicle)?.BatteryDrive is { OnBattery: true }) __result = false;
    }
}

/// <summary>
/// Vehicle Framework refuses to draft a vehicle whose tank is empty. A set with charge in its battery buffer can still creep, so the refusal
/// stands down for it. No extension point (the fuel comp's own CanDraft decides); postfix only, and only for an empty tank with a battery
/// drive that can move.
/// </summary>
[HarmonyPatch(typeof(CompFueledTravel), nameof(CompFueledTravel.CanDraft))]
public static class BatteryDriveDraftPatch
{
    public static void Postfix(CompFueledTravel __instance, ref AcceptanceReport __result)
    {
        if (__result.Accepted || !__instance.EmptyTank || __instance.Vehicle is not { } vehicle) return;
        if (CompPhobosVehicleSystems.Of(vehicle)?.BatteryDrive is { CanCreep: true }) __result = AcceptanceReport.WasAccepted;
    }
}

/// <summary>The same for a move order: Vehicle Framework's fuel comp refuses one with an empty tank. Postfix only, as above.</summary>
[HarmonyPatch(typeof(CompFueledTravel), nameof(CompFueledTravel.CanMove))]
public static class BatteryDriveMovePatch
{
    public static void Postfix(CompFueledTravel __instance, ref AcceptanceReport __result)
    {
        if (__result.Accepted || !__instance.EmptyTank || __instance.Vehicle is not { } vehicle) return;
        if (CompPhobosVehicleSystems.Of(vehicle)?.BatteryDrive is { CanCreep: true }) __result = AcceptanceReport.WasAccepted;
    }
}
