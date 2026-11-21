using HarmonyLib;
using Phobos.Radiation.Services;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Patches;

/// <summary>
/// A child born after a dose in pregnancy carries its harm (D'thorga Medical 0.23.0; docs/development/radiation-model.md, RM8; owner,
/// 22 Nov 2026: all three of loss, harm to the child and the child's own cancer risk). A postfix on Biotech's birth, which makes the baby
/// and returns it; the game has no event or extension point for a birth. It only reads the mother's record and adds conditions to the
/// newborn. Without Biotech the method is never called.
/// </summary>
[HarmonyPatch(typeof(PregnancyUtility), nameof(PregnancyUtility.ApplyBirthOutcome))]
public static class BirthPatch
{
    static bool warned;

    public static void Postfix(Thing __result, Thing birtherThing)
    {
        try
        {
            if (__result is Pawn baby && birtherThing is Pawn mother) LateEffects.Born(mother, baby);
        }
        catch (System.Exception error)
        {
            // A birth must never fail on our account: the child is born either way.
            if (!warned) Log.Error(Phobos.Medical.MedicalMod.LogPrefix + " a newborn's radiation could not be worked out: " + error);
            warned = true;
        }
    }
}
