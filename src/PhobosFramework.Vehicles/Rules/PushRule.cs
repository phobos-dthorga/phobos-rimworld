using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Pushing a vehicle by hand, game-free (Framework 0.201.0; docs/development/vehicle-bay-trips.md, batch 4; owner, 20 Nov 2026: "push light
/// ones by hand"). What it takes to move the vehicle is the winch's own sum (<see cref="RecoveryRule.Required"/>: its weight times its
/// ground's resistance, far more if its wheels will not turn); what moves it is people, each good for a steady push. Authored: one figure
/// for a person's push, a ballpark and unverified against any table, and a most that can get their hands on one vehicle.
/// </summary>
public static class PushRule
{
    /// <summary>How many people it takes to push against this much resistance, at least one.</summary>
    public static int Pushers(double requiredKgf, double perPersonKgf)
    {
        if (perPersonKgf <= 0) return int.MaxValue;
        double people = Math.Ceiling(Math.Max(0, requiredKgf) / perPersonKgf);
        return people >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)people);
    }

    /// <summary>Whether a vehicle that takes this many can be pushed at all: no more than can get their hands on it.</summary>
    public static bool CanPush(int pushers, int most) => pushers >= 1 && pushers <= most;

    /// <summary>
    /// Ticks to push it one cell with this many pushing: the base pace when they push far more than it takes, slowing to twice it at
    /// their limit, the shape the winch's line speed has (<see cref="RecoveryRule.TicksPerCell"/>).
    /// </summary>
    public static int TicksPerCell(int baseTicks, double requiredKgf, int pushing, double perPersonKgf) =>
        RecoveryRule.TicksPerCell(baseTicks, requiredKgf, Math.Max(0, pushing) * Math.Max(0, perPersonKgf));
}
