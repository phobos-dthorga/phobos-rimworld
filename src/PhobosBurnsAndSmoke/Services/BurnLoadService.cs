using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>What the burn shock tooltip shows; computed the same way the service does, never stored.</summary>
public readonly struct BurnShockReadout
{
    public readonly float AgeYears;
    public readonly float Tbsa;
    public readonly bool InhalationInjury;
    public readonly float TendedFraction;
    public readonly bool InBed;
    public readonly float Baux;
    public readonly float Target;

    public BurnShockReadout(float ageYears, float tbsa, bool inhalationInjury, float tendedFraction, bool inBed, float baux, float target)
    {
        AgeYears = ageYears;
        Tbsa = tbsa;
        InhalationInjury = inhalationInjury;
        TendedFraction = tendedFraction;
        InBed = inBed;
        Baux = baux;
        Target = target;
    }
}

/// <summary>
/// Reads a pawn's burns and writes the two derived conditions: burn load (how much of the body is burned) and burn shock
/// (how dangerous that is, by the revised Baux score and the care given). Both are recomputed every pass and never trusted
/// from the save.
/// </summary>
public static class BurnLoadService
{
    static readonly Dictionary<HediffDef, PhobosBurnLikeExtension?> burnLike = new();
    static readonly Dictionary<BodyPartRecord, PartTally> tallies = new();
    static readonly List<BurnedPart> parts = new();

    sealed class PartTally
    {
        public float severity;
        public bool allTended = true;
    }

    public static bool IsBurnLike(HediffDef def)
    {
        if (!burnLike.TryGetValue(def, out PhobosBurnLikeExtension? extension))
        {
            extension = def.GetModExtension<PhobosBurnLikeExtension>();
            burnLike[def] = extension;
        }
        return extension != null;
    }

    static float Weight(HediffDef def) => burnLike.TryGetValue(def, out PhobosBurnLikeExtension? extension) && extension != null ? extension.tbsaWeight : 1f;

    /// <summary>A burn on the skin, or a part burned off (the game records the injury that destroyed a part).</summary>
    public static bool HasBurn(Pawn pawn)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (hediff is Hediff_Injury && IsBurnLike(hediff.def)) return true;
            if (hediff is Hediff_MissingPart missing && missing.lastInjury != null && IsBurnLike(missing.lastInjury)) return true;
        }
        return false;
    }

    /// <summary>
    /// The pawn's burned fraction and tended share: the burns on its outside parts, plus every part burned off (its whole
    /// subtree's skin while the stump is fresh, fading as it heals). A part under another missing part is skipped: the engine
    /// gives each child of a lost part its own record.
    /// </summary>
    public static BurnLoad Load(Pawn pawn)
    {
        tallies.Clear();
        parts.Clear();
        HediffSet set = pawn.health.hediffSet;
        List<Hediff> hediffs = set.hediffs;
        int healTicks = (int)(PhobosBurnRulesDef.Current.burnedOffHealDays * 60000f);
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i] is Hediff_MissingPart missing)
            {
                if (missing.Part == null || missing.lastInjury == null || !IsBurnLike(missing.lastInjury)) continue;
                if (missing.Part.parent != null && set.PartIsMissing(missing.Part.parent)) continue;
                float weight = BurnLoadRule.BurnedOffWeight(missing.IsFresh, missing.ageTicks, healTicks) * Weight(missing.lastInjury);
                if (weight > 0f) parts.Add(BurnedPart.Lost(SkinShares.WithChildren(missing.Part), weight, !missing.IsFresh));
                continue;
            }
            if (hediffs[i] is not Hediff_Injury injury || injury.Part == null || injury.Part.depth != BodyPartDepth.Outside || !IsBurnLike(injury.def)) continue;
            if (!tallies.TryGetValue(injury.Part, out PartTally? tally))
            {
                tally = new PartTally();
                tallies[injury.Part] = tally;
            }
            tally.severity += injury.Severity * Weight(injury.def);
            if (!injury.IsTended()) tally.allTended = false;
        }
        foreach (KeyValuePair<BodyPartRecord, PartTally> pair in tallies)
        {
            parts.Add(new BurnedPart(pair.Value.severity, pair.Key.def.GetMaxHealth(pawn), SkinShares.For(pair.Key), pair.Value.allTended));
        }
        return BurnLoadRule.Compute(parts);
    }

    public static void Recompute(Pawn pawn, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings, float days)
    {
        BurnLoad load = Load(pawn);
        WriteBurnLoad(pawn, load, rules, settings);
        WriteBurnShock(pawn, load, rules, settings, days);
    }

    static void WriteBurnLoad(Pawn pawn, BurnLoad load, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings)
    {
        Hediff? existing = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_BurnLoad);
        bool show = settings.burnLoad && load.Tbsa >= rules.burnLoadVisibleTbsa;
        if (!show)
        {
            if (existing != null) pawn.health.RemoveHediff(existing);
            return;
        }
        if (existing == null)
        {
            existing = HediffMaker.MakeHediff(BurnsDefOf.PhobosBurns_BurnLoad, pawn);
            existing.Severity = load.Tbsa;
            pawn.health.AddHediff(existing);
        }
        else
        {
            existing.Severity = load.Tbsa;
        }
    }

    static void WriteBurnShock(Pawn pawn, BurnLoad load, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings, float days)
    {
        Hediff? shock = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_BurnShock);
        if (!settings.burnShock)
        {
            if (shock != null) pawn.health.RemoveHediff(shock);
            return;
        }
        BurnShockReadout readout = Readout(pawn, load, rules);
        if (shock == null)
        {
            if (load.Tbsa < rules.shockStartTbsa || readout.Target <= 0f) return;
            shock = HediffMaker.MakeHediff(BurnsDefOf.PhobosBurns_BurnShock, pawn);
            shock.Severity = 0.01f;
            pawn.health.AddHediff(shock);
        }
        float next = BurnShockRule.Step(shock.Severity, readout.Target, days, rules.shockRisePerDay, rules.shockFallPerDay);
        if (load.Tbsa < rules.shockStartTbsa && next < 0.02f)
        {
            pawn.health.RemoveHediff(shock);
            return;
        }
        shock.Severity = next;
    }

    public static BurnShockReadout Readout(Pawn pawn) => Readout(pawn, Load(pawn), PhobosBurnRulesDef.Current);

    static BurnShockReadout Readout(Pawn pawn, BurnLoad load, PhobosBurnRulesDef rules)
    {
        float age = pawn.ageTracker.AgeBiologicalYearsFloat;
        bool inhalation = pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_AirwayBurn)
            || (pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_CarbonMonoxide)?.Severity ?? 0f) >= rules.inhalationCarbonMonoxideThreshold;
        bool inBed = pawn.InBed();
        float baux = BurnShockRule.Baux(age, load.Tbsa, inhalation, rules.inhalationBauxPoints);
        float target = BurnShockRule.Target(baux, rules.bauxNoDeathScore, rules.bauxNoSurvivorScore)
            * BurnShockRule.TreatmentFactor(load.TendedFraction, inBed, rules.tendedShockRelief, rules.bedRestShockRelief, rules.shockTreatmentFloor);
        target = BurnShockRule.WithThresholdFloor(target, load.Tbsa, rules.shockStartTbsa, rules.shockThresholdFloor);
        return new BurnShockReadout(age, load.Tbsa, inhalation, load.TendedFraction, inBed, baux, target);
    }
}
