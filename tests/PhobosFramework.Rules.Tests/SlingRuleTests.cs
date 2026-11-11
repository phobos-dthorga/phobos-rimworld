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
}
