using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// An aeroplane's take-off roll by its weight (D'thorga Framework 0.302.0; docs/development/vehicle-c27j.md, "The aircraft in game"): it never
/// hovers, so its roll comes from its weight against its maximum take-off weight, not from a hover margin as a rotorcraft's does (RollRule).
/// A take-off run grows about with the square of the weight (the lift needed grows with it, and so does the speed needed to make it) **[I]**;
/// at its maximum take-off weight it needs its full roll; over it, it is too heavy. Game-free.
/// </summary>
public static class TakeoffRule
{
    /// <summary>The share of its maximum take-off weight it may be over and still go (none: a real limit).</summary>
    public const double OverweightAllowance = 0;

    /// <summary>Whether it may take off at this weight.</summary>
    public static bool MayTakeOff(double massKg, double maxTakeoffKg) =>
        maxTakeoffKg <= 0 || massKg <= maxTakeoffKg * (1 + OverweightAllowance);

    /// <summary>
    /// Its roll in cells at this weight: from its shortest (empty) to its full (at its maximum take-off weight), by the square of the weight's
    /// share; never under the shortest.
    /// </summary>
    public static int RollCells(double massKg, double maxTakeoffKg, int shortestCells, int fullCells)
    {
        if (maxTakeoffKg <= 0) return fullCells;
        double share = Math.Max(0, Math.Min(1, massKg / maxTakeoffKg));
        double cells = fullCells * share * share;
        return Math.Max(shortestCells, (int)Math.Ceiling(cells));
    }

    /// <summary>How many kilograms it must shed to take off, when it is too heavy; 0 otherwise.</summary>
    public static double ShedKg(double massKg, double maxTakeoffKg) =>
        MayTakeOff(massKg, maxTakeoffKg) ? 0 : massKg - maxTakeoffKg * (1 + OverweightAllowance);
}
