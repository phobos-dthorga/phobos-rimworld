using RimWorld;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>Who a pawn is to the colony's doctors. The order of the checks matters: a slave is also the player's, a prisoner is also hosted.</summary>
public enum PatientGroup
{
    Colonist,
    Slave,
    Prisoner,
    Guest,
}

/// <summary>
/// Which pawns the player's doctors look after, and under what care. One answer for every Phobos mod, so a patient Emergency
/// Dosing treats is a patient Triage triages. Nothing here changes game state.
/// </summary>
public static class Patients
{
    /// <summary>Spawned, alive, humanlike, past babyhood, and the player's own or the player's guest or prisoner.</summary>
    public static bool IsPlayersPerson(Pawn pawn) =>
        pawn.Spawned && !pawn.Dead && pawn.RaceProps.Humanlike && pawn.health?.hediffSet != null
        && !DevelopmentalStageExtensions.Baby(pawn.DevelopmentalStage)
        && (pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer);

    /// <summary>The group a player's person belongs to, or null for anyone the doctors do not look after.</summary>
    public static PatientGroup? GroupOf(Pawn pawn)
    {
        if (!IsPlayersPerson(pawn)) return null;
        if (pawn.IsPrisonerOfColony) return PatientGroup.Prisoner;
        if (pawn.IsSlaveOfColony) return PatientGroup.Slave;
        if (pawn.Faction == Faction.OfPlayer) return PatientGroup.Colonist;
        return PatientGroup.Guest;
    }

    /// <summary>The player's medical care setting for the pawn; no care for a pawn with no settings.</summary>
    public static MedicalCareCategory CareLevel(Pawn pawn) => pawn.playerSettings?.medCare ?? MedicalCareCategory.NoCare;

    /// <summary>
    /// Whether the player's care setting lets doctors act: never under no care, never under no medicine when the act needs
    /// medicine, and at least the minimum a mod's setting asks for.
    /// </summary>
    public static bool CareAllows(Pawn pawn, MedicalCareCategory minimum, bool requireMedicine)
    {
        MedicalCareCategory care = CareLevel(pawn);
        if (care == MedicalCareCategory.NoCare) return false;
        if (requireMedicine && care == MedicalCareCategory.NoMeds) return false;
        return care >= minimum;
    }
}
