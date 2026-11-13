using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using IRBM;
using Phobos.Framework;
using Verse;

namespace Phobos.MissilePatches.Patches;

/// <summary>
/// IRBM's close-in weapon systems see Combat Extended's shells (Patches 0.19.1; air-defence-tie-ins.md, AD14; owner, 13 Nov 2026: "Patch
/// it"). IRBM's CIWS picks its targets on a map from the game's projectile list and keeps only the game's own Projectile type
/// (Building_CIWS.FindBestTarget and IsTargetValid), so under Combat Extended, whose shells are its own ProjectileCE, it never fires at a
/// mortar or artillery shell. These postfixes add CE's shells under IRBM's own rules for a projectile: one that flies overhead, is not the
/// player's, and is headed for the player's things. An interception destroys the shell, as IRBM does to a vanilla one. Combat Extended is
/// read by name; nothing references its assembly.
/// Temporary (docs/development/temporary-patches.md): IRBM's gap with CE, not a fault of either alone; it can go if IRBM learns CE's
/// projectiles. Stands down without CE or with the setting off.
/// </summary>
internal static class CiwsShells
{
    public const string CombatExtended = "CETeam.CombatExtended";

    static bool looked;
    static Type? shellType;
    static FieldInfo? launcherField;
    static FieldInfo? targetField;
    static readonly MethodInfo? HeadingForPlayer = AccessTools.Method(typeof(Building_CIWS), "IsHeadingForPlayerAssets");

    static Type? ShellType
    {
        get
        {
            if (looked) return shellType;
            looked = true;
            if (!OptionalMods.IsActive(CombatExtended)) return null;
            shellType = OptionalMods.TypeNamed("CombatExtended.ProjectileCE");
            launcherField = shellType == null ? null : AccessTools.Field(shellType, "launcher");
            targetField = shellType == null ? null : AccessTools.Field(shellType, "intendedTarget");
            if (launcherField == null || targetField == null) shellType = null;
            return shellType;
        }
    }

    static bool On => MissilePatchesMod.Settings.ciwsShells && ShellType != null && HeadingForPlayer != null;

    public static bool IsShell(object? target) => target is Thing thing && ShellType is { } type && type.IsInstanceOfType(thing);

    /// <summary>IRBM's test for a projectile, applied to a CE shell.</summary>
    public static bool Valid(Building_CIWS ciws, Thing shell)
    {
        if (!On || shell.Destroyed || !shell.Spawned || shell.Map != ciws.Map || shell.def.projectile is not { flyOverhead: true } props) return false;
        Thing? launcher = launcherField!.GetValue(shell) as Thing;
        if (launcher?.Faction == RimWorld.Faction.OfPlayer) return false;
        if (targetField!.GetValue(shell) is not LocalTargetInfo target) return false;
        float radius = props.explosionRadius > 0f ? props.explosionRadius : 0f;
        return HeadingForPlayer!.Invoke(ciws, new object[] { target.Cell, radius }) is true;
    }

    /// <summary>The first incoming CE shell on the map that IRBM's rules would take.</summary>
    public static Thing? FirstShell(Building_CIWS ciws)
    {
        if (!On) return null;
        List<Thing> projectiles = ciws.Map.listerThings.ThingsInGroup(ThingRequestGroup.Projectile);
        for (int i = 0; i < projectiles.Count; i++)
        {
            if (IsShell(projectiles[i]) && Valid(ciws, projectiles[i])) return projectiles[i];
        }
        return null;
    }
}

/// <summary>With nothing else to shoot at, an incoming CE shell. A postfix; only fills an empty result.</summary>
[HarmonyPatch(typeof(Building_CIWS), "FindBestTarget")]
public static class CiwsFindShellPatch
{
    public static void Postfix(Building_CIWS __instance, ref ILoadReferenceable __result)
    {
        if (__result == null && CiwsShells.FirstShell(__instance) is { } shell) __result = shell;
    }
}

/// <summary>A CE shell stays a valid target while IRBM's rules for a projectile hold. A postfix; only turns false to true for a shell.</summary>
[HarmonyPatch(typeof(Building_CIWS), "IsTargetValid")]
public static class CiwsShellValidPatch
{
    public static void Postfix(Building_CIWS __instance, ILoadReferenceable target, ref bool __result)
    {
        if (!__result && CiwsShells.IsShell(target)) __result = CiwsShells.Valid(__instance, (Thing)target);
    }
}

/// <summary>An intercepted CE shell is destroyed, as IRBM destroys a vanilla one. A postfix; IRBM's own branches never touch a CE shell.</summary>
[HarmonyPatch(typeof(Building_CIWS), "InterceptionSuccess")]
public static class CiwsShellDestroyedPatch
{
    public static void Postfix(ILoadReferenceable target)
    {
        if (CiwsShells.IsShell(target) && target is Thing { Destroyed: false } shell) shell.Destroy();
    }
}
