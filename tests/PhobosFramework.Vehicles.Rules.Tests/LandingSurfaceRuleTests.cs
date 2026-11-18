using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The ground under an aircraft as it lifts off or touches down: engine wear from loose ground, cut by rolling and by an inlet
/// barrier; the chance of setting dry growth alight, raised in a heat wave; and what counts as dry.</summary>
public class LandingSurfaceRuleTests
{
    const double PerEvent = 0.005, Rolling = 0.5, BarrierStops = 0.8;

    [Fact]
    public void WhollyLooseGroundCostsTheFullShare()
    {
        Assert.Equal(PerEvent, LandingSurfaceRule.EngineWearShare(1, false, false, PerEvent, Rolling, BarrierStops), 9);
    }

    [Fact]
    public void FirmGroundCostsNothing()
    {
        Assert.Equal(0, LandingSurfaceRule.EngineWearShare(0, false, false, PerEvent, Rolling, BarrierStops));
    }

    [Fact]
    public void WearFollowsTheLooseShare()
    {
        Assert.Equal(PerEvent / 2, LandingSurfaceRule.EngineWearShare(0.5, false, false, PerEvent, Rolling, BarrierStops), 9);
    }

    [Fact]
    public void RollingThrowsUpLessDust()
    {
        Assert.Equal(PerEvent * Rolling, LandingSurfaceRule.EngineWearShare(1, true, false, PerEvent, Rolling, BarrierStops), 9);
    }

    [Fact]
    public void TheBarrierStopsMostOfIt()
    {
        Assert.Equal(PerEvent * (1 - BarrierStops), LandingSurfaceRule.EngineWearShare(1, false, true, PerEvent, Rolling, BarrierStops), 9);
    }

    [Fact]
    public void RollingAndTheBarrierTogether()
    {
        double expected = PerEvent * Rolling * (1 - BarrierStops);
        Assert.Equal(expected, LandingSurfaceRule.EngineWearShare(1, true, true, PerEvent, Rolling, BarrierStops), 9);
    }

    [Fact]
    public void AnUnknownShareCostsNothing()
    {
        Assert.Equal(0, LandingSurfaceRule.EngineWearShare(double.NaN, false, false, PerEvent, Rolling, BarrierStops));
    }

    [Fact]
    public void FireChanceFollowsTheGrowth()
    {
        Assert.Equal(0.025, LandingSurfaceRule.FireChance(0.5, false, 0.05, 3), 9);
        Assert.Equal(0, LandingSurfaceRule.FireChance(0, true, 0.05, 3));
    }

    [Fact]
    public void AHeatWaveRaisesTheFireChance()
    {
        Assert.Equal(0.15, LandingSurfaceRule.FireChance(1, true, 0.05, 3), 9);
    }

    [Fact]
    public void FireChanceNeverPassesOne()
    {
        Assert.Equal(1, LandingSurfaceRule.FireChance(1, true, 0.5, 3));
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0.2, 0, false)]
    [InlineData(0, 0.1, true)]
    [InlineData(0, 0.5, false)]
    public void DryMeansNoRainAndLittleSnow(double rain, double snow, bool expected)
    {
        Assert.Equal(expected, LandingSurfaceRule.Dry(rain, snow, 0.2));
    }
}
