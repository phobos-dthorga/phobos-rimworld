namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// Acid left on the skin keeps burning until it is washed off, which is why chemical burns are flushed with water for a long time
/// (first aid everywhere says so). Game-free. The deepening rate is authored; the water is the ANSI/ISEA Z358.1 figures for emergency
/// showers and eyewashes (75.7 litres a minute and 1.5 litres a minute, each for 15 minutes), as reported by ISHN; approximate.
/// </summary>
public static class ChemicalRule
{
    /// <summary>Burn damage in <paramref name="hours"/> from acid still on the skin, falling as the acid is spent.</summary>
    public static float Deepening(float severity, float perHourAtFull, float hours)
    {
        if (severity <= 0f || perHourAtFull <= 0f || hours <= 0f) return 0f;
        return perHourAtFull * (severity > 1f ? 1f : severity) * hours;
    }

    /// <summary>The water one rinse uses: an eyewash's when every acid burn is on the eyes, else a shower's.</summary>
    public static float RinseLitres(bool eyesOnly, float showerLitres, float eyewashLitres) => eyesOnly ? eyewashLitres : showerLitres;
}
