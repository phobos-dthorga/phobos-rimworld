namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// Burn shock: how dangerous a burned pawn's state is, 0 to 1, from the revised Baux score (age in years plus percent of the
/// body burned, plus 17 with an inhalation injury; Osler and colleagues' 2010 revision of Baux's 1961 score). The target runs
/// from 0 at a score of 36 to 1 at 110, marks taken from one small burn-unit cohort. Authored: larger burn-centre data put 50%
/// mortality near 110, and the Rim has no burn unit. See the design record. Treatment (tended burns, bed rest) lowers the target; severity moves toward it at authored rates.
/// Game-free.
/// </summary>
public static class BurnShockRule
{
    public static float Baux(float ageYears, float tbsa, bool inhalationInjury, float inhalationPoints) =>
        ageYears + tbsa * 100f + (inhalationInjury ? inhalationPoints : 0f);

    /// <summary>The severity an untreated pawn heads for.</summary>
    public static float Target(float baux, float noDeathScore, float noSurvivorScore)
    {
        if (noSurvivorScore <= noDeathScore) return baux >= noSurvivorScore ? 1f : 0f;
        float target = (baux - noDeathScore) / (noSurvivorScore - noDeathScore);
        return target < 0f ? 0f : target > 1f ? 1f : target;
    }

    /// <summary>
    /// How much treatment lowers the target: tended burns, bed rest, and fluids (the shock condition's own tending) in
    /// proportion to the tend quality, which the game sets from the doctor's skill and the medicine's potency (none 0.3,
    /// herbal 0.6, industrial 1, glitterworld 1.6, capped at the medicine's quality ceiling, 1.3 for glitterworld). A floor
    /// keeps care from zeroing it outright. Owner's choice (4 Nov 2026): playable over exact, with no burn ward needed.
    /// </summary>
    public static float TreatmentFactor(float tendedFraction, bool inBed, float fluidsQuality, float tendedRelief, float bedRestRelief,
        float fluidsReliefPerQuality, float fluidsQualityCap, float floor)
    {
        float quality = fluidsQuality < 0f ? 0f : fluidsQuality > fluidsQualityCap ? fluidsQualityCap : fluidsQuality;
        float factor = 1f - tendedRelief * Clamp01(tendedFraction) - (inBed ? bedRestRelief : 0f) - fluidsReliefPerQuality * quality;
        return factor < floor ? floor : factor;
    }

    /// <summary>Past the start threshold the target is at least the floor, so the compensated stage always shows there.</summary>
    public static float WithThresholdFloor(float target, float tbsa, float startTbsa, float floor) =>
        tbsa >= startTbsa && target < floor ? floor : target;

    /// <summary>One step toward the target, never overshooting it.</summary>
    public static float Step(float severity, float target, float days, float risePerDay, float fallPerDay)
    {
        if (days <= 0f) return severity;
        if (target > severity)
        {
            float next = severity + risePerDay * days;
            return next > target ? target : next;
        }
        float down = severity - fallPerDay * days;
        return down < target ? target : down;
    }

    static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
