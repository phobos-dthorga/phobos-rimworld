using System.Diagnostics;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// The drivetrain design's worked example (docs/development/vehicle-drivetrain-design.md, prototype run of 8 Nov 2026), reproduced
/// from the shipped archetypes (Drivetrains.xml). These are outputs of authored figures, not measurements: if the XML changes,
/// change these figures with it.
/// </summary>
public class DrivetrainTests
{
    // The heavy wheeled diesel archetype: the VVE Mule at Mass 240 × 12 kg, 30 kW per tonne.
    static DrivetrainSpec Mule(double drivenShare = 0.5) => new()
    {
        RatedPowerKw = 30 * 2.88,
        IdleRpm = 700, LaunchRpm = 1100, RatedRpm = 3000,
        Gears = new[] { 4.0, 2.4, 1.5, 1.0, 0.8 }, FinalDrive = 4.1, UpshiftRpm = 2400, DownshiftRpm = 1300,
        Efficiency = 0.88, WheelRadius = 0.38, DrivenShare = drivenShare, DragArea = 2.4,
        FrictionConstant = 0, IndicatedEfficiency = 0.40,
    };

    // The tracked diesel archetype: the VVE Bulldog at Mass 450 × 12 kg, 20 kW per tonne.
    static DrivetrainSpec Bulldog() => new()
    {
        RatedPowerKw = 20 * 5.4,
        IdleRpm = 650, LaunchRpm = 1000, RatedRpm = 2400,
        Gears = new[] { 5.0, 2.9, 1.8, 1.2, 0.85 }, FinalDrive = 6.0, UpshiftRpm = 2000, DownshiftRpm = 1100,
        Efficiency = 0.80, WheelRadius = 0.32, DrivenShare = 1, DragArea = 4.0,
        FrictionConstant = 0, IndicatedEfficiency = 0.40,
    };

    static DriveConditions On(double crr, double grip, double speedKmh, double massKg) => new()
    {
        Speed = speedKmh / 3.6, MassKg = massKg, RollingResistance = crr, Grip = grip, EngineRunning = true, ClutchCapacity = 1,
        DrivenWheelsWorking = 1,
    };

    [Fact]
    public void TheMuleCruisesInThirdOnAsphalt()
    {
        DriveResult r = DrivetrainModel.Evaluate(Mule(), On(0.012, 0.8, 36, 3744));
        Assert.Equal(3, r.Gear);
        Assert.InRange(r.Rpm, 1500, 1590);
        Assert.InRange(r.EnginePowerKw, 6.2, 7.0);
        Assert.InRange(r.Load, 0.11, 0.15);
        Assert.InRange(r.FuelPowerKw, 24, 28);
        Assert.InRange(r.HeatKw, 6.7, 7.8);
        Assert.Equal(1.0, r.Cap);
    }

    [Fact]
    public void OnSoilItWorksHarder()
    {
        DriveResult r = DrivetrainModel.Evaluate(Mule(), On(0.05, 0.6, 36, 3744));
        Assert.InRange(r.EnginePowerKw, 21, 24);
        Assert.InRange(r.Load, 0.40, 0.50);
        Assert.Equal(1.0, r.Cap);
    }

    [Fact]
    public void InLooseSandTwoWheelDriveBogsAndCrawls()
    {
        DriveResult r = DrivetrainModel.Evaluate(Mule(), On(0.25, 0.35, 36, 3744));
        Assert.Equal(DriveLimit.Traction, r.Limit);
        Assert.Equal(DrivetrainModel.CrawlFloor, r.Cap, 3);
    }

    [Fact]
    public void InLooseSandAllWheelDriveIsPowerLimitedNearTwentySixKmh()
    {
        DriveResult r = DrivetrainModel.Evaluate(Mule(drivenShare: 1), On(0.25, 0.35, 36, 3744));
        Assert.Equal(DriveLimit.Power, r.Limit);
        Assert.InRange(r.Cap, 0.66, 0.80);
    }

    [Fact]
    public void TheBulldogCruisesOnAsphaltAndTracksKeepGripInSand()
    {
        DriveResult road = DrivetrainModel.Evaluate(Bulldog(), On(0.012, 0.8, 26, 7020));
        Assert.Equal(1.0, road.Cap);
        Assert.InRange(road.Load, 0.05, 0.2);
        DriveResult sand = DrivetrainModel.Evaluate(Bulldog(), On(0.25, 0.35, 26, 7020));
        Assert.Equal(DriveLimit.Power, sand.Limit);
        Assert.InRange(sand.Cap, 0.55, 0.70);
    }

    [Fact]
    public void BelowFirstGearsLaunchSpeedTheClutchSlips()
    {
        DriveResult r = DrivetrainModel.Evaluate(Mule(), On(0.012, 0.8, 5, 3744));
        Assert.Equal(1, r.Gear);
        Assert.True(r.ClutchSlipping);
        Assert.Equal(1100, r.Rpm, 0);
    }

    [Fact]
    public void TheCurrentGearIsKeptBetweenTheShiftLines()
    {
        DrivetrainSpec spec = Mule();
        // At 36 km/h the free choice is 3rd; a vehicle already in 2nd stays there while 2nd stays under the upshift line.
        int free = DrivetrainModel.ChooseGear(spec, 10, 7, 5, current: 0);
        Assert.Equal(3, free);
        int kept = DrivetrainModel.ChooseGear(spec, 9, 7, 5, current: 2);
        Assert.Equal(2, kept);
    }

    [Fact]
    public void ADamagedGearboxLosesItsTopGears()
    {
        DriveConditions c = On(0.012, 0.8, 50, 3744);
        c.UsableGears = 2;
        DriveResult r = DrivetrainModel.Evaluate(Mule(), c);
        Assert.True(r.Gear <= 2);
        Assert.True(r.Cap < 1, "redline in 2nd caps the speed");
    }

    [Fact]
    public void IdlingUsesFuelButMakesNoDrivePower()
    {
        DriveConditions c = On(0.012, 0.8, 0, 3744);
        DriveResult r = DrivetrainModel.Evaluate(Mule(), c);
        Assert.Equal(700, r.Rpm);
        Assert.Equal(0, r.EnginePowerKw);
        Assert.True(r.FuelPowerKw > 0);
        c.EngineRunning = false;
        Assert.Equal(0, DrivetrainModel.Evaluate(Mule(), c).FuelPowerKw);
    }

    [Fact]
    public void ABoatFollowsThePropellerLaw()
    {
        var boat = new DrivetrainSpec { Transmission = TransmissionKind.Propeller, RatedPowerKw = 50, RatedRpm = 2800, IdleRpm = 700, DesignSpeed = 5 };
        DriveResult r = DrivetrainModel.Evaluate(boat, On(0, 1, 5 * 3.6, 1000));
        Assert.InRange(r.Load, 0.60, 0.63);
    }

    [Fact]
    public void ATurbineBurnsMostOfItsFuelIdling()
    {
        var tank = Bulldog();
        tank.IdleFuelShare = 0.6;
        double idle = DrivetrainModel.FuelPowerKw(tank, 0, 1000);
        double full = DrivetrainModel.FuelPowerKw(tank, tank.RatedPowerKw, 2400);
        Assert.Equal(0.6, idle / full, 3);
    }

    [Fact]
    public void TerrainCurvesFollowTheTables()
    {
        Assert.Equal(0.012, DrivetrainModel.RollingResistanceForPathCost(0), 3);
        Assert.Equal(0.25, DrivetrainModel.RollingResistanceForPathCost(14), 3);
        Assert.Equal(0.35, DrivetrainModel.GripForPathCost(14), 3);
    }

    [Fact]
    public void FiveHundredSamplesStayCheap()
    {
        DrivetrainSpec spec = Mule();
        var watch = Stopwatch.StartNew();
        double sum = 0;
        for (int i = 0; i < 500; i++)
        {
            sum += DrivetrainModel.Evaluate(spec, On(i % 2 == 0 ? 0.012 : 0.25, i % 2 == 0 ? 0.8 : 0.35, 20 + i % 20, 3744)).Load;
        }
        watch.Stop();
        Assert.True(sum > 0);
        // Generous for a shared CI runner: 500 vehicles sampled once (one second of game time) well under a frame.
        Assert.True(watch.Elapsed.TotalMilliseconds < 50, $"500 samples took {watch.Elapsed.TotalMilliseconds} ms");
    }
}

/// <summary>The turbine design's tables (docs/development/vehicle-turbine-design.md), from the shipped Harrier and helicopter figures.</summary>
public class TurbineTests
{
    static TurbineSpec Pegasus() => new()
    {
        Ratings =
        {
            new TurbineRating { Name = "shortLiftDry", Share = 1.34, ThrustKn = 86.3, TemperatureLimit = 900 },
            new TurbineRating { Name = "wet", Share = 1.49, ThrustKn = 95.9, TemperatureLimit = 950, Wet = true, Minutes = 0.25 },
        },
    };

    [Theory]
    [InlineData(15, 1.0, 1.15, true)]
    [InlineData(30, 0.9, 1.0, false)]
    [InlineData(40, 0.8, 0.95, false)]
    public void TheHarrierHoverMarginFollowsTheTable(double ambient, double dryLow, double wetHigh, bool landsDry)
    {
        TurbineSpec spec = Pegasus();
        spec.Ratings.Insert(0, new TurbineRating { Name = "maxContinuous", ThrustKn = 64.3, TemperatureLimit = 800 });
        (double dry, double wet) = TurbineModel.HoverMargin(spec, 8594, ambient, 1);
        Assert.Equal(landsDry, dry >= 1);
        Assert.InRange(dry, dryLow - 0.1, dryLow + 0.15);
        Assert.True(wet <= wetHigh + 0.05);
        if (ambient == 30) Assert.True(wet >= 1, "at 30 °C water lets it land");
        if (ambient == 40) Assert.True(wet < 1, "at 40 °C it cannot land vertically");
    }

    [Fact]
    public void HelicopterHoverPowerRisesWithHeatAndOneEngineCannotHold()
    {
        Assert.Equal(0.64, TurbineModel.HoverPowerNeeded(0.9, 0.8, 15), 2);
        Assert.Equal(0.67, TurbineModel.HoverPowerNeeded(0.9, 0.8, 40), 2);
        var spec = new TurbineSpec();
        Assert.Equal(0.825, TurbineModel.Derate(spec, 40), 3);
        Assert.Equal(0.454, 0.5 * 1.1 * TurbineModel.Derate(spec, 40), 2);
    }

    [Fact]
    public void SpoolAndTemperatureRiseWithDemand()
    {
        var spec = new TurbineSpec { Ratings = { new TurbineRating { Name = "takeoff", Share = 1.1, TemperatureLimit = 815, Minutes = 5 } } };
        spec.Ratings[0].TemperatureLimit = 775;
        TurbineState idle = TurbineModel.Evaluate(spec, 0, 15, 1, 2, 2, true);
        Assert.Equal(60, idle.SpeedPercent, 1);
        Assert.Equal(378, idle.Temperature, 0);
        TurbineState full = TurbineModel.Evaluate(spec, 1, 15, 1, 2, 2, true);
        Assert.Equal(100, full.SpeedPercent, 1);
        Assert.Equal(775, full.Temperature, 0);
        TurbineState over = TurbineModel.Evaluate(spec, 1.05, 15, 1, 2, 2, true);
        Assert.Equal("takeoff", over.Rating);
        Assert.True(over.OverContinuous > 0 && over.OverTop < 0);
        TurbineState oneOut = TurbineModel.Evaluate(spec, 0.6, 15, 1, 1, 2, true);
        Assert.True(oneOut.OverContinuous > 0, "one engine carrying the load runs hot");
    }

    [Fact]
    public void LifeIsUsedOnlyAboveTheLimit()
    {
        Assert.Equal(0, TurbineModel.LifePerHour(-10));
        Assert.Equal(0.02, TurbineModel.LifePerHour(25), 6);
        Assert.Equal(0.6, TurbineModel.FuelFraction(0.6, 0), 6);
        Assert.Equal(1, TurbineModel.FuelFraction(0.6, 1), 6);
    }
}
