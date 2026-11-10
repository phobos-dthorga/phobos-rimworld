using System;

namespace Phobos.Framework.Rules;

/// <summary>The arithmetic of the one volume setting every D'thorga sound answers to (docs/development/sound-generation.md), game-free.
/// A sound's authored volume is set against the game's own sounds; the setting scales it, and never past <see cref="MaxFactor"/>.</summary>
public static class SoundVolumeRule
{
    /// <summary>The most the setting may raise our sounds above their authored level (agent choice: half as loud again, for players who
    /// want them plainer; the game's own sliders still apply on top).</summary>
    public const float MaxFactor = 1.5f;

    /// <summary>The setting's value kept in range: none to <see cref="MaxFactor"/>.</summary>
    public static float Clamp(float factor) => float.IsNaN(factor) ? 1f : Math.Clamp(factor, 0f, MaxFactor);

    /// <summary>An authored volume scaled by the setting.</summary>
    public static float Scale(float authored, float factor) => authored * Clamp(factor);

    /// <summary>The setting as a whole percentage, for its label.</summary>
    public static int Percent(float factor) => (int)MathF.Round(Clamp(factor) * 100f);
}
