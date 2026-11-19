using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class WinterForecastRuleTests
{
    [Fact]
    public void SnowComingIsCountedInDays()
    {
        int? days = WinterForecastRule.DaysUntilChange(day => day >= 12 ? 1.4 : 0.2, out bool snowNow);
        Assert.False(snowNow);
        Assert.Equal(12, days);
    }

    [Fact]
    public void SnowLiftingIsCountedInDays()
    {
        int? days = WinterForecastRule.DaysUntilChange(day => day >= 5 ? 0.3 : 1.8, out bool snowNow);
        Assert.True(snowNow);
        Assert.Equal(5, days);
    }

    [Fact]
    public void NoChangeWithinTheHorizonIsNoForecast()
    {
        Assert.Null(WinterForecastRule.DaysUntilChange(_ => 0.0, out bool snowNow));
        Assert.False(snowNow);
    }

    [Fact]
    public void AChangeBeyondTheHorizonIsNotForecast() =>
        Assert.Null(WinterForecastRule.DaysUntilChange(day => day >= 40 ? 2.0 : 0.0, out _));

    [Fact]
    public void TheLineItselfCountsAsSnow()
    {
        WinterForecastRule.DaysUntilChange(_ => 1.0, out bool snowNow);
        Assert.True(snowNow);
    }
}
