using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.BurnsAndSmoke.Services;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>Makes a chemical deepen burns once an in-game hour, less as the condition's severity falls (the hourly damage is in the burn
/// rules Def, <c>chemicalDeepenPerHourAtFull</c>). On D'thorga Medical's "chemical on the skin" it spreads over every open chemical burn;
/// on any other HediffDef it deepens only that condition, while it is an open, untended injury on a body part. Goes in a HediffDef's
/// <c>comps</c>, with no fields.</summary>
public class HediffCompProperties_PhobosChemicalDeepening : HediffCompProperties
{
    public HediffCompProperties_PhobosChemicalDeepening() => compClass = typeof(HediffComp_PhobosChemicalDeepening);
}

/// <summary>
/// Once an in-game hour, acid still on the skin deepens the pawn's chemical burns; less as it is spent. On our "chemical on the skin"
/// condition it spreads over every open marked burn; on any other condition it deepens only that condition, while it is an open,
/// untended burn on a body part (the owner's card of 28 Nov 2026: until then it deepened every marked burn on the person from anywhere).
/// </summary>
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
        float damage = ChemicalRule.Deepening(parent.Severity, PhobosBurnRulesDef.Current.chemicalDeepenPerHourAtFull, HoursPerInterval);
        switch (ChemicalRule.Target(parent.def == BurnsDefOf.PhobosBurns_ChemicalOnSkin, parent is Hediff_Injury && parent.Part != null,
            parent.IsPermanent(), parent.IsTended()))
        {
            case DeepeningTarget.MarkedBurns:
                ChemicalService.Deepen(Pawn, damage);
                break;
            case DeepeningTarget.Self:
                ChemicalService.DeepenOwn(parent, damage);
                break;
        }
    }
}
