using HarmonyLib;
using RimWorld;

namespace Phobos.Framework.Skills;

/// <summary>
/// Learning faster while someone helps (Framework 0.336.0; the Elmer, docs/development/radio-advice-and-relays.md, the owner's card of 27 Nov
/// 2026: half again). A prefix on the game's <see cref="SkillRecord.Learn"/> multiplies a gain of experience by
/// <see cref="SkillLifts.LearnFactor"/> for a person a source is teaching; it never skips the original, so the game's learn rate, passions
/// and daily saturation still apply after it. Losses of experience are left alone.
/// <para>Cost: one count check per gain of experience while nobody is taught; one dictionary lookup while someone is.</para>
/// </summary>
[HarmonyPatch(typeof(SkillRecord), nameof(SkillRecord.Learn))]
public static class LearnWhileHelped
{
    public static void Prefix(SkillRecord __instance, ref float xp)
    {
        if (xp <= 0f) return;
        float factor = SkillLifts.LearnFactor(__instance.Pawn, __instance.def);
        if (factor > 1f) xp *= factor;
    }
}
