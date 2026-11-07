using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework.Gases;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// What breathing smoke does: lasting lung injury, the acute smoke condition when Combat Extended's is not running, and carbon
/// monoxide from the framework's room gas (or inferred from smoke in an enclosed space when that gas is switched off). Smoke
/// itself is read from the framework, which reuses Combat Extended's condition or the game's smoke.
/// </summary>
public static class SmokeExposureService
{
    public const string CarbonMonoxideGasDefName = "PhobosGas_CarbonMonoxide";

    static bool lookedForGas;
    static PhobosGasDef? carbonMonoxideGas;

    public static PhobosGasDef? CarbonMonoxideGas
    {
        get
        {
            if (!lookedForGas)
            {
                lookedForGas = true;
                carbonMonoxideGas = DefDatabase<PhobosGasDef>.GetNamedSilentFail(CarbonMonoxideGasDefName);
            }
            return carbonMonoxideGas;
        }
    }

    public static GasSource CarbonMonoxideSource => CarbonMonoxideGas == null ? GasSource.None : PhobosGases.Source(CarbonMonoxideGas);

    public static string DescribeCarbonMonoxideSource() =>
        CarbonMonoxideSource == GasSource.Own ? "from the framework's room gas" : "inferred from smoke in enclosed rooms";

    /// <summary>True when the pawn stands in anything worth a recompute.</summary>
    public static bool Exposed(Pawn pawn)
    {
        if (!pawn.Spawned) return false;
        if (PhobosGases.Breathed(pawn, GasKind.Smoke) > 0f) return true;
        PhobosGasDef? co = CarbonMonoxideGas;
        return co != null && PhobosGases.Breathed(pawn, co) > 0f;
    }

    public static void Apply(Pawn pawn, PhobosBurnRulesDef rules, BurnsAndSmokeSettings settings, float hours)
    {
        if (!pawn.Spawned) return;
        float breathedSmoke = PhobosGases.Breathed(pawn, GasKind.Smoke);
        if (settings.smoke && breathedSmoke > 0f)
        {
            // While the pawn is itself on fire, More Injuries' fire inhalation (when the player has it on) burns the lungs; ours
            // stands down for that moment so the lungs are never damaged twice for one exposure.
            if (!(pawn.IsBurning() && MoreInjuriesInhalation.On))
                Raise(pawn, BurnsDefOf.PhobosBurns_LungInjury, rules.lungInjuryPerHourAtFullSmoke * breathedSmoke * hours, 1f);
            if (!PhobosGases.UsesCombatExtendedSmoke || settings.acuteSmokeWithCombatExtended)
            {
                float checks = hours * 2500f / 50f;
                Raise(pawn, BurnsDefOf.PhobosBurns_SmokeInhalation, rules.acuteSmokePerCheck * breathedSmoke * checks, 1f);
            }
        }
        if (!settings.carbonMonoxide) return;
        float breathedCo = BreathedCarbonMonoxide(pawn, breathedSmoke);
        if (breathedCo <= 0f) return;
        Hediff? co = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_CarbonMonoxide);
        float uptake = CarbonMonoxideRule.Uptake(breathedCo, hours, rules.coUptakePerHourAtFullGas, co?.Severity ?? 0f, rules.coCap);
        if (uptake > 0f) Raise(pawn, BurnsDefOf.PhobosBurns_CarbonMonoxide, uptake, rules.coCap);
    }

    /// <summary>Carbon monoxide in the air at a cell, unfiltered (what an alarm there would sense): the framework's gas where it is
    /// carried, otherwise inferred from the cell's smoke and how enclosed it is. Zero while carbon monoxide is switched off.</summary>
    public static float CarbonMonoxideAt(IntVec3 cell, Map map)
    {
        if (!BurnsAndSmokeMod.Settings.carbonMonoxide) return 0f;
        PhobosGasDef? gas = CarbonMonoxideGas;
        if (gas != null && PhobosGases.Source(gas) == GasSource.Own) return PhobosGases.Concentration(cell, map, gas);
        return CarbonMonoxideRule.InferredBreathed(PhobosGases.Density(cell, map, GasKind.Smoke), PhobosGases.Enclosure(cell, map));
    }

    /// <summary>The framework's carbon monoxide where it is carried; otherwise smoke breathed in an enclosed space stands in.</summary>
    public static float BreathedCarbonMonoxide(Pawn pawn, float breathedSmoke)
    {
        PhobosGasDef? gas = CarbonMonoxideGas;
        if (gas != null && PhobosGases.Source(gas) == GasSource.Own) return PhobosGases.Breathed(pawn, gas);
        if (pawn.Map == null) return 0f;
        return CarbonMonoxideRule.InferredBreathed(breathedSmoke, PhobosGases.Enclosure(pawn.Position, pawn.Map));
    }

    static void Raise(Pawn pawn, HediffDef def, float amount, float cap)
    {
        if (amount <= 0f) return;
        Hediff? hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
        if (hediff == null)
        {
            hediff = HediffMaker.MakeHediff(def, pawn);
            hediff.Severity = amount > cap ? cap : amount;
            pawn.health.AddHediff(hediff);
            return;
        }
        float next = hediff.Severity + amount;
        hediff.Severity = next > cap ? cap : next;
    }
}
