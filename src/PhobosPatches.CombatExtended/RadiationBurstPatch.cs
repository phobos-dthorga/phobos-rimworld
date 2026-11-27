using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Health;
using Verse;

namespace Phobos.CombatExtendedPatches;

/// <summary>
/// Combat Extended's shells blow up in their own impact (ProjectileCE.Impact, read from its public source 27 Nov 2026: an explosive shell's
/// override calls it at once or after its fuse), not through the game's explosive projectile, so D'thorga Framework's radiation burst is given
/// here for a shell whose Def carries one: Combat Extended's antigrain mortar shell (Patches 0.47.0; docs/development/radiation-bursts.md).
/// By name, as the rest of this tie-in. A prefix that never skips: the dose arrives with the flash. A shell without a burst costs one null check.
/// </summary>
[HarmonyPatch]
public static class Patch_ProjectileCE_Impact_RadiationBurst
{
    public static bool Prepare() => TargetMethod() != null;

    public static MethodBase? TargetMethod() => AccessTools.Method("CombatExtended.ProjectileCE:Impact", new[] { typeof(Thing) });

    public static void Prefix(object __instance)
    {
        if (__instance is not Thing { def.modExtensions: not null } shell || RadiationBursts.Of(shell.def) is not { } burst || !shell.Spawned) return;
        RadiationBursts.Burst(shell.Map, shell.Position, burst, shell.def.projectile?.explosionRadius ?? 0f, shell.def.defName);
    }
}
