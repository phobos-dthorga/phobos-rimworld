using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class BeltRuleTests
{
    [Fact]
    public void AnEmptyWaitingBeltIsStuck() =>
        // The owner's PG6 save, 30 Oct 2026: both belts of the human-meat line were like this.
        Assert.True(BeltRule.IsStuck(waiting: true, heldStacks: 0));

    [Fact]
    public void ABeltWaitingWithAnItemIsQueueing() =>
        // A full belt waits for room ahead; VFE Factory moves it on by itself.
        Assert.False(BeltRule.IsStuck(waiting: true, heldStacks: 1));

    [Fact]
    public void AnEmptyBeltThatIsNotWaitingStillPulls() =>
        Assert.False(BeltRule.IsStuck(waiting: false, heldStacks: 0));
}
