using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Tests;

public class AssaultSizeRuleTests
{
    [Fact]
    public void AnUnstruckTownKeepsRimPactsOwnCount() => Assert.Equal(2, AssaultSizeRule.Headcount(2, null));

    [Fact]
    public void AStruckTownCountsTheGarrisonItHadBeforeTheStrike() => Assert.Equal(39, AssaultSizeRule.Headcount(2, 39));

    [Fact]
    public void TheCountIsNeverLowerThanRimPactsOwn() => Assert.Equal(12, AssaultSizeRule.Headcount(12, 5));

    [Fact]
    public void AStrikeFromBeforeCountsWereKeptLeavesRimPactsCount() => Assert.Equal(2, AssaultSizeRule.Headcount(2, -1));

    [Fact]
    public void AWipedOutTownStaysWipedOutSoTheAttackWins() => Assert.Equal(0, AssaultSizeRule.Headcount(0, 39));

    [Fact]
    public void OnlyARecentStrikeCounts()
    {
        Assert.True(AssaultSizeRule.Recent(1000, 1000 + 900000, 900000));
        Assert.False(AssaultSizeRule.Recent(1000, 1000 + 900001, 900000));
        Assert.False(AssaultSizeRule.Recent(5000, 1000, 900000));
    }
}
