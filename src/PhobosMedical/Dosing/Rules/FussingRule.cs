namespace Phobos.Dosing.Rules;

/// <summary>
/// A patient awake and in extreme pain flinches and pulls away, and is harder to tend well. Game-free. From the threshold (the game's
/// "extreme" label) the tend quality is multiplied by a factor that falls in a straight line to its worst at full pain. Pain relief, which
/// doctors give from 40% pain, keeps a patient below it. The factors are authored.
/// </summary>
public static class FussingRule
{
    public static float Factor(float pain, bool awake, float fromPain, float factorAtThreshold, float factorAtFullPain)
    {
        if (!awake || pain < fromPain || fromPain >= 1f) return 1f;
        float share = (pain - fromPain) / (1f - fromPain);
        if (share > 1f) share = 1f;
        return factorAtThreshold + (factorAtFullPain - factorAtThreshold) * share;
    }
}
