using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class AutoLightRuleTests
{
    [Fact]
    public void SomeoneInsideThePoolCounts()
    {
        Assert.True(AutoLightRule.Within(0, 0, 8));
        Assert.True(AutoLightRule.Within(8, 0, 8));
        Assert.True(AutoLightRule.Within(5, 5, 8));
    }

    [Fact]
    public void SomeoneOutsideThePoolDoesNot()
    {
        Assert.False(AutoLightRule.Within(9, 0, 8));
        Assert.False(AutoLightRule.Within(6, 6, 8));
        Assert.False(AutoLightRule.Within(0, 0, 0));
    }

    [Fact]
    public void TheLampsStayLitForTheHoldAfterTheLastPersonLeaves()
    {
        Assert.True(AutoLightRule.Held(1000, 1000, 2500));
        Assert.True(AutoLightRule.Held(1000, 3500, 2500));
        Assert.False(AutoLightRule.Held(1000, 3501, 2500));
    }

    [Fact]
    public void ATickBeforeTheSightingHoldsNothing()
    {
        // A loaded game's clock can stand behind a sighting kept in memory from another game.
        Assert.False(AutoLightRule.Held(5000, 4000, 2500));
    }

    [Fact]
    public void ANegativeHoldMeansOnlyWhileSomeoneIsThere()
    {
        Assert.True(AutoLightRule.Held(1000, 1000, -5));
        Assert.False(AutoLightRule.Held(1000, 1001, -5));
    }
}
