using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Medical.Rules;

/// <summary>What an oxygen machine is doing, in the order a player needs to fix things.</summary>
public enum OxygenStatus
{
    SwitchedOff,
    NoPower,
    NoBed,
    NoOxygen,
    Waiting,
    /// <summary>Someone is in a linked bed, but nobody there needs oxygen.</summary>
    NotNeeded,
    Treating,
}

/// <summary>What a ventilator is doing, in the order a player needs to fix things.</summary>
public enum VentilatorStatus
{
    SwitchedOff,
    NoPower,
    NoBed,
    Waiting,
    /// <summary>Someone in a linked bed breathes well enough on their own.</summary>
    NotNeeded,
    /// <summary>Someone needs it but has no breathing tube yet.</summary>
    NeedsTube,
    Treating,
}

/// <summary>
/// How a ventilator holds failing breathing up. Game-free. It pushes air in through a tube until breathing reaches a floor; it adds
/// nothing to breathing already at the floor, and nothing at zero, where no lung is left to fill (an open tube means zero is not a shut
/// airway). The floor and the step are authored, from the rules Def.
/// </summary>
public static class VentilatorRule
{
    public static bool Needs(float breathingWithout, float floor) => breathingWithout > 0f && breathingWithout < floor;

    /// <summary>The treatment stage to use: stage n adds n steps of breathing, the fewest that reach the floor, up to the last stage.</summary>
    public static int StageFor(float breathingWithout, float floor, float step, int lastStage)
    {
        if (!Needs(breathingWithout, floor) || step <= 0f || lastStage <= 0) return 0;
        int stage = (int)System.Math.Ceiling((floor - breathingWithout) / step - 0.0001f);
        if (stage < 1) stage = 1;
        return stage > lastStage ? lastStage : stage;
    }

    public static VentilatorStatus Status(bool switchedOn, bool powered, int linkedBeds, int treating, int needingTube, int notNeeding)
    {
        if (!switchedOn) return VentilatorStatus.SwitchedOff;
        if (!powered) return VentilatorStatus.NoPower;
        if (linkedBeds <= 0) return VentilatorStatus.NoBed;
        if (treating > 0) return VentilatorStatus.Treating;
        if (needingTube > 0) return VentilatorStatus.NeedsTube;
        return notNeeding > 0 ? VentilatorStatus.NotNeeded : VentilatorStatus.Waiting;
    }
}

/// <summary>Who gets oxygen, how much it costs and what the machine should say. Game-free. Every figure is passed in from the rules Def.</summary>
public static class OxygenRule
{
    const int TicksPerHour = 2500;

    /// <summary>
    /// A patient needs oxygen with a condition it helps, or breathing below the threshold whatever the cause, so any mod's lung
    /// damage qualifies. Breathing at zero means no air reaches the lungs at all, so oxygen could not help: that patient needs
    /// their airway opened.
    /// </summary>
    public static bool NeedsOxygen(float breathing, bool responsiveCondition, float breathingThreshold) =>
        breathing > 0f && (responsiveCondition || breathing < breathingThreshold);

    /// <summary>Normal breathing, which supplied oxygen never pushes past: blood that is already nearly saturated can carry almost no more.</summary>
    public const float NormalBreathing = 1f;

    /// <summary>
    /// Whether oxygen should raise this patient's breathing, given their breathing without it. Only below normal: a patient at or above
    /// normal (healthy or bionic lungs, treated for carbon monoxide) keeps exactly what they have.
    /// </summary>
    public static bool RaisesBreathing(float breathingWithoutOxygen) => breathingWithoutOxygen < NormalBreathing;

    /// <summary>How many times faster a treatment clears carbon monoxide than room air, from the two half-lives; 0 if either is unset.</summary>
    public static float TimesFaster(float roomAirHalfLifeHours, float treatmentHalfLifeHours) =>
        roomAirHalfLifeHours > 0f && treatmentHalfLifeHours > 0f ? roomAirHalfLifeHours / treatmentHalfLifeHours : 0f;

    /// <summary>The fuel the patients use in one interval.</summary>
    public static float FuelForInterval(int patients, float fuelPerPatientHour, int intervalTicks)
    {
        if (patients <= 0 || fuelPerPatientHour <= 0f || intervalTicks <= 0) return 0f;
        return patients * fuelPerPatientHour * intervalTicks / TicksPerHour;
    }

    public static OxygenStatus Status(bool switchedOn, bool powered, int linkedBeds, bool hasOxygen, int patients, int notNeeding = 0)
    {
        if (!switchedOn) return OxygenStatus.SwitchedOff;
        if (!powered) return OxygenStatus.NoPower;
        if (linkedBeds <= 0) return OxygenStatus.NoBed;
        if (!hasOxygen) return OxygenStatus.NoOxygen;
        if (patients > 0) return OxygenStatus.Treating;
        return notNeeding > 0 ? OxygenStatus.NotNeeded : OxygenStatus.Waiting;
    }

    /// <summary>The patients a machine can serve: the worst breathing first, up to its limit. Ties keep their order.</summary>
    public static List<T> WorstFirst<T>(IEnumerable<T> patients, Func<T, float> breathing, int limit)
    {
        if (limit <= 0) return new List<T>();
        return patients.OrderBy(breathing).Take(limit).ToList();
    }
}
