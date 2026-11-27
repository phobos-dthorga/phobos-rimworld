using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class RelayMastRuleTests
{
    [Fact]
    public void AMastWearsOutOverAYear()
    {
        Assert.Equal(0.5, RelayMastRule.Worn(1, 30), 6);
        Assert.Equal(0, RelayMastRule.Worn(1, 90));
        Assert.True(RelayMastRule.Works(RelayMastRule.Worn(1, 50)));
        Assert.False(RelayMastRule.Works(RelayMastRule.Worn(1, 55)));
    }

    [Fact]
    public void RaidsComeMoreOftenNearerTheirLands()
    {
        Assert.Equal(RelayMastRule.RaidMeanDaysAdjacent, RelayMastRule.RaidMeanDays(0));
        Assert.Equal(RelayMastRule.RaidMeanDaysAtEdge, RelayMastRule.RaidMeanDays(RelayMastRule.RaidReachTiles));
        Assert.True(RelayMastRule.RaidMeanDays(3) < RelayMastRule.RaidMeanDays(9));
        Assert.True(double.IsPositiveInfinity(RelayMastRule.RaidMeanDays(RelayMastRule.RaidReachTiles + 1)));
    }
}
