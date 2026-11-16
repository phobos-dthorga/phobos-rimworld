using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What rain, snow and ice do to the grip of the driven wheels (cross-links round three, G5; Framework 0.113.0; owner, 13 Nov 2026: on by
/// default, with a setting), game-free. Speed in bad weather is already Vehicle Framework's (it divides speed by the weather's move-speed
/// multiplier); this is grip, which only limits on hills, in mud and when towing. A film of water 0.05 mm thick cuts tyre-to-road friction by
/// 20 to 30 % of dry (FHWA research, cited in docs/development/vehicle-crosslinks.md); the factors themselves are authored: rain
/// ×0.85, falling snow ×0.75, ice ×0.6. Rain and snow scale with the weather's own rate (gentle snow is 0.8, hard snow 1.2, capped at 1), and
/// the worst of the three decides: they are not multiplied, since the snow already holds the water.
/// </summary>
public static class WeatherGripRule
{
    public static double Factor(double rainRate, double snowRate, bool onIce, double rainFactor, double snowFactor, double iceFactor)
    {
        double rain = Scaled(rainFactor, rainRate);
        double snow = Scaled(snowFactor, snowRate);
        double ice = onIce ? Clamp01(iceFactor) : 1;
        return Math.Min(rain, Math.Min(snow, ice));
    }

    static double Scaled(double factor, double rate) => 1 - (1 - Clamp01(factor)) * Clamp01(rate);

    static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
}
