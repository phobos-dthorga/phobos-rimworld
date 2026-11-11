using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>A slot's picture by the upgrades fitted: the first fitted node wins, the stock look otherwise, nothing without one.</summary>
public class NodeLookRuleTests
{
    static readonly string?[] TipperBody = { null, "RockBody", "BulkBody" };

    static System.Func<string, bool> Fitted(params string[] keys) => new HashSet<string>(keys).Contains;

    [Fact]
    public void TheStockLookShowsWithNothingFitted()
    {
        Assert.Equal(0, NodeLookRule.Choose(TipperBody, Fitted()));
    }

    [Fact]
    public void AFittedNodeReplacesTheStockLook()
    {
        Assert.Equal(1, NodeLookRule.Choose(TipperBody, Fitted("RockBody")));
        Assert.Equal(2, NodeLookRule.Choose(TipperBody, Fitted("BulkBody")));
    }

    [Fact]
    public void TheFirstFittedNodeInOrderWins()
    {
        Assert.Equal(1, NodeLookRule.Choose(TipperBody, Fitted("BulkBody", "RockBody")));
    }

    [Fact]
    public void NodesNotInTheSlotChangeNothing()
    {
        Assert.Equal(0, NodeLookRule.Choose(TipperBody, Fitted("WorkLights")));
    }

    [Fact]
    public void ASlotWithoutAStockLookShowsNothingUntilFitted()
    {
        string?[] onlyUpgrade = { "HeavyFan" };
        Assert.Equal(-1, NodeLookRule.Choose(onlyUpgrade, Fitted()));
        Assert.Equal(0, NodeLookRule.Choose(onlyUpgrade, Fitted("HeavyFan")));
        Assert.Equal(-1, NodeLookRule.Choose(new string?[0], Fitted("HeavyFan")));
    }
}
