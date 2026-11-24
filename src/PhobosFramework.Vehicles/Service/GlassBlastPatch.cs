using System.Reflection;
using HarmonyLib;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Every explosion judges the glass of the vehicles near it once, as it starts (Framework 0.238.0, VG3). Glass is the first thing a blast
/// breaks, well beyond where it harms anything else, and without Combat Extended Vehicle Framework tells a plate's reactors only of hits that
/// did the plate damage, so a blast its armour turned aside would otherwise break no glass. A postfix; no other mod patches this method on the
/// owner's list (query-patches, 23 Nov 2026).
/// </summary>
[HarmonyPatch(typeof(Explosion), nameof(Explosion.StartExplosion))]
public static class GlassBlastPatch
{
    public static void Postfix(Explosion __instance) => Glass.Explosion(__instance);
}

/// <summary>
/// The same for Combat Extended's blasts (fault-read 25 Nov 2026, D1): its explosion is the game's Explosion, but it starts through its own
/// method, which never calls StartExplosion, so every shell, grenade and cook-off under CE broke no glass beyond its own reach. A postfix on
/// CE's start, found by name; not applied without CE.
/// </summary>
[HarmonyPatch]
public static class GlassBlastCombatExtendedPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("CombatExtended.ExplosionCE:StartExplosionCE");

    public static void Postfix(object __instance)
    {
        if (__instance is Explosion explosion) Glass.Explosion(explosion);
    }
}
