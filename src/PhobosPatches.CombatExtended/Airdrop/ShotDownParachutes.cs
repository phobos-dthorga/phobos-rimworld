using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.CombatExtendedPatches.Airdrop;

/// <summary>
/// A D'thorga parachute load brought down by Combat Extended's CIWS lands on D'thorga's own terms (Patches 0.44.0, D'thorga Framework
/// 0.297.0; docs/development/airdrop.md, "Air defence against drops"; the owner's ask of 26 Nov 2026 for a softer outcome). Combat Extended's
/// own outcome for a downed skyfaller kills everyone inside, a vehicle too, and spills the rest; ours brings the load down where it is with
/// its canopy shredded: its people downed, a vehicle landing hard, cargo damaged. A prefix that skips Combat Extended's method only for a
/// D'thorga parachute load (Framework answers which), as its outcome would otherwise run first and leave nothing to land; every other
/// skyfaller keeps Combat Extended's own. Combat Extended's type is read by name.
/// </summary>
[HarmonyPatch]
public static class Patch_CIWSSkyfaller_OnDestroying
{
    public static bool Prepare() => TargetMethod() != null;

    public static MethodBase? TargetMethod() =>
        AccessTools.Method("CombatExtended.CompCIWSImpactHandler_Skyfaller:OnDestroying", new[] { typeof(DamageInfo) });

    public static bool Prefix(object __instance) => __instance is not ThingComp comp || !ShotDownFallers.TryShootDown(comp.parent);
}
