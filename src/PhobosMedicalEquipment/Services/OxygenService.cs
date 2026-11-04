using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Breathing;
using Phobos.Framework.Defs;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>Decides and does the supplying. The comps and the inspect text only call this and read what it reports.</summary>
public static class OxygenService
{
    /// <summary>One machine's pass: find who needs oxygen in its linked beds, serve the worst off, and burn the fuel. Returns who it served.</summary>
    public static List<Pawn> Supply(CompPhobosOxygenSupply machine, int intervalTicks)
    {
        if (!CanSupply(machine)) return new List<Pawn>();
        var rules = PhobosRulesDef.Current<PhobosMedicalRulesDef>();
        IEnumerable<Pawn> needing = PatientsInLinkedBeds(machine.Facility).Where(pawn => NeedsOxygen(pawn, rules));
        List<Pawn> patients = OxygenRule.WorstFirst(needing, Breathing, machine.Props.maxPatients);
        foreach (Pawn patient in patients) Give(patient);
        CompRefuelable? fuel = machine.Fuel;
        if (fuel != null && patients.Count > 0)
            fuel.ConsumeFuel(OxygenRule.FuelForInterval(patients.Count, machine.Props.fuelPerPatientHour, intervalTicks));
        return patients;
    }

    public static OxygenStatus StatusOf(CompPhobosOxygenSupply machine, int patients)
    {
        CompPowerTrader? power = machine.parent.TryGetComp<CompPowerTrader>();
        bool hasOxygen = machine.Fuel == null || machine.Fuel.HasFuel;
        return OxygenRule.Status(FlickUtility.WantsToBeOn(machine.parent), power == null || power.PowerOn,
            machine.Facility?.LinkedBuildings?.Count ?? 0, hasOxygen, patients);
    }

    public static bool NeedsOxygen(Pawn pawn, PhobosMedicalRulesDef rules) =>
        pawn.RaceProps.IsFlesh && !pawn.Dead
        && OxygenRule.NeedsOxygen(Breathing(pawn), PhobosBreathing.HasOxygenResponsiveCondition(pawn), rules.breathingNeedThreshold);

    static bool CanSupply(CompPhobosOxygenSupply machine) =>
        machine.parent.Spawned && machine.Facility != null && StatusOf(machine, 0) == OxygenStatus.Waiting;

    static float Breathing(Pawn pawn) => pawn.health.capacities.GetLevel(PawnCapacityDefOf.Breathing);

    static IEnumerable<Pawn> PatientsInLinkedBeds(CompFacility? facility)
    {
        if (facility?.LinkedBuildings == null) yield break;
        foreach (Thing linked in facility.LinkedBuildings)
        {
            if (linked is not Building_Bed bed) continue;
            foreach (Pawn occupant in bed.CurOccupants) yield return occupant;
        }
    }

    /// <summary>Start the treatment, or keep it going. The condition lapses by itself once nothing refreshes it.</summary>
    static void Give(Pawn patient)
    {
        Hediff? onOxygen = patient.health.hediffSet.GetFirstHediffOfDef(MedicalDefOf.PhobosMedical_OnOxygen);
        onOxygen ??= patient.health.AddHediff(MedicalDefOf.PhobosMedical_OnOxygen);
        onOxygen?.TryGetComp<HediffComp_PhobosOnOxygen>()?.Refresh();
    }
}
