using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>Who can doctor right now, by the game's own tests. Nothing here changes game state.</summary>
public static class Doctors
{
    /// <summary>The game's own test for a patient going to bed: some player doctor who could come.</summary>
    public static bool AnyAvailableFor(Pawn patient) => WorkGiver_PatientGoToBedTreatment.AnyAvailableDoctorFor(patient);

    /// <summary>
    /// A free colonist, or one of the colony's mechanoids (Framework 0.351.0: the game lets a mechanoid with Doctor work do all of it),
    /// who is up, awake, sane, not a patient in bed, and has Doctor work on.
    /// </summary>
    public static bool CanDoctorNow(Pawn pawn) =>
        (pawn.IsFreeColonist || pawn.IsColonyMech) && !pawn.Downed && !pawn.Dead && pawn.Awake() && !pawn.InMentalState && !pawn.InBed()
        && pawn.workSettings != null && pawn.workSettings.WorkIsActive(WorkTypeDefOf.Doctor)
        && !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Doctor);

    /// <summary>
    /// A doctor's Medicine level: their skill, or a mechanoid's fixed level for all its work (the game's <c>mechFixedSkillLevel</c>, 10
    /// unless its race says otherwise), or zero (Framework 0.351.0). A mechanoid has no skills of its own to read.
    /// </summary>
    public static int MedicineLevel(Pawn pawn)
    {
        SkillRecord? skill = pawn.skills?.GetSkill(SkillDefOf.Medicine);
        if (skill != null) return skill.Level;
        return pawn.RaceProps.IsMechanoid ? pawn.RaceProps.mechFixedSkillLevel : 0;
    }

    /// <summary>
    /// The best surgery success chance among the map's doctors who meet the recipe's skill requirements, excluding the patient;
    /// negative when no such doctor exists. The game's own outcome multiplies this by the recipe's factor, the bed and the medicine.
    /// </summary>
    public static float BestSurgeonSuccess(Map map, RecipeDef recipe, Pawn? patient)
    {
        float best = Best(map.mapPawns.FreeColonistsSpawned, recipe, patient, -1f);
        // The colony's mechanoids too: the game lets one with Doctor work operate, at its fixed skill (Framework 0.351.0).
        return ModsConfig.BiotechActive ? Best(map.mapPawns.SpawnedColonyMechs, recipe, patient, best) : best;
    }

    static float Best(List<Pawn> doctors, RecipeDef recipe, Pawn? patient, float best)
    {
        for (int i = 0; i < doctors.Count; i++)
        {
            Pawn doctor = doctors[i];
            if (doctor == patient || !CanDoctorNow(doctor) || !recipe.PawnSatisfiesSkillRequirements(doctor)) continue;
            float chance = doctor.GetStatValue(StatDefOf.MedicalSurgerySuccessChance);
            if (chance > best) best = chance;
        }
        return best;
    }
}
