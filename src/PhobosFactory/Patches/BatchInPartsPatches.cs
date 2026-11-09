using HarmonyLib;
using PipeSystem;

namespace Phobos.Factory;

/// <summary>
/// Saves what is left of a batch going out in parts on the process it belongs to. A process has no field for it, and
/// keeping it here rather than in a game component means removing this mod leaves nothing the game can't load.
/// </summary>
[HarmonyPatch(typeof(Process), nameof(Process.ExposeData))]
public static class ProcessExposeDataPatch
{
    public static void Postfix(Process __instance)
    {
        BillRepair.NoteMissingRecipe(__instance);
        BatchInParts.Expose(__instance);
    }
}

/// <summary>Forgets what is left of a batch once it ends or the player cancels it, so the next batch starts clean.</summary>
[HarmonyPatch(typeof(Process), nameof(Process.ResetProcess), new[] { typeof(bool) })]
public static class ProcessResetForgetsPartsPatch
{
    public static void Postfix(Process __instance) => BatchInParts.Forget(__instance);
}
