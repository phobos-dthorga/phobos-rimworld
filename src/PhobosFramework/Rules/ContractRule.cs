using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The arithmetic of a settlement's contract for a vehicle's work, game-free (docs/development/vehicle-contracts.md). What is asked, the
/// truck that can do it and the reward are the game's and the Defs'; this only counts amounts, time and pace. Every figure is the
/// agent's, for the owner to revise.
/// </summary>
public static class ContractRule
{
    /// <summary>The slowest a crew works, with no skill at all.</summary>
    public const float SlowestPace = 0.6f;
    /// <summary>How much faster each skill level makes the work: level 10 is the plain pace, level 20 is 40% faster.</summary>
    public const float PacePerLevel = 0.04f;
    /// <summary>The fastest a crew works.</summary>
    public const float FastestPace = 1.4f;
    /// <summary>The frequency slider's step: a quarter of the game's trade request rate.</summary>
    public const float FrequencyStep = 0.25f;
    /// <summary>The waking hours in a day a caravan works, outside its night's rest (the game rests caravans for eight hours).</summary>
    public const float WakingHoursPerDay = 16f;

    /// <summary>How fast the crew's best hand works, from their skill level (0 to 20).</summary>
    public static float Pace(int skillLevel) => Math.Clamp(SlowestPace + PacePerLevel * skillLevel, SlowestPace, FastestPace);

    /// <summary>
    /// The amount asked: what the settlement wants, no more than the player's largest fitting truck could bring, rounded down to the step so
    /// requests read as people make them. Zero when not even one step fits.
    /// </summary>
    public static float Amount(float wanted, float most, float step)
    {
        if (wanted <= 0f || most <= 0f) return 0f;
        float amount = Math.Min(wanted, most);
        if (step <= 0f) return amount;
        return MathF.Floor(amount / step) * step;
    }

    /// <summary>The worth of the work, in silver: what the reward is valued from.</summary>
    public static float Value(float units, float perUnit) => units <= 0f || perUnit <= 0f ? 0f : units * perUnit;

    /// <summary>
    /// The whole days the work itself can take, at the slowest pace, for the contract's time limit: the limit gives the work this long on top
    /// of the journey, so a crew that arrives in time can always finish.
    /// </summary>
    public static int WorkDays(float hours, bool worksAtNight)
    {
        if (hours <= 0f) return 0;
        float perDay = worksAtNight ? 24f : WakingHoursPerDay;
        return (int)MathF.Ceiling(hours / SlowestPace / perDay);
    }

    /// <summary>The frequency slider's value snapped to its step and kept within its ends.</summary>
    public static float Frequency(float value, float most) =>
        Math.Clamp(MathF.Round(value / FrequencyStep) * FrequencyStep, 0f, most);
}
