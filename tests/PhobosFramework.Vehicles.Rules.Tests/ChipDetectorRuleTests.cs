using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>A chip detector (Framework 0.365.0).</summary>
public class ChipDetectorRuleTests
{
    [Fact]
    public void ATenthOfAPartsHealthLostToWearLightsTheLamp() =>
        Assert.True(ChipDetectorRule.Lit(ChipDetectorRule.AfterWear(0, 0.1)));

    [Fact]
    public void ALittleWearDoesNot() => Assert.False(ChipDetectorRule.Lit(ChipDetectorRule.AfterWear(0, 0.05)));

    [Fact]
    public void FiftyHoursRunWornLightsIt() => Assert.Equal(0.5, ChipDetectorRule.AfterRunning(0, 50, 1), 6);

    [Fact]
    public void TwoWaysWornIsTwiceAsFast() => Assert.Equal(0.5, ChipDetectorRule.AfterRunning(0, 25, 2), 6);

    [Fact]
    public void AnEngineRunInGoodOrderGainsNothing() => Assert.Equal(0.2, ChipDetectorRule.AfterRunning(0.2, 100, 0), 6);

    [Fact]
    public void MetalNeverPassesWhole() => Assert.Equal(1, ChipDetectorRule.AfterWear(0.9, 1), 6);
}
