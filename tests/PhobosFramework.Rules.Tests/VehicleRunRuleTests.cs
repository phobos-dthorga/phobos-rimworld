using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class VehicleRunRuleTests
{
    const int Patience = 600;

    [Fact]
    public void TheEngineOffComesBeforeEverything() =>
        Assert.Equal(DriveWatch.EngineOff, VehicleRunRule.Watch(engineOn: false, otherOrders: true, underWay: true, 9999, Patience, atCell: true));

    [Fact]
    public void AnotherOrderComesBeforeTheDrive() =>
        Assert.Equal(DriveWatch.OtherOrders, VehicleRunRule.Watch(true, otherOrders: true, underWay: true, 9999, Patience, atCell: true));

    [Fact]
    public void AVehicleThatMovedLatelyIsOnItsWay() =>
        Assert.Equal(DriveWatch.OnWay, VehicleRunRule.Watch(true, false, underWay: true, 250, Patience, atCell: false));

    [Fact]
    public void StandingStillPastItsPatienceIsStuck() =>
        Assert.Equal(DriveWatch.Stuck, VehicleRunRule.Watch(true, false, underWay: true, Patience + 1, Patience, atCell: false));

    [Fact]
    public void ExactlyAtItsPatienceIsNotYetStuck() =>
        Assert.Equal(DriveWatch.OnWay, VehicleRunRule.Watch(true, false, underWay: true, Patience, Patience, atCell: false));

    [Fact]
    public void WithNoPatienceItIsNeverStuck() =>
        Assert.Equal(DriveWatch.OnWay, VehicleRunRule.Watch(true, false, underWay: true, 1_000_000, patienceTicks: 0, atCell: false));

    [Fact]
    public void StoppedOnItsCellHasArrived() =>
        Assert.Equal(DriveWatch.Arrived, VehicleRunRule.Watch(true, false, underWay: false, 0, Patience, atCell: true));

    [Fact]
    public void StoppedElsewhereStoppedShort() =>
        Assert.Equal(DriveWatch.StoppedShort, VehicleRunRule.Watch(true, false, underWay: false, 0, Patience, atCell: false));

    [Fact]
    public void AStoppedVehicleIsNeverStuckHoweverLongItStood() =>
        Assert.Equal(DriveWatch.StoppedShort, VehicleRunRule.Watch(true, false, underWay: false, 9999, Patience, atCell: false));

    [Fact]
    public void UnderWayOnItsCellIsStillOnItsWay() =>
        Assert.Equal(DriveWatch.OnWay, VehicleRunRule.Watch(true, false, underWay: true, 0, Patience, atCell: true));

    [Fact]
    public void TheGatheringRunsRulesAreTheSharedOnes()
    {
        Assert.Equal(VehicleRunRule.Stuck(601, 600), GatherRule.Stuck(601, 600));
        Assert.Equal(VehicleRunRule.Stuck(600, 600), GatherRule.Stuck(600, 600));
        Assert.Equal(VehicleRunRule.GiveUp(5, 5), GatherRule.GiveUp(5, 5));
        Assert.Equal(VehicleRunRule.GiveUp(4, 5), GatherRule.GiveUp(4, 5));
    }

    [Fact]
    public void GivingUpNeedsALimit()
    {
        Assert.True(VehicleRunRule.GiveUp(5, 5));
        Assert.False(VehicleRunRule.GiveUp(4, 5));
        Assert.False(VehicleRunRule.GiveUp(100, 0));
    }
}
