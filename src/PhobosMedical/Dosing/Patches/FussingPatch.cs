using HarmonyLib;
using Phobos.Dosing.Services;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Patches;

/// <summary>
/// Fussing: a postfix on the game's base tend quality, the overload every tend goes through (the one taking the medicine's potency as
/// floats; the ThingDef overload calls it). It multiplies the result by the patient's fussing factor, 1 for anyone below extreme pain or
/// not awake. The game has no extension point for the patient's state in tend quality, so a postfix it is; it never skips the original.
/// </summary>
[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.CalculateBaseTendQuality), new[] { typeof(Pawn), typeof(Pawn), typeof(float), typeof(float) })]
public static class FussingPatch
{
    public static void Postfix(Pawn patient, ref float __result)
    {
        float factor = FussingService.TendFactor(patient);
        if (factor < 1f) __result *= factor;
    }
}
