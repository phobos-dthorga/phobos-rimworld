using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BulkHoldRuleTests
{
    [Fact]
    public void RoomIsByWeight()
    {
        // A nine-tonne tank holding four tonnes, with grain at 30 grams a unit.
        Assert.Equal(166_666, BulkHoldRule.RoomFor(9000f, 4000f, 0.03f));
        Assert.Equal(100, BulkHoldRule.RoomFor(100f, 0f, 1f));
    }

    [Fact]
    public void AHoldFilledExactlyIsNotAUnitShort() => Assert.Equal(1, BulkHoldRule.RoomFor(300f, 299.7f, 0.3f));

    [Fact]
    public void AFullOrOverfullHoldTakesNothing()
    {
        Assert.Equal(0, BulkHoldRule.RoomFor(100f, 100f, 1f));
        Assert.Equal(0, BulkHoldRule.RoomFor(100f, 140f, 1f));
    }

    [Fact]
    public void GoodsWithNoWeightFitWithoutLimit() => Assert.Equal(int.MaxValue, BulkHoldRule.RoomFor(100f, 100f, 0f));

    [Fact]
    public void ATankKeepsToOneProductAndABinMixes()
    {
        Assert.True(BulkHoldRule.Takes(mixesProducts: false, empty: true, holdsSameProduct: false));
        Assert.True(BulkHoldRule.Takes(false, empty: false, holdsSameProduct: true));
        Assert.False(BulkHoldRule.Takes(false, empty: false, holdsSameProduct: false));
        Assert.True(BulkHoldRule.Takes(mixesProducts: true, false, false));
    }

    [Fact]
    public void FreshnessMixesByAmount()
    {
        Assert.Equal(0.1f, BulkHoldRule.Mixed(100, 1f, 900, 0f), 4);
        Assert.Equal(0.5f, BulkHoldRule.Mixed(50, 0.2f, 50, 0.8f), 4);
    }

    [Fact]
    public void AFreshLoadIntoAnEmptyHoldIsFresh()
    {
        Assert.Equal(0f, BulkHoldRule.Mixed(0, 0.9f, 500, 0f));
        Assert.Equal(0f, BulkHoldRule.Mixed(0, 0f, 0, 0f));
    }

    [Fact]
    public void MixingNeverLeavesTheRange()
    {
        Assert.Equal(1f, BulkHoldRule.Mixed(10, 5f, 10, 3f));
        Assert.Equal(0f, BulkHoldRule.Mixed(10, -2f, 10, float.NaN));
    }

    [Fact]
    public void HugeAmountsDoNotOverflow() => Assert.Equal(0.5f, BulkHoldRule.Mixed(2_000_000_000, 0.5f, 2_000_000_000, 0.5f), 4);

    [Fact]
    public void GrainAgesByTimeTemperatureAndTheStore()
    {
        // A tenth of its keeping time, warm, in the open: a tenth used.
        Assert.Equal(0.1f, BulkHoldRule.Aged(0f, 60_000, spoilRate: 1f, keepingTicks: 600_000, storeFactor: 1f), 4);
        // The same in an aired silo at a quarter of the rate.
        Assert.Equal(0.025f, BulkHoldRule.Aged(0f, 60_000, 1f, 600_000, storeFactor: 0.25f), 4);
        // Frozen: nothing.
        Assert.Equal(0.3f, BulkHoldRule.Aged(0.3f, 60_000, spoilRate: 0f, 600_000, 1f), 4);
    }

    [Fact]
    public void AgeingStopsAtSpoiled() => Assert.Equal(1f, BulkHoldRule.Aged(0.9f, 600_000, 1f, 600_000, 1f));

    [Fact]
    public void WhatNeverSpoilsNeverAges() => Assert.Equal(0.2f, BulkHoldRule.Aged(0.2f, 60_000, 1f, keepingTicks: 0, 1f), 4);

    [Fact]
    public void APourIsHeldBackByTheLeastOfThree()
    {
        Assert.Equal(40, BulkHoldRule.Pour(held: 40, roomThere: 500, unitsPerStep: 100));
        Assert.Equal(100, BulkHoldRule.Pour(900, 500, 100));
        Assert.Equal(25, BulkHoldRule.Pour(900, roomThere: 25, 100));
        Assert.Equal(0, BulkHoldRule.Pour(900, roomThere: 0, 100));
        Assert.Equal(0, BulkHoldRule.Pour(-5, 10, 100));
    }

    [Fact]
    public void AnAugerMovesWholeUnitsAndAtLeastOne()
    {
        // Fifty kilograms a second for four seconds, at 30 grams a unit.
        Assert.Equal(6666, BulkHoldRule.UnitsPerStep(50f, 4f, 0.03f));
        Assert.Equal(1, BulkHoldRule.UnitsPerStep(1f, 1f, 25f));
        Assert.Equal(0, BulkHoldRule.UnitsPerStep(0f, 4f, 0.03f));
        Assert.Equal(int.MaxValue, BulkHoldRule.UnitsPerStep(50f, 4f, 0f));
    }

    [Fact]
    public void SideBySideFootprintsAreInReach()
    {
        // A combine at X 0 to 3 and a chaser bin at X 4 to 6: touching, no gap.
        Assert.True(BulkHoldRule.WithinReach(0, 0, 3, 8, 4, 0, 6, 6, 0));
        // One empty column between them: a reach of one is enough, nought is not.
        Assert.True(BulkHoldRule.WithinReach(0, 0, 3, 8, 5, 0, 7, 6, 1));
        Assert.False(BulkHoldRule.WithinReach(0, 0, 3, 8, 5, 0, 7, 6, 0));
    }

    [Fact]
    public void AGapAcrossACornerCountsItsLongerSide()
    {
        // Two cells apart in X and three in Z: the reach must cover three.
        Assert.False(BulkHoldRule.WithinReach(0, 0, 1, 1, 4, 5, 5, 6, 2));
        Assert.True(BulkHoldRule.WithinReach(0, 0, 1, 1, 4, 5, 5, 6, 3));
    }

    [Fact]
    public void ReachIsTheSameWhicheverWayRound() =>
        Assert.Equal(BulkHoldRule.WithinReach(0, 0, 3, 8, 6, 2, 8, 9, 2), BulkHoldRule.WithinReach(6, 2, 8, 9, 0, 0, 3, 8, 2));

    [Fact]
    public void TheGaugeRunsFromNoughtToOne()
    {
        Assert.Equal(0.5f, BulkHoldRule.Fill(9000f, 4500f), 4);
        Assert.Equal(1f, BulkHoldRule.Fill(9000f, 12000f));
        Assert.Equal(0f, BulkHoldRule.Fill(0f, 100f));
    }
}
