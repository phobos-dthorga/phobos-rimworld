using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// How wet open ground is, 0 (dry) to 1 (soaked), so that rain counts for hours after it stops (Framework 0.204.0; LM2 of
/// docs/development/four-mods-study-2026-11-21.md, after Realistic Rain's puddles, which are pictures only). Game-free. One figure for
/// a whole map, never one for each cell: a roof is applied when the figure is read. Rain wets the ground in proportion to the game's
/// rain rate; while none falls it dries at a pace set by the temperature, the daylight and the wind, each a factor of 1 at the
/// reference (20 °C, full daylight, the game's ordinary wind), and barely at all below freezing. Every figure is authored, a gameplay
/// choice, shaped to agree with what Realistic Rain draws: dark ground within the first hour of rain, dry in about a day.
/// </summary>
public static class GroundWetnessRule
{
    /// <summary>The temperature at which the drying pace is the rules' own.</summary>
    public const double ReferenceCelsius = 20;
    /// <summary>Every 40 °C above or below the reference adds or takes away the whole pace again (so 0 °C dries at half pace).</summary>
    public const double CelsiusPerPace = 40;
    /// <summary>The temperature factor's limits above freezing: never slower than half, never faster than twice.</summary>
    public const double WarmFactorMin = 0.5, WarmFactorMax = 2;
    /// <summary>What each unit of wind above or below the game's ordinary 1 adds or takes away, and the limits.</summary>
    public const double WindSlope = 0.3, WindFactorMin = 0.7, WindFactorMax = 1.6;

    /// <summary>The wetness after <paramref name="hours"/> of the weather given. Rain at rate 1 soaks dry ground in
    /// <paramref name="soakHours"/>; with none falling, soaked ground dries in <paramref name="dryHours"/> at the reference, divided by
    /// <see cref="DryingFactor"/>. Rain partly falling (a weather easing in or out) dries in proportion to what is not falling.</summary>
    public static double Step(double wetness, double hours, double rainRate, double celsius, double daylight, double wind,
        double soakHours, double dryHours, double frozenFactor, double nightFactor)
    {
        if (double.IsNaN(wetness)) wetness = 0;
        if (double.IsNaN(hours) || hours <= 0) return Clamp01(wetness);
        double rain = Clamp01(double.IsNaN(rainRate) ? 0 : rainRate);
        double gain = soakHours > 0 ? rain / soakHours : rain > 0 ? double.PositiveInfinity : 0;
        double loss = dryHours > 0 ? DryingFactor(celsius, daylight, wind, frozenFactor, nightFactor) * (1 - rain) / dryHours : 0;
        return Clamp01(wetness + (gain - loss) * hours);
    }

    /// <summary>How fast the ground dries against the reference: warmth, daylight and wind multiplied. At or below 0 °C it is
    /// <paramref name="frozenFactor"/> in place of the warmth (water locked as ice); in full darkness the daylight part is
    /// <paramref name="nightFactor"/>, rising in a straight line to 1 in full daylight.</summary>
    public static double DryingFactor(double celsius, double daylight, double wind, double frozenFactor, double nightFactor)
    {
        double warmth = double.IsNaN(celsius) ? 1
            : celsius <= 0 ? Math.Max(0, frozenFactor)
            : Clamp(1 + (celsius - ReferenceCelsius) / CelsiusPerPace, WarmFactorMin, WarmFactorMax);
        double night = Clamp01(nightFactor);
        double light = night + (1 - night) * Clamp01(double.IsNaN(daylight) ? 1 : daylight);
        double air = Clamp(1 + WindSlope * ((double.IsNaN(wind) ? 1 : wind) - 1), WindFactorMin, WindFactorMax);
        return warmth * light * air;
    }

    /// <summary>The water under a wheel or a landing gear: the wetter of the rain falling now and the ground's own wetness, so grip
    /// still drops the moment rain starts and comes back only as the ground dries. Nothing under a roof.</summary>
    public static double Underfoot(double rainRate, double wetness, bool roofed) =>
        roofed ? 0 : Math.Max(Clamp01(double.IsNaN(rainRate) ? 0 : rainRate), Clamp01(wetness));

    static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

    static double Clamp01(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
}
