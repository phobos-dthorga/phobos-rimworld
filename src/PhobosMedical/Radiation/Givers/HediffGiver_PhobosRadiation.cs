using Phobos.Radiation.Defs;
using Phobos.Radiation.Services;
using Verse;

namespace Phobos.Radiation.Givers;

/// <summary>
/// The radiation model's engine hook (D'thorga Medical 0.21.0), patched into OrganicStandard as our burns giver is. The game calls it every
/// 60 ticks per flesh pawn; it works only for someone carrying our dose, or one of Dubs Rimatomics' conditions to convert, and only once per
/// the rules' interval, on a tick near the pawn's hash phase. Everyone else costs a check.
/// </summary>
public class HediffGiver_PhobosRadiation : HediffGiver
{
    const int GiverIntervalTicks = 60;

    public override void OnIntervalPassed(Pawn pawn, Hediff cause)
    {
        if (pawn.Dead || pawn.health?.hediffSet == null || !RadiationPart.On) return;
        int interval = PhobosRadiationRulesDef.Current.intervalTicks;
        if (interval < GiverIntervalTicks) interval = GiverIntervalTicks;
        if ((Find.TickManager.TicksGame + pawn.HashOffset()) % interval >= GiverIntervalTicks) return;
        if (!pawn.health.hediffSet.HasHediff(RadiationDefOf.PhobosRadiation_Dose) && !RimatomicsConversion.HasAny(pawn)) return;
        RadiationService.Step(pawn);
    }
}
