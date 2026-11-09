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

    /// <summary>The path cost at or above which walkable water is chest-deep. The game's shallow water costs 30 and its chest-deep water 42; reading the cost lets another mod's water count without naming it.</summary>
    public const int ChestDeepPathCost = 40;

    /// <summary>Whether water hides a hull: what nobody can wade always does, and chest-deep water does for a design built small enough to use it.</summary>
    public static bool DeepEnough(bool isWater, bool impassable, int pathCost, bool divesInChestDeepWater) => isWater && (impassable || (divesInChestDeepWater && pathCost >= ChestDeepPathCost));

    /// <summary>How well a watcher can see into the water just now, from 0 to 1: the weather's own effect on aim, and the dark, which never hides a hull entirely.</summary>
    public static float Visibility(float weatherAccuracy, float skyGlow, float darkFloor)
    {
        float floor = Math.Clamp(darkFloor, 0f, 1f);
        return Math.Clamp(weatherAccuracy, 0f, 1f) * (floor + (1f - floor) * Math.Clamp(skyGlow, 0f, 1f));
    }

    /// <summary>The chance one watcher spots a hull in one look: highest right on top of it, nothing at the edge of its range, higher for a boat on the move.</summary>
    public static float SpotChance(float chanceAtContact, float distance, float range, bool moving, float movingFactor, float visibility, float noise)
    {
        if (range <= 0f || distance >= range) return 0f;
        float closeness = 1f - Math.Max(0f, distance) / range;
        return Math.Clamp(chanceAtContact * closeness * (moving ? Math.Max(1f, movingFactor) : 1f) * Math.Clamp(visibility, 0f, 1f) * Math.Max(0f, noise), 0f, 1f);
    }

    /// <summary>Whether a watcher is still following a hull at this moment.</summary>
    public static bool Tracked(int now, int trackedUntil) => now < trackedUntil;

    /// <summary>Whether an empty reserve brings the boat up, as it does unless the crew has been told to stay down.</summary>
    public static bool EmptyForcesUp(float reserve, bool stayDown) => reserve <= 0f && !stayDown;

    /// <summary>Whether the crew is breathing what is left: fully under, nothing in reserve, and told to stay.</summary>
    public static bool BreathingStaleAir(DivePhase phase, float reserve, bool stayDown) => phase == DivePhase.Submerged && reserve <= 0f && stayDown;
}
