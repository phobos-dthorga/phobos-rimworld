using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TankRuleTests
{
    [Fact]
    public void ContentsAreAlmostThereAfterTheStatedTime() =>
        Assert.InRange(TankRule.NextTemperature(20f, 0f, 6f, 6f), 0f, 1.1f);

    [Fact]
    public void AnInsulatedTankChangesMoreSlowly() =>
        Assert.True(TankRule.NextTemperature(20f, -10f, 1f, 24f) > TankRule.NextTemperature(20f, -10f, 1f, 6f));

    [Fact]
    public void TemperatureNeverOvershootsTheOutsideAir() =>
        Assert.InRange(TankRule.NextTemperature(20f, 0f, 500f, 6f), 0f, 0.001f);

    [Theory]
    [InlineData(10f, 1f)]
    [InlineData(-8f, 1f)]
    [InlineData(-20f, 0.15f)]
    [InlineData(-40f, 0.15f)]
    public void FuelThickensDownToItsFloorAndNeverStops(float temperature, float expected) =>
        Assert.Equal(expected, TankRule.PumpFactor(temperature, -8f, -20f, 0.15f), 3);

    [Fact]
    public void HalfwayDownTheCurveIsHalfwayToTheFloor() =>
        Assert.Equal(0.575f, TankRule.PumpFactor(-14f, -8f, -20f, 0.15f), 3);

    [Fact]
    public void ContentsWithNoCurvePumpFreely() => Assert.Equal(1f, TankRule.PumpFactor(-50f, float.NaN, float.NaN, 0.15f));

    [Theory]
    [InlineData(-0.1f, true)]
    [InlineData(0f, false)]
    [InlineData(5f, false)]
    public void WaterFreezesBelowZero(float temperature, bool frozen) => Assert.Equal(frozen, TankRule.Frozen(temperature, 0f));

    [Fact]
    public void WhatHasNoFreezingPointNeverFreezes() => Assert.False(TankRule.Frozen(-100f, float.NaN));

    [Fact]
    public void TheSpoilClockPausesAtFreezing() => Assert.Equal(5f, TankRule.NextSpoilHours(5f, 0f, 1f, 1f));

    [Fact]
    public void TheSpoilClockRunsAtItsRate() => Assert.Equal(5.5f, TankRule.NextSpoilHours(5f, 10f, 1f, 0.5f));

    [Fact]
    public void ThreeQuartersOfADayIsEighteenHours()
    {
        Assert.False(TankRule.Spoiled(17.9f, 0.75f));
        Assert.True(TankRule.Spoiled(18f, 0.75f));
    }

    [Fact]
    public void WhatDoesNotSpoilNeverDoes() => Assert.False(TankRule.Spoiled(100000f, 0f));

    [Theory]
    [InlineData(600f, 0.1f, 1f, 1000f, 1000f, 60f)]
    [InlineData(600f, 0.1f, 0.15f, 1000f, 1000f, 9f)]
    [InlineData(600f, 0.1f, 1f, 20f, 1000f, 20f)]
    [InlineData(600f, 0.1f, 1f, 1000f, 5f, 5f)]
    [InlineData(600f, 0.1f, 1f, 0f, 1000f, 0f)]
    public void APumpMovesNoMoreThanThereIsOrThereIsRoomFor(float rate, float hours, float factor, float available, float space, float expected) =>
        Assert.Equal(expected, TankRule.Moved(rate, hours, factor, available, space), 3);

    [Fact]
    public void ASoundShellDoesNotLeak() => Assert.Equal(0f, TankRule.Leaked(3000f, 1f, 0.1f, 1f, 3000f));

    [Fact]
    public void AHoledShellLeaksInProportion() => Assert.Equal(150f, TankRule.Leaked(3000f, 0.5f, 0.1f, 1f, 3000f), 3);

    [Fact]
    public void ALeakCannotTakeMoreThanIsThere() => Assert.Equal(10f, TankRule.Leaked(3000f, 0f, 0.1f, 1f, 10f), 3);

    [Fact]
    public void AFullFixedTanksWorthOfFuelGivesItsOwnRadius() => Assert.Equal(12f, TankRule.ExplosionRadius(4500f, 4500f, 12f), 3);

    [Fact]
    public void AnEighthOfTheLoadGivesHalfTheRadius() => Assert.Equal(6f, TankRule.ExplosionRadius(562.5f, 4500f, 12f), 3);

    [Fact]
    public void AnEmptyTankDoesNotExplode() => Assert.Equal(0f, TankRule.ExplosionRadius(0f, 4500f, 12f));

    [Theory]
    [InlineData(3000f, 1, 3000f)]
    [InlineData(3000f, 2, 1500f)]
    [InlineData(3000f, 3, 1000f)]
    [InlineData(3000f, 0, 3000f)]
    public void CompartmentsDivideTheTankEqually(float capacity, int compartments, float expected) =>
        Assert.Equal(expected, TankRule.CompartmentCapacity(capacity, compartments), 3);

    [Fact]
    public void AStrongerLoadHasALargerBlast() => Assert.Equal(16.8f, TankRule.ExplosionRadius(4500f, 4500f, 12f, 1.4f), 3);

    [Fact]
    public void TheBlastNeverPassesItsCap() => Assert.Equal(15f, TankRule.ExplosionRadius(4500f, 4500f, 12f, 1.4f, 15f), 3);
}
