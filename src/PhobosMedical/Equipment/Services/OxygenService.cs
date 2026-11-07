using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Breathing;
using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Decides and does the supplying. The comps and the inspect text only call this and read what it reports.</summary>
public static class OxygenService
{
    /// <summary>One machine's pass: find who needs oxygen in its beds, serve the worst off, and burn the fuel. Returns who it served
    /// and who it left alone because they breathe well enough. The condition sets its own effect on breathing.</summary>
    public static TreatmentPass Supply(CompPhobosOxygenSupply machine, int intervalTicks)
    {
        if (!CanSupply(machine)) return TreatmentPass.None;
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        List<Pawn> occupants = Occupants(machine).Distinct().ToList();
        List<Pawn> needing = occupants.Where(pawn => NeedsOxygen(pawn, rules)).ToList();
        List<Pawn> notNeeding = occupants.Where(pawn => !needing.Contains(pawn)).ToList();
        List<Pawn> patients = OxygenRule.WorstFirst(needing, Breathing, machine.Props.maxPatients);
        HediffDef treatment = machine.Props.treatment ?? MedicalDefOf.PhobosMedical_OnOxygen;
        foreach (Pawn patient in patients) MedicalTreatment.GiveOrRefresh(patient, treatment);
        CompRefuelable? fuel = machine.Fuel;
        if (fuel != null && patients.Count > 0)
            fuel.ConsumeFuel(OxygenRule.FuelForInterval(patients.Count, machine.Props.fuelPerPatientHour, intervalTicks));
        return new TreatmentPass(patients, notNeeding);
    }

    public static OxygenStatus StatusOf(CompPhobosOxygenSupply machine, TreatmentPass pass) =>
        StatusOf(machine, pass.Treated.Count, pass.NotNeeding.Count);

    static OxygenStatus StatusOf(CompPhobosOxygenSupply machine, int patients, int notNeeding)
    {
        CompPowerTrader? power = machine.parent.TryGetComp<CompPowerTrader>();
        bool hasOxygen = machine.Fuel == null || machine.Fuel.HasFuel;
        int beds = machine.parent is Building_Bed ? 1 : machine.Facility?.LinkedBuildings?.Count ?? 0;
        return OxygenRule.Status(FlickUtility.WantsToBeOn(machine.parent), power == null || power.PowerOn, beds, hasOxygen, patients, notNeeding);
    }

    public static bool NeedsOxygen(Pawn pawn, PhobosMedicalRulesDef rules) =>
        pawn.RaceProps.IsFlesh && !pawn.Dead
        && OxygenRule.NeedsOxygen(Breathing(pawn), PhobosBreathing.HasOxygenResponsiveCondition(pawn), rules.breathingNeedThreshold);

    static bool CanSupply(CompPhobosOxygenSupply machine) =>
        machine.parent.Spawned && (machine.parent is Building_Bed || machine.Facility != null) && StatusOf(machine, 0, 0) == OxygenStatus.Waiting;

    public static float Breathing(Pawn pawn) => pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing);

    /// <summary>Whoever lies in the machine, if it is a bed (a pressure chamber), else whoever lies in the beds it links to.</summary>
    static IEnumerable<Pawn> Occupants(CompPhobosOxygenSupply machine) =>
        machine.parent is Building_Bed own ? own.CurOccupants : PatientsInLinkedBeds(machine.Facility);

    static IEnumerable<Pawn> PatientsInLinkedBeds(CompFacility? facility)
    {
        if (facility?.LinkedBuildings == null) yield break;
        foreach (Thing linked in facility.LinkedBuildings)
        {
            if (linked is not Building_Bed bed) continue;
            foreach (Pawn occupant in bed.CurOccupants) yield return occupant;
        }
    }
}
