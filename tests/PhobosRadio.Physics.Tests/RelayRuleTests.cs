using System.Collections.Generic;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class RelayRuleTests
{
    /// <summary>A line of nodes 0 (start), 1, 2 (relays) and 3 (target), with the margins given for each pair; any pair not given has no link.</summary>
    static RelayRule.Chain Line(Dictionary<(int, int), double> margins, int maxHops, int target = 3, int nodes = 4, System.Func<int, bool>? canRelay = null) =>
        RelayRule.Best(nodes, 0, i => i == target, canRelay ?? (_ => true),
            (a, b) => margins.TryGetValue((System.Math.Min(a, b), System.Math.Max(a, b)), out var m) ? m : null, maxHops);

    [Fact]
    public void ADirectLinkNeedsNoRelay()
    {
        var chain = Line(new() { [(0, 3)] = 30 }, 3);
        Assert.True(chain.Found);
        Assert.Equal(0, chain.Relays);
        Assert.Equal(new[] { 0, 3 }, chain.Path);
        Assert.Equal(30, chain.WorstMarginDb);
    }

    [Fact]
    public void AChainIsAsGoodAsItsWorstHop()
    {
        var chain = Line(new() { [(0, 1)] = 40, [(1, 2)] = 12, [(2, 3)] = 50 }, 3);
        Assert.True(chain.Found);
        Assert.Equal(2, chain.Relays);
        Assert.Equal(new[] { 0, 1, 2, 3 }, chain.Path);
        Assert.Equal(12, chain.WorstMarginDb);
    }

    [Fact]
    public void TheHopLimitHolds()
    {
        var margins = new Dictionary<(int, int), double> { [(0, 1)] = 40, [(1, 2)] = 40, [(2, 3)] = 40 };
        Assert.False(Line(margins, 2).Found);
        Assert.True(Line(margins, 3).Found);
    }

    [Fact]
    public void TheChainWithMostToSpareWins()
    {
        // Direct, thin; through relay 1, healthy.
        var chain = Line(new() { [(0, 3)] = 2, [(0, 1)] = 30, [(1, 3)] = 25 }, 3);
        Assert.Equal(new[] { 0, 1, 3 }, chain.Path);
        Assert.Equal(25, chain.WorstMarginDb);
    }

    [Fact]
    public void ANodeThatCannotRelayPassesNothingOn()
    {
        var margins = new Dictionary<(int, int), double> { [(0, 1)] = 40, [(1, 3)] = 40 };
        Assert.False(Line(margins, 3, canRelay: i => i != 1).Found);
    }

    [Fact]
    public void NoLinkNoChain()
    {
        Assert.False(Line(new(), 3).Found);
        Assert.False(RelayRule.Best(0, 0, _ => true, _ => true, (_, _) => 10, 3).Found);
        Assert.False(Line(new() { [(0, 3)] = 30 }, 0).Found);
    }
}

public class PlaneEarthAndSiteTests
{
    [Theory]
    [InlineData(1000.0, 10.0, 10.0, 80.0)]
    [InlineData(20000.0, 3.0, 10.0, 142.5)]
    [InlineData(0.5, 10.0, 10.0, -40.0)]
    public void PlaneEarthFallsWithTheFourthPowerAndGainsWithHeight(double metres, double a, double b, double expected) =>
        Assert.Equal(expected, Propagation.PlaneEarthPathLossDb(metres, a, b), 1);

    [Fact]
    public void DoublingTheDistanceCostsTwelveDecibels() =>
        Assert.Equal(12.04, Propagation.PlaneEarthPathLossDb(20000, 3, 10) - Propagation.PlaneEarthPathLossDb(10000, 3, 10), 2);

    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(2, 30.0)]
    [InlineData(3, 100.0)]
    [InlineData(4, 300.0)]
    [InlineData(5, 0.0)]
    [InlineData(0, 0.0)]
    public void HillsLiftAnAerial(int hilliness, double metres) => Assert.Equal(metres, Propagation.SiteHeightMetres(hilliness));
}
