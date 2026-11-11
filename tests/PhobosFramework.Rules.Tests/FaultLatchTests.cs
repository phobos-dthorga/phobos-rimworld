using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class FaultLatchTests
{
    [Fact]
    public void NothingRestsBeforeAFault() => Assert.False(new FaultLatch().Resting(100));

    [Fact]
    public void TheFirstFaultIsSaidInFull() => Assert.Equal(FaultSay.Full, new FaultLatch().Note(0, 5, 60));

    [Fact]
    public void AFaultRestsItForAWhile()
    {
        var latch = new FaultLatch();
        latch.Note(10, 5, 60);
        Assert.True(latch.Resting(14.9));
        Assert.False(latch.Resting(15));
    }

    [Fact]
    public void LaterFaultsAreQuietUntilTheReminder()
    {
        var latch = new FaultLatch();
        latch.Note(0, 5, 3);
        Assert.Equal(FaultSay.Nothing, latch.Note(5, 5, 3));
        Assert.Equal(FaultSay.Reminder, latch.Note(10, 5, 3));
        Assert.Equal(FaultSay.Nothing, latch.Note(15, 5, 3));
        Assert.Equal(4, latch.Count);
    }

    [Fact]
    public void AnHourOfFailingEveryFiveSecondsLogsAFewLinesNotThousands()
    {
        var latch = new FaultLatch();
        int lines = 0;
        for (int second = 0; second < 3600; second += 5)
        {
            if (latch.Note(second, 5, 60) != FaultSay.Nothing) lines++;
        }
        Assert.Equal(13, lines);
    }
}
