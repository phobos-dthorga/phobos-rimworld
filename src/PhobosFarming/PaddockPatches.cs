using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.Farming;

/// <summary>
/// Patch one (docs/development/farming-batch-one.md, "Paddocks"): a growing zone's buttons. Every growing zone gets "Machine-worked"; a
/// paddock also gets "Hands may help", and while machines alone work it, the game's own "Allow sowing" toggle is taken out so it cannot be
/// flipped back by hand. Reason for the patch, as the rules ask: the zone's buttons have no extension point (Dubs Bad Hygiene adds its
/// fertilise toggle the same way). A postfix; the game's method always runs.
/// </summary>
[HarmonyPatch(typeof(Zone_Growing), nameof(Zone_Growing.GetGizmos))]
public static class Patch_ZoneGrowing_GetGizmos
{
    public static void Postfix(Zone_Growing __instance, ref IEnumerable<Gizmo> __result) => __result = With(__instance, __result);

    static IEnumerable<Gizmo> With(Zone_Growing zone, IEnumerable<Gizmo> original)
    {
        bool machineOnly = Paddocks.MachineOnly(zone);
        string allowSow = machineOnly ? "CommandAllowSow".Translate().Resolve() : "";
        foreach (Gizmo gizmo in original)
        {
            if (machineOnly && gizmo is Command_Toggle toggle && toggle.defaultLabel == allowSow) continue;
            yield return gizmo;
        }
        foreach (Gizmo gizmo in Paddocks.Gizmos(zone)) yield return gizmo;
    }
}

/// <summary>
/// Patch two (docs/development/farming-batch-one.md, "Paddocks"): colonists looking for harvest work pass over a paddock that machines
/// alone work. The base method returns true and the sowing giver overrides it without calling it, so this reaches the harvest scan (and any
/// other mod's grower giver that does not override it) and never sowing, which already honours the zone's own "allow sowing". Reason for
/// the patch: there is no extension point, and without it every colonist's harvest scan walks every paddock cell. A postfix; the game's
/// method always runs.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_Grower), "ExtraRequirements")]
public static class Patch_WorkGiverGrower_ExtraRequirements
{
    public static void Postfix(IPlantToGrowSettable settable, ref bool __result)
    {
        if (__result && settable is Zone_Growing zone && Paddocks.MachineOnly(zone)) __result = false;
    }
}
