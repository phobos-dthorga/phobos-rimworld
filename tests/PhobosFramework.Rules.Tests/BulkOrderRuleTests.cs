using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BulkOrderRuleTests
{
    [Theory]
    [InlineData(3000f, 1f, 3000)]
    [InlineData(3000f, 10f, 300)]
    [InlineData(1000f, 30f, 33)]
    [InlineData(5f, 10f, 0)]
    [InlineData(0f, 1f, 0)]
    public void AnOrderIsWholeItems(float space, float unitsPerItem, int expected) =>
        Assert.Equal(expected, BulkOrderRule.MaxItems(space, unitsPerItem));

    [Theory]
    [InlineData(3.22f, 3000f, 1f, 9660)]
    [InlineData(10.5f, 3000f, 10f, 3150)]
    [InlineData(0.5f, 1f, 1f, 1)]
    [InlineData(5f, 0f, 1f, 0)]
    public void TheCostIsTheItemsAtTheItemPrice(float price, float units, float unitsPerItem, int expected) =>
        Assert.Equal(expected, BulkOrderRule.Cost(price, units, unitsPerItem));

    [Theory]
    [InlineData(100, 200, 60000, 0)]
    [InlineData(200, 200, 60000, 0)]
    [InlineData(201, 200, 60000, 1)]
    [InlineData(120200, 200, 60000, 2)]
    [InlineData(120201, 200, 60000, 3)]
    public void RestockDaysRoundUp(int next, int now, int perDay, int expected) =>
        Assert.Equal(expected, BulkOrderRule.DaysUntilRestock(next, now, perDay));
}
