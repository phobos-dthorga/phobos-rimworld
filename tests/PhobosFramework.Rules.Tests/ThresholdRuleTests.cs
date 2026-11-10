using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ThresholdRuleTests
{
    [Theory]
    [InlineData(119.9, false)]
    [InlineData(120, true)]
    [InlineData(500, true)]
    public void AnUpperBoundTripsAtOrAboveIt(double hours, bool due) => Assert.Equal(due, ThresholdRule.Passes(hours, 120, double.NaN));

    [Theory]
    [InlineData(0.61, false)]
    [InlineData(0.6, true)]
    [InlineData(0, true)]
    public void ALowerBoundTripsAtOrBelowIt(double level, bool due) => Assert.Equal(due, ThresholdRule.Passes(level, double.NaN, 0.6));

    [Fact]
    public void UnsetBoundsNeverTrip()
    {
        Assert.False(ThresholdRule.Passes(0, double.NaN, double.NaN));
        Assert.False(ThresholdRule.Passes(double.MaxValue, double.NaN, double.NaN));
    }

    [Fact]
    public void EitherBoundIsEnough()
    {
        Assert.True(ThresholdRule.Passes(5, 4, 1));
        Assert.True(ThresholdRule.Passes(0.5, 4, 1));
        Assert.False(ThresholdRule.Passes(2, 4, 1));
    }
}
