using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Why a sortie clicked on a RimPacts battle does nothing, said out loud. When a client state attacks a town, RimPacts opens that
/// town's map and, until the player joins the battle from the settlement's own button, removes every "land in this map" arrival
/// option (its spectate landing gate, WorldComponent_RimPacts.SpectateLandBlockReason). The Harrier's framework keeps only those
/// options for an open map, so the list is empty and Vehicle Framework rejects the click with no text at all (read in RimPacts,
/// the fixed-wing framework and Vehicle Framework 1.6, 7 Nov 2026; the owner's report). A postfix on the launcher's target check,
/// so the targeter shows the reason instead of silence. RimPacts' own rule still decides.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanTarget))]
public static class SortieGatePatch
{
    public static void Postfix(CompVehicleLauncher __instance, GlobalTargetInfo target, ref TargetValidation __result)
    {
        if (target.WorldObject is not MapParent parent) return;
        string? reason = RimPactsGate.BlockReason(parent);
        if (reason == null || __instance.OptionsAt(target).Any()) return;
        __result = TargetValidation.Failed with { Tooltip = "PhobosVehiclePatches_JoinBattleFirst".Translate(parent.Label.Named("PLACE")).Resolve() };
    }
}

/// <summary>RimPacts' spectate landing gate, reached by name: null when RimPacts is absent or the map is not a battle it holds closed.</summary>
public static class RimPactsGate
{
    const string ComponentType = "RimPacts.WorldComponent_RimPacts";
    static bool looked;
    static PropertyInfo? instance;
    static MethodInfo? blockReason;

    public static string? BlockReason(MapParent parent)
    {
        if (!OptionalMods.IsActive("wowgag.RimPacts")) return null;
        if (!looked)
        {
            looked = true;
            Type? type = OptionalMods.TypeNamed(ComponentType);
            instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            blockReason = type?.GetMethod("SpectateLandBlockReason", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(MapParent) }, null);
        }
        object? component = instance?.GetValue(null);
        if (component == null || blockReason == null) return null;
        try { return blockReason.Invoke(component, new object[] { parent }) as string; }
        catch (Exception) { return null; }
    }
}
