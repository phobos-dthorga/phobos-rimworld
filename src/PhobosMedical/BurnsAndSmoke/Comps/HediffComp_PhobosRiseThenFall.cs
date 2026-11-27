using Phobos.BurnsAndSmoke.Rules;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>Severity from the condition's age: rises to a peak, holds, then resolves. Used for the airway burn's delayed swelling.</summary>
public class HediffCompProperties_PhobosRiseThenFall : HediffCompProperties
{
    public float peakHours = 8f;
    public float plateauEndHours = 24f;
    public float resolveHours = 48f;

    public HediffCompProperties_PhobosRiseThenFall() => compClass = typeof(HediffComp_PhobosRiseThenFall);

    /// <summary>
    /// Timings that would empty the condition at once or skip its shape (checked on the owner's card of 28 Nov 2026, "check both, fix
    /// if real": on any Def the first check comes after the game has aged the condition, so the severity is above 0 from the start; only
    /// a peak at hour 0 or less sets it to 0 and the condition is removed at once).
    /// </summary>
    public override System.Collections.Generic.IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (peakHours <= 0f) yield return "HediffCompProperties_PhobosRiseThenFall needs peakHours above 0: at " + peakHours + " the severity is 0 from the start and the condition is removed at once.";
        if (plateauEndHours < peakHours) yield return "HediffCompProperties_PhobosRiseThenFall needs plateauEndHours (" + plateauEndHours + ") no earlier than peakHours (" + peakHours + ").";
        if (resolveHours <= plateauEndHours) yield return "HediffCompProperties_PhobosRiseThenFall needs resolveHours (" + resolveHours + ") after plateauEndHours (" + plateauEndHours + "): the condition would vanish at the end of the plateau instead of falling.";
    }
}

public class HediffComp_PhobosRiseThenFall : HediffComp
{
    const int CheckTicks = 250;

    /// <summary>The severity this episode peaks at, 0 to 1, set from the exposure when the condition starts.</summary>
    public float peak = 1f;

    HediffCompProperties_PhobosRiseThenFall Props => (HediffCompProperties_PhobosRiseThenFall)props;

    public float AgeHours => parent.ageTicks / 2500f;

    public override bool CompShouldRemove => AgeHours >= Props.resolveHours;

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref peak, "PhobosBurns_peak", 1f);
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);
        if (!Pawn.IsHashIntervalTick(CheckTicks, delta)) return;
        float target = AirwayRule.Severity(AgeHours, peak, Props.peakHours, Props.plateauEndHours, Props.resolveHours);
        severityAdjustment += target - parent.Severity;
    }

    public override string CompTipStringExtra =>
        Phobos.Medical.MedicalMod.DisplayMode == Framework.DisplayMode.Plain
            ? ""
            : "PhobosBurns_TipAirway".Translate(AgeHours.ToString("0.0").Named("AGE"), (peak * 100f).ToString("0").Named("PEAK"), Props.peakHours.ToString("0").Named("PEAKHOURS"), Props.resolveHours.ToString("0").Named("RESOLVEHOURS"));
}
