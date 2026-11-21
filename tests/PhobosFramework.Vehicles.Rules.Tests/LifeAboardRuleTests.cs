using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class LifeAboardRuleTests
{
    [Theory]
    [InlineData(true, false, 0.3)]
    [InlineData(true, true, 0.05)]
    [InlineData(false, false, 0.05)]
    public void AClosedCabWithItsWindowsUpKeepsTheSmokeIn(bool closed, bool windowsDown, double share) =>
        Assert.Equal(share, LifeAboardRule.BystanderShare(closed, windowsDown, 0.3, 0.05));

    [Fact]
    public void AJointGivesEachBystanderAShare() => Assert.Equal(0.15, LifeAboardRule.BystanderSeverity(0.5, 0.3, 0.05), 6);

    [Fact]
    public void ATraceIsNothing() => Assert.Equal(0, LifeAboardRule.BystanderSeverity(0.5, 0.05, 0.05));

    [Theory]
    [InlineData(false, false, LifeAboardRule.Feeling.Good)]
    [InlineData(true, false, LifeAboardRule.Feeling.Bad)]
    [InlineData(false, true, LifeAboardRule.Feeling.Bad)]
    public void TeetotalersAndHardDrugsLeaveABadMemory(bool teetotaler, bool hard, LifeAboardRule.Feeling feeling) =>
        Assert.Equal(feeling, LifeAboardRule.Remembers(teetotaler, hard));

    [Fact]
    public void SmokeClearsOverItsHours()
    {
        Assert.Equal(0.5, LifeAboardRule.SmokeLeft(1, 0.5, 1), 6);
        Assert.Equal(0, LifeAboardRule.SmokeLeft(1, 2, 1));
        Assert.Equal(0, LifeAboardRule.SmokeLeft(1, 0.1, 0));
    }

    [Fact]
    public void JoyGrowsWithTheHours() => Assert.Equal(0.01, LifeAboardRule.JoyGained(0.1, 0.1), 6);

    [Theory]
    [InlineData(-1, 100, 50, true)]
    [InlineData(80, 100, 50, false)]
    [InlineData(40, 100, 50, true)]
    [InlineData(200, 100, 50, true)]
    public void APersonWaitsTheirGap(int last, int now, int gap, bool rested) => Assert.Equal(rested, LifeAboardRule.Rested(last, now, gap));
}
