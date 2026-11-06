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
