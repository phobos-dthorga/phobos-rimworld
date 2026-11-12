using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class HeadlightRuleTests
{
    [Fact]
    public void FacingNorthTheBeamStandsAheadOfTheNose()
    {
        // A vehicle 6 cells long centred at (10.5, 10.5): the nose is 3 cells north of the middle, the pool 5 beyond it.
        Assert.Equal((10, 18), HeadlightRule.BeamCell(10.5, 10.5, 0, 3, 5));
    }

    [Fact]
    public void FacingEastAndSouthAndWest()
    {
        Assert.Equal((18, 10), HeadlightRule.BeamCell(10.5, 10.5, 90, 3, 5));
        Assert.Equal((10, 2), HeadlightRule.BeamCell(10.5, 10.5, 180, 3, 5));
        Assert.Equal((2, 10), HeadlightRule.BeamCell(10.5, 10.5, 270, 3, 5));
    }

    [Fact]
    public void ADiagonalHeadingLightsTheDiagonal()
    {
        (int x, int z) = HeadlightRule.BeamCell(10.5, 10.5, 45, 2, 4);
        Assert.Equal(14, x);
        Assert.Equal(14, z);
    }

    [Fact]
    public void NegativeDistancesCountAsNone()
    {
        Assert.Equal((10, 10), HeadlightRule.BeamCell(10.5, 10.5, 0, -1, -2));
    }

    [Fact]
    public void ABeamIsLookedAtOnlyEveryInterval()
    {
        Assert.False(HeadlightRule.Due(100, 119, 20));
        Assert.True(HeadlightRule.Due(100, 120, 20));
        Assert.True(HeadlightRule.Due(100, 101, 0));
    }

    [Fact]
    public void AnotherFactionsVehicleShowsItsLightsOnlyWhileMovingAtNight()
    {
        Assert.True(HeadlightRule.LiteBurns(night: true, moving: true));
        Assert.False(HeadlightRule.LiteBurns(night: true, moving: false));
        Assert.False(HeadlightRule.LiteBurns(night: false, moving: true));
    }
}
