using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class WindRuleTests
{
    [Fact]
    public void BeaufortForcesReadAsKnots()
    {
        Assert.Equal(0, WindRule.KnotsForBeaufort(0), 6);
        Assert.Equal(8.5, WindRule.KnotsForBeaufort(3), 6);
        Assert.Equal(11, WindRule.KnotsForBeaufort(3.5), 6);
        Assert.Equal(44, WindRule.KnotsForBeaufort(12), 6);
    }

    [Fact]
    public void TheGamesWindReadsAsCombatExtendedReadsIt()
    {
        // Three times the game's wind speed, at most nine: an ordinary breeze (1) is force 3, about 8.5 kt.
        Assert.Equal(3, WindRule.BeaufortForGameWind(1), 6);
        Assert.Equal(9, WindRule.BeaufortForGameWind(5), 6);
        Assert.Equal(8.5, WindRule.KnotsForGameWind(1), 6);
    }

    [Fact]
    public void ADirectionWandersTheShortWayRound()
    {
        Assert.Equal(5, WindRule.Wander(0, 90, 5), 6);
        Assert.Equal(355, WindRule.Wander(0, 270, 5), 6);
        Assert.Equal(90, WindRule.Wander(88, 90, 5), 6);
        Assert.True(WindRule.Reached(89.5, 90));
        Assert.False(WindRule.Reached(80, 90));
    }

    [Fact]
    public void DownwindPointsAlongTheBearing()
    {
        (double east, double north) = WindRule.Downwind(90);
        Assert.Equal(1, east, 6);
        Assert.Equal(0, north, 6);
        (east, north) = WindRule.Downwind(0);
        Assert.Equal(0, east, 6);
        Assert.Equal(1, north, 6);
    }
}
