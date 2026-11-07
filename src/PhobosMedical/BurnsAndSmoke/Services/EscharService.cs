using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Forms a constricting eschar on a limb burned deep enough (see <see cref="EscharRule"/>), and lets it go once the burns on that limb
/// have healed below the mark (agent choice: the crust softens as the skin heals). Run on each burns pass. A limb is a leg or an arm,
/// found by the body part's tags (moving-limb core, manipulation-limb segment), so any race's limbs count without naming them.
/// </summary>
public static class EscharService
{
    static readonly Dictionary<BodyPartRecord, float> burnOnLimb = new();
    static readonly List<Hediff> toRemove = new();

    public static void Apply(Pawn pawn, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings)
    {
        if (!settings.eschar) return;
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        burnOnLimb.Clear();
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff is not Hediff_Injury || hediff.Part == null || hediff.IsPermanent() || !BurnLoadService.IsBurnLike(hediff.def) || !IsLimb(hediff.Part)) continue;
            burnOnLimb.TryGetValue(hediff.Part, out float damage);
            burnOnLimb[hediff.Part] = damage + hediff.Severity;
        }
        toRemove.Clear();
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff.def != BurnsDefOf.PhobosBurns_Eschar || hediff.Part == null) continue;
            burnOnLimb.TryGetValue(hediff.Part, out float damage);
            if (!EscharRule.Forms(damage, hediff.Part.def.GetMaxHealth(pawn), rules.escharPartHealthFraction)) toRemove.Add(hediff);
            burnOnLimb.Remove(hediff.Part);
        }
        foreach (Hediff softened in toRemove) pawn.health.RemoveHediff(softened);
        toRemove.Clear();
        foreach (KeyValuePair<BodyPartRecord, float> limb in burnOnLimb)
        {
            if (EscharRule.Forms(limb.Value, limb.Key.def.GetMaxHealth(pawn), rules.escharPartHealthFraction))
                pawn.health.AddHediff(BurnsDefOf.PhobosBurns_Eschar, limb.Key);
        }
        burnOnLimb.Clear();
    }

    public static bool IsLimb(BodyPartRecord part)
    {
        List<BodyPartTagDef> tags = part.def.tags;
        return tags.Contains(BodyPartTagDefOf.MovingLimbCore) || tags.Contains(BodyPartTagDefOf.ManipulationLimbSegment);
    }
}
