using System;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

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
        // A fixed turret (a smoke discharger, Framework 0.319.0) fires only on a click or our own pop-when-hit, never by itself: no rule holds it.
        if (turret.def?.turretType == TurretType.Static) return false;
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

    /// <summary>
    /// Whether this remote gun may fire now. Our rule governs it on a vehicle with the station's kit, or when its Def says it is a remote
    /// weapon station of ours (<see cref="PhobosRemoteGunExtension"/>, Framework 0.285.0) on any vehicle.
    /// </summary>
    public static bool MayFire(VehiclePawn vehicle, VehicleTurret turret)
    {
        var comp = vehicle.GetComp<CompPhobosRemoteControl>();
        return RemoteGunRule.MayFire(KitFitted(vehicle, comp), Ours(turret), comp is { HasPicture: true }, FireAlone);
    }

    /// <summary>The station's kit only (Framework 0.281.0): a route has no picture to watch through, so a gun on it fires as its author built it [A].</summary>
    static bool KitFitted(VehiclePawn vehicle, CompPhobosRemoteControl? comp) => comp != null && RemoteControl.StationSource(vehicle) != null;

    /// <summary>A remote weapon station of ours: its Def carries the marker (Framework 0.285.0).</summary>
    public static bool Ours(VehicleTurret turret) => turret.def?.HasModExtension<PhobosRemoteGunExtension>() == true;

    /// <summary>
    /// What decides whether this vehicle's remote guns may fire, as one number, so its comp re-checks them only when it changes: the
    /// station's kit, the picture and the setting.
    /// </summary>
    public static int Inputs(VehiclePawn vehicle)
    {
        var comp = vehicle.GetComp<CompPhobosRemoteControl>();
        return (KitFitted(vehicle, comp) ? 1 : 0) | (comp is { HasPicture: true } ? 2 : 0) | (FireAlone ? 4 : 0);
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
        var mayFire = RemoteGuns.MayFire(__instance.vehicle, __instance);
        if (!mayFire) RemoteGuns.Restrain(__instance);
        // Our own station, held or let fire by the rule (Framework 0.285.0, row 512).
        if (RemoteGuns.Ours(__instance) && Seen.Wants("vehicles.remote-station", __instance.vehicle, mayFire ? "fires" : "held"))
        {
            Seen.Note("vehicles.remote-station", __instance.vehicle, __instance.vehicle.LabelShort, mayFire ? "fires" : "held");
        }
    }
}

/// <summary>
/// Marks a turret Def as a remote weapon station of ours (Framework 0.285.0; the owner's card of 25 Nov 2026, "Picture link, or the
/// setting", one rule everywhere): on any vehicle, with or without the station's kit, it fires only while a station's operator has the
/// vehicle's picture, or when the setting "Remote guns fire on their own" is on. Without the Radio Framework there is no picture, so it
/// fires only with that setting. Another author's turret without the marker keeps its own way on a vehicle without the kit.
/// </summary>
public class PhobosRemoteGunExtension : DefModExtension
{
}
