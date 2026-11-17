using Phobos.Framework.Rules;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Health;

/// <summary>
/// How a doctor holds a patient while treating them (Framework 0.126.0; <see cref="FieldCareRule"/>). In a bed, the game's own hold, as
/// before. On the ground, a hold on our own reservation layer: the game counts a hold only against others on the same layer
/// (ReservationManager.CanReserve), so two doctors of ours still cannot double up on one patient, while a carrier (the game's, Search
/// and Rescue's, any mod's) reserves on the game's layer and lifts the patient regardless. Lifting despawns the patient, which ends
/// the doctor's job by its own fail condition. Every job of ours that goes to a patient reserves through here, in its work giver and its
/// driver alike, so both ask about the same layer.
/// </summary>
public static class FieldCare
{
    /// <summary>The layer a doctor's hold on this patient goes on: ours on the ground, the game's (null) in a bed.</summary>
    public static ReservationLayerDef? LayerFor(Pawn patient) =>
        FieldCareRule.HoldKeepsCarriersAway(Patients.OnTheGround(patient)) ? null : PhobosHealthDefOf.PhobosFieldCare;

    /// <summary>The work giver's check. <paramref name="forced"/> (a player's order) ignores other holds, as the game's own checks do.</summary>
    public static bool CanReserve(Pawn doctor, Pawn patient, bool forced = false) =>
        doctor.CanReserve(patient, 1, -1, LayerFor(patient), forced);

    /// <summary>The driver's hold, taken before the doctor sets off.</summary>
    public static bool Reserve(Pawn doctor, Pawn patient, Job job, bool errorOnFailed) =>
        doctor.Reserve(patient, job, 1, -1, LayerFor(patient), errorOnFailed);

    /// <summary>Whether someone else has lifted the patient: what ends field care on the ground.</summary>
    public static bool BeingCarried(Pawn patient) => patient.ParentHolder is Pawn_CarryTracker;
}

[DefOf]
public static class PhobosHealthDefOf
{
    /// <summary>The reservation layer for care given on the ground (Defs/Phobos/Framework/FieldCare.xml).</summary>
    public static ReservationLayerDef PhobosFieldCare = null!;

    static PhobosHealthDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PhobosHealthDefOf));
}
