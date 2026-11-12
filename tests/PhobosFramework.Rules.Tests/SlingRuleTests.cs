using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SlingRuleTests
{
    const double Rated = 11800, From = 15, PerDegree = 0.01, MaxLoss = 0.3;

    [Fact]
    public void ACoolDayLiftsTheRatedLoad() => Assert.Equal(Rated, SlingRule.Capacity(Rated, 10, true, From, PerDegree, MaxLoss), 6);

    [Fact]
    public void EachDegreeAboveFifteenCostsOnePercent() => Assert.Equal(Rated * 0.9, SlingRule.Capacity(Rated, 25, true, From, PerDegree, MaxLoss), 6);

    [Fact]
    public void TheLossIsCapped() => Assert.Equal(Rated * 0.7, SlingRule.Capacity(Rated, 60, true, From, PerDegree, MaxLoss), 6);

    [Fact]
    public void WithDeratingOffHeatCostsNothing() => Assert.Equal(Rated, SlingRule.Capacity(Rated, 45, false, From, PerDegree, MaxLoss), 6);

    [Fact]
    public void NoHookLiftsNothing() => Assert.Equal(0, SlingRule.Capacity(0, 10, true, From, PerDegree, MaxLoss), 6);

    [Fact]
    public void ALoadWithinCapacityLifts() => Assert.True(SlingRule.CanLift(6400, 11800));

    [Fact]
    public void ATooHeavyLoadDoesNot() => Assert.False(SlingRule.CanLift(20000, 11800));

    [Fact]
    public void NothingIsNotALoad() => Assert.False(SlingRule.CanLift(0, 11800));

    [Fact]
    public void TheShareIsTheLoadOverCapacity() => Assert.Equal(0.5, SlingRule.Share(5900, 11800), 6);

    const double Strops = 3;

    [Fact]
    public void BeforeTheHelicopterMovesTheLoadIsDrawnWhereItStood()
    {
        (double x, double z, double paidOut) = SlingRule.Hang(0, 0, Strops, 8, -5);
        Assert.Equal(8, x, 6);
        Assert.Equal(-5, z, 6);
        Assert.Equal(0, paidOut, 6);
    }

    [Fact]
    public void OnceTheStropsArePaidOutTheLoadHangsBelowTheHelicopter()
    {
        (double x, double z, double paidOut) = SlingRule.Hang(20, 6, Strops, 8, -5);
        Assert.Equal(20, x, 6);
        Assert.Equal(6 - Strops, z, 6);
        Assert.Equal(1, paidOut, 6);
    }

    [Fact]
    public void WhileTheStropsTakeUpTheLoadIsBetweenItsSpotAndTheHelicopter()
    {
        (double x, double z, double paidOut) = SlingRule.Hang(0, 1.5, Strops, 8, -5);
        Assert.Equal(0.5, paidOut, 6);
        Assert.InRange(x, 0.01, 7.99);
        Assert.InRange(z, -4.99, -0.01);
    }

    [Fact]
    public void OnALandingTheLoadMeetsTheGroundUnderTheHelicopter()
    {
        (double x, double z, double paidOut) = SlingRule.Hang(0, 1, Strops, null, null);
        Assert.Equal(0, x, 6);
        Assert.Equal(0, z, 6);
        Assert.InRange(paidOut, 0.3, 0.34);
    }

    [Fact]
    public void WithNoStropsTheLoadIsDrawnAtTheHelicopter()
    {
        (double x, double z, double paidOut) = SlingRule.Hang(4, 2, 0, 8, -5);
        Assert.Equal(4, x, 6);
        Assert.Equal(2, z, 6);
        Assert.Equal(1, paidOut, 6);
    }

    [Fact]
    public void ACalmClearDayAllowsALift() => Assert.False(SlingRule.Grounded(0.8, storm: false, maxWind: 1.5));

    [Fact]
    public void StrongWindGroundsALift() => Assert.True(SlingRule.Grounded(1.7, storm: false, maxWind: 1.5));

    [Fact]
    public void AStormGroundsALiftWhateverTheWind() => Assert.True(SlingRule.Grounded(0.2, storm: true, maxWind: 1.5));

    [Fact]
    public void ASlungFlightCostsThirtyPercentMoreFuelPerTile()
    {
        (double speed, double fuelPerTick) = SlingRule.SlungFlight(0.7, 1.3);
        Assert.Equal(0.7, speed, 6);
        // Fuel per tile is fuel per tick over speed: back to the authored 1.3.
        Assert.Equal(1.3, fuelPerTick / speed, 6);
    }

    [Fact]
    public void ASlungFlightNeverStopsTheHelicopter() => Assert.True(SlingRule.SlungFlight(0, 1.3).Speed > 0);

    [Fact]
    public void ALowDropDoesNoHarm() => Assert.Equal(0, SlingRule.DropShare(1.5, 2, 40), 6);

    [Fact]
    public void HarmGrowsWithTheHeightOfTheDrop() => Assert.Equal(0.5, SlingRule.DropShare(21, 2, 40), 6);

    [Fact]
    public void AHighDropRuinsEveryPart() => Assert.Equal(1, SlingRule.DropShare(120, 2, 40), 6);
}
