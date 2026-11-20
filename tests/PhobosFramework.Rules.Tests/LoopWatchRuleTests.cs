using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

/// <summary>The loop watch (Framework 0.205.0): fetch and put away, over and over, is told once.</summary>
public class LoopWatchRuleTests
{
    static bool Turns(LoopTrail trail, int count, int start, int step)
    {
        bool told = false;
        int now = start;
        for (int i = 0; i < count; i++)
        {
            told |= LoopWatchRule.Record(trail, putAway: false, now);
            now += step;
            told |= LoopWatchRule.Record(trail, putAway: true, now);
            now += step;
        }
        return told;
    }

    [Fact]
    public void ThreeTurnsInAWhileAreALoop() => Assert.True(Turns(new LoopTrail(), 3, 1000, 200));

    [Fact]
    public void TwoTurnsAreNot() => Assert.False(Turns(new LoopTrail(), 2, 1000, 200));

    [Fact]
    public void TurnsSpreadOverDaysAreNot() => Assert.False(Turns(new LoopTrail(), 5, 1000, 3000));

    [Fact]
    public void HaulingTheSameThingTwiceIsOneTurnAtMost()
    {
        var trail = new LoopTrail();
        LoopWatchRule.Record(trail, putAway: true, 100);
        LoopWatchRule.Record(trail, putAway: true, 200);
        LoopWatchRule.Record(trail, putAway: true, 300);
        Assert.Equal(0, trail.turns);
    }

    [Fact]
    public void ALoopIsToldOnceThenQuietForADay()
    {
        var trail = new LoopTrail();
        Assert.True(Turns(trail, 3, 1000, 200));
        Assert.False(Turns(trail, 3, 2400, 200));
        Assert.True(Turns(trail, 3, 1000 + LoopWatchRule.QuietTicks + 10000, 200));
    }

    [Fact]
    public void AQuietTrailIsForgotten()
    {
        var trail = new LoopTrail();
        LoopWatchRule.Record(trail, putAway: false, 100);
        Assert.False(LoopWatchRule.Stale(trail, 200));
        Assert.True(LoopWatchRule.Stale(trail, 100 + LoopWatchRule.WindowTicks + 1));
    }
}
