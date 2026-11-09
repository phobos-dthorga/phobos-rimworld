using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class GeneratorRuleTests
{
    const float Day = 1f;

    [Fact]
    public void IdlesOnNoLoad()
    {
        GeneratorStep step = GeneratorRule.Step(1200, 0, false, 0, 0, Day, 1.5f, 9);
        Assert.Equal(1200, step.OutputWatts);
        Assert.Equal(0, step.Load);
        Assert.Equal(1.5, step.FuelUsed, 3);
    }

    [Fact]
    public void BurnsFullFuelAtFullLoad() => Assert.Equal(9, GeneratorRule.Step(1200, 1200, false, 0, 0, Day, 1.5f, 9).FuelUsed, 3);

    [Fact]
    public void FuelFollowsTheLoad() => Assert.Equal(5.25, GeneratorRule.Step(1200, 600, false, 0, 0, Day, 1.5f, 9).FuelUsed, 3);

    [Fact]
    public void LoadNeverPassesOne() => Assert.Equal(1, GeneratorRule.Step(1200, 5000, false, 0, 0, Day, 1.5f, 9).Load, 3);

    [Fact]
    public void BatteriesWithRoomRunItFlatOut() => Assert.Equal(1, GeneratorRule.Step(1200, 0, true, 0, 0, Day, 1.5f, 9).Load, 3);

    [Fact]
    public void SurplusFillsTheBufferToItsCapacity() => Assert.Equal(150, GeneratorRule.Step(1200, 0, false, 0, 150, Day, 1.5f, 9).Buffer, 3);

    [Fact]
    public void WithNoEngineTheBufferCarriesTheNetAndBurnsNothing()
    {
        GeneratorStep step = GeneratorRule.Step(0, 100, false, 150, 150, 0.5f, 1.5f, 9);
        Assert.Equal(100, step.OutputWatts);
        Assert.Equal(100, step.Buffer, 3);
        Assert.Equal(0, step.FuelUsed);
    }

    [Fact]
    public void AnEmptyBufferGivesNothing() => Assert.Equal(0, GeneratorRule.Step(0, 500, false, 0, 150, Day, 1.5f, 9).OutputWatts);

    [Fact]
    public void ABufferThatRanOutEndsAtZero() => Assert.Equal(0, GeneratorRule.Step(0, 500, false, 100, 150, Day, 1.5f, 9).Buffer, 3);

    [Fact]
    public void ALostBatteryNodeDropsTheStoredCharge() => Assert.Equal(0, GeneratorRule.Step(0, 0, false, 150, 0, Day, 1.5f, 9).Buffer, 3);
}
