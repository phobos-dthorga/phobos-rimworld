using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class RotorRuleTests
{
    [Fact]
    public void ItSpinsUpEvenlyToFullWhileTheEnginesRun()
    {
        // The Cherokee's 48 degrees a tick, over six seconds: an eighth of full each three quarters of a second.
        Assert.Equal(6, RotorRule.Next(0, 48, true, 0.75, 6, 10), 6);
        Assert.Equal(48, RotorRule.Next(45, 48, true, 1, 6, 10), 6);
    }

    [Fact]
    public void ItRunsDownEvenlyToRestAfter()
    {
        Assert.Equal(43.2, RotorRule.Next(48, 48, false, 1, 6, 10), 6);
        Assert.Equal(0, RotorRule.Next(2, 48, false, 1, 6, 10), 6);
    }

    [Fact]
    public void NoFullRateMeansNoSpin()
    {
        Assert.Equal(0, RotorRule.Next(30, 0, true, 1, 6, 10));
    }

    [Fact]
    public void ZeroTimesJumpAtOnce()
    {
        Assert.Equal(48, RotorRule.Next(0, 48, true, 0.1, 0, 10));
        Assert.Equal(0, RotorRule.Next(48, 48, false, 0.1, 6, 0));
    }

    [Fact]
    public void ARateAboveFullIsBroughtBack()
    {
        Assert.Equal(48, RotorRule.Next(90, 48, true, 0.1, 6, 10), 6);
    }
}
