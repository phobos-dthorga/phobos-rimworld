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

    // The engineering drone bay: its own fill order brings it to 60 of 360 (Fortified's maxToFill is its starting store).
    [Fact]
    public void ItsOwnLevelIsWhatItsFillOrderWouldBring()
    {
        Assert.Equal(40, TopUpRule.Wanted(ownWanted: 40, held: 20, capacity: 360, TopUpLevel.OwnLevel));
        Assert.Equal(0, TopUpRule.Wanted(ownWanted: 0, held: 60, capacity: 360, TopUpLevel.OwnLevel));
    }

    [Fact]
    public void AHigherLevelFillsByCapacity()
    {
        Assert.Equal(160, TopUpRule.Wanted(ownWanted: 40, held: 20, capacity: 360, TopUpLevel.Half));          // to 180
        Assert.Equal(250, TopUpRule.Wanted(ownWanted: 40, held: 20, capacity: 360, TopUpLevel.ThreeQuarters)); // to 270
        Assert.Equal(340, TopUpRule.Wanted(ownWanted: 40, held: 20, capacity: 360, TopUpLevel.Full));
        Assert.Equal(300, TopUpRule.Wanted(ownWanted: 0, held: 60, capacity: 360, TopUpLevel.Full));           // its own level met, more wanted
    }

    [Fact]
    public void ALevelNeverFillsBelowTheMachinesOwn()
    {
        // The Cluster Weaver starts full at 450 of 450: half its capacity is below its own level, which stands.
        Assert.Equal(100, TopUpRule.Wanted(ownWanted: 100, held: 350, capacity: 450, TopUpLevel.Half));
    }

    [Fact]
    public void NeverPastCapacity()
    {
        Assert.Equal(0, TopUpRule.Wanted(ownWanted: 0, held: 360, capacity: 360, TopUpLevel.Full));
        Assert.Equal(0, TopUpRule.Wanted(ownWanted: 0, held: 400, capacity: 360, TopUpLevel.Full));
    }

    [Fact]
    public void AnOddCapacityRoundsTheShareUp()
    {
        Assert.Equal(38, TopUpRule.Wanted(ownWanted: 0, held: 0, capacity: 75, TopUpLevel.Half));
    }

    [Fact]
    public void WithNoCapacityKnownItIsTheStoresOwnWant()
    {
        Assert.Equal(40, TopUpRule.Wanted(ownWanted: 40, held: 20, capacity: 0, TopUpLevel.Full));
        Assert.Equal(0, TopUpRule.Wanted(ownWanted: -5, held: 20, capacity: 0, TopUpLevel.Full));
    }
}
