using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>A bedside machine that breathes for a patient through a breathing tube. Other mods can add ventilators with this in XML.</summary>
public class CompProperties_PhobosVentilator : CompProperties
{
    public int maxPatients = 1;
    /// <summary>The condition it gives; empty for the ordinary ventilation condition.</summary>
    public HediffDef? treatment;

    public CompProperties_PhobosVentilator() => compClass = typeof(CompPhobosVentilator);
}

public class CompPhobosVentilator : ThingComp
{
    /// <summary>Its last pass, for the inspect text. Derived, so never saved.</summary>
    VentilatorPass pass = VentilatorPass.None;

    public CompProperties_PhobosVentilator Props => (CompProperties_PhobosVentilator)props;
    public CompFacility? Facility => parent.TryGetComp<CompFacility>();

    public override void CompTickRare()
    {
        base.CompTickRare();
        pass = VentilatorService.Supply(this);
    }

    public override string CompInspectStringExtra() => (VentilatorService.StatusOf(this, pass) switch
    {
        VentilatorStatus.SwitchedOff => "PhobosMedical_VentilatorSwitchedOff".Translate(),
        VentilatorStatus.NoPower => "PhobosMedical_StatusNoPower".Translate(),
        VentilatorStatus.NoBed => "PhobosMedical_StatusNoBed".Translate(),
        VentilatorStatus.Treating => "PhobosMedical_VentilatorTreating".Translate(TreatmentPass.Names(pass.Treated).Named("PATIENTS")),
        VentilatorStatus.NeedsTube => "PhobosMedical_VentilatorNeedsTube".Translate(TreatmentPass.Names(pass.NeedingTube).Named("PATIENTS")),
        VentilatorStatus.NotNeeded => "PhobosMedical_VentilatorNotNeeded".Translate(TreatmentPass.Names(pass.NotNeeding).Named("PATIENTS")),
        _ => "PhobosMedical_VentilatorWaiting".Translate(),
    }).Resolve();
}

/// <summary>
/// Breathing held up by a ventilator. Its Def has one stage per step of breathing added; the stage is chosen from the patient's breathing
/// measured without any breathing treatment, the fewest steps that reach the floor (see <see cref="VentilatorRule"/>), when it starts and
/// every few seconds after. The choice is derived, so never saved; it lapses through its treatment comp when the machine stops.
/// </summary>
public class Hediff_PhobosVentilated : HediffWithComps
{
    const int CheckIntervalTicks = 250;

    int stage;
    int ticksToCheck;

    public override int CurStageIndex => Hediff_PhobosOnOxygen.MeasuringWithout ? 0 : stage;

    public override void PostAdd(DamageInfo? dinfo)
    {
        base.PostAdd(dinfo);
        stage = ChooseStage();
    }

    public override void PostTickInterval(int delta)
    {
        base.PostTickInterval(delta);
        ticksToCheck -= delta;
        if (ticksToCheck > 0) return;
        ticksToCheck = CheckIntervalTicks;
        int next = ChooseStage();
        if (next == stage) return;
        stage = next;
        pawn.health.Notify_HediffChanged(this);
    }

    int ChooseStage()
    {
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        int last = def.stages == null ? 0 : def.stages.Count - 1;
        return VentilatorRule.StageFor(Hediff_PhobosOnOxygen.BreathingWithoutTreatments(pawn), rules.ventilatorBreathingFloor, rules.ventilatorStep, last);
    }
}
