using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The rules of a hold of loose goods kept as amounts, not stacks (Framework 0.182.0; docs/development/farming-batch-one.md, "The bulk
/// grain hold"): a combine's grain tank, a chaser bin, a field bin, a silo. How much fits by weight, whether a hold takes a second
/// product, how freshness mixes when a load joins what is held, how it ages, and how much moves in one step of pouring. Freshness is one
/// figure for each product: the share of its keeping time already used, nought fresh and one spoiled, as the bulk tanker keeps one quality
/// for each fluid. Game-free, so every hold and both ends of a pour share it.
/// </summary>
public static class BulkHoldRule
{
    /// <summary>A slack on weights, so a hold filled to exactly its capacity in whole units is not a unit short through rounding.</summary>
    const float MassSlackKg = 0.0005f;

    /// <summary>How many more units fit, by weight. Goods with no weight fit without limit; a hold already over its capacity takes none.</summary>
    public static int RoomFor(float capacityKg, float heldKg, float massPerUnitKg)
    {
        if (massPerUnitKg <= 0f) return int.MaxValue;
        float room = capacityKg - heldKg + MassSlackKg;
        if (room <= 0f) return 0;
        double units = Math.Floor(room / massPerUnitKg);
        return units >= int.MaxValue ? int.MaxValue : (int)units;
    }

    /// <summary>
    /// Whether a hold takes this product: one that mixes takes anything; one that keeps a single product (a combine's tank) takes it when
    /// empty or already holding the same.
    /// </summary>
    public static bool Takes(bool mixesProducts, bool empty, bool holdsSameProduct) => mixesProducts || empty || holdsSameProduct;

    /// <summary>
    /// The freshness of a product after a load joins what is held: each part's used share weighted by its amount, so a little old grain
    /// in a lot of new ages the lot a little. Nothing held and nothing added is fresh.
    /// </summary>
    public static float Mixed(int heldCount, float heldUsed, int addedCount, float addedUsed)
    {
        long held = Math.Max(heldCount, 0);
        long added = Math.Max(addedCount, 0);
        if (held + added == 0) return 0f;
        double used = (held * (double)Clamp01(heldUsed) + added * (double)Clamp01(addedUsed)) / (held + added);
        return Clamp01((float)used);
    }

    /// <summary>
    /// The used share after some time: time passed, times the game's rate of spoiling at the temperature (nought frozen, one warm), times
    /// the store's own factor (an aired silo below one), over the product's keeping time. A product that never spoils stays as it is.
    /// </summary>
    public static float Aged(float used, int ticks, float spoilRate, int keepingTicks, float storeFactor)
    {
        if (keepingTicks <= 0 || ticks <= 0) return Clamp01(used);
        double gained = ticks * (double)Math.Max(spoilRate, 0f) * Math.Max(storeFactor, 0f) / keepingTicks;
        return Clamp01((float)(used + gained));
    }

    /// <summary>How many units one step of pouring moves: no more than is held, than fits where it goes, or than the auger moves in a step.</summary>
    public static int Pour(int held, int roomThere, int unitsPerStep) => Math.Max(0, Math.Min(Math.Min(held, roomThere), unitsPerStep));

    /// <summary>How many units an auger moves in a step: its rate by weight a second over the time, in whole units, at least one for goods that weigh anything it can lift.</summary>
    public static int UnitsPerStep(float kilogramsPerSecond, float seconds, float massPerUnitKg)
    {
        if (kilogramsPerSecond <= 0f || seconds <= 0f) return 0;
        if (massPerUnitKg <= 0f) return int.MaxValue;
        double units = Math.Floor(kilogramsPerSecond * (double)seconds / massPerUnitKg);
        return units >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)units);
    }

    /// <summary>
    /// Whether two footprints are within an auger's reach: the gap between their rectangles, counted in cells the long way round a
    /// corner (the larger of the gaps across and along), is no more than the reach. Touching or overlapping footprints have no gap.
    /// </summary>
    public static bool WithinReach(int aMinX, int aMinZ, int aMaxX, int aMaxZ, int bMinX, int bMinZ, int bMaxX, int bMaxZ, int reachCells)
    {
        int gapX = Math.Max(0, Math.Max(aMinX - bMaxX, bMinX - aMaxX) - 1);
        int gapZ = Math.Max(0, Math.Max(aMinZ - bMaxZ, bMinZ - aMaxZ) - 1);
        return Math.Max(gapX, gapZ) <= Math.Max(0, reachCells);
    }

    /// <summary>How full a hold is, nought to one, for a gauge.</summary>
    public static float Fill(float capacityKg, float heldKg) => capacityKg <= 0f ? 0f : Clamp01(heldKg / capacityKg);

    static float Clamp01(float value) => value < 0f || float.IsNaN(value) ? 0f : value > 1f ? 1f : value;
}
