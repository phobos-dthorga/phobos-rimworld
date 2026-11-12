using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class WaterDropRuleTests
{
    [Fact]
    public void AStrongHookCarriesTheWholeBucket() => Assert.Equal(3000, WaterDropRule.Litres(3000, 11800), 6);

    [Fact]
    public void AWeakHookCarriesWhatItLifts() => Assert.Equal(2500, WaterDropRule.Litres(3000, 2500), 6);

    [Fact]
    public void NoHookCarriesNothing() => Assert.Equal(0, WaterDropRule.Litres(3000, 0), 6);

    [Fact]
    public void TheRadiusGivesEachCellItsShare()
    {
        double radius = WaterDropRule.Radius(3000, 100);
        Assert.Equal(30, System.Math.PI * radius * radius, 6);
    }

    [Fact]
    public void NoWaterPutsNothingOut() => Assert.Equal(0, WaterDropRule.Radius(0, 100), 6);

    [Fact]
    public void FlightTakesAtLeastOneTick() => Assert.Equal(1, WaterDropRule.FlightTicks(0.01, 0.15));

    [Fact]
    public void FlightTicksFollowTheDistance() => Assert.Equal(200, WaterDropRule.FlightTicks(30, 0.15));

    static WaterRunPlan Circuit() => WaterDropRule.Plan(30, 0, 30, 40, 0.15, 180, 360, 120);

    [Fact]
    public void ThePlanAddsEveryLeg()
    {
        WaterRunPlan plan = Circuit();
        Assert.Equal(200, plan.ToWater);
        Assert.Equal(267, plan.ToFire);
        Assert.Equal(334, plan.Home);
        Assert.Equal(180 + 200 + 360 + 267 + 120 + 334 + 180, plan.Total);
    }

    [Fact]
    public void ItStartsOnTheGroundOverThePad()
    {
        WaterRunPoint start = WaterDropRule.At(Circuit(), 0, 30, 0, 30, 40);
        Assert.Equal(0, start.Height, 6);
        Assert.Equal(WaterRunStage.Climb, start.Stage);
    }

    [Fact]
    public void ItHoldsOverTheWaterForTheDip()
    {
        WaterRunPlan plan = Circuit();
        WaterRunPoint dip = WaterDropRule.At(plan, plan.Climb + plan.ToWater + 10, 30, 0, 30, 40);
        Assert.Equal(WaterRunStage.Dip, dip.Stage);
        Assert.Equal(30, dip.X, 6);
        Assert.Equal(0, dip.Z, 6);
    }

    [Fact]
    public void ThePourEndsOverTheFire()
    {
        WaterRunPlan plan = Circuit();
        WaterRunPoint pour = WaterDropRule.At(plan, plan.PourEnds - 1, 30, 0, 30, 40);
        Assert.Equal(WaterRunStage.Pour, pour.Stage);
        Assert.Equal(30, pour.X, 6);
        Assert.Equal(40, pour.Z, 6);
    }

    [Fact]
    public void ItEndsBackOnThePad()
    {
        WaterRunPlan plan = Circuit();
        WaterRunPoint end = WaterDropRule.At(plan, plan.Total, 30, 0, 30, 40);
        Assert.Equal(WaterRunStage.Done, end.Stage);
        Assert.Equal(0, end.X, 6);
        Assert.Equal(0, end.Height, 6);
    }

    [Fact]
    public void ALegNeverJumps()
    {
        WaterRunPlan plan = Circuit();
        int start = plan.Climb + plan.ToWater + plan.Dip;
        WaterRunPoint before = WaterDropRule.At(plan, start - 1, 30, 0, 30, 40);
        WaterRunPoint after = WaterDropRule.At(plan, start, 30, 0, 30, 40);
        Assert.Equal(before.X, after.X, 6);
        Assert.Equal(before.Z, after.Z, 6);
    }

    [Fact]
    public void TheBucketIsFullOnlyBetweenTheDipAndThePour()
    {
        Assert.False(WaterDropRule.BucketFull(WaterRunStage.ToWater));
        Assert.True(WaterDropRule.BucketFull(WaterRunStage.ToFire));
        Assert.True(WaterDropRule.BucketFull(WaterRunStage.Pour));
        Assert.False(WaterDropRule.BucketFull(WaterRunStage.Home));
    }
}
