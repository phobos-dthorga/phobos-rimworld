using System;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BatteryChargeRuleTests
{
    // The figures D'thorga Patches' mech recharging uses (docs/development/mech-recharging-real-power.md): 480 watt-days for each
    // point of body size, half a battery a day, 84% of the wall's power reaching the battery, easing from 80% to a quarter rate at full.
    const double WattDaysPerSize = 480, HalfADay = 0.5, Efficiency = 0.84, EasesFrom = 0.8, RateWhenFull = 0.25;

    static double Wall(double bodySize, double charge = 0) => BatteryChargeRule.WallWatts(
        BatteryChargeRule.StoredWatts(BatteryChargeRule.CapacityWattDays(bodySize, WattDaysPerSize), HalfADay,
            BatteryChargeRule.Taper(charge, EasesFrom, RateWhenFull)), Efficiency);

    [Fact]
    public void ALightMechDrawsWhatItsRechargerListsToday() => Assert.Equal(200, Wall(0.7), 6);

    [Theory]
    [InlineData(0.3, 85.714)]
    [InlineData(3.5, 1000)]
    [InlineData(4, 1142.857)]
    public void ABiggerBodyDrawsInProportion(double bodySize, double watts) => Assert.Equal(watts, Wall(bodySize), 3);

    [Fact]
    public void ABatteryBuiltLargerHoldsMore()
    {
        Assert.Equal(336, BatteryChargeRule.CapacityWattDays(0.7, WattDaysPerSize), 9);
        Assert.Equal(672, BatteryChargeRule.CapacityWattDays(0.7, WattDaysPerSize, 2), 9);
        Assert.Equal(0, BatteryChargeRule.CapacityWattDays(-1, WattDaysPerSize), 9);
    }

    [Fact]
    public void TheChargeIsFullRateUntilItEases()
    {
        Assert.Equal(1, BatteryChargeRule.Taper(0, EasesFrom, RateWhenFull), 9);
        Assert.Equal(1, BatteryChargeRule.Taper(0.8, EasesFrom, RateWhenFull), 9);
        Assert.Equal(0.625, BatteryChargeRule.Taper(0.9, EasesFrom, RateWhenFull), 9);
        Assert.Equal(0.25, BatteryChargeRule.Taper(1, EasesFrom, RateWhenFull), 9);
        Assert.Equal(0.25, BatteryChargeRule.Taper(1.5, EasesFrom, RateWhenFull), 9);
    }

    [Fact]
    public void ATaperThatNeverStartsLeavesTheRateAlone() => Assert.Equal(1, BatteryChargeRule.Taper(0.99, 1, 0.25), 9);

    [Fact]
    public void TheDrawEasesWithTheCharge()
    {
        Assert.Equal(200, Wall(0.7, 0.5), 6);
        Assert.Equal(125, Wall(0.7, 0.9), 6);
        Assert.Equal(50, Wall(0.7, 1), 6);
    }

    [Fact]
    public void WhatTheWallGivesIsWhatIsStoredPlusWhatIsLost()
    {
        double stored = BatteryChargeRule.StoredWatts(1680, HalfADay);
        Assert.Equal(840, stored, 9);
        Assert.Equal(1000, BatteryChargeRule.WallWatts(stored, Efficiency), 9);
        Assert.Equal(160, BatteryChargeRule.LostWatts(stored, Efficiency), 9);
        Assert.Equal(0, BatteryChargeRule.LostWatts(stored, 1), 9);
    }

    [Fact]
    public void BelowTheKneeTimeIsStraightLine()
    {
        Assert.Equal(1.6, BatteryChargeRule.DaysToReach(0, 0.8, HalfADay, EasesFrom, RateWhenFull), 9);
        Assert.Equal(0.6, BatteryChargeRule.DaysToReach(0.5, 0.8, HalfADay, EasesFrom, RateWhenFull), 9);
        Assert.Equal(0, BatteryChargeRule.DaysToReach(0.8, 0.5, HalfADay, EasesFrom, RateWhenFull), 9);
    }

    [Fact]
    public void TheLastFifthTakesLonger()
    {
        // The rate falls from full to a quarter over the last fifth: 0.4 days at full rate becomes 0.4 * ln(4) / 0.75.
        double lastFifth = 0.4 * Math.Log(4) / 0.75;
        Assert.Equal(lastFifth, BatteryChargeRule.DaysToReach(0.8, 1, HalfADay, EasesFrom, RateWhenFull), 9);
        Assert.Equal(1.6 + lastFifth, BatteryChargeRule.DaysToReach(0, 1, HalfADay, EasesFrom, RateWhenFull), 9);
        Assert.True(lastFifth > 0.4);
    }

    [Fact]
    public void TheTimeMatchesSmallStepsOfTheTaper()
    {
        // Walk the charge up in small steps at the tapered rate; the closed form must agree.
        double level = 0.3, days = 0, step = 1e-5;
        while (level < 0.95)
        {
            level += HalfADay * BatteryChargeRule.Taper(level, EasesFrom, RateWhenFull) * step;
            days += step;
        }
        Assert.Equal(days, BatteryChargeRule.DaysToReach(0.3, 0.95, HalfADay, EasesFrom, RateWhenFull), 3);
    }

    [Fact]
    public void WithNoTaperTimeIsStraightLineToFull() =>
        Assert.Equal(2, BatteryChargeRule.DaysToReach(0, 1, HalfADay, EasesFrom, 1), 9);

    [Fact]
    public void AnEasedChargeReachesFullWhenTheChargerStopsAtTheTop()
    {
        // The game's recharger adds 50/60000 a tick and stops at the top; easing only the gap it reports never arrived (0.286.1).
        const float Max = 100f, FullStep = 50f / 60000f;
        float level = 99.99f;
        int steps = 0;
        while (level < Max && steps < 1000)
        {
            float after = Math.Min(level + FullStep, Max);
            level = (float)BatteryChargeRule.TaperedLevel(level, after, FullStep, RateWhenFull, Max);
            steps++;
        }
        Assert.Equal(Max, level);
        Assert.True(steps < 100);
        // A mech already stuck just short of full finishes in one step.
        Assert.Equal(Max, (float)BatteryChargeRule.TaperedLevel(99.9999924f, Max, FullStep, RateWhenFull, Max));
    }

    [Fact]
    public void BelowTheTopOnlyWhatWasAddedIsEased()
    {
        Assert.Equal(50.25, BatteryChargeRule.TaperedLevel(50, 51, 1, 0.25, 100), 9);
        Assert.Equal(50.5, BatteryChargeRule.TaperedLevel(50, 52, 1, 0.25, 100), 9);
        Assert.Equal(40, BatteryChargeRule.TaperedLevel(50, 40, 1, 0.25, 100), 9);
    }

    [Fact]
    public void AChargeThatStopsBeforeFullNeverArrives()
    {
        Assert.True(double.IsPositiveInfinity(BatteryChargeRule.DaysToReach(0, 1, HalfADay, EasesFrom, 0)));
        Assert.True(double.IsPositiveInfinity(BatteryChargeRule.DaysToReach(0, 1, 0, EasesFrom, RateWhenFull)));
        Assert.False(double.IsInfinity(BatteryChargeRule.DaysToReach(0, 0.9, HalfADay, EasesFrom, 0)));
    }
}
