using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>Drones called home in time, and wrecks taken home (docs/development/drone-recall-design.md).</summary>
public class RecallRuleTests
{
    [Fact]
    public void WhatADroneNeedsIsThePaddedWalkTheWaitAndTheSpare()
    {
        // 1,000 ticks of walk, padded by half: 1,500; Fortified's platforms keep a drone 180 ticks; 600 spare.
        Assert.Equal(1500 + 180 + RecallRule.SpareTicks, RecallRule.Needed(1000, 180));
    }

    [Fact]
    public void ANegativeWalkOrWaitCountsAsNone()
    {
        Assert.Equal(RecallRule.SpareTicks, RecallRule.Needed(-50, -10));
    }

    [Fact]
    public void ADroneIsCalledHomeOncePowerBarelyCoversTheWalk()
    {
        int needed = RecallRule.Needed(1000, 180);
        Assert.False(RecallRule.CallHome(needed + 1, 1000, 180));
        Assert.True(RecallRule.CallHome(needed, 1000, 180));
        Assert.True(RecallRule.CallHome(0, 1000, 180));
    }

    [Fact]
    public void AFarDroneIsCalledHomeLongBeforeTheFixedTwoHours()
    {
        // A walk the path finder prices at 4,000 ticks (the uca in the owner's save took over 3,000 for its first part): 6,780 needed.
        Assert.True(RecallRule.CallHome(5500, 4000, 180)); // above Fortified's own 5,000-tick call, yet too little to get home
    }

    [Fact]
    public void ANearDroneIsLeftAtWork()
    {
        Assert.False(RecallRule.CallHome(5000, 10 * 15, 180));
    }

    [Fact]
    public void ThePathIsPricedOnlyWhenAStretchedStraightLineComesClose()
    {
        Assert.False(RecallRule.WorthPricing(30000, 300, 180)); // plenty of power: no path asked for
        Assert.True(RecallRule.WorthPricing(2000, 300, 180));
    }

    [Fact]
    public void AWreckFillsTheStoreAndTheRestGoesBeside()
    {
        Assert.Equal((25, 0), RecallRule.TakeBack(25, 100));
        Assert.Equal((10, 15), RecallRule.TakeBack(25, 10));
        Assert.Equal((0, 25), RecallRule.TakeBack(25, 0));
    }

    [Fact]
    public void AStoreWithNoKnownCapacityTakesItAll()
    {
        Assert.Equal((30, 0), RecallRule.TakeBack(30, -1));
    }

    [Fact]
    public void NothingIsMadeFromNothing()
    {
        Assert.Equal((0, 0), RecallRule.TakeBack(-5, 10));
        for (int room = -1; room <= 40; room++)
        {
            (int into, int beside) = RecallRule.TakeBack(30, room);
            Assert.Equal(30, into + beside);
        }
    }
}
