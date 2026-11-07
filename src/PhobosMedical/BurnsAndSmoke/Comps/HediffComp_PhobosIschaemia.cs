using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using Phobos.Framework;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

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
