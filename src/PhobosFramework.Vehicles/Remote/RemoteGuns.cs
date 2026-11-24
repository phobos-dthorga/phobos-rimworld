using System;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// Remote guns (Framework 0.280.0; docs/development/autonomous-vehicles.md, step 6; the owner's cards of 24 and 25 Nov 2026). A remote gun
/// is a turret no seat works, as GTek's L-ATV's guns are, on a vehicle that carries the remote control kit. Vehicle Framework fires such a
/// turret with nobody aboard (it counts it as manned); with the kit fitted it may fire only while a station's operator has the vehicle's
/// picture, or always when the setting "Remote guns fire on their own" is on (RemoteGunRule). A vehicle without the kit is untouched.
/// <para>Cost: the patch runs only when Vehicle Framework re-checks a turret (a seat filled or left, damage), and the vehicle's comp asks
/// it again only when whether its guns may fire has changed, at most once every 250 ticks.</para>
/// </summary>
public static class RemoteGuns
{
    /// <summary>The setting "Remote guns fire on their own" (off by default: the risk is high). Live.</summary>
    public static bool FireAlone => VehicleSystemsMod.Settings.remoteGunsFireAlone;

    /// <summary>Whether the vehicle has a turret no seat works.</summary>
    public static bool HasAny(VehiclePawn vehicle)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null) return false;
        foreach (var turret in turrets) if (IsRemoteGun(turret)) return true;
        return false;
    }

    /// <summary>A turret no seat works, as Vehicle Framework reads it (VehicleTurret.RecacheMannedStatus): no role handles turrets naming it.</summary>
    public static bool IsRemoteGun(VehicleTurret turret)
    {
        var vehicle = turret.vehicle;
        if (vehicle?.handlers == null) return false;
        foreach (var handler in vehicle.handlers)
        {
            if ((handler.role.HandlingTypes & HandlingType.Turret) == 0) continue;
            var ids = handler.role.TurretIds;
            if (ids != null && (ids.Contains(turret.key) || (!string.IsNullOrEmpty(turret.groupKey) && ids.Contains(turret.groupKey)))) return false;
        }
        return true;
    }

    /// <summary>Whether this vehicle's remote guns may fire now.</summary>
    public static bool MayFire(VehiclePawn vehicle)
    {
        var comp = vehicle.GetComp<CompPhobosRemoteControl>();
        var kitFitted = comp != null && RemoteControl.SourceFor(vehicle) != null;
        return RemoteGunRule.MayFire(kitFitted, comp is { HasPicture: true }, FireAlone);
    }

    /// <summary>Asks Vehicle Framework to re-check every turret of the vehicle, which our patch then answers.</summary>
    public static void Recache(VehiclePawn vehicle)
    {
        var turrets = vehicle.CompVehicleTurrets?.Turrets;
        if (turrets == null) return;
        foreach (var turret in turrets) turret.RecacheMannedStatus();
    }

    private static readonly Action<VehicleTurret, bool> SetManned =
        AccessTools.MethodDelegate<Action<VehicleTurret, bool>>(AccessTools.PropertySetter(typeof(VehicleTurret), nameof(VehicleTurret.IsManned)));

    /// <summary>Marks a remote gun unmanned: Vehicle Framework then neither aims, fires nor reloads it (TurretDisabled, CanTarget).</summary>
    internal static void Restrain(VehicleTurret turret) => SetManned(turret, false);
}

/// <summary>
/// Vehicle Framework counts a turret no seat works as manned, so it fires with nobody aboard. On a vehicle with the remote control kit, our
/// rule decides instead: only while a station's operator has its picture, or with the setting to fire alone. Postfix: it only ever turns a
/// "manned" into "not manned", only for such a turret, and reads our comp and the setting; nothing is worked out per tick.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.RecacheMannedStatus))]
public static class RemoteGunMannedPatch
{
    public static void Postfix(VehicleTurret __instance)
    {
        if (!__instance.IsManned || __instance.vehicle == null || !RemoteGuns.IsRemoteGun(__instance)) return;
        if (!RemoteGuns.MayFire(__instance.vehicle)) RemoteGuns.Restrain(__instance);
    }
}
