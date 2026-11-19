using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Draught (Framework 0.182.0): an implement dragged through the ground loads the engine at every speed, and never through air.</summary>
public class DraughtTests
{
    // The light utility truck of TowRuleTests: about 86 kW, five gears, rear-wheel drive, design speed 12 m/s.
    static DrivetrainSpec Truck() => new()
    {
        RatedPowerKw = 30 * 2.88,
        IdleRpm = 700, LaunchRpm = 1100, RatedRpm = 3000,
        Gears = new[] { 4.0, 2.4, 1.5, 1.0, 0.8 }, FinalDrive = 4.1, UpshiftRpm = 2400, DownshiftRpm = 1300,
        Efficiency = 0.88, WheelRadius = 0.38, DrivenShare = 0.5, DragArea = 2.4,
        FrictionConstant = 0, IndicatedEfficiency = 0.40, DesignSpeed = 12,
    };

    [Fact]
    public void DraughtAddsItsNewtonsToTheRoadLoadAtAnySpeed()
    {
        var spec = new DrivetrainSpec();
        var bare = new DriveConditions { MassKg = 5000, RollingResistance = 0.05 };
        var working = bare;
        working.DraughtN = 12000;
        foreach (double speed in new[] { 0.5, 2.0, 8.0 })
        {
            Assert.Equal(12000, DrivetrainModel.RoadLoad(spec, working, speed) - DrivetrainModel.RoadLoad(spec, bare, speed), 6);
        }
    }

    [Fact]
    public void ANegativeDraughtCountsAsNone()
    {
        var spec = new DrivetrainSpec();
        var bare = new DriveConditions { MassKg = 5000, RollingResistance = 0.05 };
        var odd = bare;
        odd.DraughtN = -500;
        Assert.Equal(DrivetrainModel.RoadLoad(spec, bare, 3), DrivetrainModel.RoadLoad(spec, odd, 3), 6);
    }

    [Fact]
    public void DraughtAddsNoMassToTheTrain()
    {
        var c = new DriveConditions { MassKg = 5000, TowedMassKg = 2000, DraughtN = 30000 };
        Assert.Equal(7000, DrivetrainModel.TrainKg(c), 6);
    }

    [Fact]
    public void TheForecastWithNoDraughtIsAsBefore()
    {
        TowVerdict before = TowRule.Modelled(Truck(), 3000, 2000, 0.05, 0.6, 1, 0);
        TowVerdict after = TowRule.Modelled(Truck(), 3000, 2000, 0.05, 0.6, 1, 0, draughtN: 0);
        Assert.Equal(before.SpeedShare, after.SpeedShare, 9);
        Assert.Equal(before.Outcome, after.Outcome);
    }

    [Fact]
    public void AHeavyDraughtHoldsTheWorkBack()
    {
        double free = TowRule.Modelled(Truck(), 3000, 0, 0.05, 0.6, 1, 0).SpeedShare;
        double dragging = TowRule.Modelled(Truck(), 3000, 0, 0.05, 0.6, 1, 0, draughtN: 15000).SpeedShare;
        Assert.True(dragging < free, $"dragging {dragging} should be slower than free {free}");
    }

    [Fact]
    public void MoreDraughtIsNeverFaster()
    {
        double light = TowRule.Modelled(Truck(), 3000, 0, 0.05, 0.6, 1, 0, draughtN: 4000).SpeedShare;
        double heavy = TowRule.Modelled(Truck(), 3000, 0, 0.05, 0.6, 1, 0, draughtN: 12000).SpeedShare;
        Assert.True(heavy <= light);
    }
}
