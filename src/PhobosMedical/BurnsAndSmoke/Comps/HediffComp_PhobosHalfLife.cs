using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework.Breathing;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>First-order clearance: the condition halves every so many hours, faster in open air and, where the Def says so
/// (<see cref="oxygenSpeeds"/>), faster still on supplied oxygen. Used for carbon monoxide.</summary>
public class HediffCompProperties_PhobosHalfLife : HediffCompProperties
{
    public float halfLifeHoursRoofed = 5.3f;
    public float halfLifeHoursOutdoors = 3f;
    public float removeBelow = 0.01f;
    /// <summary>Whether supplied oxygen speeds the clearing too, at the treatment's <c>carbonMonoxideHalfLifeHours</c> (the owner's card of
    /// 28 Nov 2026, "per-condition switch"): on for our carbon monoxide, off unless a Def asks, so oxygen speeds only what it should.</summary>
    public bool oxygenSpeeds;

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
            Props.oxygenSpeeds ? PhobosBreathing.SuppliedCarbonMonoxideHalfLifeHours(Pawn) : null);
        float remaining = CarbonMonoxideRule.Remaining(parent.Severity, HoursPerCheck, halfLife);
        severityAdjustment += remaining - parent.Severity;
    }
}
