using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How fast a weapon works when its mount is damaged or its gunner is hurt (owner, 16 and 17 Nov 2026;
/// docs/development/vehicle-knocked-out-systems.md, T2 and C4). Game-free. A pace is a share of the weapon's own speed: 1 is as its
/// author made it; a turn rate is multiplied by it, a time divided by it.
/// </summary>
public static class TurretPaceRule
{
    /// <summary>The slowest a pace can go, so a time never becomes endless.</summary>
    public const double FloorPace = 0.05;

    /// <summary>
    /// The pace from the mount's health: full at full health, falling in a straight line to <paramref name="slowest"/> at the line where the
    /// weapon stops (<paramref name="stopsAt"/>). Below that line the weapon is stopped anyway, and the slowest pace holds.
    /// </summary>
    public static double DamagePace(double health, double stopsAt, double slowest)
    {
        if (health >= 1) return 1;
        if (health <= stopsAt || stopsAt >= 1) return Math.Max(FloorPace, slowest);
        double share = (health - stopsAt) / (1 - stopsAt);
        return Math.Max(FloorPace, slowest + (1 - slowest) * share);
    }

    /// <summary>The pace from the gunner's capacity (sight to aim, manipulation to traverse and load): never faster than a healthy gunner,
    /// never under <paramref name="floor"/>. Nobody at the weapon: 1 (the weapon cannot fire unmanned anyway).</summary>
    public static double CrewPace(double? capacity, double floor) =>
        capacity == null ? 1 : Math.Max(Math.Max(FloorPace, floor), Math.Min(1, capacity.Value));

    /// <summary>What a time (aiming, reloading) is multiplied by at this pace.</summary>
    public static double TimeFactor(double pace) => 1 / Math.Max(FloorPace, Math.Min(1, pace));
}
