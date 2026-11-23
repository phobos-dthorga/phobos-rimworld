using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Descents, retarders and brake heat (Framework 0.251.0; docs/development/world-elevation-and-descents.md).</summary>
public class DescentRuleTests
{
    [Fact]
    public void AHillClimbsForHalfALegAndDescendsForTheOtherHalf()
    {
        Assert.Equal(0.05, DescentRule.LegGrade(0.05, 0.2, 0), 9);
        Assert.Equal(-0.05, DescentRule.LegGrade(0.05, 0.7, 0), 9);
        Assert.Equal(0, DescentRule.LegGrade(0, 0.7, 0), 9); // flat country stays flat
    }

    [Fact]
    public void TheNetSlopeLiesOnTopOfTheHills()
    {
        Assert.Equal(0.06, DescentRule.LegGrade(0.05, 0.2, 0.01), 9);
        Assert.Equal(-0.06, DescentRule.LegGrade(0.05, 0.7, -0.01), 9);
    }

    [Fact]
    public void TheNetSlopeIsTheHeightOverTheDistanceAndHeldInBounds()
    {
        Assert.Equal(0.01, DescentRule.NetSlope(100, 400, 30000), 9); // 300 m over a 30 km tile
        Assert.Equal(-DescentRule.MaxNetSlope, DescentRule.NetSlope(5000, 0, 30000), 9);
        Assert.Equal(0, DescentRule.NetSlope(0, 500, 0), 9);
    }

    [Fact]
    public void LegProgressIsWhatHasBeenPaid()
    {
        Assert.Equal(0.25, DescentRule.LegProgress(75, 100), 9);
        Assert.Equal(0, DescentRule.LegProgress(10, 0), 9);
    }

    [Fact]
    public void ACompressionBrakeHoldsAboutTheEnginesPowerAndAnExhaustBrakeHalf()
    {
        Assert.Equal(300, DescentRule.RetarderKw(300, 1, false), 9);
        Assert.Equal(150, DescentRule.RetarderKw(300, 0, true), 9);
        Assert.Equal(300, DescentRule.RetarderKw(300, 1, true), 9); // the stronger, not both
        Assert.Equal(0, DescentRule.RetarderKw(300, 0, false), 9);
    }

    [Fact]
    public void ARetarderIsStrongestJustAboveRatedSpeed()
    {
        Assert.Equal(150, DescentRule.RetarderKwAt(300, 1050, 2100), 9);
        Assert.Equal(330, DescentRule.RetarderKwAt(300, 3000, 2100), 9);
    }

    [Fact]
    public void BrakesWarmByThePowerOverTheirHeatCapacity()
    {
        // 220 kW into a 36 t truck's brakes (230 kJ per degree): about 3,400 degrees an hour before any cooling.
        Assert.Equal(220 * 3600.0 / (DescentRule.HeatCapacityPerTonne * 36), DescentRule.HeatPerHour(220, 36000), 6);
        Assert.Equal(0, DescentRule.HeatPerHour(-5, 36000), 9);
    }

    [Fact]
    public void BrakesFadeOnlyPastTheirLiningsLimit()
    {
        Assert.Equal(0, DescentRule.FadeLoss(250), 9);
        Assert.Equal(DescentRule.FadeMostLoss / 2, DescentRule.FadeLoss((DescentRule.FadeStarts + DescentRule.FadeWorst) / 2), 9);
        Assert.Equal(DescentRule.FadeMostLoss, DescentRule.FadeLoss(900), 9);
    }

    // ---- The overrun in the drivetrain model ----

    static DriveConditions Descent(double grade, double speed = 12, double mass = 17000) => new()
    {
        EngineRunning = true,
        Speed = speed,
        MassKg = mass,
        RollingResistance = 0.012,
        Grip = 0.8,
        ClutchCapacity = 1,
        DrivenWheelsWorking = 1,
        Grade = grade,
    };

    static DrivetrainSpec Truck() => new() { RatedPowerKw = 224, RatedRpm = 2100, IdleRpm = 600, Gears = new[] { 4.0, 2.4, 1.5, 1.0, 0.8 } };

    [Fact]
    public void OnADescentTheEngineIsDrivenAndBurnsNothing()
    {
        DriveResult r = DrivetrainModel.Evaluate(Truck(), Descent(-0.08));
        Assert.True(r.Overrun);
        Assert.Equal(0, r.FuelPowerKw, 9);
        Assert.True(r.RetardedKw > 0);
        Assert.True(r.BrakeKw > 0); // a loaded truck on 8 % needs its brakes as well as its engine
    }

    [Fact]
    public void ARetarderTakesLoadOffTheBrakes()
    {
        DriveConditions plain = Descent(-0.08);
        DriveConditions braked = plain;
        braked.RetarderKwAtRated = DescentRule.RetarderKw(224, 1, false);
        Assert.True(DrivetrainModel.Evaluate(Truck(), braked).BrakeKw < DrivetrainModel.Evaluate(Truck(), plain).BrakeKw);
    }

    [Fact]
    public void FadedBrakesSlowTheVehicle()
    {
        DriveConditions hot = Descent(-0.08);
        hot.BrakeFadeLoss = 0.7;
        DriveResult r = DrivetrainModel.Evaluate(Truck(), hot);
        Assert.Equal(DriveLimit.Brakes, r.Limit);
        Assert.True(r.Cap < 1);
        Assert.Equal(1, DrivetrainModel.Evaluate(Truck(), Descent(-0.08)).Cap, 9);
    }

    [Fact]
    public void AGentleDescentOnSoftGroundNeedsNoOverrun()
    {
        DriveConditions soft = Descent(-0.02);
        soft.RollingResistance = 0.08;
        Assert.False(DrivetrainModel.Evaluate(Truck(), soft).Overrun);
    }

    [Fact]
    public void AnElectricDriveRegeneratesOnADescent()
    {
        var spec = new DrivetrainSpec { Engine = EngineKind.Electric, Transmission = TransmissionKind.Direct, RatedPowerKw = 150, RatedRpm = 8000 };
        DriveConditions down = Descent(-0.06, 12, 2000);
        down.RegenShare = DescentRule.RegenShareOfRated;
        DriveResult r = DrivetrainModel.Evaluate(spec, down);
        Assert.True(r.Overrun);
        Assert.True(r.RegenKw > 0);
        Assert.Equal(0, r.FuelPowerKw, 9);
    }
}
