using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class ReachRuleTests
{
    [Theory]
    [InlineData(12, 3, 5)]     // the Hind across: a twelve-cell rotor over a three-cell body, 4.5 a side, rounded up
    [InlineData(12, 9, 2)]     // the Hind along its length
    [InlineData(8.4, 5, 2)]    // the Harrier across: 1.7 a side
    [InlineData(8.4, 7, 1)]    // and along: 0.7
    [InlineData(5, 5, 0)]      // a truck drawn to its footprint
    [InlineData(3.5, 5, 0)]    // drawn smaller than its footprint
    [InlineData(7, 5, 1)]      // exactly one cell a side
    public void TheMarginCoversThePictureOnEachSide(double drawn, int footprint, int margin) =>
        Assert.Equal(margin, ReachRule.Margin(drawn, footprint));

    [Fact]
    public void FlightRangeTurnsVehicleFrameworksFuelNeedRound()
    {
        // Vehicle Framework: fuel needed = rate x multiplier x tiles / (0.001 x speed). For 10 tiles at speed 16, rate 0.002, multiplier 1.5:
        double needed = 0.002 * 1.5 * 10 / (0.001 * 16);
        Assert.Equal(10, ReachRule.FlightTiles(needed, 16, 0.002, 1.5), 6);
    }

    [Fact]
    public void ARangeBecomesAnAngleByTheGamesOwnConversion()
    {
        Assert.Equal(0.5, ReachRule.FlightAngle(110, 220), 6);
        Assert.Equal(0, ReachRule.FlightAngle(0, 220));
        Assert.Equal(0, ReachRule.FlightAngle(110, 0));
    }

    [Fact]
    public void HalfWayRoundThePlanetIsEverywhere()
    {
        Assert.False(ReachRule.CoversPlanet(ReachRule.FlightAngle(110, 220)));
        Assert.True(ReachRule.CoversPlanet(ReachRule.FlightAngle(1280, 220)));
        Assert.True(ReachRule.CoversPlanet(System.Math.PI));
    }

    [Fact]
    public void NoFuelOrNoBurnIsNoRange()
    {
        Assert.Equal(0, ReachRule.FlightTiles(0, 16, 0.002, 1));
        Assert.Equal(0, ReachRule.FlightTiles(100, 16, 0, 1));
        Assert.Equal(0, ReachRule.FlightTiles(100, 0, 0.002, 1));
    }
}
