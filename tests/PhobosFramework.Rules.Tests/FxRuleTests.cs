using System;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FxRuleTests
{
    [Theory]
    [InlineData(2500, 15, 0.99, 15)]     // one a tick: exactly fifteen in fifteen ticks, whatever the roll
    [InlineData(100, 15, 0.0, 1)]        // 0.6 expected: a roll under 0.6 makes one
    [InlineData(100, 15, 0.7, 0)]        // and a roll over it none
    [InlineData(0, 15, 0.0, 0)]
    [InlineData(-5, 15, 0.0, 0)]
    [InlineData(100, 0, 0.0, 0)]
    public void CountSpreadsTheRateOverThePass(double perHour, int ticks, double roll, int expected) =>
        Assert.Equal(expected, FxRule.Count(perHour, ticks, roll));

    [Fact]
    public void CountKeepsTheLongRunRate()
    {
        int made = 0;
        for (int i = 0; i < 1000; i++) made += FxRule.Count(50, 15, (i + 0.5) / 1000.0);
        Assert.Equal(300, made);    // 50 an hour over 15,000 ticks is 300
    }

    [Theory]
    [InlineData(0, 1, 0, 0, 1)]      // facing north, ahead is north
    [InlineData(90, 1, 0, 1, 0)]     // facing east, ahead is east
    [InlineData(0, 0, 1, 1, 0)]      // facing north, the right is east
    [InlineData(180, -1, 0, 0, 1)]   // facing south, behind is north
    [InlineData(90, 0, 1, 0, -1)]    // facing east, the right is south
    public void OffsetFollowsTheHeading(double heading, double forward, double right, double x, double z)
    {
        (double ox, double oz) = FxRule.Offset(heading, forward, right);
        Assert.Equal(x, ox, 6);
        Assert.Equal(z, oz, 6);
    }

    [Theory]
    [InlineData(FxLevel.Off, true, 0)]
    [InlineData(FxLevel.Off, false, 0)]
    [InlineData(FxLevel.Light, true, 1)]
    [InlineData(FxLevel.Light, false, 0.4)]
    [InlineData(FxLevel.Full, false, 1)]
    public void LightThinsOnlyWhatMovingVehiclesThrowUp(FxLevel level, bool sign, double expected) =>
        Assert.Equal(expected, FxRule.Density(level, sign, 0.4), 6);

    [Theory]
    [InlineData(1, 0, 30, 1)]
    [InlineData(1, 15, 30, 0.5)]
    [InlineData(0.5, 0, 30, 0.5)]
    [InlineData(1, 30, 30, 0)]
    [InlineData(1, 50, 30, 0)]
    [InlineData(0, 0, 30, 0)]
    public void DownwashFadesWithHeightAndRotorSpeed(double rotor, double height, double reach, double expected) =>
        Assert.Equal(expected, FxRule.Downwash(rotor, height, reach), 6);

    [Theory]
    [InlineData(0.1, 0.15, 20, 200, 0)]       // idling: nothing
    [InlineData(1.0, 0.15, 20, 200, 200)]     // full load: the hard rate
    [InlineData(0.15, 0.15, 20, 200, 0)]
    public void ExhaustFollowsTheLoad(double load, double idle, double cruise, double hard, double expected) =>
        Assert.Equal(expected, FxRule.ExhaustRate(load, idle, cruise, hard), 6);

    [Fact]
    public void ExhaustRisesWithLoad() =>
        Assert.True(FxRule.ExhaustRate(0.5, 0.15, 20, 200) < FxRule.ExhaustRate(0.9, 0.15, 20, 200));

    [Theory]
    [InlineData(false, 7, 60, 0, 0.2, true)]    // steady: always
    [InlineData(true, 0, 60, 0, 0.2, true)]     // a flash opens its period
    [InlineData(true, 11, 60, 0, 0.2, true)]
    [InlineData(true, 13, 60, 0, 0.2, false)]
    [InlineData(true, 30, 60, 0.5, 0.2, true)]  // half a period late: its turn comes halfway
    [InlineData(true, 0, 60, 0.5, 0.2, false)]
    [InlineData(true, 5, 0, 0, 0.2, false)]     // no period: never
    public void LampsFlashInTurn(bool flashing, long ticks, double period, double phase, double litShare, bool expected) =>
        Assert.Equal(expected, FxRule.LampLit(flashing, ticks, period, phase, litShare));

    [Theory]
    [InlineData(3, 0, 0.3, 0.25, true)]       // stopping
    [InlineData(3, 2, 0.3, 0.25, true)]       // lost a third
    [InlineData(3, 2.5, 0.3, 0.25, false)]    // a little slower: no
    [InlineData(0.2, 0, 0.3, 0.25, false)]    // was barely moving
    [InlineData(2, 3, 0.3, 0.25, false)]      // speeding up
    public void BrakeLampsLightWhenSlowing(double before, double now, double min, double drop, bool expected) =>
        Assert.Equal(expected, FxRule.Slowing(before, now, min, drop));

    [Fact]
    public void DragSlowsAThrownFlare()
    {
        Assert.Equal(1, FxRule.Drag(0, 1.2), 6);
        Assert.Equal(Math.Exp(-1), FxRule.Drag(1.2, 1.2), 6);
        Assert.Equal(0, FxRule.Drag(1, 0), 6);
    }
}
