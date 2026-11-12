using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>The winch's balance of pulls and the recovery tow's weight limit (Framework 0.103.0).</summary>
public class RecoveryRuleTests
{
    const double Mire = 0.3;

    [Fact]
    public void FirmGroundIsNotSoftAndTheSoftestIsFully()
    {
        Assert.Equal(0, RecoveryRule.Softness(TreadRules.FirmGrip), 6);
        Assert.Equal(1, RecoveryRule.Softness(TreadRules.SoftestGrip), 6);
        Assert.Equal(0.5, RecoveryRule.Softness((TreadRules.FirmGrip + TreadRules.SoftestGrip) / 2), 6);
    }

    [Fact]
    public void ARollingVehicleOnFirmGroundTakesOnlyItsRollingResistance()
    {
        double resistance = RecoveryRule.Resistance(DrivetrainModel.RollingResistanceForPathCost(0), DrivetrainModel.GripForPathCost(0), rolls: true, Mire);
        Assert.Equal(0.012, resistance, 6);
    }

    [Fact]
    public void MudAddsSuctionAndADraggedVehicleTakesMore()
    {
        double rolling = DrivetrainModel.RollingResistanceForPathCost(14), grip = DrivetrainModel.GripForPathCost(14);
        double rolls = RecoveryRule.Resistance(rolling, grip, rolls: true, Mire);
        double dragged = RecoveryRule.Resistance(rolling, grip, rolls: false, Mire);
        Assert.True(rolls > rolling);
        Assert.True(dragged > rolls);
    }

    [Fact]
    public void AWinchGivesNoMoreThanTheVehicleHoldsOnItsBrakes()
    {
        Assert.Equal(5443, RecoveryRule.Available(5443, anchorKg: 12000, anchorGrip: 0.6), 6);
        Assert.Equal(1200, RecoveryRule.Available(5443, anchorKg: 2000, anchorGrip: 0.6), 6);
    }

    [Fact]
    public void ALightTruckInMudCanBePulledAndATankCannot()
    {
        double rolling = DrivetrainModel.RollingResistanceForPathCost(14), grip = DrivetrainModel.GripForPathCost(14);
        double resistance = RecoveryRule.Resistance(rolling, grip, rolls: true, Mire);
        double available = RecoveryRule.Available(5443, anchorKg: 9000, anchorGrip: 0.6);
        Assert.True(RecoveryRule.CanPull(RecoveryRule.Required(7100, resistance), available));
        Assert.False(RecoveryRule.CanPull(RecoveryRule.Required(30000, resistance), available));
    }

    [Fact]
    public void AHeavierPullIsSlowerUpToTwiceTheBasePace()
    {
        Assert.Equal(180, RecoveryRule.TicksPerCell(180, 0, 1000));
        Assert.Equal(270, RecoveryRule.TicksPerCell(180, 500, 1000));
        Assert.Equal(360, RecoveryRule.TicksPerCell(180, 1000, 1000));
        Assert.Equal(360, RecoveryRule.TicksPerCell(180, 5000, 1000));
        Assert.Equal(int.MaxValue, RecoveryRule.TicksPerCell(180, 10, 0));
    }

    [Fact]
    public void AStepHeadsStraightAlongALongAxisAndDiagonallyOtherwise()
    {
        Assert.Equal((1, 0), RecoveryRule.Step(0, 0, 10, 2));
        Assert.Equal((0, -1), RecoveryRule.Step(0, 0, 1, -9));
        Assert.Equal((1, 1), RecoveryRule.Step(0, 0, 5, 4));
        Assert.Equal((0, 0), RecoveryRule.Step(3, 3, 3, 3));
    }

    [Fact]
    public void TheTowLimitIsAMultipleOfTheTractorsWeight()
    {
        Assert.True(RecoveryRule.CanTow(6000, 9000, 1.5));
        Assert.False(RecoveryRule.CanTow(6000, 9001, 1.5));
        Assert.False(RecoveryRule.CanTow(0, 100, 1.5));
    }
}
