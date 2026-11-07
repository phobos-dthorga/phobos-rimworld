using Phobos.Dosing.Defs;
using Phobos.Dosing.Rules;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>How much a patient's fussing lowers the quality of a tend. The Harmony postfix only calls this.</summary>
public static class FussingService
{
    public static float TendFactor(Pawn? patient)
    {
        if (patient?.health?.hediffSet == null || !DosingMod.Settings.fussing || patient.Dead) return 1f;
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        return FussingRule.Factor(patient.health.hediffSet.PainTotal, patient.Awake(), rules.fussingPainThreshold, rules.fussingFactorAtThreshold, rules.fussingFactorAtFullPain);
    }
}
