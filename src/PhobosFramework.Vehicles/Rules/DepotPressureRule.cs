using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What time does to a depot, game-free (Framework 0.200.0; docs/development/vehicle-depots.md, batch 2: DP7, DP8, DP9). Every pressure has
/// a cause the player can see coming, and no roll: a hostile settlement's distance sets when it finds the depot; frost or a wet climate
/// wears what stands there; stored fuel goes stale after months. Every figure is authored, a ballpark. The real effects behind DP9 are
/// ordinary (petrol loses its light ends and oxidises in storage; diesel gathers water and gums), but the months given are unverified;
/// chemfuel is the game's own and follows the same rule, a gameplay choice.
/// </summary>
public static class DepotPressureRule
{
    /// <summary>A hostile settlement this many tiles away or nearer finds a depot (authored).</summary>
    public const double FoundWithinTiles = 6;

    /// <summary>Days it takes to find it for each tile of distance (authored: next door in a few days, six tiles away in about a month).</summary>
    public const double DaysPerTile = 4;

    /// <summary>The fewest days before a depot is found, however near (authored).</summary>
    public const double FewestDays = 2;

    /// <summary>The days until a hostile settlement this far away finds the depot, or null when it is too far to.</summary>
    public static double? DaysToBeFound(double tiles) =>
        tiles < 0 || tiles > FoundWithinTiles ? null : Math.Max(FewestDays, DaysPerTile * tiles);

    /// <summary>Below this temperature, from the season, a depot weathers (authored: freezing).</summary>
    public const double FrostCelsius = 0;

    /// <summary>A climate this wet or wetter weathers a depot all year (authored, millimetres a year: a wet temperate forest's rain).</summary>
    public const double WetMillimetres = 2000;

    /// <summary>The share of each outer part's full health a vehicle loses for each day it weathers (authored).</summary>
    public const double WearSharePerDay = 0.003;

    /// <summary>The share of loose fuel cans that leak away for each day a depot weathers (authored).</summary>
    public const double CanLeakSharePerDay = 0.005;

    /// <summary>Whether a depot weathers today: frost, or a wet climate.</summary>
    public static bool Weathers(double seasonCelsius, double rainfallMillimetres) =>
        seasonCelsius < FrostCelsius || rainfallMillimetres >= WetMillimetres;

    /// <summary>Days stored fuel keeps before it starts to go stale (authored, a ballpark).</summary>
    public const double FreshDays = 90;

    /// <summary>The share of the stored fuel that goes stale each day after that (authored).</summary>
    public const double StaleSharePerDay = 0.01;

    /// <summary>The share of the fuel that goes stale today, fuel of this age in days.</summary>
    public static double StaleShare(double ageDays) => ageDays > FreshDays ? StaleSharePerDay : 0;

    /// <summary>
    /// Whole units lost today and the fraction carried to tomorrow: a share of a stock rarely makes a whole unit, so the remainder is kept
    /// rather than rolled for, and a small stock still goes, a unit at a time.
    /// </summary>
    public static (int Units, double Carry) WholeUnits(double stock, double share, double carry)
    {
        double owed = Math.Max(0, stock) * Math.Max(0, share) + Math.Max(0, carry);
        int units = (int)Math.Floor(owed);
        return (Math.Min(units, (int)Math.Max(0, stock)), owed - units);
    }

    /// <summary>The fuel's age clock after more is added: the average of the old fuel's time and now, weighed by the amounts, so topping a
    /// depot up freshens it in proportion and no more.</summary>
    public static double BlendClock(double oldUnits, double oldClock, double addedUnits, double now)
    {
        if (addedUnits <= 0) return oldClock;
        if (oldUnits <= 0) return now;
        return (oldUnits * oldClock + addedUnits * now) / (oldUnits + addedUnits);
    }
}
