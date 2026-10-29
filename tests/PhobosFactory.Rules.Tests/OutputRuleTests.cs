using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class OutputRuleTests
{
    [Fact]
    public void AnIdenticalGunLeavesNoRoomForTheNext() =>
        Assert.False(OutputRule.BatchFits(stackCount: 1, batch: 1, stackLimit: 1));

    [Fact]
    public void AFullStackTakesNoBatch()
    {
        // VFE's refinery: 25 chemfuel per batch, limit 150 = the stack limit, so Pipe System never waits.
        Assert.False(OutputRule.BatchFits(stackCount: 150, batch: 25, stackLimit: 150));
        Assert.False(OutputRule.BatchFits(stackCount: 126, batch: 25, stackLimit: 150));
        Assert.True(OutputRule.BatchFits(stackCount: 125, batch: 25, stackLimit: 150));
    }

    [Fact]
    public void AGeneratedBillWaitsAtANumberTheItemCanReach()
    {
        Assert.Equal(1f, OutputRule.WaitLimit(75f, stackLimit: 1));
        Assert.Equal(25f, OutputRule.WaitLimit(75f, stackLimit: 25));
        Assert.Equal(75f, OutputRule.WaitLimit(75f, stackLimit: 500));
    }

    [Fact]
    public void AWaitLimitIsNeverZero() => Assert.Equal(1f, OutputRule.WaitLimit(0f, stackLimit: 75));
}
