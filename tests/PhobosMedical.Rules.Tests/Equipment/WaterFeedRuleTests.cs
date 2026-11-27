using Phobos.Medical.Rules;
using Xunit;

namespace Phobos.Medical.Rules.Tests;

public class WaterFeedRuleTests
{
    [Fact]
    public void ItDrawsOnlyWhileProducing()
    {
        Assert.Equal(WaterFeedStep.Draw, WaterFeedRule.Step(running: true, producing: true, shutForWater: false));
        Assert.Equal(WaterFeedStep.Nothing, WaterFeedRule.Step(running: true, producing: false, shutForWater: false));
    }

    [Fact]
    public void SwitchedOffOrUnpoweredItDoesNothing() =>
        Assert.Equal(WaterFeedStep.Nothing, WaterFeedRule.Step(running: false, producing: false, shutForWater: true));

    [Fact]
    public void StoppedForWaterItLooksWithoutDrawing() =>
        Assert.Equal(WaterFeedStep.Look, WaterFeedRule.Step(running: true, producing: false, shutForWater: true));
}
