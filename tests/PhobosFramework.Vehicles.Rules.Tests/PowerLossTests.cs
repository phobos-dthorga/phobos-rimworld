using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>Step S4 (Framework 0.130.0): a fault takes power from the engine, so the vehicle slows only where power runs short.</summary>
public class PowerLossTests
{
    // The heavy wheeled diesel archetype, as DrivetrainTests has it.
    static DrivetrainSpec Mule() => new()
    {
        RatedPowerKw = 30 * 2.88,
        IdleRpm = 700, LaunchRpm = 1100, RatedRpm = 3000,
        Gears = new[] { 4.0, 2.4, 1.5, 1.0, 0.8 }, FinalDrive = 4.1, UpshiftRpm = 2400, DownshiftRpm = 1300,
        Efficiency = 0.88, WheelRadius = 0.38, DrivenShare = 0.5, DragArea = 2.4,
        FrictionConstant = 0, IndicatedEfficiency = 0.40,
    };

    static DriveConditions On(double speedKmh, double grade, double powerLoss) => new()
    {
        Speed = speedKmh / 3.6, MassKg = 3744, RollingResistance = 0.012, Grip = 0.8, Grade = grade, EngineRunning = true, ClutchCapacity = 1,
        DrivenWheelsWorking = 1, PowerLoss = powerLoss,
    };

    [Fact]
    public void NoLossChangesNothing()
    {
        DriveResult without = DrivetrainModel.Evaluate(Mule(), On(36, 0, 0));
        DriveResult zero = DrivetrainModel.Evaluate(Mule(), On(36, 0, 0.0));
        Assert.Equal(without.Cap, zero.Cap, 9);
        Assert.Equal(without.FuelPowerKw, zero.FuelPowerKw, 9);
    }

    [Fact]
    public void OnTheFlatAtACruiseALossDoesNotSlowIt()
    {
        DriveResult sound = DrivetrainModel.Evaluate(Mule(), On(36, 0, 0));
        DriveResult choked = DrivetrainModel.Evaluate(Mule(), On(36, 0, 0.25));
        Assert.Equal(1.0, choked.Cap);
        Assert.Equal(sound.EnginePowerKw, choked.EnginePowerKw, 6);
        // The engine works harder for the same road: its load is a bigger share of what it has left.
        Assert.True(choked.Load > sound.Load);
    }

    [Fact]
    public void OnAClimbALossHoldsItSlower()
    {
        DriveResult sound = DrivetrainModel.Evaluate(Mule(), On(60, 0.12, 0));
        DriveResult choked = DrivetrainModel.Evaluate(Mule(), On(60, 0.12, 0.25));
        Assert.True(sound.Cap < 1, "the test needs a climb the sound engine is already held on");
        Assert.True(choked.Cap < sound.Cap);
        Assert.Equal(DriveLimit.Power, choked.Limit);
    }

    [Fact]
    public void WhatTheEngineGivesToOtherThingsComesOffDriving()
    {
        // The engine-load ledger (Framework 0.131.0): 15 kW to a fan and an air conditioner holds a climbing truck slower.
        DriveConditions bare = On(60, 0.12, 0);
        DriveConditions loaded = On(60, 0.12, 0);
        loaded.AccessoryKw = 15;
        DriveResult without = DrivetrainModel.Evaluate(Mule(), bare);
        DriveResult with = DrivetrainModel.Evaluate(Mule(), loaded);
        Assert.True(with.Cap < without.Cap);
    }

    [Fact]
    public void OnTheFlatTheSameLoadsOnlyMakeTheEngineWorkHarder()
    {
        DriveConditions loaded = On(36, 0, 0);
        loaded.AccessoryKw = 15;
        DriveResult without = DrivetrainModel.Evaluate(Mule(), On(36, 0, 0));
        DriveResult with = DrivetrainModel.Evaluate(Mule(), loaded);
        Assert.Equal(1.0, with.Cap);
        Assert.True(with.Load > without.Load);
    }

    [Fact]
    public void ADieselElectricLosesItsPowerTheSameWay()
    {
        var spec = new DrivetrainSpec
        {
            Transmission = TransmissionKind.DieselElectric, RatedPowerKw = 300, IdleRpm = 450, LaunchRpm = 450, RatedRpm = 1000,
            Efficiency = 0.8, WheelRadius = 0.5, DrivenShare = 1, DragArea = 8,
        };
        var c = new DriveConditions
        {
            Speed = 20, MassKg = 36000, RollingResistance = 0.002, Grip = 0.3, Grade = 0.02, EngineRunning = true, ClutchCapacity = 1,
            DrivenWheelsWorking = 1,
        };
        DriveResult sound = DrivetrainModel.Evaluate(spec, c);
        c.PowerLoss = 0.25;
        DriveResult choked = DrivetrainModel.Evaluate(spec, c);
        Assert.True(choked.Cap < sound.Cap);
    }
}
