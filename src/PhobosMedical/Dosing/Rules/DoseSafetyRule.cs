namespace Phobos.Dosing.Rules;

/// <summary>Why a dose is not given; None means it may be.</summary>
public enum DoseRefusal
{
    None,
    NoPainRelief,
    SedativeNotWanted,
    RecreationalOff,
    PatientWillNotTake,
    StillActive,
    Overdosed,
    DangerousDose,
    Consciousness,
    Breathing,
    NoImmunityGain,
    /// <summary>The patient is bleeding hard: a painkiller waits until that is under control.</summary>
    BleedingHold,
    /// <summary>Self-medication only: the pawn's drug policy does not allow it. Never reaches a doctor's refusal text.</summary>
    PolicyForbids,
}

/// <summary>The patient's figures the safety rule needs, read by the service.</summary>
public readonly struct PatientState
{
    public readonly float Consciousness;
    public readonly float Breathing;
    /// <summary>The game's drug overdose severity, 0 when none.</summary>
    public readonly float Overdose;
    /// <summary>Whether the game says the patient would take this drug (teetotalers and precepts refuse).</summary>
    public readonly bool WillTake;
    /// <summary>How fast the patient is bleeding, the figure the game shows per day; 1 is 100% a day.</summary>
    public readonly float BleedRate;

    public PatientState(float consciousness, float breathing, float overdose, bool willTake, float bleedRate = 0f)
    {
        BleedRate = bleedRate;
        Consciousness = consciousness;
        Breathing = breathing;
        Overdose = overdose;
        WillTake = willTake;
    }
}

/// <summary>The authored limits and the two player settings the safety rule reads.</summary>
public readonly struct DoseLimits
{
    /// <summary>No new dose while an earlier one's condition is still at this share of a dose or more.</summary>
    public readonly float RedoseBelowFraction;
    public readonly float OverdoseLimit;
    /// <summary>Consciousness and breathing must stay at or above this after the dose's offsets.</summary>
    public readonly float CapacityFloor;
    public readonly bool RecreationalAllowed;
    /// <summary>Give medical drugs even to a patient who would refuse drugs on principle.</summary>
    public readonly bool IgnoreAversionForMedical;
    /// <summary>A painkiller waits while the patient bleeds this fast or faster.</summary>
    public readonly float BleedHoldRate;

    public DoseLimits(float redoseBelowFraction, float overdoseLimit, float capacityFloor, bool recreationalAllowed, bool ignoreAversionForMedical, float bleedHoldRate = float.MaxValue)
    {
        BleedHoldRate = bleedHoldRate;
        RedoseBelowFraction = redoseBelowFraction;
        OverdoseLimit = overdoseLimit;
        CapacityFloor = capacityFloor;
        RecreationalAllowed = recreationalAllowed;
        IgnoreAversionForMedical = ignoreAversionForMedical;
    }
}

/// <summary>
/// Every reason not to give a dose, checked in order: it must do what the need asks; a sedative only when sedation is wanted;
/// recreational drugs only by setting; the patient must be willing (or the setting overrides it for medical drugs); the
/// last dose must have worn off; no overdose under way; the dose must not reach a dangerous stage; and its offsets must
/// leave consciousness and breathing above the floor, because the game kills at zero. Game-free.
/// </summary>
public static class DoseSafetyRule
{
    public static DoseRefusal Check(DoseEffect effect, DoseNeed need, float activeFraction, bool reachesDanger, PatientState patient, DoseLimits limits)
    {
        if (need == DoseNeed.Immunity)
        {
            if (effect.ImmunityGain <= 0f) return DoseRefusal.NoImmunityGain;
        }
        else if (!effect.RelievesPain)
        {
            return DoseRefusal.NoPainRelief;
        }
        if (effect.Sedative && need != DoseNeed.Sedation) return DoseRefusal.SedativeNotWanted;
        if (effect.Recreational && !limits.RecreationalAllowed) return DoseRefusal.RecreationalOff;
        if (!patient.WillTake && (effect.Recreational || !limits.IgnoreAversionForMedical)) return DoseRefusal.PatientWillNotTake;
        // Some painkillers raise bleeding in ways a mod applies from its own code, which the index cannot see. A sedative is exempt, because
        // a patient past the pain-shock band still needs relief, and a drug for immunity is not a painkiller.
        if (need != DoseNeed.Immunity && !effect.Sedative && patient.BleedRate >= limits.BleedHoldRate) return DoseRefusal.BleedingHold;
        if (activeFraction >= limits.RedoseBelowFraction) return DoseRefusal.StillActive;
        if (patient.Overdose >= limits.OverdoseLimit) return DoseRefusal.Overdosed;
        if (reachesDanger) return DoseRefusal.DangerousDose;
        if (patient.Consciousness - effect.ConsciousnessCost < limits.CapacityFloor) return DoseRefusal.Consciousness;
        if (patient.Breathing - effect.BreathingCost < limits.CapacityFloor) return DoseRefusal.Breathing;
        return DoseRefusal.None;
    }
}
