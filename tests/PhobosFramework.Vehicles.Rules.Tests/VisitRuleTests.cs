using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class VisitRuleTests
{
    const int Hour = 2500;

    [Theory]
    [InlineData(0, 4999, 2, false)]
    [InlineData(0, 5000, 2, true)]
    [InlineData(0, 999999, 0, false)]
    public void AVisitEndsAtItsLongest(int since, int now, double maxHours, bool over) =>
        Assert.Equal(over, VisitRule.OverTime(since, now, maxHours, Hour));

    [Theory]
    [InlineData(true, 0.36)]
    [InlineData(false, 0.08)]
    public void SomeoneWhoCameToListenGetsTheGamesOwnRate(bool cameToListen, double rate) =>
        Assert.Equal(rate, VisitRule.StereoJoyPerHour(cameToListen, 0.36, 0.08));

    [Theory]
    [InlineData(0.25, 0.2, false, false)]
    [InlineData(0.2, 0.2, false, true)]
    [InlineData(0.1, 0.2, true, false)]
    [InlineData(0.1, double.NaN, false, false)]
    public void TheRadioGoesOffAtTheStartLineUnlessThePlayerLetsItRunFlat(double reading, double line, bool mayFlatten, bool off) =>
        Assert.Equal(off, VisitRule.RadioOffForTheStart(reading, line, mayFlatten));

    [Theory]
    [InlineData(false, 0.8)]
    [InlineData(true, 2.4)]
    public void AFriendInTheCabMakesASessionLikelier(bool friendAboard, double chance) =>
        Assert.Equal(chance, VisitRule.HotboxChance(0.8, friendAboard, 3), 6);

    [Fact]
    public void AFriendsFactorBelowOneNeverMakesItLessLikely() => Assert.Equal(0.8, VisitRule.HotboxChance(0.8, true, 0.5), 6);

    [Theory]
    [InlineData(-1, 9999, false)]
    [InlineData(100, 2599, false)]
    [InlineData(100, 2600, true)]
    public void AHotboxVisitorStaysAnHourAfterTheirSmoke(int smokedAt, int now, bool done) =>
        Assert.Equal(done, VisitRule.SmokedAndShared(smokedAt, now, 1, Hour));

    [Theory]
    [InlineData(2499, false)]
    [InlineData(2500, true)]
    public void AnHoursNapIsRemembered(int now, bool remembered) => Assert.Equal(remembered, VisitRule.NapRemembered(0, now, 1, Hour));

    [Theory]
    [InlineData(16, true)]
    [InlineData(26, true)]
    [InlineData(27, false)]
    [InlineData(15.9, false)]
    public void ComfortIncludesItsEnds(double temperature, bool comfortable) =>
        Assert.Equal(comfortable, VisitRule.Comfortable(temperature, 16, 26));
}
