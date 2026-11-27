using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GameMeterRuleTests
{
    [Theory]
    [InlineData(1000, 1003, 3)]
    [InlineData(1000, 1000, 0)]                 // paused: no ticks
    [InlineData(1000, 900, 0)]                  // another save loaded, further back
    [InlineData(1000, 1000 + 2500, 2500)]       // the most one frame counts
    [InlineData(1000, 1000 + 2501, 0)]          // a jump: time advanced, or a load
    public void TicksCountOnlyAsTheSimulationMovesForward(int before, int now, int counted) =>
        Assert.Equal(counted, GameMeterRule.TicksAdvanced(before, now));

    [Fact]
    public void MillisecondsFromAStopwatch()
    {
        Assert.Equal(16.5, GameMeterRule.Milliseconds(165_000, 10_000_000), 6);
        Assert.Equal(0.0, GameMeterRule.Milliseconds(5, 0));
    }
}
