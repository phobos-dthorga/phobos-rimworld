using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a fitting or a tune costs on one vehicle, game-free (Framework 0.91.0; owner, 11 Nov 2026: "I want these upgrades and tunes to feel
/// earned"). A bigger vehicle's parts are bigger: its cost grows with its weight against a reference, by a power below one, so twice the
/// weight costs less than twice as much; the player's presets multiply on top. Counts round up and never fall to nothing.
/// </summary>
public static class FitCostRule
{
    /// <summary>(kilograms / reference) to the power given, held between min and max; 1 for a vehicle of no known weight.</summary>
    public static double WeightFactor(double kilograms, double reference, double exponent, double min, double max)
    {
        if (kilograms <= 0 || reference <= 0) return 1;
        double factor = Math.Pow(kilograms / reference, exponent);
        return Math.Max(min, Math.Min(max, factor));
    }

    /// <summary>A material count after the factors: rounded up, never below one where the base asks for any.</summary>
    public static int Count(int baseCount, double factor) => CostShareRule.Count(baseCount, factor);

    /// <summary>Work in ticks after the factors, never below one tick.</summary>
    public static float Work(float baseWork, double factor) => (float)Math.Max(1, baseWork * factor);
}
