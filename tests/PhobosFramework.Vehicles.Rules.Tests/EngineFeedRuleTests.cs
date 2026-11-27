using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Each aircraft engine's own fuel and oil (Framework 0.360.0, the vehicle standard's batch 3).</summary>
public class EngineFeedRuleTests
{
    [Theory]
    [InlineData(0.05, 0.0)]
    [InlineData(0.1, 0.75)]
    [InlineData(0.49, 0.75)]
    [InlineData(0.5, 1.0)]
    [InlineData(1.0, 1.0)]
    public void APumpStarvesOrWeakensItsEngine(double feed, double order) => Assert.Equal(order, EngineFeedRule.FeedOrder(feed), 9);

    [Theory]
    [InlineData(1.0, 1.0, 1.0)]
    [InlineData(1.0, 0.6, 1.0)]
    [InlineData(1.0, 0.3, 0.4)]
    [InlineData(1.0, 0.45, 0.7)]
    [InlineData(1.0, 0.0, 0.0)]
    [InlineData(0.5, 1.0, 0.5)]
    [InlineData(0.0, 1.0, 0.0)]
    public void PressureNeedsOilAndAPump(double pump, double level, double pressure) =>
        Assert.Equal(pressure, EngineFeedRule.OilPressure(pump, level), 9);

    [Fact]
    public void ARunningEngineBurnsItsOwnOil() => Assert.Equal(1 - 0.0015 * 10, EngineFeedRule.OilAfterBurn(1, 0.0015, 1, 1, 10), 9);

    [Fact]
    public void AStoppedEngineBurnsNone() => Assert.Equal(0.8, EngineFeedRule.OilAfterBurn(0.8, 0.0015, 0, 1, 10), 9);

    [Fact]
    public void AWornEngineBurnsTwice() => Assert.Equal(1 - 0.003, EngineFeedRule.OilAfterBurn(1, 0.0015, 1, 0.3, 1), 9);

    [Fact]
    public void AHitDrainsTheSump() => Assert.Equal(0.7, EngineFeedRule.OilAfterHit(1, 0.5, 0.6), 9);

    [Fact]
    public void ASumpNeverGoesBelowEmpty() => Assert.Equal(0, EngineFeedRule.OilAfterHit(0.1, 1, 0.6), 9);

    [Theory]
    [InlineData(-40, 0.0)]
    [InlineData(-47, 0.0)]
    [InlineData(-52, 0.5)]
    [InlineData(-57, 1.0)]
    [InlineData(-70, 1.0)]
    public void JetFuelFreezesFromItsFreezingPoint(double celsius, double frozen) =>
        Assert.Equal(frozen, EngineFeedRule.Frozen(celsius, -47, -57), 9);

    [Fact]
    public void TheAirCoolsWithHeight() => Assert.Equal(15 - 6.5 * 5, EngineFeedRule.AirAtHeight(15, 0, 5000), 6);

    [Fact]
    public void TheCoolingStopsAtTheTopOfTheTroposphere() =>
        Assert.Equal(EngineFeedRule.AirAtHeight(15, 0, 11000), EngineFeedRule.AirAtHeight(15, 0, 14000), 9);

    [Fact]
    public void HeightIsCountedFromTheGround() => Assert.Equal(-10 - 6.5 * 2, EngineFeedRule.AirAtHeight(-10, 3000, 5000), 6);

    [Fact]
    public void TheAirIsNeverColderThanTheFloor() => Assert.Equal(EngineFeedRule.ColdestAirCelsius, EngineFeedRule.AirAtHeight(-60, 0, 11000), 9);

    [Fact]
    public void SpeedWarmsTheAirItMeets() => Assert.InRange(EngineFeedRule.RamRise(-55, 0.7), 21, 22);

    [Fact]
    public void AHelicopterBarelyWarmsIt() => Assert.InRange(EngineFeedRule.RamRise(-55, 0.2), 1.7, 1.8);
}
