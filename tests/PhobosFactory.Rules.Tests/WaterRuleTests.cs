using Phobos.Factory.Rules;
using Xunit;

namespace Phobos.Factory.Rules.Tests;

public class WaterRuleTests
{
    [Fact]
    public void AWholeBatchUsesTheBillsLitres()
    {
        // Cook simple soup: 20 L over 6000 ticks, drawn every 100 ticks as the machine works.
        float total = 0f;
        for (int worked = 0; worked < 6000; worked += 100) total += WaterRule.LitresForTicks(20f, 100, 6000);
        Assert.Equal(20f, total, precision: 3);
    }

    [Fact]
    public void AnOverclockedMachineUsesTheSameWaterPerBatch()
    {
        // Overclocking to 3 makes the same batch take a third of the ticks.
        float total = 0f;
        for (int worked = 0; worked < 2000; worked += 100) total += WaterRule.LitresForTicks(20f, 100, 2000);
        Assert.Equal(20f, total, precision: 3);
    }

    [Fact]
    public void AHugeStepNeverDrawsMoreThanTheBatch() =>
        // Pipe System's developer button ticks a process by 60,000 in one go.
        Assert.Equal(20f, WaterRule.LitresForTicks(20f, 60000, 6000));

    [Theory]
    [InlineData(0f, 100, 6000)]
    [InlineData(20f, 0, 6000)]
    [InlineData(20f, 100, 0)]
    public void NothingToDrawIsNoWater(float litres, int ticks, int ticksPerBatch) =>
        Assert.Equal(0f, WaterRule.LitresForTicks(litres, ticks, ticksPerBatch));

    [Fact]
    public void AllContaminatedWaterGivesTheBillsWholeChance() =>
        Assert.Equal(0.25f, WaterRule.BatchPoisonChance(riskLitres: 20f * 0.25f, litres: 20f), precision: 5);

    [Fact]
    public void ContaminatedWaterForHalfABatchGivesHalfTheChance() =>
        Assert.Equal(0.125f, WaterRule.BatchPoisonChance(riskLitres: 10f * 0.25f, litres: 20f), precision: 5);

    [Fact]
    public void CleanWaterAndNoWaterCarryNoRisk()
    {
        Assert.Equal(0f, WaterRule.BatchPoisonChance(riskLitres: 0f, litres: 20f));
        Assert.Equal(0f, WaterRule.BatchPoisonChance(riskLitres: 0f, litres: 0f));
    }

    [Fact]
    public void TheChanceNeverPassesCertainty() =>
        Assert.Equal(1f, WaterRule.BatchPoisonChance(riskLitres: 30f, litres: 20f));
}
