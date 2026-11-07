using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class VentilatorRuleTests
{
    [Fact]
    public void ItBreathesOnlyForFailingBreathingWithLungsLeft()
    {
        Assert.True(VentilatorRule.Needs(0.3f, 0.6f));
        Assert.False(VentilatorRule.Needs(0.6f, 0.6f));
        Assert.False(VentilatorRule.Needs(0f, 0.6f));
    }

    [Fact]
    public void ItUsesTheFewestStepsThatReachTheFloor()
    {
        Assert.Equal(3, VentilatorRule.StageFor(0.3f, 0.6f, 0.1f, 6));
        Assert.Equal(1, VentilatorRule.StageFor(0.55f, 0.6f, 0.1f, 6));
        Assert.Equal(6, VentilatorRule.StageFor(0.01f, 0.6f, 0.1f, 6));
        Assert.Equal(0, VentilatorRule.StageFor(0.8f, 0.6f, 0.1f, 6));
        Assert.Equal(0, VentilatorRule.StageFor(0f, 0.6f, 0.1f, 6));
    }

    [Fact]
    public void TheStatusNamesWhatToFixFirst()
    {
        Assert.Equal(VentilatorStatus.SwitchedOff, VentilatorRule.Status(false, true, 1, 1, 0, 0));
        Assert.Equal(VentilatorStatus.NoPower, VentilatorRule.Status(true, false, 1, 0, 0, 0));
        Assert.Equal(VentilatorStatus.NoBed, VentilatorRule.Status(true, true, 0, 0, 0, 0));
        Assert.Equal(VentilatorStatus.Treating, VentilatorRule.Status(true, true, 1, 1, 1, 0));
        Assert.Equal(VentilatorStatus.NeedsTube, VentilatorRule.Status(true, true, 1, 0, 1, 1));
        Assert.Equal(VentilatorStatus.NotNeeded, VentilatorRule.Status(true, true, 1, 0, 0, 1));
        Assert.Equal(VentilatorStatus.Waiting, VentilatorRule.Status(true, true, 1, 0, 0, 0));
    }

    [Fact]
    public void ATubeHelpsAClosingAirwayOrAWaitingVentilator()
    {
        Assert.Equal(DeviceRefusal.None, DeviceRule.Intubation(false, true, false));
        Assert.Equal(DeviceRefusal.None, DeviceRule.Intubation(false, false, true));
        Assert.Equal(DeviceRefusal.NotNeeded, DeviceRule.Intubation(false, false, false));
        Assert.Equal(DeviceRefusal.AlreadyHasTube, DeviceRule.Intubation(true, true, true));
    }
}
