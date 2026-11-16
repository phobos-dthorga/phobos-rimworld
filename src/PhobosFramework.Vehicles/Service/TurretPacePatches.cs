using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
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
/// T5 and T6 under Combat Extended (Framework 0.119.0; reworked in 0.120.2): while a turret cranked by hand fires through Vehicle
/// Framework's Combat Extended path, this prefix notes which turret and how much worse it aims, and the finalizer forgets it whatever
/// happens. TurretAimSwayCombatExtended then widens that one shot's sway. Nothing is skipped, and nothing of Vehicle Framework's is
/// replaced.
///
/// Never put a wrapper of ours into VehicleTurret.ProjectileAngleCE, as 0.119.0 did (the crash of 17 Nov 2026). That field is one shared
/// function, and other mods swap their own into it for a shot and put back what they found (the Mongoose's and the Mi-24 Hind's
/// Patch_FireTurret_Converge). Ours stayed in it for good, so theirs kept ours as "the original" while ours took theirs as "Combat
/// Extended's": each called the other until the stack ran out and the game closed. tests/test_no_shared_hook_swaps.py refuses the pattern.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), "FireTurretCE")]
public static class TurretAimSway
{
    /// <summary>The hand-cranked turret firing now, or null: outside a shot, or for a turret aiming as well as ever.</summary>
    internal static VehicleTurret? firing;
    /// <summary>That turret's accuracy factor, below 1.</summary>
    internal static float accuracy = 1f;

    public static void Prefix(VehicleTurret __instance)
    {
        float factor = TurretPace.For(__instance).accuracy;
        if (factor >= 1f) return;
        firing = __instance;
        accuracy = factor;
    }

    public static void Finalizer() => firing = null;
}

/// <summary>
/// The sway itself (Framework 0.120.2): a prefix on Combat Extended's own function that turns a shot's sway into its angle, the one its
/// vehicle compatibility hands to Vehicle Framework. It only widens the sway handed in, for the one turret TurretAimSway noted; whatever
/// other mods wrap round that function, every shot ends here once. The function has no extension point of its own. A tie-in by name (no
/// reference to Combat Extended's assembly), which stands down when Combat Extended is absent or the function is renamed: hand-cranked
/// turrets then aim as well as powered ones under Combat Extended, and the load says so. Nothing is skipped.
/// </summary>
[HarmonyPatch]
public static class TurretAimSwayCombatExtended
{
    static bool Prepare()
    {
        if (!OptionalMods.IsActive(OptionalMods.CombatExtended)) return false;
        if (TargetMethod() != null) return true;
        VehicleSystemsMod.Logger.Warn("Combat Extended's shot-angle function was not found, so a turret cranked by hand aims no worse than a powered one.");
        return false;
    }

    // scripts/check-targets.py does not read a "Type:Method" name yet, so this one was checked by hand against Combat Extended's installed
    // assembly (17 Nov 2026): public static, with parameters named shooter and sway.
    static MethodBase? TargetMethod() => OptionalMods.IsActive(OptionalMods.CombatExtended)
        ? AccessTools.Method("CombatExtended.Compatibility.VehiclesCompat.VehiclesCompat:ProjectileAngleCE")
        : null;

    public static void Prefix(Thing shooter, ref float sway)
    {
        VehicleTurret? turret = TurretAimSway.firing;
        if (turret == null || turret.vehicle != shooter) return;
        PhobosKnockOutRulesDef rules = Phobos.Framework.Defs.PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        sway = (float)Phobos.Framework.Vehicles.Rules.TurretAimRule.CombatExtendedSway(sway, TurretAimSway.accuracy, rules.combatExtendedSwayCap);
        Seen.Note("vehicles.cranked-sway-widened", turret.vehicle, turret.key);
    }
}

/// <summary>
/// An autonomous weapon with no power and nobody aboard is out of action (Framework 0.120.0; owner, 17 Nov 2026): Vehicle Framework asks
/// TurretDisabled before it scans, aims and fires, so a postfix that only ever turns "works" into "out" stops it, from the weapon's cached
/// pace (one table read). A turret class that overrides TurretDisabled without calling the base keeps its own answer.
/// </summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretDisabled), MethodType.Getter)]
public static class TurretPowerStop
{
    public static void Postfix(VehicleTurret __instance, ref bool __result)
    {
        if (!__result && TurretPace.For(__instance).stoppedNoPower) __result = true;
    }
}

/// <summary>The same weapon's reason, where Vehicle Framework's own gizmo and our Systems tab ask why it is out.</summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.IsDisabled))]
public static class TurretPowerStopReason
{
    public static void Postfix(VehicleTurret __instance, ref bool __result, ref string reason)
    {
        if (__result || !TurretPace.For(__instance).stoppedNoPower) return;
        __result = true;
        reason = "PhobosVehicleSystems_WeaponNoPowerStopped".Translate().CapitalizeFirst().Resolve();
    }
}
