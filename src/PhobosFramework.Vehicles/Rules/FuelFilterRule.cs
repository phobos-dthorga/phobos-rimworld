using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A fuel filter that clogs (Framework 0.365.0; docs/development/vehicle-fuel-filter.md; the owner's answers of 28 Nov 2026: weaken only by
/// default, a setting lets it starve the engine, and stale depot fuel is dirty fuel). Game-free. The filter's lines are the land fuel
/// system's stages (Systems_Lubrication.xml), read here for the aircraft's engines, which work out their feed in code.
/// </summary>
public static class FuelFilterRule
{
    /// <summary>From here a lamp lights: the filter is dirty, the engine still fed.</summary>
    public const double DirtyFrom = 0.5;
    /// <summary>From here the filter feeds poorly: the engine gives <see cref="CloggedOrder"/> of its power.</summary>
    public const double CloggedFrom = 0.7;
    /// <summary>From here it is blocked: with the player's setting, the engine is starved and stops.</summary>
    public const double BlockedFrom = 0.95;
    /// <summary>What a clogged filter leaves of an engine's power: three quarters, as a weak fuel pump does.</summary>
    public const double CloggedOrder = 0.75;

    /// <summary>What a filter leaves of an engine's working order: all of it until clogged, three quarters from there, and nothing once
    /// blocked when <paramref name="mayStarve"/> (the player's setting), else still three quarters.</summary>
    public static double Order(double clogged, bool mayStarve) =>
        clogged >= BlockedFrom && mayStarve ? 0 : clogged >= CloggedFrom ? CloggedOrder : 1;

    /// <summary>
    /// How dirty a depot's fuel is from its age: clean while fresh (<paramref name="freshDays"/>), then dirtier day by day, wholly dirty
    /// at twice that age (authored: stale fuel holds the gums and water of a store kept too long).
    /// </summary>
    public static double DepotDirt(double ageDays, double freshDays)
    {
        if (freshDays <= 0) return 0;
        return Clamp01((ageDays - freshDays) / freshDays);
    }

    /// <summary>The tank's dirt after <paramref name="added"/> units of fuel at <paramref name="addedDirt"/> join <paramref name="held"/>
    /// units at <paramref name="heldDirt"/>: the average, weighed by amount. Clean fuel dilutes it; an empty tank takes the new fuel's.</summary>
    public static double Mix(double held, double heldDirt, double added, double addedDirt)
    {
        held = Math.Max(0, held);
        added = Math.Max(0, added);
        double total = held + added;
        return total <= 0 ? Clamp01(heldDirt) : Clamp01((held * Clamp01(heldDirt) + added * Clamp01(addedDirt)) / total);
    }

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
