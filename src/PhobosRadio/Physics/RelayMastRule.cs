using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// How a relay mast left on the world map is lost (Radio 0.37.0; the owner's cards of 27 Nov 2026: "Raids and wear", behind a setting that
/// starts off as a high risk). Game-free; compiled into the tests. Every figure is authored, for the owner to revise.
/// <para><b>Wear</b>: a mast loses a share of its condition each day in the weather, and passes nothing on once worn out, until a convoy
/// services it. <b>Raids</b>: a mast within reach of a hostile faction's settlement is knocked down now and then, more often the nearer it
/// stands.</para>
/// </summary>
public static class RelayMastRule
{
    /// <summary>Days a mast stands before it is worn out: a RimWorld year (authored).</summary>
    public const double DaysToWearOut = 60;
    /// <summary>Below this condition it passes nothing on (authored: a mast a tenth worn still works).</summary>
    public const double WorkingFloor = 0.1;
    /// <summary>World tiles within which a hostile settlement's raiders find a mast (authored).</summary>
    public const double RaidReachTiles = 12;
    /// <summary>Mean days between raids on a mast standing right beside a hostile settlement (authored).</summary>
    public const double RaidMeanDaysAdjacent = 20;
    /// <summary>Mean days at the edge of their reach (authored).</summary>
    public const double RaidMeanDaysAtEdge = 120;

    /// <summary>The condition after this many days of weather, never below nothing.</summary>
    public static double Worn(double condition, double days) => Math.Max(0, condition - Math.Max(0, days) / DaysToWearOut);

    /// <summary>Whether a mast in this condition still passes calls on.</summary>
    public static bool Works(double condition) => condition >= WorkingFloor;

    /// <summary>
    /// The mean days between raids on a mast this many tiles from the nearest hostile settlement, from beside it to the edge of their
    /// reach in a straight line; beyond their reach, none (positive infinity).
    /// </summary>
    public static double RaidMeanDays(double tilesToHostile)
    {
        if (tilesToHostile > RaidReachTiles) return double.PositiveInfinity;
        double t = Math.Max(0, tilesToHostile) / RaidReachTiles;
        return RaidMeanDaysAdjacent + (RaidMeanDaysAtEdge - RaidMeanDaysAdjacent) * t;
    }
}
