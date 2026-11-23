using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Wear per engine (Framework 0.247.0, per-engine realism step 4).</summary>
public class EngineWearRuleTests
{
    [Fact]
    public void EqualEnginesWearAsTheOneFigure()
    {
        double[] shares = EngineWearRule.Shares(new[] { 1.0, 1.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(1, shares[1], 9);
    }

    [Fact]
    public void AnEngineOutDoesNotWearAndTheOtherWearsAsOne()
    {
        double[] shares = EngineWearRule.Shares(new[] { 1.0, 0.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(0, shares[1], 9);
    }

    [Fact]
    public void TheStrongerEngineWearsMore()
    {
        double[] shares = EngineWearRule.Shares(new[] { 1.0, 0.5 });
        Assert.True(shares[0] > 1 && shares[1] < 1);
        Assert.Equal(2, shares[0] + shares[1], 9);
    }

    [Fact]
    public void WithNoneRunningEachWearsAsOne()
    {
        double[] shares = EngineWearRule.Shares(new[] { 0.0, 0.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(1, shares[1], 9);
    }

    [Fact]
    public void TheMostWornIsTheHighest() => Assert.Equal(1, EngineWearRule.MostWorn(new[] { 0.4, 1.2, 0.9 }));

    [Fact]
    public void NoneListedIsNone() => Assert.Equal(-1, EngineWearRule.MostWorn(System.Array.Empty<double>()));
}
