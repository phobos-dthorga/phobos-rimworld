using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Wear per part (Framework 0.247.0 for engines; generic since 0.248.0).</summary>
public class PartWearRuleTests
{
    [Fact]
    public void EqualEnginesWearAsTheOneFigure()
    {
        double[] shares = PartWearRule.Shares(new[] { 1.0, 1.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(1, shares[1], 9);
    }

    [Fact]
    public void AnEngineOutDoesNotWearAndTheOtherWearsAsOne()
    {
        double[] shares = PartWearRule.Shares(new[] { 1.0, 0.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(0, shares[1], 9);
    }

    [Fact]
    public void TheStrongerEngineWearsMore()
    {
        double[] shares = PartWearRule.Shares(new[] { 1.0, 0.5 });
        Assert.True(shares[0] > 1 && shares[1] < 1);
        Assert.Equal(2, shares[0] + shares[1], 9);
    }

    [Fact]
    public void WithNoneRunningEachWearsAsOne()
    {
        double[] shares = PartWearRule.Shares(new[] { 0.0, 0.0 });
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(1, shares[1], 9);
    }

    [Fact]
    public void TheMostWornIsTheHighest() => Assert.Equal(1, PartWearRule.MostWorn(new[] { 0.4, 1.2, 0.9 }));

    [Fact]
    public void NoneListedIsNone() => Assert.Equal(-1, PartWearRule.MostWorn(System.Array.Empty<double>()));

    [Fact]
    public void TheFrontAxleWearsMoreAndTheMeanStaysOne()
    {
        // Two axles, front at 2, rear at -2; the front weighs 1.5.
        double[] shares = PartWearRule.AxleShares(new[] { 2f, 2f, -2f, -2f }, 1.5, 0.75f);
        Assert.Equal(1.2, shares[0], 9);
        Assert.Equal(1.2, shares[1], 9);
        Assert.Equal(0.8, shares[2], 9);
        Assert.Equal(4, shares[0] + shares[1] + shares[2] + shares[3], 9);
    }

    [Fact]
    public void OneAxleWearsEvenly()
    {
        double[] shares = PartWearRule.AxleShares(new[] { 0f, 0f }, 1.5, 0.75f);
        Assert.Equal(1, shares[0], 9);
        Assert.Equal(1, shares[1], 9);
    }

    [Fact]
    public void ARestoreByAxleDoesTheWholeAxle()
    {
        var done = PartWearRule.RestoreSet(new[] { 0.3, 0.2, 0.9, 0.8 }, new[] { 2f, 2f, -2f, -2f }, 0.75f);
        done.Sort();
        Assert.Equal(new[] { 2, 3 }, done);
    }

    [Fact]
    public void ARestoreWithoutAxlesDoesTheMostWornAlone() =>
        Assert.Equal(new[] { 2 }, PartWearRule.RestoreSet(new[] { 0.3, 0.2, 0.9 }, null, 0.75f));
}
