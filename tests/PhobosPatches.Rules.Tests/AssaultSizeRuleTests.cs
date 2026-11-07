using Phobos.MissilePatches.Rules;
using Xunit;

namespace Phobos.MissilePatches.Tests;

public class AssaultSizeRuleTests
{
    [Fact]
    public void AnUnstruckTownKeepsRimPactsOwnSquad() => Assert.Equal(2, AssaultSizeRule.Target(2, null, 1f));

    [Fact]
    public void AStruckTownFacesTheSquadItHadBeforeTheStrike()
    {
        Assert.Equal(37, AssaultSizeRule.Target(2, 37, 1f));
        Assert.Equal(19, AssaultSizeRule.Target(2, 37, 0.5f));
    }

    [Fact]
    public void ASquadIsNeverMadeSmallerThanRimPactsWouldSend() => Assert.Equal(12, AssaultSizeRule.Target(12, 5, 1f));

    [Fact]
    public void AStrikeFromBeforeCountsWereKeptIsNotTrimmed() => Assert.Equal(AssaultSizeRule.NoTrim, AssaultSizeRule.Target(2, -1, 1f));

    [Fact]
    public void TheTechFactorMatchesRimPacts()
    {
        Assert.Equal(1f, AssaultSizeRule.TechFactor(4, 4));
        Assert.Equal(1f, AssaultSizeRule.TechFactor(3, 5));
        Assert.Equal(0.8f, AssaultSizeRule.TechFactor(6, 4), 4);
        Assert.Equal(0.5f, AssaultSizeRule.TechFactor(7, 1));
    }

    [Fact]
    public void OnlyARecentStrikeCounts()
    {
        Assert.True(AssaultSizeRule.Recent(1000, 1000 + 900000, 900000));
        Assert.False(AssaultSizeRule.Recent(1000, 1000 + 900001, 900000));
        Assert.False(AssaultSizeRule.Recent(5000, 1000, 900000));
    }
}
