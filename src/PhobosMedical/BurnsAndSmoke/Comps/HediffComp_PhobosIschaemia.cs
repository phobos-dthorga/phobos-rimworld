using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>Makes a tight condition on a body part starve it of blood: once an in-game hour, above the burn rules Def's
/// <c>escharIschaemiaFromSeverity</c>, the part takes ischaemic damage, rising with severity, until it is lost as any part is. Goes in
/// the <c>comps</c> of a HediffDef that sits on a part (D'thorga Medical's constricting eschar), with no fields. It stands down while
/// More Injuries is active, whose gangrene takes the limb instead.</summary>
public class HediffCompProperties_PhobosIschaemia : HediffCompProperties
{
    public HediffCompProperties_PhobosIschaemia() => compClass = typeof(HediffComp_PhobosIschaemia);
}

/// <summary>
/// Once an hour, a tight eschar starves its limb: ischaemic damage through the game's own damage system, so the limb is lost the
/// way any part is when its health runs out. With More Injuries active this stands down: its gangrene, started by a patch onto the
/// eschar through its own secondary-condition handlers, takes the limb instead, so the two never stack.
/// </summary>
public class HediffComp_PhobosIschaemia : HediffComp
{
    const int IntervalTicks = 2500;
    const float HoursPerInterval = 1f;

    int ticksToNext = IntervalTicks;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        ticksToNext -= delta;
        if (ticksToNext > 0) return;
        ticksToNext = IntervalTicks;
        if (parent.Part == null || Pawn.Dead || OptionalMods.IsActive(OptionalMods.MoreInjuries)) return;
        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
        float damage = EscharRule.IschaemicDamage(parent.Severity, rules.escharIschaemiaFromSeverity, rules.ischaemicDamagePerHourAtFull, HoursPerInterval);
        if (damage <= 0f) return;
        var dinfo = new DamageInfo(BurnsDefOf.PhobosBurns_Ischaemia, damage, hitPart: parent.Part);
        dinfo.SetAllowDamagePropagation(false);
        Pawn.TakeDamage(dinfo);
    }
}
