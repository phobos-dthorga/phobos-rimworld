using System.Linq;
using HarmonyLib;
using IRBM;
using Phobos.Framework;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// IRBM's close-in weapon systems fire at enemy aircraft in the air over the map (Patches 0.44.0, D'thorga Framework 0.297.0;
/// docs/development/airdrop.md, "Air defence against drops"; the owner's ask of 26 Nov 2026). IRBM's CIWS aims at skyfallers, projectiles
/// and missiles only, so an enemy aircraft on a drop pass flew over it untouched. With nothing else to shoot, it takes the first enemy
/// aircraft D'thorga Framework names in the air over its map; each hit IRBM rolls does a little bullet damage to it, through the aircraft's
/// own parts, until it leaves, lands or comes down. Behind its setting, on by default.
/// </summary>
internal static class CiwsAircraft
{
    /// <summary>The bullet damage of one hit IRBM rolls on an aircraft (a short burst of 20 mm; IRBM rolls a hit most ticks it fires). 8: the
    /// owner's answer on the Decisions page, 26 Nov 2026 (it was 4, authored).</summary>
    public const float DamagePerHit = 8f;

    public static bool On => MissilePatchesMod.Settings.ciwsAircraft && AirTargets.Over != null;

    public static bool IsAircraft(object? target) => On && target is Thing thing && AirTargets.IsHostileInAir?.Invoke(thing) == true;

    public static readonly AccessTools.FieldRef<Building_CIWS, ILoadReferenceable> CurrentTarget =
        AccessTools.FieldRefAccess<Building_CIWS, ILoadReferenceable>("currentTarget");
}

/// <summary>With nothing else to shoot at, an enemy aircraft in the air over the map. A postfix; only fills an empty result.</summary>
[HarmonyPatch(typeof(Building_CIWS), "FindBestTarget")]
public static class CiwsAircraftFindPatch
{
    public static void Postfix(Building_CIWS __instance, ref ILoadReferenceable __result)
    {
        if (__result != null || !CiwsAircraft.On || __instance.Map is not { } map) return;
        Thing? aircraft = AirTargets.Over!(map).FirstOrDefault();
        if (aircraft != null) __result = aircraft;
    }
}

/// <summary>An enemy aircraft in the air is a valid target. A postfix; it only adds aircraft, never takes a target away.</summary>
[HarmonyPatch(typeof(Building_CIWS), "IsTargetValid")]
public static class CiwsAircraftValidPatch
{
    public static void Postfix(Building_CIWS __instance, ILoadReferenceable target, ref bool __result)
    {
        if (!__result && CiwsAircraft.IsAircraft(target) && target is Thing thing && thing.Map == __instance.Map) __result = true;
    }
}

/// <summary>A hit on an aircraft damages it, rather than IRBM's handling of a projectile or a skyfaller, which would do nothing to it. A
/// prefix that skips IRBM's method for an aircraft only, after clearing the target as IRBM does, so the next tick aims afresh.</summary>
[HarmonyPatch(typeof(Building_CIWS), "InterceptionSuccess")]
public static class CiwsAircraftHitPatch
{
    public static bool Prefix(Building_CIWS __instance, ILoadReferenceable target)
    {
        if (!CiwsAircraft.IsAircraft(target) || target is not Thing thing) return true;
        AirTargets.Hit?.Invoke(thing, CiwsAircraft.DamagePerHit);
        CiwsAircraft.CurrentTarget(__instance) = null!;
        return false;
    }
}
