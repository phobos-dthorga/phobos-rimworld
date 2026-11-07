using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>One ventilator pass: who it breathed for, who needs a tube first, and who breathes well enough. Derived, never saved.</summary>
public sealed class VentilatorPass
{
    public static readonly VentilatorPass None = new(new List<Pawn>(), new List<Pawn>(), new List<Pawn>());

    public readonly List<Pawn> Treated;
    public readonly List<Pawn> NeedingTube;
    public readonly List<Pawn> NotNeeding;

    public VentilatorPass(List<Pawn> treated, List<Pawn> needingTube, List<Pawn> notNeeding)
    {
        Treated = treated;
        NeedingTube = needingTube;
        NotNeeding = notNeeding;
    }
}

/// <summary>Decides and does the ventilating. The comp and its inspect text only call this.</summary>
public static class VentilatorService
{
    public static VentilatorPass Supply(CompPhobosVentilator machine)
    {
        if (!Working(machine)) return VentilatorPass.None;
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        var treated = new List<Pawn>();
        var needingTube = new List<Pawn>();
        var notNeeding = new List<Pawn>();
        foreach (Pawn patient in LinkedBeds.Patients(machine.Facility).Distinct())
        {
            if (!patient.RaceProps.IsFlesh || patient.Dead || !VentilatorRule.Needs(Hediff_PhobosOnOxygen.BreathingWithoutTreatments(patient), rules.ventilatorBreathingFloor))
                notNeeding.Add(patient);
            else if (!AirwayTubes.HasTube(patient))
                needingTube.Add(patient);
            else if (treated.Count < machine.Props.maxPatients)
                treated.Add(patient);
        }
        foreach (Pawn patient in treated) MedicalTreatment.GiveOrRefresh(patient, machine.Props.treatment ?? MedicalDefOf.PhobosMedical_Ventilated);
        return new VentilatorPass(treated, needingTube, notNeeding);
    }

    public static VentilatorStatus StatusOf(CompPhobosVentilator machine, VentilatorPass pass)
    {
        CompPowerTrader? power = machine.parent.TryGetComp<CompPowerTrader>();
        int beds = machine.Facility?.LinkedBuildings?.Count ?? 0;
        return VentilatorRule.Status(FlickUtility.WantsToBeOn(machine.parent), power == null || power.PowerOn, beds,
            pass.Treated.Count, pass.NeedingTube.Count, pass.NotNeeding.Count);
    }

    /// <summary>Whether this patient lies in a bed with a working ventilator that would breathe for them once a tube is in.</summary>
    public static bool WaitingForTube(Pawn patient)
    {
        if (AirwayTubes.HasTube(patient)) return false;
        Building_Bed? bed = patient.CurrentBed();
        List<Thing>? facilities = bed?.TryGetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading;
        if (facilities == null) return false;
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        if (!VentilatorRule.Needs(Hediff_PhobosOnOxygen.BreathingWithoutTreatments(patient), rules.ventilatorBreathingFloor)) return false;
        for (int i = 0; i < facilities.Count; i++)
        {
            CompPhobosVentilator? machine = facilities[i].TryGetComp<CompPhobosVentilator>();
            if (machine != null && Working(machine)) return true;
        }
        return false;
    }

    static bool Working(CompPhobosVentilator machine)
    {
        if (!machine.parent.Spawned || machine.Facility == null || !FlickUtility.WantsToBeOn(machine.parent)) return false;
        CompPowerTrader? power = machine.parent.TryGetComp<CompPowerTrader>();
        return power == null || power.PowerOn;
    }

}
