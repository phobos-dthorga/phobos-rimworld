using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>A fuel filter that clogs (Framework 0.365.0).</summary>
public class FuelFilterRuleTests
{
    [Theory]
    [InlineData(0.0, false, 1.0)]
    [InlineData(0.69, false, 1.0)]
    [InlineData(0.7, false, 0.75)]
    [InlineData(1.0, false, 0.75)]
    [InlineData(0.94, true, 0.75)]
    [InlineData(0.95, true, 0.0)]
    public void AFilterWeakensAnEngineAndStarvesItOnlyWhenThePlayerAllows(double clogged, bool mayStarve, double order) =>
        Assert.Equal(order, FuelFilterRule.Order(clogged, mayStarve), 6);

    [Theory]
    [InlineData(30, 0.0)]
    [InlineData(90, 0.0)]
    [InlineData(135, 0.5)]
    [InlineData(180, 1.0)]
    [InlineData(400, 1.0)]
    public void DepotFuelIsCleanWhileFreshAndWhollyDirtyAtTwiceThatAge(double ageDays, double dirt) =>
        Assert.Equal(dirt, FuelFilterRule.DepotDirt(ageDays, 90), 6);

    [Fact]
    public void CleanFuelDilutesADirtyTank() => Assert.Equal(0.25, FuelFilterRule.Mix(50, 1, 150, 0), 6);

    [Fact]
    public void AnEmptyTankTakesTheNewFuelsDirt() => Assert.Equal(0.6, FuelFilterRule.Mix(0, 0, 80, 0.6), 6);

    [Fact]
    public void NothingAddedKeepsTheTanksDirt() => Assert.Equal(0.3, FuelFilterRule.Mix(20, 0.3, 0, 1), 6);
}
