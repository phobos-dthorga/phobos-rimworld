using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Comps;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework.Gases;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// The upper airway: a pawn that burns, or takes burn damage to the head or neck, may get an airway burn whose swelling builds
/// over hours. Fresh head and neck burn damage is noted as burns are added (the giver's hook) and consumed at the next pass.
/// </summary>
public static class AirwayService
{
    static readonly Dictionary<Pawn, float> freshHeadNeckDamage = new();
    static BodyPartDef? neck;

    /// <summary>Called for every hediff the game adds to a pawn; notes burn damage on the head group or the neck.</summary>
    public static void NoteHediffAdded(Pawn pawn, Hediff hediff)
    {
        if (hediff is not Hediff_Injury || hediff.Part == null || !BurnLoadService.IsBurnLike(hediff.def)) return;
        if (!IsHeadOrNeck(hediff.Part)) return;
        freshHeadNeckDamage.TryGetValue(pawn, out float damage);
        freshHeadNeckDamage[pawn] = damage + hediff.Severity;
    }

    static bool IsHeadOrNeck(BodyPartRecord part)
    {
        neck ??= DefDatabase<BodyPartDef>.GetNamedSilentFail("Neck");
        if (neck != null && part.def == neck) return true;
        return part.IsInGroup(BodyPartGroupDefOf.FullHead) || part.IsInGroup(BodyPartGroupDefOf.UpperHead);
    }

    public static void Apply(Pawn pawn, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings)
    {
        float fresh = 0f;
        if (freshHeadNeckDamage.TryGetValue(pawn, out float damage))
        {
            fresh = damage;
            freshHeadNeckDamage.Remove(pawn);
        }
        if (!settings.airway || pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_AirwayBurn)) return;
        bool onFire = pawn.IsBurning();
        float chance = AirwayRule.OnsetChance(onFire, fresh, rules.airwayBaseChance, rules.airwayReferenceDamage, rules.airwayChanceAtReferenceDamage);
        if (chance <= 0f || !Rand.Chance(chance)) return;
        float exposure = onFire ? 1f : fresh / rules.airwayReferenceDamage;
        float smoke = pawn.Spawned ? PhobosGases.Breathed(pawn, GasKind.Smoke) : 0f;
        if (smoke > exposure) exposure = smoke;
        Start(pawn, exposure);
    }

    /// <summary>Only keeps the condition alive until its comp sets the real severity on its first check; not a tuning figure.</summary>
    const float StartingSeverity = 0.001f;

    public static void Start(Pawn pawn, float exposure)
    {
        BodyPartRecord? part = NeckOf(pawn);
        Hediff hediff = HediffMaker.MakeHediff(BurnsDefOf.PhobosBurns_AirwayBurn, pawn, part);
        hediff.Severity = StartingSeverity;
        var comp = hediff.TryGetComp<HediffComp_PhobosRiseThenFall>();
        if (comp != null) comp.peak = AirwayRule.Peak(exposure, PhobosBurnRulesDef.Current.airwayLightestPeak);
        pawn.health.AddHediff(hediff, part);
    }

    static BodyPartRecord? NeckOf(Pawn pawn)
    {
        neck ??= DefDatabase<BodyPartDef>.GetNamedSilentFail("Neck");
        if (neck == null) return null;
        foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts())
        {
            if (part.def == neck) return part;
        }
        return null;
    }

    /// <summary>Drops notes for pawns that no longer exist, so the dictionary cannot grow without bound.</summary>
    public static void Forget(Pawn pawn) => freshHeadNeckDamage.Remove(pawn);
}
