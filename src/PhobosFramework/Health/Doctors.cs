using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>Who can doctor right now, by the game's own tests. Nothing here changes game state.</summary>
public static class Doctors
{
    /// <summary>The game's own test for a patient going to bed: some player doctor who could come.</summary>
    public static bool AnyAvailableFor(Pawn patient) => WorkGiver_PatientGoToBedTreatment.AnyAvailableDoctorFor(patient);

    /// <summary>A free colonist who is up, awake, sane, not a patient in bed, and has Doctor work on.</summary>
    public static bool CanDoctorNow(Pawn pawn) =>
        pawn.IsFreeColonist && !pawn.Downed && !pawn.Dead && pawn.Awake() && !pawn.InMentalState && !pawn.InBed()
        && pawn.workSettings != null && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Doctor)
        && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);

    /// <summary>
    /// The best surgery success chance among the map's doctors who meet the recipe's skill requirements, excluding the patient;
    /// negative when no such doctor exists. The game's own outcome multiplies this by the recipe's factor, the bed and the medicine.
    /// </summary>
    public static float BestSurgeonSuccess(Map map, RecipeDef recipe, Pawn? patient)
    {
        float best = -1f;
        List<Pawn> colonists = map.mapPawns.FreeColonistsSpawned;
        for (int i = 0; i < colonists.Count; i++)
        {
            Pawn doctor = colonists[i];
            if (doctor == patient || !CanDoctorNow(doctor) || !recipe.PawnSatisfiesSkillRequirements(doctor)) continue;
            float chance = doctor.GetStatValue(StatDefOf.MedicalSurgerySuccessChance);
            if (chance > best) best = chance;
        }
        return best;
    }
}
