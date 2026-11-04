namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// A burn of the upper airway: the danger is delayed, as the swelling builds over hours (Walker and colleagues, Critical Care
/// 2015, on inhalation injury). Severity rises from 0 to a peak, holds, then resolves, all from the condition's age; the peak
/// depends on how bad the exposure was. Game-free.
/// </summary>
public static class AirwayRule
{
    /// <summary>The shape over time, 0 to 1: linear rise to the peak hour, flat to the plateau's end, linear fall to zero at resolve.</summary>
    public static float Shape(float ageHours, float peakHours, float plateauEndHours, float resolveHours)
    {
        if (ageHours <= 0f || peakHours <= 0f) return 0f;
        if (ageHours < peakHours) return ageHours / peakHours;
        if (ageHours < plateauEndHours) return 1f;
        if (resolveHours <= plateauEndHours || ageHours >= resolveHours) return 0f;
        return 1f - (ageHours - plateauEndHours) / (resolveHours - plateauEndHours);
    }

    /// <summary>The peak a given exposure reaches: half for the lightest, the full stage table for the worst. Authored.</summary>
    public static float Peak(float exposure)
    {
        float clamped = exposure < 0f ? 0f : exposure > 1f ? 1f : exposure;
        return 0.5f + 0.5f * clamped;
    }

    public static float Severity(float ageHours, float peak, float peakHours, float plateauEndHours, float resolveHours) =>
        peak * Shape(ageHours, peakHours, plateauEndHours, resolveHours);

    /// <summary>
    /// Chance per check that an airway burn starts: full base chance while the pawn is on fire, else scaled by fresh burn
    /// damage to the head and neck over a reference amount.
    /// </summary>
    public static float OnsetChance(bool onFire, float freshHeadNeckBurnDamage, float baseChance, float referenceDamage)
    {
        if (onFire) return baseChance;
        if (freshHeadNeckBurnDamage <= 0f || referenceDamage <= 0f) return 0f;
        float scale = 0.5f * freshHeadNeckBurnDamage / referenceDamage;
        return baseChance * (scale > 1f ? 1f : scale);
    }
}
