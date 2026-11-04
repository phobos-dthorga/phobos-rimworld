using System;

namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// Carbon monoxide in the blood, as a fraction 0 to 1 standing in for carboxyhaemoglobin. Uptake follows what the pawn breathes;
/// clearance is first-order with a half-life of about 320 minutes on room air (Weaver and colleagues, 2000, as cited in emergency
/// medicine references), faster in open air (authored). Game-free.
/// </summary>
public static class CarbonMonoxideRule
{
    /// <summary>Severity gained over <paramref name="hours"/> at a breathed gas fraction, capped.</summary>
    public static float Uptake(float breathed, float hours, float uptakePerHourAtFullGas, float current, float cap)
    {
        if (breathed <= 0f || hours <= 0f) return 0f;
        float gained = uptakePerHourAtFullGas * breathed * hours;
        float room = cap - current;
        if (room <= 0f) return 0f;
        return gained > room ? room : gained;
    }

    /// <summary>Inference when no carbon monoxide gas is carried: smoke breathed in an enclosed space makes it.</summary>
    public static float InferredBreathed(float breathedSmoke, float enclosure) =>
        breathedSmoke <= 0f || enclosure <= 0f ? 0f : breathedSmoke * enclosure;

    /// <summary>What remains after <paramref name="hours"/> of clearance at a half-life in hours.</summary>
    public static float Remaining(float severity, float hours, float halfLifeHours)
    {
        if (severity <= 0f || hours <= 0f || halfLifeHours <= 0f) return severity;
        return severity * (float)Math.Pow(0.5, hours / halfLifeHours);
    }
}
