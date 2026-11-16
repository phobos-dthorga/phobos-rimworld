using HarmonyLib;
using UnityEngine;
using Vehicles;

namespace Phobos.VehicleSystems;

// The hooks that let a weapon's pace (TurretPace) change how fast it turns, aims and reloads (Framework 0.115.0, T2, C4, T3). A turret
// reads its figures from its Def, which every vehicle mounting it shares and which has no extension point, so the figures are scaled per
// weapon. None of these skips Vehicle Framework's code. Other mods' own turret classes that override the turn (the Harrier's, VTOL
// Vehicle's, Turbo's BMP-1) keep their own figures for turning.

/// <summary>Aiming (the warm-up before a shot) takes longer at a slower pace.</summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.WarmupTicks), MethodType.Getter)]
public static class TurretAimPace
{
    public static void Postfix(VehicleTurret __instance, ref int __result)
    {
        if (__result <= 0) return;
        float pace = TurretPace.For(__instance).aim;
        if (pace < 1f) __result = Mathf.CeilToInt(__result / Mathf.Max(0.05f, pace));
    }
}

/// <summary>Reloading takes longer at a slower pace.</summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.MaxTicks), MethodType.Getter)]
public static class TurretReloadPace
{
    public static void Postfix(VehicleTurret __instance, ref int __result)
    {
        if (__result <= 0) return;
        float pace = TurretPace.For(__instance).reload;
        if (pace < 1f) __result = Mathf.CeilToInt(__result / Mathf.Max(0.05f, pace));
    }
}

/// <summary>
/// Turning: the Def's turn rate is lowered for this one call and put back after it, by the finalizer, whatever happens. The game ticks on
/// one thread, so no other vehicle sees the lowered figure.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), "TurretRotationTick")]
public static class TurretTraversePace
{
    public static void Prefix(VehicleTurret __instance, out float __state)
    {
        __state = float.NaN;
        VehicleTurretDef? def = __instance.def;
        if (def == null) return;
        float pace = TurretPace.For(__instance).traverse;
        if (pace >= 1f) return;
        __state = def.rotationSpeed;
        def.rotationSpeed *= Mathf.Max(0.05f, pace);
    }

    public static void Finalizer(VehicleTurret __instance, float __state)
    {
        if (!float.IsNaN(__state) && __instance.def != null) __instance.def.rotationSpeed = __state;
    }
}
