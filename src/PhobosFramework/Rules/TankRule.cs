using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The rules of a tank of fluid on the move, game-free (docs/development/utility-vehicles-design.md, Bulk tanker). Limits come from the
/// caller, which reads them from a Def; the figures are authored balance unless the design record names a source.
/// </summary>
public static class TankRule
{
    /// <summary>How many time constants "reaches the outside temperature" means: three leaves about 5% of the difference.</summary>
    const float TimeConstantsToReach = 3f;

    /// <summary>The next temperature of the contents, moving exponentially towards the outside air.</summary>
    public static float NextTemperature(float current, float outside, float hours, float hoursToReachOutside)
    {
        if (hoursToReachOutside <= 0f) return outside;
        if (hours <= 0f) return current;
        return outside + (current - outside) * MathF.Exp(-TimeConstantsToReach * hours / hoursToReachOutside);
    }

    /// <summary>What share of its full rate a pump manages for this thickness: 1 above thickensBelow, falling to the floor at worstAt.
    /// NaN limits mean the contents never thicken.</summary>
    public static float PumpFactor(float temperature, float thickensBelow, float worstAt, float floor)
    {
        if (float.IsNaN(thickensBelow) || float.IsNaN(worstAt) || thickensBelow <= worstAt) return 1f;
        if (temperature >= thickensBelow) return 1f;
        if (temperature <= worstAt) return floor;
        return floor + (1f - floor) * (temperature - worstAt) / (thickensBelow - worstAt);
    }

    /// <summary>Whether contents with this freezing point are frozen. NaN means they never freeze.</summary>
    public static bool Frozen(float temperature, float freezesBelow) => !float.IsNaN(freezesBelow) && temperature < freezesBelow;

    /// <summary>The spoilage clock in hours after this step: it runs at the given rate above freezing and pauses at or below it.</summary>
    public static float NextSpoilHours(float clock, float temperature, float hours, float rate) =>
        temperature <= 0f ? clock : clock + hours * rate;

    /// <summary>Whether the spoilage clock has run out. Zero or less means the contents never spoil.</summary>
    public static bool Spoiled(float clockHours, float spoilsAfterDays) => spoilsAfterDays > 0f && clockHours >= spoilsAfterDays * 24f;

    /// <summary>Units a pump can move in this step: never more than is there to take or there is room for.</summary>
    public static float Moved(float ratePerHour, float hours, float factor, float available, float space) =>
        MathF.Max(0f, MathF.Min(ratePerHour * hours * factor, MathF.Min(available, space)));

    /// <summary>Units leaking in this step from a shell at this integrity (1 is sound, 0 is destroyed).</summary>
    public static float Leaked(float capacity, float integrity, float fractionPerHourAtZero, float hours, float held) =>
        MathF.Min(held, MathF.Max(0f, 1f - integrity) * capacity * fractionPerHourAtZero * hours);

    /// <summary>The blast radius of a load, scaling with the cube root of what is aboard: the volume of a blast follows its energy.</summary>
    public static float ExplosionRadius(float amount, float referenceAmount, float referenceRadius)
    {
        if (amount <= 0f || referenceAmount <= 0f) return 0f;
        return referenceRadius * MathF.Cbrt(amount / referenceAmount);
    }
}
