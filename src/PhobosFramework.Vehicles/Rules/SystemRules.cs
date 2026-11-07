using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Which stage a quantity is in. Game-free.</summary>
public static class StageRule
{
    /// <summary>
    /// The first stage whose range holds the value (from inclusive, to exclusive; NaN means open-ended), or -1 when none does. Stages
    /// may leave gaps: a value in a gap is in no stage and nothing happens.
    /// </summary>
    public static int StageIndex(double value, IReadOnlyList<double> from, IReadOnlyList<double> to)
    {
        for (int i = 0; i < from.Count; i++)
        {
            bool aboveFloor = double.IsNaN(from[i]) || value >= from[i];
            bool belowCeiling = double.IsNaN(to[i]) || value < to[i];
            if (aboveFloor && belowCeiling) return i;
        }
        return -1;
    }

    /// <summary>Whether the stages are in rising order with no overlap, for the schema check.</summary>
    public static bool Ordered(IReadOnlyList<double> from, IReadOnlyList<double> to)
    {
        double previousTop = double.NegativeInfinity;
        for (int i = 0; i < from.Count; i++)
        {
            double bottom = double.IsNaN(from[i]) ? double.NegativeInfinity : from[i];
            double top = double.IsNaN(to[i]) ? double.PositiveInfinity : to[i];
            if (top <= bottom || bottom < previousTop) return false;
            previousTop = top;
        }
        return true;
    }
}

/// <summary>An electrical supply against its demand, in amperes. Game-free.</summary>
public static class ElectricalBalance
{
    /// <summary>What is left over for charging the battery.</summary>
    public static double Surplus(double supplyAmps, double demandAmps) => Math.Max(0, supplyAmps - demandAmps);

    /// <summary>What the battery has to make up.</summary>
    public static double Deficit(double supplyAmps, double demandAmps) => Math.Max(0, demandAmps - supplyAmps);
}

/// <summary>Breakdown odds as the game rolls them: a mean time between failures. Game-free.</summary>
public static class FailureRule
{
    /// <summary>The chance of at least one failure in <paramref name="hours"/> for a mean time between failures in hours.</summary>
    public static double ChanceIn(double hours, double mtbHours)
    {
        if (hours <= 0) return 0;
        if (mtbHours <= 0) return 1;
        return 1 - Math.Exp(-hours / mtbHours);
    }
}

/// <summary>How much a loadout claims a vehicle: a named vehicle beats a whole mod, which beats a match by what the vehicle does.</summary>
public enum LoadoutPriority
{
    Default,
    Effect,
    Mod,
    Vehicle,
}

/// <summary>Picks the loadout for a vehicle among those that match. Game-free.</summary>
public static class LoadoutChoice
{
    /// <summary>
    /// The index of the winning candidate: the highest priority, and among equals the last loaded. <paramref name="tied"/> says
    /// whether another candidate had the same priority, so the load line can warn.
    /// </summary>
    public static int Pick(IReadOnlyList<LoadoutPriority> priorities, out bool tied)
    {
        tied = false;
        int best = -1;
        for (int i = 0; i < priorities.Count; i++)
        {
            if (best < 0 || priorities[i] > priorities[best])
            {
                best = i;
                tied = false;
            }
            else if (priorities[i] == priorities[best])
            {
                best = i;
                tied = true;
            }
        }
        return best;
    }
}

/// <summary>A system's parameters: its defaults, then each loadout's overrides in order. Game-free.</summary>
public static class ParameterRule
{
    public static Dictionary<string, double> Resolve(IEnumerable<KeyValuePair<string, double>> defaults,
        IEnumerable<KeyValuePair<string, double>> overrides, ICollection<string>? unknown = null)
    {
        var result = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, double> pair in defaults) result[pair.Key] = pair.Value;
        foreach (KeyValuePair<string, double> pair in overrides)
        {
            if (!result.ContainsKey(pair.Key)) unknown?.Add(pair.Key);
            result[pair.Key] = pair.Value;
        }
        return result;
    }
}

/// <summary>How many game hours have passed since a system last ran, capped. Game-free.</summary>
public static class ElapsedRule
{
    public const int TicksPerHour = 2500;

    /// <summary>Hours since <paramref name="lastTick"/>, 0 when time has not moved or went backwards, never above the cap.</summary>
    public static double Hours(int nowTick, int lastTick, double capHours)
    {
        if (lastTick < 0 || nowTick <= lastTick) return 0;
        double hours = (nowTick - lastTick) / (double)TicksPerHour;
        return hours > capHours ? capHours : hours;
    }
}
