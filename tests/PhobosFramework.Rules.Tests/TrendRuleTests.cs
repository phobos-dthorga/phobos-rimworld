using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TrendRuleTests
{
    [Fact]
    public void AStraightRiseIsProjected()
    {
        double[] wear = { 0.1, 0.2, 0.3, 0.4 };
        Assert.Equal(2, TrendRule.SamplesUntil(wear.Length, i => wear[i], 0.6, 3, 1e-9, 1e-9)!.Value, 6);
    }

    [Fact]
    public void OnlyTheRiseSinceTheLastFallCounts()
    {
        // Worn to 0.8, serviced back to 0, then rising 0.05 a sample: the old rise must not steepen the line.
        double[] wear = { 0.6, 0.7, 0.8, 0.0, 0.05, 0.1, 0.15 };
        Assert.Equal(3, TrendRule.RiseStart(wear.Length, i => wear[i], 1e-9));
        Assert.Equal(9, TrendRule.SamplesUntil(wear.Length, i => wear[i], 0.6, 3, 1e-9, 1e-9)!.Value, 6);
    }

    [Fact]
    public void AReadingAlreadyThereIsDueNow() => Assert.Equal(0, TrendRule.SamplesUntil(2, i => 0.9, 0.6, 3, 1e-9, 1e-9));

    [Fact]
    public void AFlatReadingHasNoDate()
    {
        double[] wear = { 0.3, 0.3, 0.3, 0.3 };
        Assert.Null(TrendRule.SamplesUntil(wear.Length, i => wear[i], 0.6, 3, 1e-9, 1e-9));
    }

    [Fact]
    public void ATooShortRiseHasNoDate()
    {
        double[] wear = { 0.5, 0.0, 0.1 };
        Assert.Null(TrendRule.SamplesUntil(wear.Length, i => wear[i], 0.6, 3, 1e-9, 1e-9));
    }

    [Fact]
    public void IdleStretchesSlowTheEstimate()
    {
        // Driven every other sample: the line is the rate of use over the calendar, half the driving rate.
        double[] wear = { 0.0, 0.1, 0.1, 0.2, 0.2, 0.3 };
        double? until = TrendRule.SamplesUntil(wear.Length, i => wear[i], 0.5, 3, 1e-9, 1e-9);
        Assert.NotNull(until);
        Assert.True(until > 3.5 && until < 4.5);
    }

    [Fact]
    public void ASlopeNeedsTwoSamples() => Assert.Null(TrendRule.Slope(1, i => 1, 0));
}
