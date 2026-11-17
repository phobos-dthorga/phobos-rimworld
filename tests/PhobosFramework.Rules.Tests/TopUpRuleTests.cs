using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>Topping up (docs/development/top-up-design.md): when a store that wants more is worth a hauler's walk, and how much to bring.</summary>
public class TopUpRuleTests
{
    [Fact]
    public void AStoreThatWantsNothingIsNeverWorthAWalk()
    {
        Assert.False(TopUpRule.Worth(0, 1, forced: false));
        Assert.False(TopUpRule.Worth(-5, 1, forced: true));
    }

    [Fact]
    public void ItIsWorthAWalkOnceTheLeastLoadIsMissing()
    {
        Assert.False(TopUpRule.Worth(39, 40, forced: false)); // a drone costs 40 steel: one piece short of a drone's worth
        Assert.True(TopUpRule.Worth(40, 40, forced: false));
        Assert.True(TopUpRule.Worth(100, 40, forced: false));
    }

    [Fact]
    public void AnOrderByHandIgnoresTheLeastLoad()
    {
        Assert.True(TopUpRule.Worth(1, 40, forced: true));
    }

    [Fact]
    public void ALeastLoadBelowOneCountsAsOne()
    {
        Assert.True(TopUpRule.Worth(1, 0, forced: false));
        Assert.True(TopUpRule.Worth(1, -3, forced: false));
    }

    [Fact]
    public void WhatOthersAlreadyBringIsNotBroughtTwice()
    {
        Assert.Equal(40, TopUpRule.ToBring(40, 0));
        Assert.Equal(15, TopUpRule.ToBring(40, 25));
        Assert.Equal(0, TopUpRule.ToBring(40, 60));
        Assert.Equal(40, TopUpRule.ToBring(40, -10));
    }
}
