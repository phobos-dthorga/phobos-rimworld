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

    /// <summary>
    /// One of the colony's people with a flesh body: what every Phobos treatment needs before anything else. A vehicle is a pawn to
    /// the game but not humanlike, so it never passes (the owner's game, 7 Nov 2026, showed a tank checked as a patient).
    /// </summary>
    public static bool IsFleshPerson(Pawn pawn) => IsPlayersPerson(pawn) && pawn.RaceProps.IsFlesh;

    /// <summary>Lying down where a doctor can treat them: downed, or in a bed.</summary>
    public static bool IsLyingDown(Pawn pawn) => pawn.Downed || pawn.InBed();

    /// <summary>A patient a doctor can treat where they lie: one of the colony's flesh people, downed or in bed. Each mod adds its own
    /// need and care-setting check on top.</summary>
    public static bool IsLyingPatient(Pawn pawn) => IsFleshPerson(pawn) && IsLyingDown(pawn);

    /// <summary>The group a player's person belongs to, or null for anyone the doctors do not look after.</summary>
    public static PatientGroup? GroupOf(Pawn pawn)
    {
        if (!IsPlayersPerson(pawn)) return null;
        if (pawn.IsPrisonerOfColony) return PatientGroup.Prisoner;
        if (pawn.IsSlaveOfColony) return PatientGroup.Slave;
        if (pawn.Faction == Faction.OfPlayer) return PatientGroup.Colonist;
        return PatientGroup.Guest;
    }

    /// <summary>
    /// Whether something stops surgeons operating on this patient for now, and why (in-world, from the mod that owns the bar). A mod
    /// that queues operations holds them while this is true, rather than withdrawing them. See <see cref="SurgeryBars"/>.
    /// </summary>
    public static bool SurgeryBarred(Pawn pawn, out string? reason)
    {
        reason = SurgeryBars.ReasonFor(pawn);
        return reason != null;
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
