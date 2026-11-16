using HarmonyLib;
using UnityEngine;
using Vehicles;
using Verse;

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

/// <summary>
/// T5 and T6 (Framework 0.119.0): a turret cranked by hand aims worse on the move and in the dark. Without Combat Extended, Vehicle Framework
/// decides a hit from this report, so the turret's own hit factor is scaled; the report is a value handed back, so the change reaches only this
/// shot. A postfix; nothing of Vehicle Framework's is skipped.
/// </summary>
[HarmonyPatch(typeof(TurretShotReport), nameof(TurretShotReport.HitReportFor))]
public static class TurretAimReport
{
    static readonly AccessTools.StructFieldRef<TurretShotReport, float> FactorFromTurret =
        AccessTools.StructFieldRefAccess<TurretShotReport, float>("factorFromTurret");

    public static void Postfix(VehicleTurret turret, ref TurretShotReport __result)
    {
        if (turret == null) return;
        float accuracy = TurretPace.For(turret).accuracy;
        if (accuracy < 1f) FactorFromTurret(ref __result) *= accuracy;
    }
}

/// <summary>
/// T5 and T6 under Combat Extended (Framework 0.119.0): Combat Extended's compatibility code hands Vehicle Framework a function that turns a
/// shot's sway into its angle (VehicleTurret.ProjectileAngleCE), and a turret's shot goes through it. While one of a vehicle's turrets fires,
/// this prefix notes which, and puts our wrapper round that function once: the wrapper widens the sway of a turret cranked by hand, then
/// calls Combat Extended's own. The finalizer forgets the turret whatever happens. Nothing is skipped.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), "FireTurretCE")]
public static class TurretAimSway
{
    static VehicleTurret? firing;
    static System.Func<float, float, Thing, LocalTargetInfo, Vector3, bool, float, float, float, float, Vector2>? combatExtended;

    public static void Prefix(VehicleTurret __instance)
    {
        firing = __instance;
        var current = VehicleTurret.ProjectileAngleCE;
        if (current != null && current != (System.Func<float, float, Thing, LocalTargetInfo, Vector3, bool, float, float, float, float, Vector2>)Wrapped)
        {
            combatExtended = current;
            VehicleTurret.ProjectileAngleCE = Wrapped;
        }
    }

    public static void Finalizer() => firing = null;

    static Vector2 Wrapped(float speed, float range, Thing shooter, LocalTargetInfo target, Vector3 origin, bool flyOverhead, float gravity,
        float sway, float spread, float recoil)
    {
        VehicleTurret? turret = firing;
        if (turret != null && turret.vehicle == shooter)
        {
            float accuracy = TurretPace.For(turret).accuracy;
            if (accuracy < 1f)
            {
                PhobosKnockOutRulesDef rules = Phobos.Framework.Defs.PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
                sway = (float)Phobos.Framework.Vehicles.Rules.TurretAimRule.CombatExtendedSway(sway, accuracy, rules.combatExtendedSwayCap);
            }
        }
        return combatExtended!(speed, range, shooter, target, origin, flyOverhead, gravity, sway, spread, recoil);
    }
}
