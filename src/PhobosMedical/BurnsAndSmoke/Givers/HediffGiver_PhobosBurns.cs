using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Givers;

/// <summary>
/// The one engine hook: the game calls every hediff giver in a race's giver sets every 60 ticks per pawn, and tells it about
/// every condition added. This one is patched into the OrganicStandard set, so it reaches every flesh race, and works only on
/// humanlike pawns that have burns, are burning, carry one of our conditions, or stand in smoke or carbon monoxide: everyone
/// else costs a few comparisons. Every pass recomputes the burn load and shock, applies smoke and carbon monoxide, rolls the
/// airway burn, and forms or releases eschar on deeply burned limbs.
/// </summary>
public class HediffGiver_PhobosBurns : HediffGiver
{
    const int GiverIntervalTicks = 60;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (pawn.Dead || !pawn.RaceProps.Humanlike || pawn.health?.hediffSet == null) return;
        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
        int interval = rules.recomputeIntervalTicks < GiverIntervalTicks ? GiverIntervalTicks : rules.recomputeIntervalTicks;
        // The game calls this once per 60 ticks, on a tick near the pawn's hash phase; one call in each interval lands here.
        if ((Find.TickManager.TicksGame + pawn.HashOffset()) % interval >= GiverIntervalTicks) return;
        if (!Interesting(pawn)) return;
        BurnsAndSmokeSettings settings = BurnsAndSmokeMod.Settings;
        float days = interval / 60000f;
        float hours = interval / 2500f;
        BurnLoadService.Recompute(pawn, rules, settings, days);
        SmokeExposureService.Apply(pawn, rules, settings, hours);
        AirwayService.Apply(pawn, rules, settings);
        EscharService.Apply(pawn, rules, settings);
        CausticService.Apply(pawn, rules, settings, interval);
    }

    public override bool OnHediffAdded(Pawn pawn, Hediff hediff)
    {
        if (!pawn.RaceProps.Humanlike) return false;
        AirwayService.NoteHediffAdded(pawn, hediff);
        ChemicalService.NoteHediffAdded(pawn, hediff);
        return false;
    }

    static bool Interesting(Pawn pawn)
    {
        HediffSet set = pawn.health.hediffSet;
        if (set.HasHediff(BurnsDefOf.PhobosBurns_BurnLoad) || set.HasHediff(BurnsDefOf.PhobosBurns_BurnShock)
            || set.HasHediff(BurnsDefOf.PhobosBurns_AirwayBurn) || set.HasHediff(BurnsDefOf.PhobosBurns_CarbonMonoxide)
            || set.HasHediff(BurnsDefOf.PhobosBurns_LungInjury) || set.HasHediff(BurnsDefOf.PhobosBurns_SmokeInhalation)
            || set.HasHediff(BurnsDefOf.PhobosBurns_Eschar) || set.HasHediff(BurnsDefOf.PhobosBurns_CausticExposure))
        {
            return true;
        }
        if (BurnLoadService.HasBurn(pawn) || pawn.IsBurning()) return true;
        return SmokeExposureService.Exposed(pawn) || CausticService.InToxGas(pawn);
    }
}
