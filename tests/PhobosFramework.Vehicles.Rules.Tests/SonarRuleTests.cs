using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class SonarRuleTests
{
    const float SettledShare = 0.5f;

    [Fact]
    public void WithSomeoneAwakeTheWholeFittedRange() => Assert.Equal(60f, SonarRule.Range(60f, true), 3);

    [Theory]
    [InlineData(60f, false)]
    [InlineData(0f, true)]
    [InlineData(-5f, true)]
    public void NoSetOrNobodyToWorkItFindsNothing(float fitted, bool someone) => Assert.Equal(0f, SonarRule.Range(fitted, someone));

    [Theory]
    [InlineData(ContactKind.InWater, 59f, true)]
    [InlineData(ContactKind.InWater, 60f, false)]
    [InlineData(ContactKind.HiddenBoat, 59f, true)]
    [InlineData(ContactKind.OnLand, 1f, false)]
    public void APingFindsWhatIsInTheWaterAndNothingOnLand(ContactKind kind, float distance, bool found) =>
        Assert.Equal(found, SonarRule.Finds(kind, distance, 60f, false, SettledShare));

    [Theory]
    [InlineData(29f, true)]
    [InlineData(30f, false)]
    public void ABoatOnTheBottomIsLostInTheSeabedsEchoesBeyondItsShare(float distance, bool found) =>
        Assert.Equal(found, SonarRule.Finds(ContactKind.HiddenBoat, distance, 60f, true, SettledShare));

    [Fact]
    public void SettlingHidesOnlyABoat() => Assert.True(SonarRule.Finds(ContactKind.InWater, 50f, 60f, true, SettledShare));

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    public void NoRangeFindsNothing(float range) => Assert.False(SonarRule.Finds(ContactKind.InWater, 1f, range, false, SettledShare));

    [Fact]
    public void ANegativeDistanceFindsNothing() => Assert.False(SonarRule.Finds(ContactKind.InWater, -1f, 60f, false, SettledShare));

    [Fact]
    public void ThePingIsHeardFurtherThanItReaches() => Assert.Equal(120f, SonarRule.HeardWithin(60f, 2f), 3);

    [Fact]
    public void ThePingIsNeverHeardShorterThanItReaches() => Assert.Equal(60f, SonarRule.HeardWithin(60f, 0.5f), 3);

    [Fact]
    public void NoSetMeansNoPingToHear() => Assert.Equal(0f, SonarRule.HeardWithin(0f, 2f));

    [Theory]
    [InlineData(-1, 100, true)]
    [InlineData(1000, 2200, true)]
    [InlineData(1000, 2199, false)]
    public void ReadyOnceTheCooldownHasPassed(int last, int now, bool ready) => Assert.Equal(ready, SonarRule.Ready(now, last, 1200));

    [Theory]
    [InlineData(1000, 1000, 20)]
    [InlineData(1000, 1001, 20)]
    [InlineData(1000, 2141, 1)]
    [InlineData(1000, 2200, 0)]
    public void SecondsUntilReadyRoundUp(int last, int now, int seconds) => Assert.Equal(seconds, SonarRule.SecondsUntilReady(now, last, 1200, 60f));

    [Theory]
    [InlineData(true, true, true, 0.05f, true)]
    [InlineData(true, true, true, 0.5f, false)]
    [InlineData(false, true, true, 0f, false)]
    [InlineData(true, false, true, 0f, false)]
    [InlineData(true, true, false, 0f, false)]
    public void AnotherCrewPingsOnlyUnderReadyWithSomethingInReachAndByChance(bool under, bool ready, bool hostile, float roll, bool pings) =>
        Assert.Equal(pings, SonarRule.CrewPingsNow(under, ready, hostile, roll, 0.15f));
}
