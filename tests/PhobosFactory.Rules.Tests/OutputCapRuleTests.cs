using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class OutputCapRuleTests
{
    [Fact]
    public void VfeMachiningLimitOfOneLosesAnItemThatDoesNotStack()
    {
        // VFE Factory's machining bills: limit 1, a weapon stacks to 1, one per batch.
        Assert.False(OutputCapRule.IsSafe(1f, stackLimit: 1, batch: 1));
        Assert.Equal(OutputCapRule.WaitWhileAnyIsThere, OutputCapRule.Clamp(1f, 1, 1));
    }

    [Fact]
    public void BelowOneMakesTheMachineWaitWhileAnyIdenticalItemIsThere()
    {
        // Pipe System waits when the stack in the output cell holds more than the limit; any stack is 1 or more.
        Assert.InRange(OutputCapRule.WaitWhileAnyIsThere, 0.01f, 0.99f);
        Assert.True(OutputCapRule.IsSafe(OutputCapRule.WaitWhileAnyIsThere, stackLimit: 1, batch: 1));
    }

    [Fact]
    public void AStackingOutputKeepsRoomForOneMoreBatch()
    {
        Assert.Equal(74f, OutputCapRule.Clamp(75f, stackLimit: 75, batch: 1));
        Assert.Equal(146f, OutputCapRule.Largest(stackLimit: 150, batch: 4));
        Assert.True(OutputCapRule.IsSafe(75f, stackLimit: 150, batch: 4));
        Assert.Equal(75f, OutputCapRule.Clamp(75f, 150, 4));
    }

    [Fact]
    public void NoLimitIsNeverSafe()
    {
        // 0 tells Pipe System "no limit", so a nearly full stack takes part of a batch and the rest is lost.
        Assert.False(OutputCapRule.IsSafe(0f, 150, 4));
        Assert.Equal(146f, OutputCapRule.Clamp(0f, 150, 4));
    }

    [Fact]
    public void ABatchBiggerThanTheStackWaitsWhileAnyIsThere() =>
        Assert.Equal(OutputCapRule.WaitWhileAnyIsThere, OutputCapRule.Largest(stackLimit: 10, batch: 10));
}
