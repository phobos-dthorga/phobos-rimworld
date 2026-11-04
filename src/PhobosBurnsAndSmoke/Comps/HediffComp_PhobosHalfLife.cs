using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework.Breathing;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>First-order clearance: the condition halves every so many hours, faster in open air and faster still on supplied oxygen. Used for carbon monoxide.</summary>
public class HediffCompProperties_PhobosHalfLife : HediffCompProperties
{
    public float halfLifeHoursRoofed = 5.3f;
    public float halfLifeHoursOutdoors = 3f;
    public float removeBelow = 0.01f;

    public HediffCompProperties_PhobosHalfLife() => compClass = typeof(HediffComp_PhobosHalfLife);
}

public class HediffComp_PhobosHalfLife : HediffComp
{
    const int CheckTicks = 250;
    const float HoursPerCheck = CheckTicks / 2500f;

    HediffCompProperties_PhobosHalfLife Props => (HediffCompProperties_PhobosHalfLife)props;

    public override bool CompShouldRemove => parent.Severity < Props.removeBelow;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (!Pawn.IsHashIntervalTick(CheckTicks, delta)) return;
        bool roofed = Pawn.Spawned && Pawn.Map != null && Pawn.Position.Roofed(Pawn.Map);
        float halfLife = BreathingRule.ClearanceHalfLifeHours(roofed ? Props.halfLifeHoursRoofed : Props.halfLifeHoursOutdoors,
            PhobosBreathing.SuppliedCarbonMonoxideHalfLifeHours(Pawn));
        float remaining = CarbonMonoxideRule.Remaining(parent.Severity, HoursPerCheck, halfLife);
        severityAdjustment += remaining - parent.Severity;
    }
}
