using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The rules of a tank of fluid on the move, game-free (docs/development/vehicle-bulk-tanker.md). Limits come from the
/// caller, which reads them from a Def; the figures are authored balance unless the design record names a source.
/// </summary>
public static class TankRule
{
    /// <summary>How many time constants "reaches the outside temperature" means: three leaves about 5% of the difference.</summary>
    const float TimeConstantsToReach = 3f;

    /// <summary>The next temperature of the contents, moving exponentially towards the outside air.</summary>
    public static float NextTemperature(float current, float outside, float hours, float hoursToReachOutside)
    {
        if (hoursToReachOutside <= 0f) return outside;
        if (hours <= 0f) return current;
        return outside + (current - outside) * MathF.Exp(-TimeConstantsToReach * hours / hoursToReachOutside);
    }

    /// <summary>What share of its full rate a pump manages for this thickness: 1 above thickensBelow, falling to the floor at worstAt.
    /// NaN limits mean the contents never thicken.</summary>
    public static float PumpFactor(float temperature, float thickensBelow, float worstAt, float floor)
    {
        if (float.IsNaN(thickensBelow) || float.IsNaN(worstAt) || thickensBelow <= worstAt) return 1f;
        if (temperature >= thickensBelow) return 1f;
        if (temperature <= worstAt) return floor;
        return floor + (1f - floor) * (temperature - worstAt) / (thickensBelow - worstAt);
    }

    /// <summary>Whether contents with this freezing point are frozen. NaN means they never freeze.</summary>
    public static bool Frozen(float temperature, float freezesBelow) => !float.IsNaN(freezesBelow) && temperature < freezesBelow;

    /// <summary>The spoilage clock in hours after this step: it runs at the given rate above freezing and pauses at or below it.</summary>
    public static float NextSpoilHours(float clock, float temperature, float hours, float rate) =>
        temperature <= 0f ? clock : clock + hours * rate;

    /// <summary>Whether the spoilage clock has run out. Zero or less means the contents never spoil.</summary>
    public static bool Spoiled(float clockHours, float spoilsAfterDays) => spoilsAfterDays > 0f && clockHours >= spoilsAfterDays * 24f;

    /// <summary>Units a pump can move in this step: never more than is there to take or there is room for.</summary>
    public static float Moved(float ratePerHour, float hours, float factor, float available, float space) =>
        MathF.Max(0f, MathF.Min(ratePerHour * hours * factor, MathF.Min(available, space)));

    /// <summary>Units leaking in this step from a shell at this integrity (1 is sound, 0 is destroyed).</summary>
    public static float Leaked(float capacity, float integrity, float fractionPerHourAtZero, float hours, float held) =>
        MathF.Min(held, MathF.Max(0f, 1f - integrity) * capacity * fractionPerHourAtZero * hours);

    /// <summary>Whether a shell at this integrity may be filled. The pump and a bulk order ask the same question.</summary>
    public static bool TakesLoad(float integrity, float floor) => integrity >= floor;

    /// <summary>Whether being set up pins the vehicle down. With no tank fitted there is nothing to pack up, and no button to do it with.</summary>
    public static bool HoldsVehicle(bool setUp, bool tankFitted) => setUp && tankFitted;

    /// <summary>
    /// The whole items a pump step moves. A step smaller than one item still moves one, while one fits or one is there: a slowed pump
    /// could otherwise never handle a large container. An authored simplification: such a pump runs a little ahead of its rate.
    /// </summary>
    public static int WholeItems(float most, float unitsPerItem, float limit)
    {
        if (unitsPerItem <= 0f || most <= 0f) return 0;
        int whole = (int)(most / unitsPerItem);
        return whole > 0 || limit < unitsPerItem ? whole : 1;
    }

    /// <summary>
    /// The tank units a building's fuel store stands for (Framework 0.349.0): the game counts a refuelable building's store as items times
    /// its fuel multiplier, and a tank counts unitsPerItem to the item (thirty units of sludge to a wastepack). Nothing for a broken figure.
    /// </summary>
    public static float UnitsInStore(float storeFuel, float fuelMultiplier, float unitsPerItem) =>
        fuelMultiplier <= 0f || unitsPerItem <= 0f ? 0f : MathF.Max(0f, storeFuel) / fuelMultiplier * unitsPerItem;

    /// <summary>The fuel a building's store gives up for the tank units taken from it: <see cref="UnitsInStore"/> turned round.</summary>
    public static float StoreFuelForUnits(float units, float fuelMultiplier, float unitsPerItem) =>
        unitsPerItem <= 0f ? 0f : MathF.Max(0f, units) / unitsPerItem * fuelMultiplier;

    /// <summary>
    /// Whether a tank can push its load out now (Framework 0.283.0; the owner's cards of 25 Nov 2026): one that needs no pump power always
    /// can (a truck's engine or battery drives it); a tank trailer only with its electric pump fitted and powered. Without, it works by gravity.
    /// </summary>
    public static bool Pushes(bool needsPumpPower, bool pumpFitted, bool powered) => !needsPumpPower || (pumpFitted && powered);

    /// <summary>
    /// Whether one tank can pass its load to another beside it (Framework 0.283.0): either one's pump will do, the giver's pushing or the
    /// taker's drawing. Two tanks with no pump between them move nothing: a hose laid flat has no fall to run down.
    /// </summary>
    public static bool Transfers(bool giverPushes, bool takerPushes) => giverPushes || takerPushes;

    /// <summary>Units of waste a gravity drain lets go in this step: its rate, never more than is held (Framework 0.283.0).</summary>
    public static float Drained(float held, float perHour, float hours) => MathF.Max(0f, MathF.Min(held, perHour * hours));

    /// <summary>How much one of a tank's equal compartments holds. Filled alike, they still add up to the whole tank.</summary>
    public static float CompartmentCapacity(float capacity, int compartments) => capacity / MathF.Max(1, compartments);

    /// <summary>The blast radius of a load, scaling with the cube root of what is aboard: the volume of a blast follows its energy.</summary>
    public static float ExplosionRadius(float amount, float referenceAmount, float referenceRadius, float factor = 1f, float maxRadius = float.MaxValue)
    {
        if (amount <= 0f || referenceAmount <= 0f) return 0f;
        return MathF.Min(maxRadius, referenceRadius * MathF.Cbrt(amount / referenceAmount) * factor);
    }
}
