using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Where a submersible is in its dive. Saved by name, so never rename one.</summary>
public enum DivePhase
{
    Surfaced,
    Diving,
    Submerged,
    Surfacing,
}

/// <summary>
/// The arithmetic of a dive, free of the game (docs/development/submarine-framework.md). The reserve is one number, hours of air and charge at rest; the
/// figures that feed it are authored on each vehicle's Def and are gameplay choices, not measurements.
/// </summary>
public static class DiveRule
{
    /// <summary>The reserve after this many hours. It burns only while fully under, faster on the move, and refills only on the surface. A reserve never exceeds what the parts can hold now.</summary>
    public static float ReserveAfter(float reserve, float capacity, float hours, DivePhase phase, bool moving, float moveBurnFactor, float rechargeFactor)
    {
        float top = Math.Max(0f, capacity);
        float change = phase switch
        {
            DivePhase.Submerged => -hours * (moving ? Math.Max(1f, moveBurnFactor) : 1f),
            DivePhase.Surfaced => hours * Math.Max(0f, rechargeFactor),
            _ => 0f,
        };
        return Math.Clamp(reserve + change, 0f, top);
    }

    /// <summary>Whether there is enough in reserve to go under at all: a boat should not dive to come straight back up.</summary>
    public static bool EnoughToDive(float reserve, float capacity, float minimumShare) => capacity > 0f && reserve > 0f && reserve >= capacity * minimumShare;

    /// <summary>How long the reserve lasts at this pace, in hours.</summary>
    public static float HoursLeft(float reserve, bool moving, float moveBurnFactor) => Math.Max(0f, reserve) / (moving ? Math.Max(1f, moveBurnFactor) : 1f);

    /// <summary>The phase once this many ticks have passed in a changing phase. A settled phase stays.</summary>
    public static DivePhase Settled(DivePhase phase, int ticksInPhase, int ticksNeeded)
    {
        if (ticksInPhase < ticksNeeded) return phase;
        return phase switch
        {
            DivePhase.Diving => DivePhase.Submerged,
            DivePhase.Surfacing => DivePhase.Surfaced,
            _ => phase,
        };
    }

    /// <summary>Whether a boat in this phase is out of sight and out of reach. A hull still flooding its tanks has not got away yet, and one blowing them has already come back.</summary>
    public static bool Hidden(DivePhase phase) => phase == DivePhase.Submerged;

    /// <summary>Whether it is on its way down or already down.</summary>
    public static bool GoingOrGone(DivePhase phase) => phase is DivePhase.Diving or DivePhase.Submerged;

    /// <summary>How much of a hit reaches a boat that is under: nothing unless the damage is of a kind that carries through water.</summary>
    public static float DamageBelow(float amount, bool reaches, float factor) => reaches ? amount * Math.Max(0f, factor) : 0f;
}
