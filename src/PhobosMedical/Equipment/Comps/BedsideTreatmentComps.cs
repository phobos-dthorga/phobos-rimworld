using Phobos.Framework.Health;
using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>
/// A bedside machine that gives a treatment to patients in the beds it links to who have a condition carrying its <see cref="tag"/> (see
/// PhobosBedsideTreatableExtension). It may burn a consumable as fuel. The nebuliser uses it; other mods can add machines in XML.
/// </summary>
public class CompProperties_PhobosBedsideTreatment : CompProperties
{
    public string tag = "";
    public HediffDef treatment = null!;
    public int maxPatients = 1;
    /// <summary>Fuel per patient per in-game hour, if the building has a fuel comp.</summary>
    public float fuelPerPatientHour;
    /// <summary>The start of its inspect text keys: the status's name is added (SwitchedOff, NoFuel, Waiting, Treating, NotNeeded).</summary>
    public string statusKeyPrefix = "";

    public CompProperties_PhobosBedsideTreatment() => compClass = typeof(CompPhobosBedsideTreatment);

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (treatment == null) yield return "CompProperties_PhobosBedsideTreatment needs a treatment HediffDef.";
        if (string.IsNullOrEmpty(tag)) yield return "CompProperties_PhobosBedsideTreatment needs a tag.";
    }
}

public class CompPhobosBedsideTreatment : ThingComp
{
    const int RareTickInterval = 250;

    /// <summary>Its last pass, for the inspect text. Derived, so never saved.</summary>
    TreatmentPass pass = TreatmentPass.None;

    public CompProperties_PhobosBedsideTreatment Props => (CompProperties_PhobosBedsideTreatment)props;
    public CompFacility? Facility => parent.TryGetComp<CompFacility>();
    public CompRefuelable? Fuel => parent.TryGetComp<CompRefuelable>();

    public override void CompTickRare()
    {
        base.CompTickRare();
        pass = BedsideTreatmentService.Supply(this, RareTickInterval);
    }

    public override string CompInspectStringExtra()
    {
        OxygenStatus status = BedsideTreatmentService.StatusOf(this, pass);
        string prefix = Props.statusKeyPrefix;
        return (status switch
        {
            OxygenStatus.SwitchedOff => (prefix + "SwitchedOff").Translate(),
            OxygenStatus.NoPower => "PhobosMedical_StatusNoPower".Translate(),
            OxygenStatus.NoBed => "PhobosMedical_StatusNoBed".Translate(),
            OxygenStatus.NoOxygen => (prefix + "NoFuel").Translate(),
            OxygenStatus.Treating => (prefix + "Treating").Translate(TreatmentPass.Names(pass.Treated).Named("PATIENTS")),
            OxygenStatus.NotNeeded => (prefix + "NotNeeded").Translate(TreatmentPass.Names(pass.NotNeeding).Named("PATIENTS")),
            _ => (prefix + "Waiting").Translate(),
        }).Resolve();
    }
}
