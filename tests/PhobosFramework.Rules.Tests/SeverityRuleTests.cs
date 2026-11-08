using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class SeverityRuleTests
{
    [Fact]
    public void AddsThenFadesWithinZeroAndOne()
    {
        Assert.Equal(0.5, SeverityRule.Next(0.2, 0.3, 0, 1), 6);
        Assert.Equal(0.4, SeverityRule.Next(0.5, 0, 0.05, 2), 6);
        Assert.Equal(1, SeverityRule.Next(0.9, 0.5, 0, 0));
        Assert.Equal(0, SeverityRule.Next(0.1, 0, 1, 1));
    }

    [Fact]
    public void NothingNegativeIsAdded()
    {
        Assert.Equal(0.3, SeverityRule.Next(0.3, -0.2, 0, 0), 6);
    }

    [Fact]
    public void AHitAddsForTheHealthLost()
    {
        // A radiator of 30 health hit for 10: 0.03 a point gives 0.3.
        Assert.Equal(0.3, SeverityRule.FromHit(0.03, 10), 6);
        Assert.Equal(0, SeverityRule.FromHit(0.03, 0));
        Assert.Equal(0, SeverityRule.FromHit(0.03, -4));
    }

    [Fact]
    public void ARepairToFullClears()
    {
        Assert.True(SeverityRule.ClearedByRepair(true, 20, 30, 30));
        // Vehicle Framework stops repairing at 99.5%.
        Assert.True(SeverityRule.ClearedByRepair(true, 20, 29.86, 30));
        Assert.False(SeverityRule.ClearedByRepair(true, 20, 25, 30));
        Assert.False(SeverityRule.ClearedByRepair(false, 20, 30, 30));
    }

    [Fact]
    public void StandingAtFullHealthIsNotARepair()
    {
        Assert.False(SeverityRule.ClearedByRepair(true, 30, 30, 30));
    }

    [Fact]
    public void TheEndOfAFadeIsGone()
    {
        Assert.True(SeverityRule.IsGone(0.0005));
        Assert.False(SeverityRule.IsGone(0.01));
    }
}
