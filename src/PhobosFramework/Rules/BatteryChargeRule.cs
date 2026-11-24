using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Charging a battery from the grid, game-free (Framework 0.198.0; docs/development/mech-recharging-real-power.md): how much a battery
/// of a given size holds, how the charge eases off as it nears full, what the charger takes from the wall to put that charge in, what
/// it loses as heat, and how long the charge takes. Energy is conserved: what the wall gives is what the battery gains plus what the
/// charger loses. Fractions are 0 to 1; power is in watts and energy in watt-days, the game's own units. Every figure passed in is the
/// caller's; this class holds none.
/// </summary>
public static class BatteryChargeRule
{
    /// <summary>What a battery holds, in watt-days: its size (a body size, a cell count) times the watt-days one unit of size holds,
    /// times how much larger or smaller than standard it is built (<paramref name="build"/>, 1 for standard).</summary>
    public static double CapacityWattDays(double size, double wattDaysPerSize, double build = 1) =>
        Math.Max(0, size) * Math.Max(0, wattDaysPerSize) * Math.Max(0, build);

    /// <summary>
    /// The share of the full charging rate a battery takes at a charge level: all of it up to <paramref name="easesFrom"/>, then
    /// falling in a straight line to <paramref name="rateWhenFull"/> at full. A simple stand-in for a real charger's constant-voltage
    /// stage, where the current falls away as the cell fills.
    /// </summary>
    public static double Taper(double charge, double easesFrom, double rateWhenFull)
    {
        double floor = Clamp01(rateWhenFull);
        double from = Clamp01(easesFrom);
        double level = Clamp01(charge);
        if (level <= from || from >= 1) return 1;
        return 1 - (1 - floor) * (level - from) / (1 - from);
    }

    /// <summary>
    /// The level after one charging step eased by the taper (Framework 0.286.1). <paramref name="after"/> is the level the charger
    /// reached at its full rate, and <paramref name="fullStep"/> what one step adds when nothing stops it. A charger that stops at the
    /// top adds only the gap left, and easing that gap closes only a share of it each step: the level creeps towards full and never
    /// gets there, until rounding freezes it just short (mechs sat on their rechargers for weeks at 99.9999924 of 100, D'thorga
    /// Patches 0.42.1). So when the charger stopped at the top, the eased step is taken from the full step, and it ends at the top.
    /// </summary>
    public static double TaperedLevel(double before, double after, double fullStep, double taper, double max)
    {
        double gained = after - before;
        if (gained <= 0) return after;
        double step = after >= max ? Math.Max(gained, Math.Max(0, fullStep)) : gained;
        return Math.Min(before + step * Clamp01(taper), max);
    }

    /// <summary>The power going into the battery, in watts: its capacity, times the share of it the full rate adds in a day, times
    /// the taper.</summary>
    public static double StoredWatts(double capacityWattDays, double fractionPerDay, double taper = 1) =>
        Math.Max(0, capacityWattDays) * Math.Max(0, fractionPerDay) * Clamp01(taper);

    /// <summary>The power the charger takes from the wall to put <paramref name="storedWatts"/> into the battery at an efficiency
    /// (PowerUnits.Input, with its floor).</summary>
    public static double WallWatts(double storedWatts, double efficiency) => PowerUnits.Input(storedWatts, efficiency);

    /// <summary>The power the charger loses as heat: what the wall gives less what the battery gains.</summary>
    public static double LostWatts(double storedWatts, double efficiency) => WallWatts(storedWatts, efficiency) - Math.Max(0, storedWatts);

    /// <summary>
    /// Days to charge from one level to another at a full rate of <paramref name="fractionPerDay"/>, with the taper: straight-line
    /// below <paramref name="easesFrom"/>, and above it the exact time under a rate that falls in a straight line. Zero when there is
    /// nothing to add; endless when the rate is nothing, or when the rate at full is nothing and the target is full.
    /// </summary>
    public static double DaysToReach(double from, double to, double fractionPerDay, double easesFrom, double rateWhenFull)
    {
        double start = Clamp01(from), end = Clamp01(to);
        if (end <= start) return 0;
        if (fractionPerDay <= 0) return double.PositiveInfinity;
        double knee = Clamp01(easesFrom), floor = Clamp01(rateWhenFull);
        double days = 0;
        if (start < knee)
        {
            days += (Math.Min(end, knee) - start) / fractionPerDay;
            start = knee;
        }
        if (end <= start) return days;
        // Above the knee the rate is fractionPerDay * (1 - slope * (level - knee)); the time between two levels is its integral.
        double slope = (1 - floor) / (1 - knee);
        if (slope <= 0) return days + (end - start) / fractionPerDay;
        double rateAtStart = 1 - slope * (start - knee), rateAtEnd = 1 - slope * (end - knee);
        if (rateAtEnd <= 0) return double.PositiveInfinity;
        return days + Math.Log(rateAtStart / rateAtEnd) / (fractionPerDay * slope);
    }

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
