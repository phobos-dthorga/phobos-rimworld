using HarmonyLib;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Hygiene;

/// <summary>
/// Clean people catch worms less (D'thorga Medical 0.35.0; docs/development/medical-care-round-two.md, slice 3, the owner's card of 27
/// Nov 2026: hygiene that prevents, "small, as proposed"). The game's chance of a disease taking hold, which its disease events and
/// quests roll against, is cut in step with Dubs Bad Hygiene's hygiene need, by up to the rules Def's share at full hygiene, for a
/// disease of the worms class only. Food poisoning stays Dubs Bad Hygiene's own, through the cook's hygiene. A postfix that only ever
/// lowers the game's answer; the game has no extension point for what lowers the chance of catching something.
/// </summary>
[HarmonyPatch(typeof(ImmunityHandler), nameof(ImmunityHandler.DiseaseContractChanceFactor), new[] { typeof(HediffDef), typeof(HediffDef), typeof(BodyPartRecord) },
    new[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal })]
public static class CleanAgainstWormsPatch
{
    /// <summary>Dubs Bad Hygiene's hygiene need, by its Def's name; null without it, when nothing changes.</summary>
    static readonly NeedDef? Hygiene = DefDatabase<NeedDef>.GetNamedSilentFail("Hygiene");

    public static void Postfix(ImmunityHandler __instance, HediffDef diseaseDef, ref float __result)
    {
        if (__result <= 0f || Hygiene == null || diseaseDef == null) return;
        PhobosDiseaseClassDef? diseaseClass = DiseaseClasses.Of(diseaseDef);
        if (diseaseClass == null || diseaseClass != PhobosDiseaseClassDefOf.PhobosDisease_Worms) return;
        Need? need = __instance.pawn?.needs?.TryGetNeed(Hygiene);
        if (need == null) return;
        __result *= PrimedImmunityRule.CleanChanceFactor(need.CurLevelPercentage, Dosing.Defs.PhobosDosingRulesDef.Current.hygieneWormCut);
        if (Medical.Seen.Wants("medical.clean-against-worms", __instance.pawn))
            Medical.Seen.Note("medical.clean-against-worms", __instance.pawn, diseaseDef.defName + " at hygiene " + need.CurLevelPercentage.ToStringPercent());
    }
}
