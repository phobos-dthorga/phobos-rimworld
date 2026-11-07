namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// The caustic gas model (docs/development/caustic-gas.md). Game-free. A game design with no real-agent data (owner, 5 Nov 2026):
/// every figure is authored, passed in from the rules Def. Three exposures (eyes, lungs, skin) grow from the gas; the delayed tracks
/// read them through these curves.
/// </summary>
public static class CausticRule
{
    /// <summary>Dose gained in <paramref name="hours"/> at an exposure of 0 to 1; the dose never passes 1.</summary>
    public static float AddDose(float dose, float exposure, float ratePerHourAtFull, float hours)
    {
        if (exposure <= 0f || ratePerHourAtFull <= 0f || hours <= 0f) return dose;
        float next = dose + exposure * ratePerHourAtFull * hours;
        return next > 1f ? 1f : next;
    }

    /// <summary>Dose left after <paramref name="days"/> of fading.</summary>
    public static float Fade(float dose, float perDay, float days)
    {
        float next = dose - perDay * days;
        return next < 0f ? 0f : next;
    }

    /// <summary>How much of the skin's dose reaches it: nothing under a sealed suit, bare skin in full, covered skin a share.</summary>
    public static float SkinFactor(bool sealedSuit, float coveredShare, float coveredFactor)
    {
        if (sealedSuit) return 0f;
        float covered = coveredShare < 0f ? 0f : coveredShare > 1f ? 1f : coveredShare;
        return 1f - covered + covered * coveredFactor;
    }

    /// <summary>0 before the onset hour, rising in a straight line to 1 at the peak hour (1 at once when the peak is the onset).</summary>
    public static float Ramp(float hours, float onset, float peak)
    {
        if (hours < onset) return 0f;
        if (peak <= onset) return 1f;
        float share = (hours - onset) / (peak - onset);
        return share > 1f ? 1f : share;
    }

    /// <summary>Fast-acting lungs: injury starts sooner after a bigger dose, from the latest hour at no dose to the soonest at full.</summary>
    public static float FastLungOnsetHours(float dose, float latest, float soonest)
    {
        float d = dose < 0f ? 0f : dose > 1f ? 1f : dose;
        return latest + (soonest - latest) * d;
    }

    /// <summary>Weakened immunity from a heavy dose: none below heavy, mild from heavy, severe from massive.</summary>
    public static float Weakness(float dose, float heavy, float massive, float mild, float severe)
    {
        if (dose < heavy) return 0f;
        return dose >= massive ? severe : mild;
    }
}
