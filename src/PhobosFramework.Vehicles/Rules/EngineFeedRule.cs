using System;
using Phobos.Framework.Rules;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Each aircraft engine's own fuel and oil (Framework 0.360.0; docs/development/vehicle-standard.md, batch 3; the owner's cards of 27 Nov
/// 2026: each engine its own pumps and oil, real consequences in the air behind a setting, jet fuel freezing at its real point). Game-free.
/// The thresholds are the land fuel and lubrication systems' own (Systems_Lubrication.xml), so a pump or an oil level reads alike on a
/// truck and an aircraft.
/// </summary>
public static class EngineFeedRule
{
    /// <summary>Below this share of a pump's work no fuel reaches its engine, which stops (the land fuel feed's "none" stage).</summary>
    public const double FeedNone = 0.1;
    /// <summary>Below this an engine's pump feeds it poorly (the land fuel feed's "weak" stage).</summary>
    public const double FeedWeak = 0.5;
    /// <summary>What a poorly fed engine still gives (the land "weak" stage's 0.75).</summary>
    public const double WeakOrder = 0.75;
    /// <summary>Oil pressure below this reaches nothing: the engine seizes while it runs (the land "gone" stage).</summary>
    public const double PressureGone = 0.15;
    /// <summary>Oil pressure below this is low: the engine wears while it runs (the land "low" stage).</summary>
    public const double PressureLow = 0.45;
    /// <summary>Fuel this far frozen stops every engine (half way from its freezing point to solid; authored).</summary>
    public const double FrozenStops = 0.5;
    /// <summary>The coldest the air around an aircraft is taken to be, °C (authored: about the coldest the stratosphere reaches over the
    /// poles in winter).</summary>
    public const double ColdestAirCelsius = -80;

    const double KelvinOffset = 273.15;

    /// <summary>What an engine's working order becomes from its pump's work: nothing starved, three quarters fed poorly, else whole.</summary>
    public static double FeedOrder(double feed) => feed < FeedNone ? 0 : feed < FeedWeak ? WeakOrder : 1;

    /// <summary>Oil reaching an engine's bearings, 0 to 1: its pump's work times what its sump level allows (the land system's curve:
    /// nothing empty, 0.4 at 30%, all from 60%).</summary>
    public static double OilPressure(double pumpEfficiency, double oilLevel)
    {
        double level = Clamp01(oilLevel);
        double fromLevel = level <= 0.3 ? level / 0.3 * 0.4 : level >= 0.6 ? 1 : 0.4 + (level - 0.3) / 0.3 * 0.6;
        return Clamp01(pumpEfficiency) * fromLevel;
    }

    /// <summary>An engine's sump after <paramref name="hours"/> of running at <paramref name="output"/> (0 to 1), burning
    /// <paramref name="perHour"/> at full output; a worn engine burns more, twice as much at 40% health and below (the land crosslink 3).</summary>
    public static double OilAfterBurn(double level, double perHour, double output, double engineHealthShare, double hours)
    {
        double health = Math.Min(1, Math.Max(0.4, engineHealthShare));
        double worn = 2 - (health - 0.4) / 0.6;
        return Clamp01(level - Math.Max(0, perHour) * Clamp01(output) * worn * Math.Max(0, hours));
    }

    /// <summary>An engine's sump after its engine or oil pump lost <paramref name="shareLost"/> of its health to a hit, each share losing
    /// <paramref name="perShare"/> of the sump.</summary>
    public static double OilAfterHit(double level, double shareLost, double perShare) => Clamp01(level - Math.Max(0, shareLost) * Math.Max(0, perShare));

    /// <summary>How far the fuel has frozen, 0 to 1: nothing at its freezing point, all at <paramref name="solidCelsius"/>.</summary>
    public static double Frozen(double fuelCelsius, double freezeCelsius, double solidCelsius)
    {
        if (solidCelsius >= freezeCelsius) return fuelCelsius <= freezeCelsius ? 1 : 0;
        return Clamp01((freezeCelsius - fuelCelsius) / (freezeCelsius - solidCelsius));
    }

    /// <summary>The air at <paramref name="cruiseMetres"/> above the sea, from the ground's air at <paramref name="groundMetres"/>: the
    /// standard atmosphere's fall of 6.5 °C a kilometre, which stops at the top of the troposphere (11 km), never colder than
    /// <see cref="ColdestAirCelsius"/>. Below the ground reads as the ground.</summary>
    public static double AirAtHeight(double groundCelsius, double groundMetres, double cruiseMetres)
    {
        double top = AirRule.TroposphereTopMetres;
        double from = Math.Min(top, Math.Max(0, groundMetres));
        double to = Math.Min(top, Math.Max(0, cruiseMetres));
        double air = groundCelsius - AirRule.LapseKelvinPerMetre * Math.Max(0, to - from);
        return Math.Max(ColdestAirCelsius, air);
    }

    /// <summary>How much warmer the air is where it meets the aircraft, moving at <paramref name="mach"/>, than the still air around it:
    /// the total air temperature's rise, T × 0.2 M² for air (γ = 1.4).</summary>
    public static double RamRise(double airCelsius, double mach) => Math.Max(0, airCelsius + KelvinOffset) * 0.2 * mach * mach;

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
