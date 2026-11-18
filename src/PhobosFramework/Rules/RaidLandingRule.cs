using System;

namespace Phobos.Framework.Rules;

/// <summary>How a raiders' aircraft puts its people down (Framework 0.157.0; owner, 19 Nov 2026: "a mix of all three", by chance).</summary>
public enum RaidLanding
{
    /// <summary>Lands on clear ground beyond the colony's turrets, a short walk out.</summary>
    OutsideTurrets,
    /// <summary>Hovers close in and puts them down its ropes, whatever the turrets.</summary>
    RopeClose,
    /// <summary>Lands as close to the colony as it can, whatever the turrets.</summary>
    Closest,
}

/// <summary>
/// Where a raiders' aircraft puts its people down (D'thorga Framework 0.157.0; docs/development/vehicle-flight-over-the-map.md, slice 6's
/// raider half): which of the three ways, by chance, and how a candidate spot scores for it. Distances in cells. Game-free.
/// </summary>
public static class RaidLandingRule
{
    /// <summary>The chance of each way, in the enum's order (authored: the cautious landing most often, the bold one least).</summary>
    public static readonly double[] Weights = { 0.5, 0.3, 0.2 };

    /// <summary>No spot nearer the colony's middle than this: not on top of its buildings (authored).</summary>
    public const double NearestCells = 6;

    /// <summary>How far out from the colony a spot is looked for, cells (authored).</summary>
    public const double FarthestCells = 60;

    /// <summary>Cells beyond a turret's range a cautious landing keeps clear of (authored).</summary>
    public const double TurretMarginCells = 4;

    /// <summary>The way for a roll in [0, 1).</summary>
    public static RaidLanding Choose(double roll)
    {
        double total = 0;
        foreach (double w in Weights) total += Math.Max(0, w);
        double at = Math.Min(Math.Max(0, roll), 0.999999) * total;
        for (int i = 0; i < Weights.Length; i++)
        {
            at -= Math.Max(0, Weights[i]);
            if (at < 0) return (RaidLanding)i;
        }
        return RaidLanding.Closest;
    }

    /// <summary>
    /// A candidate spot's score for a way of landing, lower being better, or infinity where it will not do: too near the middle, too far out,
    /// or, for a cautious landing, within a turret's reach. Every way prefers the spot nearest the colony that it will take.
    /// </summary>
    public static double Score(RaidLanding mode, double distanceCells, bool inTurretReach)
    {
        if (distanceCells < NearestCells || distanceCells > FarthestCells) return double.PositiveInfinity;
        if (mode == RaidLanding.OutsideTurrets && inTurretReach) return double.PositiveInfinity;
        return distanceCells;
    }
}
