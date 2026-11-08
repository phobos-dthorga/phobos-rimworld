using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TickThrottleTests
{
    [Fact]
    public void NeverDoneIsDue()
    {
        Assert.True(TickThrottle.Due(now: 500, lastTick: TickThrottle.Never, intervalTicks: 60000));
    }

    [Fact]
    public void DueOnlyOnceTheIntervalHasPassed()
    {
        Assert.False(TickThrottle.Due(now: 59999, lastTick: 0, intervalTicks: 60000));
        Assert.True(TickThrottle.Due(now: 60000, lastTick: 0, intervalTicks: 60000));
    }

    [Fact]
    public void AnEarlierGameLoadedSinceIsDue()
    {
        // A static throttle remembers tick 900000 from one game; the player loads another at tick 1000.
        Assert.True(TickThrottle.Due(now: 1000, lastTick: 900000, intervalTicks: 60000));
    }
}
