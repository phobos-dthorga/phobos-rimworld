using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class RepairRulesTests
{
    [Fact]
    public void AShareRoundsUpAndIsNeverFreeAboveZero()
    {
        Assert.Equal(10, CostShareRule.Count(20, 0.5));
        Assert.Equal(1, CostShareRule.Count(1, 0.1));
        Assert.Equal(11, CostShareRule.Count(21, 0.5));
    }

    [Fact]
    public void AShareOfZeroCostsNothing() => Assert.Equal(0, CostShareRule.Count(20, 0));

    [Fact]
    public void TheWholePartIsAskedForOnlyAtTheTop()
    {
        Assert.True(CostShareRule.WantsWhole(1));
        Assert.False(CostShareRule.WantsWhole(0.9));
    }

    [Fact]
    public void ACostByHealthFollowsThePart()
    {
        Assert.Equal(75, CostShareRule.PerHealth(0.5, 300, 0.5));
        Assert.Equal(0, CostShareRule.PerHealth(0.5, 300, 0));
        Assert.Equal(1, CostShareRule.PerHealth(0.02, 10, 0.5));
    }

    [Fact]
    public void AClockCountsDownToTheLevel() => Assert.Equal(5, DangerClockRule.Hours(0.8, 0.4, -0.08), 6);

    [Fact]
    public void AClockNeverRunsWhenTheQuantityIsNotHeadingThere()
    {
        Assert.Equal(DangerClockRule.Never, DangerClockRule.Hours(0.8, 0.4, 0));
        Assert.Equal(DangerClockRule.Never, DangerClockRule.Hours(0.8, 0.4, 0.1));
        Assert.Equal(DangerClockRule.Never, DangerClockRule.Hours(0.4, 0.4, -0.1));
    }

    [Fact]
    public void ARateIsMeasuredBetweenSamples()
    {
        Assert.Equal(-0.1, DangerClockRule.Rate(0.9, 0.8, 1), 6);
        Assert.Equal(0, DangerClockRule.Rate(0.9, 0.8, 0));
    }

    [Fact]
    public void WorkThatGetsNowhereStalls()
    {
        var watch = new ProgressWatch();
        Assert.False(watch.Note(100, 500, 2500, 600));
        Assert.False(watch.Note(600, 500, 2500, 600));
        Assert.False(watch.Note(1200, 500, 2500, 600));
        Assert.False(watch.Note(1800, 500, 2500, 600));
        Assert.True(watch.Note(2600, 500, 2500, 800));
    }

    [Fact]
    public void ProgressRestartsTheClock()
    {
        var watch = new ProgressWatch();
        watch.Note(100, 500, 2500, 600);
        watch.Note(600, 500, 2500, 600);
        Assert.False(watch.Note(1100, 480, 2500, 600));
        Assert.False(watch.Note(1700, 480, 2500, 600));
        Assert.False(watch.Note(2300, 480, 2500, 600));
    }

    [Fact]
    public void WorkResumedAfterALongGapIsNotStalled()
    {
        var watch = new ProgressWatch();
        watch.Note(100, 500, 2500, 600);
        Assert.False(watch.Note(90000, 500, 2500, 600));
    }

    [Fact]
    public void AnEarlierGameLoadedSinceIsNotStalled()
    {
        var watch = new ProgressWatch();
        watch.Note(90000, 500, 2500, 600);
        Assert.False(watch.Note(1000, 500, 2500, 600));
    }

    [Fact]
    public void AFieldFixHalvesTheSeverity() => Assert.Equal(0.3, SeverityRule.FieldFixed(0.6), 6);
}
