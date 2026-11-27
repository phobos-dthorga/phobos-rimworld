using System;
using System.Collections.Generic;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class TerrainRuleTests
{
    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(2, 30.0)]
    [InlineData(3, 100.0)]
    [InlineData(4, 300.0)]
    [InlineData(5, 500.0)]
    [InlineData(0, 0.0)]
    public void GroundStandsAsHighAsItLiftsAnAerialAndImpassableHigher(int hilliness, double metres) =>
        Assert.Equal(metres, TerrainRule.GroundHeightMetres(hilliness));

    [Fact]
    public void AnEdgeWellUnderTheLineCostsNothing()
    {
        Assert.Equal(0.0, TerrainRule.KnifeEdgeLossDb(-0.78), 6);
        Assert.Equal(0.0, TerrainRule.KnifeEdgeLossDb(-3.0), 6);
        Assert.Equal(0.0, TerrainRule.KnifeEdgeLossDb(double.NegativeInfinity), 6);
        Assert.Equal(0.0, TerrainRule.KnifeEdgeLossDb(double.NaN), 6);
    }

    [Fact]
    public void AGrazingEdgeCostsSixDecibels() => Assert.Equal(6.03, TerrainRule.KnifeEdgeLossDb(0.0), 2);

    [Theory]
    // 6.9 + 20 log10(sqrt((v - 0.1)^2 + 1) + v - 0.1), worked by hand.
    [InlineData(1.0, 13.9)]
    [InlineData(2.4, 20.5)]
    [InlineData(8.92, 31.9)]
    public void ADeeperShadowCostsMore(double v, double expected) => Assert.Equal(expected, TerrainRule.KnifeEdgeLossDb(v), 1);

    [Fact]
    public void TheLossNeverFallsAsTheEdgeRises()
    {
        double last = 0;
        for (double v = -2; v <= 30; v += 0.05)
        {
            double loss = TerrainRule.KnifeEdgeLossDb(v);
            Assert.True(loss >= last - 1e-9, "fell at v = " + v);
            last = loss;
        }
    }

    [Fact]
    public void TheLinePassesBetweenTheAerialsLessThePlanetsBulge()
    {
        // Half way between a 10 m and a 30 m aerial 20 km apart: 20 m on the line, less 10,000 x 10,000 / (2 x 8,495,000) = 5.89 m.
        Assert.Equal(14.11, TerrainRule.LineHeightMetres(10, 30, 10000, 10000), 2);
        // At an aerial the line is at the aerial.
        Assert.Equal(10.0, TerrainRule.LineHeightMetres(10, 30, 0, 20000), 6);
    }

    [Fact]
    public void TheBulgeAgreesWithTheRadioHorizon()
    {
        // Two 10 m aerials at their horizon: the line touches smooth ground half way, within a few centimetres.
        double horizon = Propagation.RadioHorizonMetres(10, 10);
        Assert.Equal(0.0, TerrainRule.LineHeightMetres(10, 10, horizon / 2, horizon / 2), 1);
    }

    [Fact]
    public void AShorterWaveLosesMoreBehindTheSameHill()
    {
        // A 300 m mountain 1 km from a 10 m mast, the far end 300 m up and 20 km off: about 32 dB on 2 m, 36 dB on 70 cm.
        double over = 300 - TerrainRule.LineHeightMetres(10, 310, 1000, 19000);
        double figure = TerrainRule.EdgeFigure(over, 1000, 19000);
        double vhf = TerrainRule.LossDb(figure, 146);
        double uhf = TerrainRule.LossDb(figure, 435);
        Assert.InRange(vhf, 30, 34);
        Assert.InRange(uhf, vhf + 3, vhf + 6);
    }

    [Fact]
    public void NoEdgeCostsNothingOnAnyBand()
    {
        Assert.Equal(0.0, TerrainRule.LossDb(TerrainRule.Edge.None.Figure, 146), 6);
        Assert.Equal(0.0, TerrainRule.LossDb(5.0, 0), 6);
    }

    [Fact]
    public void FlatGroundBetweenIsNoEdge()
    {
        Assert.False(TerrainRule.Worst(new List<double> { 0, 0, 0, 0 }, 5000, 3, 10).Any);
        Assert.False(TerrainRule.Worst(new List<double>(), 1000, 3, 10).Any);
    }

    [Fact]
    public void TheHighestGroundOverTheLineDecides()
    {
        // Nine tiles between two 10 m masts 10 km apart: small hills at the second, a mountain at the sixth.
        var ground = new List<double> { 0, 30, 0, 0, 0, 300, 0, 0, 0 };
        TerrainRule.Edge edge = TerrainRule.Worst(ground, 10000, 10, 10);
        Assert.True(edge.Any);
        Assert.Equal(5, edge.Index);
        Assert.InRange(edge.OverLineMetres, 290, 293);
    }

    [Fact]
    public void AHillNearAnAerialBitesHarderThanTheSameHillHalfWay()
    {
        var near = new List<double> { 100, 0, 0, 0, 0, 0, 0, 0, 0 };
        var middle = new List<double> { 0, 0, 0, 0, 100, 0, 0, 0, 0 };
        Assert.True(TerrainRule.Worst(near, 10000, 10, 10).Figure > TerrainRule.Worst(middle, 10000, 10, 10).Figure);
    }

    [Fact]
    public void AHigherAerialClearsAHillALowOneCannot()
    {
        // Small hills (30 m) half way between two ends 10 km apart.
        var ground = new List<double> { 0, 0, 0, 0, 30, 0, 0, 0, 0 };
        double low = TerrainRule.LossDb(TerrainRule.Worst(ground, 10000, 3, 10).Figure, 146);
        double high = TerrainRule.LossDb(TerrainRule.Worst(ground, 10000, 110, 110).Figure, 146);
        Assert.True(low > 6, "low aerials: " + low);
        Assert.Equal(0.0, high, 6);
    }
}
