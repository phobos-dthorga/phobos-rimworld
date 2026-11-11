using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class EventMarkRuleTests
{
    [Fact]
    public void AFirstEventIsNeverTooSoon() => Assert.False(EventMarkRule.TooSoon(null, 100, 2500));

    [Fact]
    public void TheSameEventWithinTheGapIsTooSoon() => Assert.True(EventMarkRule.TooSoon(1000, 3000, 2500));

    [Fact]
    public void TheSameEventAfterTheGapIsMarked() => Assert.False(EventMarkRule.TooSoon(1000, 3500, 2500));

    [Fact]
    public void NothingIsDroppedFromAShortRecentLog() => Assert.Equal(0, EventMarkRule.DropCount(new[] { 900, 950, 990 }, 1000, 500, 10));

    [Fact]
    public void MarksOlderThanTheLimitAreDropped() => Assert.Equal(2, EventMarkRule.DropCount(new[] { 100, 200, 900 }, 1000, 500, 10));

    [Fact]
    public void TheOldestBeyondTheCountAreDropped() => Assert.Equal(2, EventMarkRule.DropCount(new[] { 900, 910, 920, 930, 940 }, 1000, 500, 3));

    [Fact]
    public void AgeAndCountTogether() => Assert.Equal(3, EventMarkRule.DropCount(new[] { 100, 900, 910, 920, 930 }, 1000, 500, 2));

    [Fact]
    public void AnEmptyLogDropsNothing() => Assert.Equal(0, EventMarkRule.DropCount(new int[0], 1000, 500, 3));
}
