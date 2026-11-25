using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Framework 0.300.0: a turbine's compressor fouls with its hours, faster off paving, and costs it output until washed.</summary>
public class CompressorFoulingRuleTests
{
    const double PerHour = 0.01, RoughFactor = 2, Exposure = 0.5, FullLoss = 0.05;

    [Fact]
    public void OnPavingAWashFallsDueAfterAboutFiftyHours() =>
        Assert.Equal(50, 0.5 / CompressorFoulingRule.PerHour(PerHour, RoughFactor, roughShare: 0, Exposure), 6);

    [Fact]
    public void OnRoughGroundItFallsDueInHalfTheTime() =>
        Assert.Equal(25, 0.5 / CompressorFoulingRule.PerHour(PerHour, RoughFactor, roughShare: 1, Exposure), 6);

    [Fact]
    public void AnEngineWithNoExposureFoulsAtTheSameRateAnywhere() =>
        Assert.Equal(CompressorFoulingRule.PerHour(PerHour, RoughFactor, 0, 0), CompressorFoulingRule.PerHour(PerHour, RoughFactor, 1, 0), 9);

    [Fact]
    public void ACleanCompressorCostsNothing() => Assert.Equal(1, CompressorFoulingRule.ConditionFactor(0, FullLoss), 9);

    [Fact]
    public void AFullyFouledOneLosesItsFullShareAndNoMore()
    {
        Assert.Equal(0.95, CompressorFoulingRule.ConditionFactor(1, FullLoss), 9);
        Assert.Equal(0.95, CompressorFoulingRule.ConditionFactor(3, FullLoss), 9);
    }

    [Fact]
    public void FullyFouledItBurnsAboutTwoPercentMoreThroughTheWearFuelRule() =>
        // The shipped wear-fuel penalty (Rules_VehicleSystems.xml, turbineWearFuelPenalty) is 0.45: 0.45 × 5 % lost = 2.25 % more fuel.
        Assert.Equal(1.0225, TurbineModel.WearFuelFactor(CompressorFoulingRule.ConditionFactor(1, FullLoss), 0.45), 9);
}
