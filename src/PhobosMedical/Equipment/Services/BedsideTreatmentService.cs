using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Decides and does a tagged bedside treatment (the nebuliser). The comp and its inspect text only call this.</summary>
public static class BedsideTreatmentService
{
    public static TreatmentPass Supply(CompPhobosBedsideTreatment machine, int intervalTicks)
    {
        if (!machine.parent.Spawned || machine.Facility == null || StatusOf(machine, 0, 0) != OxygenStatus.Waiting) return TreatmentPass.None;
        List<Pawn> occupants = LinkedBeds.Patients(machine.Facility).Distinct().ToList();
        List<Pawn> needing = occupants.Where(pawn => !pawn.Dead && Needs(pawn, machine.Props.tag)).Take(machine.Props.maxPatients).ToList();
        List<Pawn> notNeeding = occupants.Where(pawn => !needing.Contains(pawn)).ToList();
        foreach (Pawn patient in needing) MedicalTreatment.GiveOrRefresh(patient, machine.Props.treatment);
        CompRefuelable? fuel = machine.Fuel;
        if (fuel != null && needing.Count > 0)
            fuel.ConsumeFuel(OxygenRule.FuelForInterval(needing.Count, machine.Props.fuelPerPatientHour, intervalTicks));
        return new TreatmentPass(needing, notNeeding);
    }

    public static bool Needs(Pawn pawn, string tag)
    {
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            PhobosBedsideTreatableExtension? treatable = hediffs[i].def.GetModExtension<PhobosBedsideTreatableExtension>();
            if (treatable != null && hediffs[i].Severity >= treatable.minSeverity && treatable.tags.Contains(tag)) return true;
        }
        return false;
    }

    public static OxygenStatus StatusOf(CompPhobosBedsideTreatment machine, TreatmentPass pass) => StatusOf(machine, pass.Treated.Count, pass.NotNeeding.Count);

    static OxygenStatus StatusOf(CompPhobosBedsideTreatment machine, int patients, int notNeeding)
    {
        CompPowerTrader? power = machine.parent.TryGetComp<CompPowerTrader>();
        bool hasFuel = machine.Fuel == null || machine.Fuel.HasFuel;
        int beds = machine.Facility?.LinkedBuildings?.Count ?? 0;
        return OxygenRule.Status(FlickUtility.WantsToBeOn(machine.parent), power == null || power.PowerOn, beds, hasFuel, patients, notNeeding);
    }
}
