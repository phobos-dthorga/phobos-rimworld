using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class HoldRuleTests
{
    [Fact]
    public void AVehicleFitsEitherWayRound()
    {
        Assert.True(HoldRule.Fits(2, 3, 2, 3));
        Assert.True(HoldRule.Fits(3, 2, 2, 3));
        Assert.False(HoldRule.Fits(3, 3, 2, 3));
        Assert.False(HoldRule.Fits(2, 4, 2, 3));
    }

    [Fact]
    public void TheHoldTakesUpToItsWeight()
    {
        Assert.True(HoldRule.Takes(2500, 0, 2500));
        Assert.False(HoldRule.Takes(1500, 1500, 2500));
        Assert.False(HoldRule.Takes(-1, 0, 2500));
    }
}
