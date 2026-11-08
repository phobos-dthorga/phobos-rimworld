using System;
using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Comps;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework.Gases;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// The caustic gas model (owner's choices of 5 Nov 2026: two models behind one setting, slow-acting by default; the effects live in the
/// burns part). On each burns pass a pawn in tox gas gains three exposures, and the dose's delayed tracks act: eyes, throat (the airway
/// burn), lungs (the lung injury), skin (the game's chemical burn), weakened immunity and irritable airways. The game's own irritation and
/// toxic buildup are untouched. Every figure is the rules Def's, authored.
/// </summary>
public static class CausticService
{
    const float TicksPerHour = 2500f;
    const float Trace = 0.01f;

    /// <summary>True when the pawn stands in tox gas: worth a burns pass.</summary>
    public static bool InToxGas(Pawn pawn) =>
        BurnsAndSmokeMod.Settings.causticGas && pawn.Spawned && pawn.Map != null && PhobosGases.Density(pawn.Position, pawn.Map, GasKind.Toxic) > 0f;

    public static void Apply(Pawn pawn, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings, int intervalTicks)
    {
        if (!settings.causticGas || pawn.Dead) return;
        float hours = intervalTicks / TicksPerHour;
        bool fast = settings.causticFastActing;
        var dose = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_CausticExposure) as Hediff_PhobosCausticExposure;
        float density = pawn.Spawned && pawn.Map != null ? PhobosGases.Density(pawn.Position, pawn.Map, GasKind.Toxic) : 0f;
        bool exposed = density > 0f;
        float clothes = settings.causticStripAndRinse ? ClothesExposure(pawn, rules) : 0f;
        if (dose == null)
        {
            if (!exposed && clothes <= 0f) return;
            dose = (Hediff_PhobosCausticExposure)pawn.health.AddHediff(BurnsDefOf.PhobosBurns_CausticExposure);
        }
        if (exposed) Expose(pawn, dose, density, rules, fast, hours);
        if (exposed && settings.causticStripAndRinse) ContaminateWorn(pawn, density, rules, hours);
        if (clothes > 0f) dose.skinDose = CausticRule.AddDose(dose.skinDose, clothes, rules.causticDosePerHourAtFull, hours);
        dose.ticksSinceFirst += intervalTicks;
        dose.ticksSinceExposed = exposed ? 0 : dose.ticksSinceExposed + intervalTicks;
        float elapsed = dose.ticksSinceFirst / TicksPerHour;

        if (settings.causticEyes) Eyes(pawn, dose, rules, fast, exposed, elapsed);
        if (settings.causticThroat && settings.airway) Throat(pawn, dose, rules, fast, elapsed);
        if (settings.causticLungs && settings.smoke) Lungs(pawn, dose, rules, fast, elapsed);
        if (settings.causticSkin) Skin(pawn, dose, rules, fast, elapsed, hours);
        if (settings.causticImmunity && !fast) Immunity(pawn, dose, rules, elapsed);
        if (settings.causticAftermath) Aftermath(pawn, dose, rules);

        if (!exposed && (fast || elapsed >= rules.causticSlowOnsetHours))
        {
            float days = hours / 24f;
            dose.eyeDose = CausticRule.Fade(dose.eyeDose, rules.causticDoseFadePerDay, days);
            dose.lungDose = CausticRule.Fade(dose.lungDose, rules.causticDoseFadePerDay, days);
            dose.skinDose = CausticRule.Fade(dose.skinDose, rules.causticDoseFadePerDay, days);
        }
        float max = dose.MaxDose;
        bool tracksDone = elapsed >= LastTrackHour(dose, rules, fast);
        dose.Severity = max < Trace && tracksDone ? 0f : (max < Trace ? Trace : max);
    }

    static void Expose(Pawn pawn, Hediff_PhobosCausticExposure dose, float density, PhobosBurnRulesDef rules, bool fast, float hours)
    {
        float eyes = GasUtility.IsAffectedByExposure(pawn) && (!fast || density >= rules.causticFastEyeDensity) ? density : 0f;
        float lungs = PhobosGases.Breathed(pawn, GasKind.Toxic);
        float skin = fast
            ? (density >= rules.causticFastSkinDensity ? density * CausticRule.SkinFactor(SealedSuit(pawn), CoveredShare(pawn), 0f) : 0f)
            : density * CausticRule.SkinFactor(SealedSuit(pawn), CoveredShare(pawn), rules.causticCoveredSkinFactor);
        dose.eyeDose = CausticRule.AddDose(dose.eyeDose, eyes, rules.causticDosePerHourAtFull, hours);
        dose.lungDose = CausticRule.AddDose(dose.lungDose, lungs, rules.causticDosePerHourAtFull, hours);
        dose.skinDose = CausticRule.AddDose(dose.skinDose, skin, rules.causticDosePerHourAtFull, hours);
        if (dose.eyeDose > dose.eyePeak) dose.eyePeak = dose.eyeDose;
        if (dose.lungDose > dose.lungPeak) dose.lungPeak = dose.lungDose;
    }

    static void Eyes(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, bool fast, bool exposed, float elapsed)
    {
        bool rising = fast ? exposed : elapsed <= rules.causticEyePeakHours + 1f;
        if (!rising || dose.eyePeak <= 0f) return;
        float ramp = fast ? 1f : CausticRule.Ramp(elapsed, rules.causticSlowOnsetHours, rules.causticEyePeakHours);
        float target = dose.eyePeak * rules.causticEyeSeverityPerDose * ramp;
        if (target <= 0f) return;
        foreach (BodyPartRecord eye in pawn.health.hediffSet.GetNotMissingParts())
        {
            if (!eye.def.tags.Contains(BodyPartTagDefOf.SightSource)) continue;
            Hediff? injury = FirstOn(pawn, BurnsDefOf.PhobosBurns_CausticEye, eye);
            if (injury == null) pawn.health.AddHediff(Make(pawn, BurnsDefOf.PhobosBurns_CausticEye, eye, target), eye);
            else if (injury.Severity < target) injury.Severity = target;
        }
    }

    static void Throat(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, bool fast, float elapsed)
    {
        if (dose.throatRolled) return;
        bool due = fast ? dose.lungPeak >= rules.causticLightDose : elapsed >= rules.causticSlowOnsetHours;
        if (!due) return;
        dose.throatRolled = true;
        if (pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_AirwayBurn)) return;
        float chance = dose.lungPeak * rules.causticThroatChancePerDose;
        if (chance > 0f && Rand.Chance(chance > 1f ? 1f : chance)) AirwayService.Start(pawn, dose.lungPeak);
    }

    static void Lungs(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, bool fast, float elapsed)
    {
        float onset = fast ? CausticRule.FastLungOnsetHours(dose.lungPeak, rules.causticFastLungLatestHours, rules.causticFastLungSoonestHours) : rules.causticSlowOnsetHours;
        float peak = fast ? onset + rules.causticFastLungRiseHours : rules.causticSlowLungPeakHours;
        float target = dose.lungPeak * rules.causticLungInjuryPerDose * CausticRule.Ramp(elapsed, onset, peak);
        float add = target - dose.lungGiven;
        if (add <= 0f) return;
        dose.lungGiven = target;
        Hediff? lungs = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_LungInjury);
        if (lungs == null) pawn.health.AddHediff(Make(pawn, BurnsDefOf.PhobosBurns_LungInjury, null, add > 1f ? 1f : add));
        else lungs.Severity = lungs.Severity + add > 1f ? 1f : lungs.Severity + add;
    }

    static void Skin(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, bool fast, float elapsed, float hours)
    {
        if (dose.skinDose < Trace || (!fast && elapsed < rules.causticSlowOnsetHours)) return;
        dose.skinPending += dose.skinDose * rules.causticSkinBurnPerHourAtFull * hours;
        while (dose.skinPending >= 1f && !pawn.Dead)
        {
            dose.skinPending -= 1f;
            BodyPartRecord? part = pawn.health.hediffSet.GetRandomNotMissingPart(BurnsDefOf.PhobosBurns_CausticSkin, BodyPartHeight.Undefined, BodyPartDepth.Outside);
            if (part == null) return;
            var dinfo = new DamageInfo(BurnsDefOf.PhobosBurns_CausticSkin, 1f, hitPart: part);
            dinfo.SetIgnoreArmor(true);
            dinfo.SetAllowDamagePropagation(false);
            pawn.TakeDamage(dinfo);
        }
    }

    static void Immunity(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, float elapsed)
    {
        if (dose.immunityRolled || elapsed < rules.causticImmunityOnsetHours) return;
        dose.immunityRolled = true;
        float peakDose = dose.lungPeak > dose.eyePeak ? dose.lungPeak : dose.eyePeak;
        float severity = CausticRule.Weakness(peakDose, rules.causticHeavyDose, rules.causticMassiveDose, rules.causticWeaknessMild, rules.causticWeaknessSevere);
        if (severity > 0f && !pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_CausticWeakness))
            pawn.health.AddHediff(Make(pawn, BurnsDefOf.PhobosBurns_CausticWeakness, null, severity));
    }

    static void Aftermath(Pawn pawn, Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules)
    {
        if (dose.aftermathRolled) return;
        float lungs = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_LungInjury)?.Severity ?? 0f;
        if (lungs < rules.causticAftermathLungSeverity) return;
        dose.aftermathRolled = true;
        bool critical = lungs >= rules.causticAftermathCriticalLungSeverity;
        HediffDef def = critical && Rand.Chance(rules.causticAftermathLastingChance) ? BurnsDefOf.PhobosBurns_IrritableAirwaysLasting : BurnsDefOf.PhobosBurns_IrritableAirways;
        if (!pawn.health.hediffSet.HasHediff(def)) pawn.health.AddHediff(Make(pawn, def, null, critical ? 0.6f : 0.3f));
    }

    /// <summary>Rinsing at a safety shower washes the gas off the skin and out of the clothes: the skin dose goes.</summary>
    public static void Decontaminate(Pawn pawn)
    {
        if (pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_CausticExposure) is Hediff_PhobosCausticExposure dose)
        {
            dose.skinDose = 0f;
            dose.skinPending = 0f;
        }
    }

    // ---- Contaminated clothing (strip and rinse, a setting) and washing elsewhere (Dubs Bad Hygiene) ----

    /// <summary>Ways other mods tell us a pawn is washing (Dubs Bad Hygiene's shower or bath): each says whether the pawn is washing
    /// its whole body now. Registered by tie-ins at startup.</summary>
    public static readonly List<Func<Pawn, bool>> WashingProbes = new();

    public static bool WashingElsewhere(Pawn pawn)
    {
        for (int i = 0; i < WashingProbes.Count; i++)
        {
            try
            {
                if (WashingProbes[i](pawn)) return true;
            }
            catch (Exception)
            {
                // A tie-in's probe must never break the burns pass.
            }
        }
        return false;
    }

    /// <summary>Called on every burns pass: a pawn washing elsewhere is decontaminated and rinsed as at a safety shower.</summary>
    public static void CheckWashing(Pawn pawn, BurnsAndSmokeSettings settings)
    {
        if (WashingProbes.Count == 0 || !pawn.Spawned) return;
        bool needs = ChemicalService.NeedsRinse(pawn)
            || (pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_CausticExposure) is Hediff_PhobosCausticExposure dose && dose.skinDose > 0f);
        if (!needs || !WashingElsewhere(pawn)) return;
        if (settings.causticStripAndRinse) StripContaminated(pawn);
        ChemicalService.Rinse(pawn);
    }

    static void ContaminateWorn(Pawn pawn, float density, PhobosBurnRulesDef rules, float hours)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null || SealedSuit(pawn)) return;
        for (int i = 0; i < worn.Count; i++)
        {
            worn[i].TryGetComp<CompPhobosContamination>()?.Add(density * rules.causticClothesPerHourAtFull * hours);
        }
    }

    /// <summary>How much the worn clothes dose the skin, as a gas density: each garment's residue times the share of the skin it covers.</summary>
    static float ClothesExposure(Pawn pawn, PhobosBurnRulesDef rules)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return 0f;
        float total = 0f;
        for (int i = 0; i < worn.Count; i++)
        {
            CompPhobosContamination? comp = worn[i].TryGetComp<CompPhobosContamination>();
            if (comp == null || !comp.Contaminated) continue;
            total += comp.Level * CoveredShare(pawn, worn[i]);
        }
        total *= rules.causticClothesDoseFactor;
        return total > 1f ? 1f : total;
    }

    public static bool WearsContaminated(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null) return false;
        for (int i = 0; i < worn.Count; i++)
        {
            if (worn[i].TryGetComp<CompPhobosContamination>()?.Contaminated == true) return true;
        }
        return false;
    }

    /// <summary>Strip and rinse: takes off every contaminated garment and drops it, forbidden, where the pawn stands. Says so.</summary>
    public static void StripContaminated(Pawn pawn)
    {
        Pawn_ApparelTracker? tracker = pawn.apparel;
        List<Apparel>? worn = tracker?.WornApparel;
        if (tracker == null || worn == null || !pawn.Spawned) return;
        var stripped = new List<Apparel>();
        for (int i = 0; i < worn.Count; i++)
        {
            if (worn[i].TryGetComp<CompPhobosContamination>()?.Contaminated == true) stripped.Add(worn[i]);
        }
        foreach (Apparel apparel in stripped)
        {
            tracker.TryDrop(apparel, out Apparel _, pawn.PositionHeld, forbid: true);
        }
        if (stripped.Count > 0 && pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosBurns_MsgStripped".Translate(pawn.LabelShort.Named("PAWN"), stripped.Count.Named("COUNT")), pawn, MessageTypeDefOf.NeutralEvent);
        }
    }

    /// <summary>The share of the skin one garment covers.</summary>
    static float CoveredShare(Pawn pawn, Apparel apparel)
    {
        float covered = 0f;
        foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside))
        {
            if (apparel.def.apparel.CoversBodyPart(part)) covered += SkinShares.For(part);
        }
        return covered > 1f ? 1f : covered;
    }

    /// <summary>Hours until the delayed harm begins, for the tooltip; 0 once it has.</summary>
    public static float HoursToOnset(Hediff_PhobosCausticExposure dose)
    {
        if (BurnsAndSmokeMod.Settings.causticFastActing) return 0f;
        float left = PhobosBurnRulesDef.Current.causticSlowOnsetHours - dose.ticksSinceFirst / TicksPerHour;
        return left > 0f ? left : 0f;
    }

    /// <summary>The hour by which every delayed track has done what it will, so a spent dose can go.</summary>
    static float LastTrackHour(Hediff_PhobosCausticExposure dose, PhobosBurnRulesDef rules, bool fast)
    {
        if (fast) return CausticRule.FastLungOnsetHours(dose.lungPeak, rules.causticFastLungLatestHours, rules.causticFastLungSoonestHours) + rules.causticFastLungRiseHours;
        return rules.causticSlowLungPeakHours > rules.causticImmunityOnsetHours ? rules.causticSlowLungPeakHours : rules.causticImmunityOnsetHours;
    }

    /// <summary>Any worn item that keeps out the environment completely (toxic environment resistance of 0.999 or more).</summary>
    static bool SealedSuit(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null) return false;
        for (int i = 0; i < worn.Count; i++)
        {
            List<StatModifier>? offsets = worn[i].def.equippedStatOffsets;
            if (offsets == null) continue;
            for (int j = 0; j < offsets.Count; j++)
            {
                if (offsets[j].stat == StatDefOf.ToxicEnvironmentResistance && offsets[j].value >= 0.999f) return true;
            }
        }
        return false;
    }

    /// <summary>The share of the skin that worn clothes cover, by each outside part's share of the skin.</summary>
    static float CoveredShare(Pawn pawn)
    {
        List<Apparel>? worn = pawn.apparel?.WornApparel;
        if (worn == null || worn.Count == 0) return 0f;
        float covered = 0f;
        foreach (BodyPartRecord part in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Outside))
        {
            for (int i = 0; i < worn.Count; i++)
            {
                if (!worn[i].def.apparel.CoversBodyPart(part)) continue;
                covered += SkinShares.For(part);
                break;
            }
        }
        return covered > 1f ? 1f : covered;
    }

    static Hediff? FirstOn(Pawn pawn, HediffDef def, BodyPartRecord part)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def == def && hediffs[i].Part == part) return hediffs[i];
        }
        return null;
    }

    static Hediff Make(Pawn pawn, HediffDef def, BodyPartRecord? part, float severity)
    {
        Hediff hediff = HediffMaker.MakeHediff(def, pawn, part);
        hediff.Severity = severity;
        return hediff;
    }
}
