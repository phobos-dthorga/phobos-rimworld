using Phobos.Framework.Patching;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// A condition that, once it heals, can leave a lasting one on the same body part by the worst it ever reached (a burned eye leaves
/// a scarred cornea). Nothing below <c>fromSeverity</c>; above it, the lasting condition's severity follows <see cref="LastingRule"/>.
/// <c>flag</c>, when given, is a setting flag (<see cref="PatchFlags"/>) that must be on. The worst severity is saved with the
/// condition. Nothing is left on a part that has gone, when the pawn has died, or during the uninstall sweep.
/// </summary>
public class HediffCompProperties_PhobosLeavesLasting : HediffCompProperties
{
    public HediffDef? lasting;
    public float fromSeverity = 0.5f;
    public string? flag;

    public HediffCompProperties_PhobosLeavesLasting() => compClass = typeof(HediffComp_PhobosLeavesLasting);

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (lasting == null) yield return "lasting must name the condition left behind";
    }
}

public class HediffComp_PhobosLeavesLasting : HediffComp
{
    float peak;

    public HediffCompProperties_PhobosLeavesLasting Props => (HediffCompProperties_PhobosLeavesLasting)props;

    /// <summary>The worst severity the condition has reached.</summary>
    public float Peak => peak;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (parent.Severity > peak) peak = parent.Severity;
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        if (parent.Severity > peak) peak = parent.Severity;
    }

    public override void CompPostPostRemoved()
    {
        if (Props.lasting == null || Pawn == null || Pawn.Dead || Pawn.health?.hediffSet == null || Uninstall.CleanupService.Sweeping) return;
        if (!Props.flag.NullOrEmpty() && !PatchFlags.IsOn(Props.flag!)) return;
        if (parent.Severity > peak) peak = parent.Severity;
        float severity = (float)LastingRule.SeverityFor(peak, Props.fromSeverity);
        if (severity <= 0f) return;
        BodyPartRecord? part = parent.Part;
        // A part that has gone, or that has been replaced (a bionic eye), keeps nothing of the old one.
        if (part != null && (Pawn.health.hediffSet.PartIsMissing(part) || Pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part))) return;
        foreach (Hediff existing in Pawn.health.hediffSet.hediffs)
        {
            if (existing.def != Props.lasting || existing.Part != part) continue;
            if (existing.Severity < severity) existing.Severity = severity;
            return;
        }
        Hediff left = HediffMaker.MakeHediff(Props.lasting, Pawn, part);
        left.Severity = severity;
        Pawn.health.AddHediff(left, part);
    }

    public override void CompExposeData()
    {
        Scribe_Values.Look(ref peak, "PhobosFramework_PeakSeverity", 0f);
    }
}
