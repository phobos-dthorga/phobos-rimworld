using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.BurnsAndSmoke.Services;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

public class HediffCompProperties_PhobosChemicalDeepening : HediffCompProperties
{
    public HediffCompProperties_PhobosChemicalDeepening() => compClass = typeof(HediffComp_PhobosChemicalDeepening);
}

/// <summary>Once an in-game hour, acid still on the skin deepens the pawn's chemical burns; less as it is spent.</summary>
public class HediffComp_PhobosChemicalDeepening : HediffComp
{
    const int IntervalTicks = 2500;
    const float HoursPerInterval = 1f;

    int ticksToNext = IntervalTicks;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        ticksToNext -= delta;
        if (ticksToNext > 0) return;
        ticksToNext = IntervalTicks;
        ChemicalService.Deepen(Pawn, ChemicalRule.Deepening(parent.Severity, PhobosBurnRulesDef.Current.chemicalDeepenPerHourAtFull, HoursPerInterval));
    }
}
