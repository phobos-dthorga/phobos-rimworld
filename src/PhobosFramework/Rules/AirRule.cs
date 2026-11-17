using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Thin air at height (D'thorga Framework 0.132.0; docs/development/vehicle-altitude-and-turbines.md): the standard atmosphere's pressure
/// and density against elevation in metres, and what an engine that breathes it has left. The pressure and density formulas are the
/// International Standard Atmosphere's for the troposphere (sea level 288.15 K, falling 6.5 K a kilometre); the engine relations name
/// their sources in the record. Game-free, and in the core because anything that breathes or flies can use it.
/// </summary>
public static class AirRule
{
    /// <summary>The standard atmosphere's sea-level temperature, kelvin.</summary>
    public const double SeaLevelKelvin = 288.15;

    /// <summary>How fast the standard atmosphere cools with height, kelvin a metre.</summary>
    public const double LapseKelvinPerMetre = 0.0065;

    /// <summary>The exponent on the temperature ratio that gives the pressure ratio (g M / R L for the standard atmosphere).</summary>
    public const double PressureExponent = 5.25588;

    /// <summary>The top of the troposphere, where the formula stops holding; no land in the game reaches it.</summary>
    public const double TroposphereTopMetres = 11000;

    static double TemperatureRatio(double elevationMetres)
    {
        double height = elevationMetres < 0 ? 0 : elevationMetres > TroposphereTopMetres ? TroposphereTopMetres : elevationMetres;
        return 1 - LapseKelvinPerMetre * height / SeaLevelKelvin;
    }

    /// <summary>Air pressure at a height as a share of sea level's: 1 at the sea, 0.83 at 1,500 m, 0.62 at 4,000 m. Below sea level reads as
    /// sea level.</summary>
    public static double PressureRatio(double elevationMetres) => Math.Pow(TemperatureRatio(elevationMetres), PressureExponent);

    /// <summary>The standard atmosphere's air density at a height as a share of sea level's, at the standard temperature for that height:
    /// 0.95 at 500 m, 0.86 at 1,500 m, 0.67 at 4,000 m.</summary>
    public static double StandardDensityRatio(double elevationMetres) => Math.Pow(TemperatureRatio(elevationMetres), PressureExponent - 1);

    /// <summary>Air density at a height and the temperature it really is there, as a share of sea-level air at 15 °C (ideal gas): what a
    /// rotor or a wing works in.</summary>
    public static double DensityRatio(double elevationMetres, double ambientCelsius) =>
        PressureRatio(elevationMetres) * SeaLevelKelvin / Math.Max(150, 273.15 + ambientCelsius);

    /// <summary>The empirical correction for an engine with no supercharger, as Leishman's textbook gives it ("often used in practice"):
    /// 1.132 times the density ratio, less 0.132. Its friction stays while its breathing falls, so it loses a little more than the air
    /// does. Widely credited to Gagg and Farrar (SAE, 1934), whose paper the record could not read.</summary>
    public const double DensitySlope = 1.132, DensityOffset = 0.132;

    /// <summary>
    /// The share of its sea-level power an engine that breathes at the air's own pressure keeps in air of a density ratio:
    /// 1.132 times the ratio, less 0.132. A little worse than the density alone, never below zero.
    /// </summary>
    public static double NaturallyAspiratedPower(double densityRatio)
    {
        double share = DensitySlope * densityRatio - DensityOffset;
        return share < 0 ? 0 : share > 1 ? 1 : share;
    }

    /// <summary>
    /// The share of its rated power a piston engine keeps at a height. A naturally aspirated one loses it from the sea up
    /// (<see cref="NaturallyAspiratedPower"/>). A turbocharged one holds its rated power up to <paramref name="holdsToMetres"/>, where its
    /// turbocharger runs out of speed, and above that falls as a naturally aspirated engine would from there.
    /// </summary>
    public static double PistonPower(double elevationMetres, bool forcedInduction, double holdsToMetres)
    {
        double here = NaturallyAspiratedPower(StandardDensityRatio(elevationMetres));
        if (!forcedInduction) return here;
        if (elevationMetres <= holdsToMetres) return 1;
        double atLimit = NaturallyAspiratedPower(StandardDensityRatio(holdsToMetres));
        return atLimit <= 0 ? 0 : Math.Min(1, here / atLimit);
    }

    /// <summary>
    /// How well air carries heat away at a height, as a share of sea level's: the air's mass flow falls with its pressure, and the heat
    /// it takes falls a little less than in step (<paramref name="exponent"/> below 1; 1 for in step).
    /// </summary>
    public static double Cooling(double elevationMetres, double exponent) => Math.Pow(PressureRatio(elevationMetres), exponent);
}
