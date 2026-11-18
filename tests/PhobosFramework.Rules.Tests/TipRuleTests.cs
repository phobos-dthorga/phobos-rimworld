using System.Linq;
using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TipRuleTests
{
    [Fact]
    public void TheCellBehindTheTailIsInTheFan() => Assert.True(TipRule.InFan(0, 0, 0, -1, 8));

    [Fact]
    public void CellsBehindAreIn() => Assert.True(TipRule.InFan(3, -5, 0, -1, 8));

    [Fact]
    public void CellsForwardOfTheTailAreOut() => Assert.False(TipRule.InFan(0, 1, 0, -1, 8));

    [Fact]
    public void CellsPastTheRadiusAreOut() => Assert.False(TipRule.InFan(0, -9, 0, -1, 8));

    [Fact]
    public void ItFacesTheWayTheTruckDoes() => Assert.True(TipRule.InFan(4, 0, 1, 0, 8));

    [Fact]
    public void AStepComesToWholeThings()
    {
        float allowance = 0f;
        Assert.Equal(8, TipRule.WholeThings(ref allowance, 2f, 4.17f));
        Assert.Equal(0.34, allowance, 2);
    }

    [Fact]
    public void WhatIsLeftOverCarries()
    {
        float allowance = 0.9f;
        Assert.Equal(1, TipRule.WholeThings(ref allowance, 0.5f, 0.4f));
    }
}

public class GrabRuleTests
{
    [Fact]
    public void ALightStackComesWhole() => Assert.Equal(20, GrabRule.CountForLift(20, 1, 500, 2000));

    [Fact]
    public void TheLiftLimitsAHeavyStack() => Assert.Equal(6, GrabRule.CountForLift(10, 75, 500, 2000));

    [Fact]
    public void TheRoomAboardLimitsIt() => Assert.Equal(2, GrabRule.CountForLift(10, 75, 500, 160));

    [Fact]
    public void OneTooHeavyIsLeft() => Assert.Equal(0, GrabRule.CountForLift(1, 600, 500, 2000));

    [Fact]
    public void AFullTruckTakesNothing() => Assert.Equal(0, GrabRule.CountForLift(5, 1, 500, 0));

    [Fact]
    public void AnExactFitIsTaken() => Assert.Equal(5, GrabRule.CountForLift(5, 100, 500, 2000));

    [Fact]
    public void LooseThingsAreAlwaysLiftable() => Assert.True(GrabRule.MayLift(false, false, false, true));

    [Fact]
    public void StorageSwitchedOnLetsItLift() => Assert.True(GrabRule.MayLift(true, true, true, false));

    [Fact]
    public void StorageSwitchedOffKeepsItsGoods() => Assert.False(GrabRule.MayLift(true, true, false, false));

    [Fact]
    public void AnotherSidesStorageIsNeverEmptied() => Assert.False(GrabRule.MayLift(true, false, true, false));

    [Fact]
    public void AClaimedStoredThingIsLeft() => Assert.False(GrabRule.MayLift(true, true, true, true));

    static (int, double)[] Chunks(int count, double kilograms)
    {
        var lots = new (int, double)[count];
        for (int i = 0; i < count; i++) lots[i] = (1, kilograms);
        return lots;
    }

    [Fact]
    public void ALiftOfGraniteChunksTakesTwenty()
    {
        int[] counts = GrabRule.FillLift(Chunks(25, 25), 500, 2400);
        Assert.Equal(20, counts.Sum());
        Assert.Equal(0, counts[20]);
    }

    [Fact]
    public void TheRoomAboardEndsALiftEarly() => Assert.Equal(4, GrabRule.FillLift(Chunks(25, 25), 500, 110).Sum());

    [Fact]
    public void TheFirstLotStillTakesWhatOneLiftAlwaysDid() => Assert.Equal(6, GrabRule.FillLift(new[] { (10, 75.0), (10, 75.0) }, 500, 2000)[0]);

    [Fact]
    public void WhatIsLeftOfALiftGoesToTheNext()
    {
        int[] counts = GrabRule.FillLift(new[] { (10, 75.0), (10, 75.0) }, 500, 2000);
        Assert.Equal(new[] { 6, 0 }, counts);
    }

    [Fact]
    public void ALighterLotFillsWhatAHeavyOneCannot()
    {
        int[] counts = GrabRule.FillLift(new[] { (1, 450.0), (1, 100.0), (1, 25.0) }, 500, 2000);
        Assert.Equal(new[] { 1, 0, 1 }, counts);
    }

    [Fact]
    public void AFullTruckLiftsNothing() => Assert.Equal(0, GrabRule.FillLift(Chunks(5, 25), 500, 0).Sum());

    [Fact]
    public void NoLotsNoLift() => Assert.Empty(GrabRule.FillLift(System.Array.Empty<(int, double)>(), 500, 2000));
}

public class TipDestinationTests
{
    [Fact]
    public void StorageWithRoomTakesIt() => Assert.Equal(TipDestination.Storage, TipRule.Destination(true, true, true));

    [Fact]
    public void FullStorageKeepsItAboardEvenWithGroundFree() => Assert.Equal(TipDestination.StaysAboard, TipRule.Destination(true, false, true));

    [Fact]
    public void WhatNoStorageTakesGoesOnTheGround() => Assert.Equal(TipDestination.Ground, TipRule.Destination(false, false, true));

    [Fact]
    public void NoRoomAnywhereKeepsItAboard() => Assert.Equal(TipDestination.StaysAboard, TipRule.Destination(false, true, false));
}
