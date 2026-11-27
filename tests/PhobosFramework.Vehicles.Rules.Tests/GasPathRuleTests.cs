using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The schematic's heat gradient along a turbine's gas path (Framework 0.378.0, SC8). Figures are the rule's authored shape.</summary>
public class GasPathRuleTests
{
    [Fact]
    public void TheIntakeIsTheOutsideAir() => Assert.Equal(15, GasPathRule.Temperature(15, 800, 0), 6);

    [Fact]
    public void TheCompressorWarmsTheAirUpToItsShare() =>
        Assert.Equal(15 + 785 * GasPathRule.CompressorRise, GasPathRule.Temperature(15, 800, GasPathRule.BurnersFrom - 1e-9), 3);

    [Fact]
    public void TheBurnersRunAtTheTurbineFigure() => Assert.Equal(800, GasPathRule.Temperature(15, 800, 0.5), 6);

    [Fact]
    public void TheExhaustIsCoolerThanTheBurnersButWarmerThanTheCompressor()
    {
        double exhaust = GasPathRule.Temperature(15, 800, 1);
        Assert.Equal(15 + 785 * GasPathRule.ExhaustShare, exhaust, 6);
        Assert.True(exhaust < 800);
        Assert.True(exhaust > GasPathRule.Temperature(15, 800, 0.39));
    }

    [Fact]
    public void AStoppedEngineReadsTheOutsideAirAllAlong()
    {
        foreach (double p in new[] { 0.0, 0.3, 0.5, 0.8, 1.0 }) Assert.Equal(15, GasPathRule.Temperature(15, 15, p), 6);
    }

    [Fact]
    public void ColdIsBlueAndTheLimitIsRed()
    {
        (double r, double _, double b) = GasPathRule.Colour(-10, 800);
        Assert.True(b > r);
        (double hr, double hg, double hb) = GasPathRule.Colour(820, 800);
        Assert.True(hr > hg && hr > hb);
    }

    [Fact]
    public void ColoursBlendBetweenStops()
    {
        (double r, double g, double b) = GasPathRule.Colour(100, 800);
        Assert.InRange(r, 0.30, 0.62);
        Assert.InRange(g, 0.52, 0.78);
        Assert.InRange(b, 0.88, 0.95);
    }
}
