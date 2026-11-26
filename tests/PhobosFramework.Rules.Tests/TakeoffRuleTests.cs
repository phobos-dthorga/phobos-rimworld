using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class TakeoffRuleTests
{
    [Fact]
    public void AtItsMaximumWeightItNeedsItsFullRoll()
    {
        Assert.Equal(35, TakeoffRule.RollCells(32500, 32500, 12, 35));
    }

    [Fact]
    public void LighterItRollsShorterButNeverUnderTheShortest()
    {
        // Half its maximum: a quarter of the full roll, 9, so its shortest of 12.
        Assert.Equal(12, TakeoffRule.RollCells(16250, 32500, 12, 35));
        // 80 %: 0.64 x 35 = 22.4, so 23.
        Assert.Equal(23, TakeoffRule.RollCells(26000, 32500, 12, 35));
    }

    [Fact]
    public void OverItsMaximumItMayNotGo()
    {
        Assert.True(TakeoffRule.MayTakeOff(32500, 32500));
        Assert.False(TakeoffRule.MayTakeOff(33000, 32500));
        Assert.Equal(500, TakeoffRule.ShedKg(33000, 32500), 6);
        Assert.Equal(0, TakeoffRule.ShedKg(30000, 32500), 6);
    }

    [Fact]
    public void WithNoMaximumItTakesItsFullRoll()
    {
        Assert.True(TakeoffRule.MayTakeOff(99999, 0));
        Assert.Equal(35, TakeoffRule.RollCells(10000, 0, 12, 35));
    }
}
