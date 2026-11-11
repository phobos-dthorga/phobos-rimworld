using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FitCostRuleTests
{
    [Fact]
    public void TheReferenceWeightCostsTheListedFigure() => Assert.Equal(1, FitCostRule.WeightFactor(5000, 5000, 0.5, 0.5, 3), 6);

    [Fact]
    public void FourTimesTheWeightCostsTwiceTheMaterials() => Assert.Equal(2, FitCostRule.WeightFactor(20000, 5000, 0.5, 0.5, 3), 6);

    [Fact]
    public void TimeGrowsLessThanMaterials() =>
        Assert.True(FitCostRule.WeightFactor(20000, 5000, 0.25, 0.5, 3) < FitCostRule.WeightFactor(20000, 5000, 0.5, 0.5, 3));

    [Fact]
    public void ALightVehicleIsHeldAtTheFloor() => Assert.Equal(0.5, FitCostRule.WeightFactor(600, 5000, 0.5, 0.5, 3), 6);

    [Fact]
    public void AHeavyVehicleIsHeldAtTheCeiling() => Assert.Equal(3, FitCostRule.WeightFactor(200000, 5000, 0.5, 0.5, 3), 6);

    [Fact]
    public void AnUnknownWeightCostsTheListedFigure() => Assert.Equal(1, FitCostRule.WeightFactor(0, 5000, 0.5, 0.5, 3), 6);

    [Fact]
    public void ACountRoundsUp() => Assert.Equal(13, FitCostRule.Count(5, 2.5));

    [Fact]
    public void ACountNeverFallsToNothing() => Assert.Equal(1, FitCostRule.Count(2, 0.1));

    [Fact]
    public void NoBaseCostsNothing() => Assert.Equal(0, FitCostRule.Count(0, 3));

    [Fact]
    public void WorkScales() => Assert.Equal(15000f, FitCostRule.Work(10000f, 1.5), 3);
}
