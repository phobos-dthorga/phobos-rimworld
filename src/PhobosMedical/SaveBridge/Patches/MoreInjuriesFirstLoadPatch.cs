using System.Reflection;
using HarmonyLib;
using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.Patches;

/// <summary>
/// Postfix on More Injuries' MoreInjuryComp.PostExposeData, reached by name so this assembly never references More Injuries.
/// On the first load of a save without More Injuries, that comp finds no saved job cache and sets its FailedLoading flag; More
/// Injuries' own Pawn.ExposeData postfix then removes every wound and missing part from the pawn, clears its surgery bills and
/// capacities, and, if the pawn is dead, forces it back to Mobile. In the owner's log of 3 November 2026 that wiped 287 pawns and
/// revived 105 corpses, whose empty trackers then broke the UI every frame. With the body parts re-numbered there is nothing
/// for the wipe to fix, so while the gate is open the flag is cleared as soon as it is set. Once the save has been written with
/// More Injuries, the comp finds its cache and never sets the flag again.
/// </summary>
public static class MoreInjuriesFirstLoadPatch
{
    public const string CompTypeName = "MoreInjuries.HealthConditions.MoreInjuryComp";
    public const string FlagName = "FailedLoading";

    static PropertyInfo? failedLoading;

    /// <summary>Finds the comp's flag. Null when More Injuries is absent or has changed shape.</summary>
    public static MethodBase? FindTarget()
    {
        System.Type? comp = AccessTools.TypeByName(CompTypeName);
        if (comp == null) return null;
        failedLoading = AccessTools.Property(comp, FlagName);
        if (failedLoading?.GetSetMethod(nonPublic: true) == null || failedLoading.PropertyType != typeof(bool)) return null;
        return AccessTools.Method(comp, "PostExposeData");
    }

    public static void Postfix(object __instance)
    {
        if (!BridgeState.Active || Scribe.mode != LoadSaveMode.LoadingVars || failedLoading == null) return;
        if (failedLoading.GetValue(__instance) is not true) return;
        failedLoading.SetValue(__instance, false);
        BridgeState.CountWipePrevented();
    }
}
