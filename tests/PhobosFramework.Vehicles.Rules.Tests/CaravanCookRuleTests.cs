using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class CaravanCookRuleTests
{
    [Theory]
    [InlineData(true, false, false, CaravanCookRule.Way.Camp)]
    [InlineData(true, false, true, CaravanCookRule.Way.Camp)]
    [InlineData(false, true, false, CaravanCookRule.Way.Simmer)]
    [InlineData(false, true, true, CaravanCookRule.Way.Halt)]
    [InlineData(false, false, false, CaravanCookRule.Way.None)]
    [InlineData(false, false, true, CaravanCookRule.Way.None)]
    public void TheWayFollowsTheNight(bool resting, bool drivingOn, bool halts, CaravanCookRule.Way way) =>
        Assert.Equal(way, CaravanCookRule.WayFor(resting, drivingOn, halts));

    [Fact]
    public void ARestingCaravanCooksAsAtCampEvenIfItSomehowAlsoDrivesOn() =>
        Assert.Equal(CaravanCookRule.Way.Camp, CaravanCookRule.WayFor(true, true, false));

    [Theory]
    [InlineData(20, 1.0, 20)]
    [InlineData(20, 0.5, 10)]
    [InlineData(15, 0.5, 7)]
    [InlineData(20, 0.0, 0)]
    [InlineData(20, 2.0, 20)]
    public void ACooksShareIsRoundedDown(int mealsPerCook, double share, int expected) =>
        Assert.Equal(expected, CaravanCookRule.PerCook(mealsPerCook, share));

    [Fact]
    public void TheCooksSetTheMostMeals() => Assert.Equal(20, CaravanCookRule.Meals(2, 10, 100, 0.5, 100, 0.5));

    [Fact]
    public void TheFoodSetsTheMostMeals() => Assert.Equal(7, CaravanCookRule.Meals(2, 10, 3.5, 0.5, 100, 0.5));

    [Fact]
    public void TheFuelSetsTheMostMeals() => Assert.Equal(6, CaravanCookRule.Meals(2, 10, 100, 0.5, 3, 0.5));

    [Fact]
    public void TheWaterSetsTheMostMeals() => Assert.Equal(4, CaravanCookRule.Meals(2, 10, 100, 0.5, 100, 0.5, 4.5, 1));

    [Fact]
    public void AKitchenThatAsksForNoWaterNeedsNone() => Assert.Equal(20, CaravanCookRule.Meals(2, 10, 100, 0.5, 100, 0.5, 0, 0));

    [Fact]
    public void AMealThatBurnsNothingNeedsNoFuel() => Assert.Equal(20, CaravanCookRule.Meals(2, 10, 100, 0.5, 0, 0));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(2, 0)]
    public void NoCooksOrNoShareMeansNoMeals(int cooks, int perCook) => Assert.Equal(0, CaravanCookRule.Meals(cooks, perCook, 100, 0.5, 100, 0.5));

    [Theory]
    [InlineData(CaravanCookRule.Way.Camp, true, 0)]
    [InlineData(CaravanCookRule.Way.Camp, false, 0.02)]
    [InlineData(CaravanCookRule.Way.Simmer, true, 0.02)]
    [InlineData(CaravanCookRule.Way.Halt, true, 0.02)]
    public void AClosedTentAtCampKeepsTheOutdoorChanceOff(CaravanCookRule.Way way, bool closedTent, double chance) =>
        Assert.Equal(chance, CaravanCookRule.PlaceChance(way, closedTent, 0.02));
}
