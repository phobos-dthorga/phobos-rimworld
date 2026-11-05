using System.Collections.Generic;
using System.Linq;
using Phobos.Medical.Rules;
using Phobos.Medical.Services;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>A bed that gives a treatment condition to a burned occupant while its power is on. Other mods can add beds with this in XML.</summary>
public class CompProperties_PhobosBurnCare : CompProperties
{
    /// <summary>The condition an occupant with an open burn gets. Its effects are on its own Def, so they tune in XML.</summary>
    public HediffDef treatment = null!;

    public CompProperties_PhobosBurnCare() => compClass = typeof(CompPhobosBurnCare);
}

public class CompPhobosBurnCare : ThingComp
{
    /// <summary>Who it served on its last pass, for the inspect text. Derived, so never saved.</summary>
    List<Pawn> patients = new();

    public CompProperties_PhobosBurnCare Props => (CompProperties_PhobosBurnCare)props;

    public override void CompTickRare()
    {
        base.CompTickRare();
        patients = BurnCareService.Care(this);
    }

    public override string CompInspectStringExtra() => BurnCareService.StatusOf(this, patients.Count) switch
    {
        BurnCareStatus.SwitchedOff => "PhobosMedical_BurnBedSwitchedOff".Translate(),
        BurnCareStatus.NoPower => "PhobosMedical_BurnBedNoPower".Translate(),
        BurnCareStatus.Treating => "PhobosMedical_BurnBedTreating".Translate(string.Join(", ", patients.Select(p => p.LabelShort)).Named("PATIENTS")),
        _ => "PhobosMedical_BurnBedWaiting".Translate(),
    };
}
