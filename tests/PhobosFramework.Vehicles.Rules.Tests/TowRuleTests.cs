using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>"Can this pull that?" (hitch slice 3): the forecast before a tow.</summary>
public class TowRuleTests
{
    // A light utility truck, as DrivetrainTests' mule: about 86 kW, five gears, rear-wheel drive. Design speed 12 m/s (43 km/h).
    static DrivetrainSpec Truck() => new()
    {
        RatedPowerKw = 30 * 2.88,
        IdleRpm = 700, LaunchRpm = 1100, RatedRpm = 3000,
        Gears = new[] { 4.0, 2.4, 1.5, 1.0, 0.8 }, FinalDrive = 4.1, UpshiftRpm = 2400, DownshiftRpm = 1300,
        Efficiency = 0.88, WheelRadius = 0.38, DrivenShare = 0.5, DragArea = 2.4,
        FrictionConstant = 0, IndicatedEfficiency = 0.40, DesignSpeed = 12,
    };

    [Fact]
    public void NothingBehindKeepsTheWholeSpeedOnARoad()
    {
        TowVerdict verdict = TowRule.Modelled(Truck(), 3000, 0, 0.015, 0.8, 1, 0);
        Assert.Equal(TowOutcome.FullSpeed, verdict.Outcome);
        Assert.Equal(1, verdict.SpeedShare, 6);
    }

    [Fact]
    public void AHeavierTrailerNeverGoesFasterThanALighterOne()
    {
        double light = TowRule.Modelled(Truck(), 3000, 1000, 0.12, 0.55, 1, 0).SpeedShare;
        double heavy = TowRule.Modelled(Truck(), 3000, 20000, 0.12, 0.55, 1, 0).SpeedShare;
        Assert.True(heavy <= light, $"heavy {heavy} should not beat light {light}");
    }

    [Fact]
    public void AHugeTowOnSoftGroundIsHeldBackAndNamesWhy()
    {
        TowVerdict verdict = TowRule.Modelled(Truck(), 3000, 40000, 0.3, 0.35, 1, 0);
        Assert.NotEqual(TowOutcome.FullSpeed, verdict.Outcome);
        Assert.NotEqual(DriveLimit.None, verdict.Limit);
        Assert.InRange(verdict.SpeedShare, DrivetrainModel.CrawlFloor, 0.995);
        Assert.Equal(43000, verdict.TrainKg, 6);
    }

    [Fact]
    public void TheForecastIsTheDrivesOwnAnswer()
    {
        // The same conditions through the drivetrain model give the same share: the forecast cannot promise what the drive will not do.
        DrivetrainSpec spec = Truck();
        var conditions = new DriveConditions
        {
            EngineRunning = true, Speed = spec.DesignSpeed, MassKg = 3000, TowedMassKg = 15000, RollingResistance = 0.25, Grip = 0.35,
            ClutchCapacity = 1, DrivenWheelsWorking = 1,
        };
        DriveResult drive = DrivetrainModel.Evaluate(spec, conditions);
        TowVerdict verdict = TowRule.Modelled(spec, 3000, 15000, 0.25, 0.35, 1, 0);
        Assert.Equal(System.Math.Min(1, drive.Cap) >= TowRule.FullShare ? 1 : drive.Cap, verdict.SpeedShare, 6);
    }

    [Fact]
    public void ThePlainRuleIsTheTractorsShareOfTheTrain()
    {
        TowVerdict verdict = TowRule.Plain(3000, 1000, DrivetrainModel.CrawlFloor);
        Assert.Equal(TowOutcome.Slowed, verdict.Outcome);
        Assert.Equal(0.75, verdict.SpeedShare, 6);
        Assert.Equal(TowOutcome.Crawls, TowRule.Plain(1000, 50000, DrivetrainModel.CrawlFloor).Outcome);
        Assert.Equal(TowOutcome.FullSpeed, TowRule.Plain(3000, 0, DrivetrainModel.CrawlFloor).Outcome);
    }

    [Fact]
    public void ARecoveryTowOverItsRatioIsRefused()
    {
        TowVerdict verdict = TowRule.TooHeavy(3000, 6000);
        Assert.Equal(TowOutcome.TooHeavy, verdict.Outcome);
        Assert.Equal(0, verdict.SpeedShare);
        Assert.Equal(9000, verdict.TrainKg, 6);
    }

    [Fact]
    public void SharesAreClampedAndTheFloorIsACrawl()
    {
        Assert.Equal(TowOutcome.FullSpeed, TowRule.FromShare(1.4, DriveLimit.Power, 1, 0.15).Outcome);
        Assert.Equal(TowOutcome.Crawls, TowRule.FromShare(0.15, DriveLimit.Traction, 1, 0.15).Outcome);
        Assert.Equal(DriveLimit.Traction, TowRule.FromShare(0.15, DriveLimit.Traction, 1, 0.15).Limit);
        Assert.Equal(DriveLimit.Power, TowRule.FromShare(0.5, DriveLimit.None, 1, 0.15).Limit);
    }
}
