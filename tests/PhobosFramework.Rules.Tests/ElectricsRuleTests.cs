using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ElectricsRuleTests
{
    [Fact]
    public void NothingIsShedWithTheEngineStopped()
    {
        Assert.Equal(0, ElectricsRule.ShedDepth(false, 0, 0.1, true, 3, 6));
    }

    [Fact]
    public void NothingIsShedWhileTheChargingKeepsUp()
    {
        Assert.Equal(0, ElectricsRule.ShedDepth(true, 1, 0.9, false, 0, 6));
        Assert.Equal(0, ElectricsRule.ShedDepth(true, 1, 0.25, false, 0, 6));
    }

    [Fact]
    public void ALostGeneratorShedsTheComfortLoadsFirstThenMoreAsTheChargeFalls()
    {
        Assert.Equal(2, ElectricsRule.ShedDepth(true, 0, 0.9, true, 0, 6));
        Assert.Equal(4, ElectricsRule.ShedDepth(true, 0, 0.5, true, 0, 6));
        Assert.Equal(5, ElectricsRule.ShedDepth(true, 0, 0.35, true, 0, 6));
        Assert.Equal(6, ElectricsRule.ShedDepth(true, 0, 0.2, true, 0, 6));
    }

    [Fact]
    public void ALowBatteryRunningDownShedsEverythingButTheEssentials()
    {
        Assert.Equal(6, ElectricsRule.ShedDepth(true, 1, 0.25, true, 0, 6));
    }

    [Fact]
    public void ShedLoadsStayOffUntilTheChargeHasRecovered()
    {
        // Recovering from 0.3 to 0.35: still shedding, but the lights (rank 6) are back first.
        Assert.Equal(5, ElectricsRule.ShedDepth(true, 1, 0.32, false, 6, 6));
        Assert.Equal(0, ElectricsRule.ShedDepth(true, 1, 0.36, false, 6, 6));
    }

    [Fact]
    public void TheDepthNeverExceedsTheRanksAVehicleHas()
    {
        Assert.Equal(1, ElectricsRule.ShedDepth(true, 0, 0.9, true, 0, 1));
        Assert.Equal(0, ElectricsRule.ShedDepth(true, 0, 0.9, true, 0, 0));
    }

    [Fact]
    public void EssentialLoadsAreNeverShed()
    {
        Assert.False(ElectricsRule.IsShed(0, 6));
        Assert.True(ElectricsRule.IsShed(2, 2));
        Assert.False(ElectricsRule.IsShed(3, 2));
    }

    [Fact]
    public void TheGeneratorFollowsTheSpoolAndTheWorkingEngines()
    {
        Assert.Equal(0.5, ElectricsRule.GeneratorShare(0.5, 2, 2), 9);
        Assert.Equal(0.5, ElectricsRule.GeneratorShare(1, 1, 2), 9);
        Assert.Equal(0, ElectricsRule.GeneratorShare(1, 0, 2), 9);
        Assert.Equal(1, ElectricsRule.GeneratorShare(1.4, 1, 0), 9);
    }

    [Fact]
    public void TheBatteryClockIsChargeTimesCapacityOverTheDrain()
    {
        Assert.Equal(0.5, ElectricsRule.HoursLeft(0.25, 30, 15), 9);
        Assert.True(double.IsPositiveInfinity(ElectricsRule.HoursLeft(0.5, 45, 0)));
    }
}
