using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// The game's two ways a thing blows up, each giving the thing's radiation burst first when its Def carries one (Framework 0.358.0;
/// docs/development/radiation-bursts.md): the explosive comp (IED traps, charges, shells cooking off, a nuclear boiler, warpods) and the
/// explosive projectile (a mortar shell). Prefixes that never skip: the dose arrives with the flash, before the blast. Under Combat Extended
/// the comp's detonation still runs (Combat Extended swaps only the explosion inside it); its own shells are caught in D'thorga Patches'
/// Combat Extended tie-in. A Def without a burst costs one null check.
/// </summary>
[HarmonyPatch(typeof(CompExplosive), "Detonate", new[] { typeof(Map), typeof(bool) })]
public static class Patch_CompExplosive_Detonate_RadiationBurst
{
    public static void Prefix(CompExplosive __instance, Map map)
    {
        ThingWithComps parent = __instance.parent;
        if (parent?.def?.modExtensions == null || RadiationBursts.Of(parent.def) is not { } burst || !burst.Armed(parent)) return;
        RadiationBursts.Burst(map ?? parent.MapHeld, parent.PositionHeld, burst, __instance.Props.explosiveRadius, parent.def.defName);
    }
}

/// <summary>The explosive projectile's half of <see cref="Patch_CompExplosive_Detonate_RadiationBurst"/>.</summary>
[HarmonyPatch(typeof(Projectile_Explosive), "Explode")]
public static class Patch_ProjectileExplosive_Explode_RadiationBurst
{
    public static void Prefix(Projectile_Explosive __instance)
    {
        if (__instance.def?.modExtensions == null || RadiationBursts.Of(__instance.def) is not { } burst) return;
        RadiationBursts.Burst(__instance.Map, __instance.Position, burst, __instance.def.projectile?.explosionRadius ?? 0f, __instance.def.defName);
    }
}
