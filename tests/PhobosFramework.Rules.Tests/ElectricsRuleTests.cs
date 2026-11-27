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
    public void AFlatBatteryWithTheEngineOffPowersNothing()
    {
        Assert.True(ElectricsRule.LoadsPowered(false, 1, false, true, 0.6, 0.05, true));
        Assert.False(ElectricsRule.LoadsPowered(false, 1, false, true, 0.01, 0.05, true));
    }

    [Fact]
    public void AGridOrARunningGeneratorPowersTheLoadsOnAFlatBattery()
    {
        Assert.True(ElectricsRule.LoadsPowered(false, 1, true, true, 0, 0.05, false));
        Assert.True(ElectricsRule.LoadsPowered(true, 1, false, true, 0, 0.05, false));
    }

    [Fact]
    public void ALostGeneratorLeavesTheLoadsOnTheBattery()
    {
        Assert.False(ElectricsRule.LoadsPowered(true, 0.1, false, true, 0.01, 0.05, true));
        Assert.True(ElectricsRule.LoadsPowered(true, 0.1, false, true, 0.5, 0.05, true));
    }

    [Fact]
    public void AVehicleWithNoBatteryIsAlwaysPowered()
    {
        Assert.True(ElectricsRule.LoadsPowered(false, 0, false, false, 0, 0.05, false));
    }

    [Fact]
    public void AGeneratorWithNoBatteryPowersItsLoadsOnlyWhileItTurns()
    {
        // A steam locomotive's turbo-generator (Framework 0.376.0): nothing with the fire out, nothing from a ruined generator, power in steam.
        Assert.False(ElectricsRule.LoadsPowered(false, 1, false, false, 0, 0.05, true, generatorOnly: true));
        Assert.False(ElectricsRule.LoadsPowered(true, 0.1, false, false, 0, 0.05, true, generatorOnly: true));
        Assert.True(ElectricsRule.LoadsPowered(true, 1, false, false, 0, 0.05, false, generatorOnly: true));
        // A grid still carries them, as for any vehicle.
        Assert.True(ElectricsRule.LoadsPowered(false, 0, true, false, 0, 0.05, false, generatorOnly: true));
    }

    [Fact]
    public void AFlatBatteryComesBackOnlyAboveTheRecoveryMargin()
    {
        // Gone flat: 0.07 is above the line but under twice it, so still dead; once powered, 0.07 holds.
        Assert.False(ElectricsRule.LoadsPowered(false, 1, false, true, 0.07, 0.05, false));
        Assert.True(ElectricsRule.LoadsPowered(false, 1, false, true, 0.1, 0.05, false));
        Assert.True(ElectricsRule.LoadsPowered(false, 1, false, true, 0.07, 0.05, true));
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
