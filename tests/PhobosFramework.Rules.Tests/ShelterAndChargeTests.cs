using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ShelterComfortTests
{
    // A typical colonist's comfort range in ordinary clothes is about 16 to 26 degrees (game defaults with apparel; authored here).
    const double Min = 16, Max = 26;

    [Theory]
    [InlineData(20, -15, ShelterStage.ShelteredFromCold)]
    [InlineData(20, 40, ShelterStage.ShelteredFromHeat)]
    [InlineData(20, 21, ShelterStage.None)]
    [InlineData(10, -15, ShelterStage.Chilly)]
    [InlineData(5, -15, ShelterStage.Freezing)]
    [InlineData(30, 40, ShelterStage.Hot)]
    [InlineData(37, 40, ShelterStage.Stifling)]
    public void TheStageFollowsTheInsideFirstThenTheOutside(double inside, double outside, ShelterStage expected) =>
        Assert.Equal(expected, ShelterComfort.Stage(inside, outside, Min, Max));

    [Fact]
    public void TheEdgesOfTheRangeAreComfortable()
    {
        Assert.Equal(ShelterStage.None, ShelterComfort.Stage(Min, Min, Min, Max));
        Assert.Equal(ShelterStage.None, ShelterComfort.Stage(Max, Max, Min, Max));
        Assert.Equal(ShelterStage.Chilly, ShelterComfort.Stage(Min - ShelterComfort.SevereMargin, 0, Min, Max));
    }

    [Fact]
    public void ASwappedRangeIsReadTheRightWayRound() =>
        Assert.Equal(ShelterStage.ShelteredFromCold, ShelterComfort.Stage(20, -15, Max, Min));
}

public class ChargeRuleTests
{
    [Fact]
    public void AUseTakesOneWholeCharge()
    {
        Assert.Equal(3, ChargeRules.AfterUse(4), 6);
        Assert.Equal(0.5, ChargeRules.AfterUse(1.5), 6);
        Assert.Equal(0.9, ChargeRules.AfterUse(0.9), 6);
        Assert.False(ChargeRules.CanUse(0.99));
        Assert.Equal(1, ChargeRules.WholeUses(1.99));
    }

    [Fact]
    public void RechargingAddsAUsePerHoursPerUseAndStopsAtFull()
    {
        Assert.Equal(1.5, ChargeRules.Recharged(1, 4, 1, 2), 6);
        Assert.Equal(4, ChargeRules.Recharged(3.9, 4, 10, 2), 6);
        Assert.Equal(1, ChargeRules.Recharged(1, 4, 5, 0), 6);
    }
}

public class LastingRuleTests
{
    [Fact]
    public void NothingIsLeftBelowTheThreshold() => Assert.Equal(0, LastingRule.SeverityFor(0.59, 0.6), 6);

    [Fact]
    public void AtTheThresholdAFaintTraceAndAtFullTheWorst()
    {
        Assert.Equal(LastingRule.Faintest, LastingRule.SeverityFor(0.6, 0.6), 6);
        Assert.Equal(1, LastingRule.SeverityFor(1, 0.6), 6);
        Assert.Equal(0.55, LastingRule.SeverityFor(0.8, 0.6), 6);
    }
}
