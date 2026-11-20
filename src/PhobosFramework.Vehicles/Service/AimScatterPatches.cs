using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

// Aircraft service-life upgrades (avionics A10, A11; 21 Nov 2026): a weapon's accuracy (TurretPace) reaches the shots that the turret report
// does not decide. A bomb or rocket fired with a forced miss radius is scattered by that radius alone, so it is widened for the one shot; and
// the two flight frameworks fire Combat Extended shots through a method of their own, which TurretAimSway now watches too. Nothing is skipped.

/// <summary>
/// Widens a turret's forced miss radius for one shot, by its accuracy, and puts it back. The radius is the fire mode's, which every turret of
/// the design shares, so it is changed only for the call and restored by the finalizer whatever happens (the game ticks on one thread). A
/// framework's turret that hands over to Vehicle Framework's own firing inside its call is widened once, by the outer call.
/// </summary>
public static class AimScatter
{
    static FireMode? widened;
    static float original;

    /// <summary>Widens this turret's scatter for the shot; true when it did, so the caller's finalizer puts it back.</summary>
    public static bool Widen(VehicleTurret? turret)
    {
        if (widened != null || turret?.vehicle == null) return false;
        float accuracy = TurretPace.For(turret).accuracy;
        if (accuracy >= 1f) return false;
        FireMode? mode = turret.CurrentFireMode;
        if (mode == null || mode.forcedMissRadius <= 0f) return false;
        widened = mode;
        original = mode.forcedMissRadius;
        mode.forcedMissRadius = (float)AvionicsAimRule.MissRadius(original, accuracy);
        if (Seen.Wants("vehicles.scatter-widened", turret.vehicle, turret.key)) Seen.Note("vehicles.scatter-widened", turret.vehicle, turret.key,
            values: new[] { new KeyValuePair<string, double>("radius", original), new KeyValuePair<string, double>("widened", mode.forcedMissRadius) });
        return true;
    }

    public static void Restore()
    {
        if (widened == null) return;
        widened.forcedMissRadius = original;
        widened = null;
    }
}

/// <summary>Vehicle Framework's own firing.</summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.FireTurret))]
public static class TurretScatterPace
{
    public static void Prefix(VehicleTurret __instance, out bool __state) => __state = AimScatter.Widen(__instance);

    public static void Finalizer(bool __state)
    {
        if (__state) AimScatter.Restore();
    }
}

/// <summary>
/// The two flight frameworks' turrets, which override the firing (the fixed-wing framework's for the Harrier and Close Air Support's jets,
/// rabiosus's VTOL Vehicle for the Hind and the Mongoose). Reached by name: no core assembly references either. A framework that is not
/// loaded, or renames the method, is simply not patched.
/// </summary>
[HarmonyPatch]
public static class FrameworkTurretScatterPace
{
    // scripts/check-targets.py reads these names.
    static readonly string[] Targets = { "FixedWingAircraftFramework.VtolVehicleTurret:FireTurret", "VTOLVehicle.VtolVehicleTurret:FireTurret" };

    static bool Prepare() => Found().GetEnumerator().MoveNext();

    static IEnumerable<MethodBase> TargetMethods() => Found();

    static IEnumerable<MethodBase> Found()
    {
        foreach (string name in Targets)
        {
            if (AccessTools.Method(name) is { } method) yield return method;
        }
    }

    public static void Prefix(object __instance, out bool __state) => __state = AimScatter.Widen(__instance as VehicleTurret);

    public static void Finalizer(bool __state)
    {
        if (__state) AimScatter.Restore();
    }
}

/// <summary>
/// The flight frameworks' Combat Extended firing, which hands its sway to the same shared angle function as Vehicle Framework's own: noting the
/// turret here lets TurretAimSwayCombatExtended widen that shot's sway by its accuracy, as for Vehicle Framework's FireTurretCE. By name.
/// </summary>
[HarmonyPatch]
public static class FrameworkTurretAimSway
{
    // scripts/check-targets.py reads these names.
    static readonly string[] Targets =
    {
        "FixedWingAircraftFramework.VtolVehicleTurret:FireTurretCEFromFirePoint",
        "VTOLVehicle.VtolVehicleTurret:FireTurretCEFromFirePoint",
    };

    static bool Prepare() => OptionalMods.IsActive(OptionalMods.CombatExtended) && Found().GetEnumerator().MoveNext();

    static IEnumerable<MethodBase> TargetMethods() => Found();

    static IEnumerable<MethodBase> Found()
    {
        foreach (string name in Targets)
        {
            if (AccessTools.Method(name) is { } method) yield return method;
        }
    }

    public static void Prefix(object __instance)
    {
        if (__instance is not VehicleTurret turret) return;
        float factor = TurretPace.For(turret).accuracy;
        if (factor >= 1f) return;
        TurretAimSway.firing = turret;
        TurretAimSway.accuracy = factor;
    }

    public static void Finalizer() => TurretAimSway.firing = null;
}
