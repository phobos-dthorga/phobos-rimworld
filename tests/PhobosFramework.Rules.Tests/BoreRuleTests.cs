using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class BoreRuleTests
{
    [Theory]
    [InlineData(100f, true)]
    [InlineData(249f, true)]
    [InlineData(250f, false)]
    [InlineData(1200f, false)]
    public void ADesertTileIsADryHole(float rain, bool dry) => Assert.Equal(dry, BoreRule.Dry(rain));

    [Theory]
    [InlineData(100f, 0f)]
    [InlineData(250f, 0f)]
    [InlineData(500f, 200f)]
    [InlineData(1000f, 600f)]
    [InlineData(4000f, 1000f)]
    public void TheYieldRisesWithRainToThePumpsLimit(float rain, float expected) =>
        Assert.Equal(expected, BoreRule.LitresPerHour(rain), 2);

    [Theory]
    [InlineData(0f, 0.1f, 1f, 0.1f)]
    [InlineData(7.95f, 0.1f, 1f, 8f)]
    [InlineData(2f, 0.1f, 1.4f, 2.14f)]
    [InlineData(8f, 0.1f, 1f, 8f)]
    public void DrillingStopsAtTheDrilledDepth(float soFar, float hours, float pace, float expected) =>
        Assert.Equal(expected, BoreRule.Drilled(soFar, hours, pace), 3);
}
