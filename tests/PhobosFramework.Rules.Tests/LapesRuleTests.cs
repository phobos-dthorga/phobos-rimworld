using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class LapesRuleTests
{
    [Fact]
    public void TheSlideIsTheRealOneAtTheDriftsMapShare()
    {
        // 150 ft = 45.72 m; a fifth of it on the map is 9.144 m; at 1.5 m a cell, 6.1, so 6 cells.
        Assert.Equal(6, LapesRule.SlideCells(1.5));
        Assert.Equal(1, LapesRule.SlideCells(0));
    }

    [Fact]
    public void TheStripHoldsTheSlideThePlatformAndOneSpare()
    {
        Assert.Equal(6 + 5 + 1, LapesRule.StripCells(1.5, 5));
        Assert.Equal(6 + 1 + 1, LapesRule.StripCells(1.5, 0));
    }

    [Fact]
    public void ItFliesAtEightFeetAsAShareOfItsHeight()
    {
        // 8 ft = 2.4384 m against a 30 m flying height.
        Assert.Equal(2.4384 / 30, LapesRule.HeightShare(30), 6);
        Assert.Equal(1, LapesRule.HeightShare(2), 6);
        Assert.Equal(1, LapesRule.HeightShare(0), 6);
    }

    [Fact]
    public void ThePlatformSlowsEvenlyToRest()
    {
        Assert.Equal(0, LapesRule.SlidShare(0), 6);
        Assert.Equal(0.75, LapesRule.SlidShare(0.5), 6);
        Assert.Equal(1, LapesRule.SlidShare(1), 6);
        Assert.Equal(1, LapesRule.SlidShare(2), 6);
    }

    [Fact]
    public void ItLandsHardMoreOftenThanByParachute()
    {
        double hard = LapesRule.HardLandingChance, strike = LapesRule.GroundStrikeChance;
        Assert.True(hard > ParachuteRule.HardLandingChance);
        Assert.InRange(strike, 0.001, 0.05);
    }

    [Fact]
    public void OnlyAnEmptyLoadWithinTheLimitGoes()
    {
        Assert.True(LapesRule.MayExtract(4800, 5000, crewInside: false));
        Assert.False(LapesRule.MayExtract(5200, 5000, crewInside: false));
        Assert.False(LapesRule.MayExtract(4800, 5000, crewInside: true));
        Assert.False(LapesRule.MayExtract(4800, 0, crewInside: false));
    }
}
