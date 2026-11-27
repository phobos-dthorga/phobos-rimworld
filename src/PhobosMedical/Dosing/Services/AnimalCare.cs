using Phobos.Framework.Health;
using RimWorld;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>
/// Which of the colony's animals the doctors dose (Medical 0.34.0; the owner's card of 27 Nov 2026: "Bonded or with a master"): the ones
/// the colony would grieve. Behind its own setting, on. Which needs an animal can have is each care need's Def (<c>forAnimals</c>).
/// </summary>
public static class AnimalCare
{
    public static bool Covered(Pawn pawn) =>
        DosingMod.Settings.animalCare && Patients.IsPlayersAnimal(pawn)
        && (pawn.playerSettings?.Master != null || pawn.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Bond) != null);

    /// <summary>One of the colony's people, or an animal the doctors look after.</summary>
    public static bool IsPersonOrCoveredAnimal(Pawn pawn) => Patients.IsPlayersPerson(pawn) || Covered(pawn);
}
