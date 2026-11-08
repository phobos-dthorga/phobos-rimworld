using System;

namespace Phobos.Framework.Rules;

/// <summary>The arithmetic of buying a fluid in bulk, game-free (docs/development/utility-vehicles-design.md). The price of one item comes from
/// the game's own buying price; everything here only counts.</summary>
public static class BulkOrderRule
{
    /// <summary>The most whole items that fit in the free space, so nothing is sold that could not be poured back out.</summary>
    public static int MaxItems(float space, float unitsPerItem)
    {
        if (space <= 0f || unitsPerItem <= 0f) return 0;
        return (int)MathF.Floor(space / unitsPerItem);
    }

    /// <summary>The silver for these units at the game's price for one item, rounded up so a purchase never comes out cheaper than the trade window.</summary>
    public static int Cost(float pricePerItem, float units, float unitsPerItem)
    {
        if (units <= 0f || unitsPerItem <= 0f) return 0;
        return (int)MathF.Ceiling(pricePerItem * (units / unitsPerItem));
    }

    /// <summary>Whole days until a settlement can sell this fluid in bulk again, rounded up so "1 day" never means "now".</summary>
    public static int DaysUntilRestock(int nextSaleTick, int nowTick, int ticksPerDay)
    {
        if (nextSaleTick <= nowTick || ticksPerDay <= 0) return 0;
        return (int)MathF.Ceiling((nextSaleTick - nowTick) / (float)ticksPerDay);
    }
}
