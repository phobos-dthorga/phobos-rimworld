using UnityEngine;

namespace Phobos.Radio.UI;

/// <summary>
/// The radio's one colour ramp for signal strength, dark to bright: deep indigo, violet, rose, orange, gold, pale cream (the mock-up's).
/// The coverage overlay (Radio 0.2.0) and the waterfall (Radio 0.6.0) both paint with it, so a colour means the same in each.
/// </summary>
public static class RadioPalette
{
    public static readonly Color NoContact = new(0.42f, 0.06f, 0.08f);

    private static readonly (float at, Color color)[] Ramp =
    {
        (0f, new Color(0.11f, 0.09f, 0.25f)),
        (0.25f, new Color(0.33f, 0.13f, 0.48f)),
        (0.5f, new Color(0.69f, 0.20f, 0.44f)),
        (0.72f, new Color(0.94f, 0.45f, 0.23f)),
        (0.88f, new Color(0.98f, 0.78f, 0.35f)),
        (1f, new Color(1f, 0.96f, 0.82f)),
    };

    /// <summary>The colour at a point on the ramp, from 0 (darkest) to 1 (brightest).</summary>
    public static Color At(float t)
    {
        t = Mathf.Clamp01(t);
        for (var i = 1; i < Ramp.Length; i++)
        {
            if (t > Ramp[i].at) continue;
            var span = Ramp[i].at - Ramp[i - 1].at;
            return Color.Lerp(Ramp[i - 1].color, Ramp[i].color, span <= 0 ? 1f : (t - Ramp[i - 1].at) / span);
        }
        return Ramp[Ramp.Length - 1].color;
    }
}
