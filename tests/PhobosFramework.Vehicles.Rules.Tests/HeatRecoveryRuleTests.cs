using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Exhaust heat recovery: heat follows the output, and stops when the room is warm.</summary>
public class HeatRecoveryRuleTests
{
    const double PerWatt = 21.0 / 175.0; // the game's heater: 21 per second for 175 W

    [Fact]
    public void HeatFollowsTheOutput()
    {
        double half = HeatRecoveryRule.HeatPerSecond(600, 0.6, PerWatt, 5, 21);
        double full = HeatRecoveryRule.HeatPerSecond(1200, 0.6, PerWatt, 5, 21);
        Assert.Equal(43.2, half, 3);
        Assert.Equal(2 * half, full, 3);
    }

    [Fact]
    public void NothingWhenWarmIdleOrNotFitted()
    {
        Assert.Equal(0, HeatRecoveryRule.HeatPerSecond(1200, 0.6, PerWatt, 21, 21));
        Assert.Equal(0, HeatRecoveryRule.HeatPerSecond(0, 0.6, PerWatt, 5, 21));
        Assert.Equal(0, HeatRecoveryRule.HeatPerSecond(1200, 0, PerWatt, 5, 21));
    }
}
