using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Health;
using Phobos.Medical.Comps;
using Phobos.Medical.Defs;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Services;

/// <summary>
/// Pressure chambers: who should be carried to one, which one, and why no operation is done inside. The work giver, the patch and
/// the inspect text only call this.
/// </summary>
public static class ChamberService
{
    const int CacheIntervalTicks = 250;

    static readonly MapPatientCache<Pawn> waiting = new(Rebuild);

    public static bool IsChamber(Thing? thing) => thing is Building_Bed bed && bed.def.HasModExtension<PhobosPressureChamberExtension>();

    /// <summary>Downed patients who need oxygen and lie on the ground, while the map has at least one chamber; rebuilt every few seconds.</summary>
    public static IReadOnlyList<Pawn> PatientsForChamber(Map map) => waiting.Get(map, CacheIntervalTicks);

    /// <summary>
    /// A patient a doctor should carry to a chamber: one of the colony's people, downed and not yet in a bed, who needs oxygen, with a
    /// care setting that lets doctors act. Patients already in a bed stay where they are (the game's own rescue has placed them).
    /// </summary>
    public static bool WantsChamber(Pawn patient) =>
        Patients.IsPlayersPerson(patient) && patient.RaceProps.IsFlesh && patient.Downed && !patient.InBed()
        && Patients.CareAllows(patient, MedicalCareCategory.NoMeds, requireMedicine: false)
        && OxygenService.NeedsOxygen(patient, PhobosRulesDef.Current<PhobosMedicalRulesDef>());

    /// <summary>The nearest chamber that is ready to treat (switched on, powered, fuelled), free for this patient, and reachable.</summary>
    public static Building_Bed? FreeChamberFor(Pawn patient, Pawn doctor)
    {
        Building_Bed? best = null;
        float bestDistance = float.MaxValue;
        foreach (Building_Bed bed in Chambers(doctor.Map))
        {
            CompPhobosOxygenSupply? oxygen = bed.TryGetComp<CompPhobosOxygenSupply>();
            if (oxygen == null || OxygenService.StatusOf(oxygen, TreatmentPass.None) != OxygenStatus.Waiting) continue;
            if (!RestUtility.IsValidBedFor(bed, patient, doctor, checkSocialProperness: false)) continue;
            if (!doctor.CanReserveAndReach(bed, PathEndMode.OnCell, Danger.Deadly)) continue;
            float distance = bed.Position.DistanceToSquared(patient.Position);
            if (distance >= bestDistance) continue;
            best = bed;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>The game's own rescue job, aimed at the chamber: the doctor picks the patient up and lays them in it.</summary>
    public static Job? MakeJob(Pawn doctor, Pawn patient)
    {
        if (!WantsChamber(patient)) return null;
        Building_Bed? chamber = FreeChamberFor(patient, doctor);
        if (chamber == null) return null;
        Job job = JobMaker.MakeJob(JobDefOf.Rescue, patient, chamber);
        job.count = 1;
        return job;
    }

    /// <summary>No operation on someone lying in a pressure chamber: the surgeon can't reach them through it.</summary>
    public static bool BlocksSurgery(Bill_Medical bill) => bill.GiverPawn is Pawn patient && SurgeryBarReason(patient) != null;

    /// <summary>The chamber's entry in the framework's surgery bars: why this patient can't be operated on now, or null. In-world words,
    /// so another mod (D'thorga Triage) can show it as it holds the operation.</summary>
    public static string? SurgeryBarReason(Pawn patient) =>
        IsChamber(patient.CurrentBed()) ? "PhobosMedical_SurgeryBarChamber".Translate().Resolve() : null;

    /// <summary>An occupant with an operation waiting, for the chamber's inspect line, or null.</summary>
    public static Pawn? OccupantWaitingForSurgery(Building_Bed chamber)
    {
        foreach (Pawn occupant in chamber.CurOccupants)
        {
            if (occupant.health?.surgeryBills?.Bills.Any(bill => !bill.suspended) == true) return occupant;
        }
        return null;
    }

    static IEnumerable<Building_Bed> Chambers(Map map)
    {
        List<Building> buildings = map.listerBuildings.allBuildingsColonist;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i] is Building_Bed bed && IsChamber(bed)) yield return bed;
        }
    }

    static void Rebuild(Map map, List<Pawn> list)
    {
        if (!Chambers(map).Any()) return;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (WantsChamber(pawns[i])) list.Add(pawns[i]);
        }
    }
}
