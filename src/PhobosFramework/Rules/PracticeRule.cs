using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// How practice at a kind of thing grows, game-free (Framework 0.190.0; docs/development/vehicle-emergent-play.md, EM11 and EM12). Hours
/// spent at one kind of thing (a kind of vehicle at first) give a share of the gain that kind offers: half of it after
/// <see cref="HalfHours"/>, three quarters after twice that, and never quite all of it. The person most practised at one particular thing,
/// with at least <see cref="RegularHours"/> at it, is its regular. Both figures are authored, a gameplay choice: a few weeks of
/// ordinary use makes someone at home with a machine, as with a real driver's first few hundred hours.
/// </summary>
public static class PracticeRule
{
    /// <summary>Hours of practice that give half the gain (authored).</summary>
    public const double HalfHours = 40;

    /// <summary>Hours at one thing that make someone its regular, when nobody has more (authored).</summary>
    public const double RegularHours = 12;

    /// <summary>The share of the gain these hours give, 0 to just under 1.</summary>
    public static double Share(double hours, double halfHours = HalfHours) =>
        hours <= 0 || halfHours <= 0 ? 0 : 1 - Math.Pow(0.5, hours / halfHours);

    /// <summary>A factor that moves from 1 toward <paramref name="bestFactor"/> as the share grows: a seasoned hand's figure.</summary>
    public static double Factor(double share, double bestFactor) => 1 + (bestFactor - 1) * Math.Max(0, Math.Min(1, share));

    /// <summary>The key with the most hours, at least <paramref name="atLeast"/>; null when nobody has enough. Ties go to the key that sorts
    /// first, so the answer never depends on the dictionary's order.</summary>
    public static string? Regular(IEnumerable<KeyValuePair<string, float>> hours, double atLeast = RegularHours)
    {
        string? best = null;
        float most = 0f;
        foreach (KeyValuePair<string, float> pair in hours)
        {
            if (pair.Value < atLeast) continue;
            if (best != null && (pair.Value < most || (pair.Value == most && string.CompareOrdinal(pair.Key, best) > 0))) continue;
            best = pair.Key;
            most = pair.Value;
        }
        return best;
    }
}
