using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

/// <summary>
/// Power and lift (docs/development/vehicle-power-and-lift-design.md): the diesel-electric transmission, the rotor drive's limit and
/// over-torque, and thrust-lifted craft losing thrusters. The figures are authored test cases, not a real vehicle's.
/// </summary>
public class PowerAndLiftTests
{
    static DrivetrainSpec Shunter() => new()
    {
        Engine = EngineKind.Piston, Transmission = TransmissionKind.DieselElectric,
        IdleRpm = 450, RatedRpm = 1000, RatedPowerKw = 300, Efficiency = 0.8,
        MaxTractiveEffortN = 150_000, ContinuousTractiveEffortN = 100_000, DrivenShare = 1, DragArea = 8,
    };

    static DriveConditions OnRails(double speed, double massKg, double acceleration = 0, double grade = 0) => new()
    {
        Speed = speed, MassKg = massKg, Acceleration = acceleration, Grade = grade, RollingResistance = 0.002, Grip = 0.3,
        EngineRunning = true, ClutchCapacity = 1, DrivenWheelsWorking = 1,
    };

    [Fact]
    public void ADieselElectricRunsItsEngineAtThePowerAskedNotTheWheelSpeed()
    {
        DriveResult r = DrivetrainModel.Evaluate(Shunter(), OnRails(10, 50_000));
        Assert.Equal(DriveLimit.None, r.Limit);
        Assert.Equal(0, r.Gear);
        Assert.InRange(r.Load, 0.01, 0.1);
        Assert.Equal(450 + 550 * r.Load, r.Rpm, 3);
    }

    [Fact]
    public void AHardStartIsHeldToTheMotorsPullAndHeatsThem()
    {
        // 50 t asked to gain 4 m/s² at walking pace: 200 kN, more than both the rails' grip (0.3 × 50 t × g, 147 kN) and the motors'
        // 150 kN, so the vehicle is held back and the motors pull past their continuous 100 kN.
        DriveResult r = DrivetrainModel.Evaluate(Shunter(), OnRails(1.5, 50_000, acceleration: 4));
        Assert.True(r.Cap < 1);
        Assert.True(r.TractiveForceN <= 150_000);
        Assert.True(DrivetrainModel.TractionMotorLoad(Shunter(), r.TractiveForceN) > 1, "pulling past the continuous rating heats the motors");
    }

    [Fact]
    public void OnAGradeTheEnginesPowerSetsTheSpeed()
    {
        // 50 t up 2%: rolling 1.0 kN, grade 9.8 kN, drag 4.8 v² N. 300 kW × 0.8 gives 240 kW at the wheels, which holds about
        // 19 m/s (12.5 kN × 19.1 m/s), well short of the 30 asked.
        DriveResult r = DrivetrainModel.Evaluate(Shunter(), OnRails(30, 50_000, grade: 0.02));
        Assert.Equal(DriveLimit.Power, r.Limit);
        Assert.InRange(r.Cap * 30, 18.5, 20);
        Assert.Equal(1, r.Load, 3);
        Assert.Equal(1000, r.Rpm, 3);
    }

    [Fact]
    public void TheRotorDriveLimitsLiftAndMarksOverTorque()
    {
        var spec = new TurbineSpec { DriveLimitShare = 0.9 };
        Assert.Equal(1.0 / 0.9 - 1, TurbineModel.OverTorque(spec, 1.0), 6);
        Assert.True(TurbineModel.OverTorque(spec, 0.8) < 0);
        Assert.Equal(-1, TurbineModel.OverTorque(new TurbineSpec(), 5), 6);
        // Two healthy engines at 15 °C could give 1.0; the drive takes only 0.9.
        Assert.Equal(0.9, TurbineModel.LiftPowerAvailable(spec, 15, 1, 1), 6);
        // One engine out: the engines (0.5) limit, not the drive.
        Assert.Equal(0.5, TurbineModel.LiftPowerAvailable(spec, 15, 1, 0.5), 6);
        TurbineState state = TurbineModel.Evaluate(spec, 1.0, 15, 1, 2, 2, true);
        Assert.True(state.OverTorque > 0);
    }

    [Fact]
    public void APodLosingThrustersLosesLift()
    {
        // Built to lift 1.5 times its maximum gross weight of 10 t on all four thrusters (authored).
        double thrust = TurbineModel.ThrustForLiftToWeight(1.5, 10_000);
        Assert.Equal(147.15, thrust, 2);
        var spec = new TurbineSpec { DeratePerDegree = 0, Ratings = { new TurbineRating { Name = "full", ThrustKn = thrust } } };
        spec.Ratings.RemoveAt(0);
        Assert.Equal(1.5, TurbineModel.HoverMargin(spec, 10_000, 40, 1).dry, 3);
        Assert.Equal(1.125, TurbineModel.HoverMargin(spec, 10_000, 15, 0.75).dry, 3);
        Assert.True(TurbineModel.HoverMargin(spec, 10_000, 15, 0.5).dry < 1, "two of four out at full load: it cannot lift");
        Assert.True(TurbineModel.HoverMargin(spec, 6_000, 15, 0.5).dry >= 1, "lightened, it can");
    }

    // The traction motors system's figures (Systems_PowerAndLift.xml), fitted to a US Army manual's short-time ratings for a diesel
    // switcher: 1.17, 1.33 and 1.6 times the continuous current for 20, 10 and 5 minutes (TM 55-2210-223-12).
    const double MotorHeat = 910, MotorCooling = 7, Air = 20, Overheating = 180;

    static double MinutesToOverheat(double ratio)
    {
        double temperature = Air;
        var flows = new List<FlowSample>
        {
            new() { Quantity = 0, Kind = FlowKind.Into, Rate = MotorHeat * ratio * ratio },
            new() { Quantity = 0, Kind = FlowKind.Toward, Rate = MotorCooling, Target = Air },
        };
        for (int minute = 1; minute <= 120; minute++)
        {
            var values = new[] { temperature };
            SystemIntegrator.Advance(values, new[] { -60.0 }, new[] { 300.0 }, flows, 1 / 60.0);
            temperature = values[0];
            if (temperature >= Overheating) return minute;
        }
        return double.PositiveInfinity;
    }

    [Theory]
    [InlineData(1.17, 16, 24)]
    [InlineData(1.33, 8, 12)]
    [InlineData(1.6, 4, 7)]
    public void TractionMotorsOverheatAtTheManualsShortTimeRatings(double ratio, double earliest, double latest) =>
        Assert.InRange(MinutesToOverheat(ratio), earliest, latest);

    [Fact]
    public void AtTheContinuousRatingTheMotorsNeverOverheat() => Assert.True(double.IsPositiveInfinity(MinutesToOverheat(1)));

    // The L-ATV's real figures (mods/PhobosVehicles/Defs/Drivetrains_RealVehicles.xml): 254 kW at 3,200 rpm, the Allison 2500's
    // ratios, a 9.42 final drive worked out from its top speed, 0.55 m wheels, 6,400 kg kerb, permanent four-wheel drive.
    static DrivetrainSpec Latv() => new()
    {
        IdleRpm = 700, LaunchRpm = 1325, RatedRpm = 3200, RatedPowerKw = 254, Gears = new[] { 3.51, 1.90, 1.44, 1.00, 0.74, 0.64 },
        FinalDrive = 9.42, UpshiftRpm = 2624, DownshiftRpm = 1440, Efficiency = 0.88, WheelRadius = 0.55, DrivenShare = 1, DragArea = 5.0,
    };

    static DriveConditions Ground(double speed, double massKg, double crr, double grip) => new()
    {
        Speed = speed, MassKg = massKg, RollingResistance = crr, Grip = grip, EngineRunning = true, ClutchCapacity = 1, DrivenWheelsWorking = 1,
    };

    [Fact]
    public void TheLatvCruisesFreelyOnARoadAtItsInGameSpeed()
    {
        // Vehicle Framework's 12.8 cells a second, laden with a crew of four and a ton of cargo.
        DriveResult r = DrivetrainModel.Evaluate(Latv(), Ground(12.8, 7_800, 0.012, 0.8));
        Assert.Equal(DriveLimit.None, r.Limit);
        Assert.InRange(r.Gear, 4, 6);
        Assert.InRange(r.Rpm, 1440, 2624);
        Assert.True(r.Load < 0.5);
    }

    [Fact]
    public void TheLatvSlowsButNeverBogsInSoftSandWithAllWheelsDriving()
    {
        // At its full 2,313 kg payload in soft sand, four-wheel drive keeps the grip (0.35 x 8.7 t x g, 30 kN, over the 21 kN rolling
        // load), but 254 kW cannot hold 12.8 m/s against it: the engine, not the tyres, sets the pace.
        DriveResult r = DrivetrainModel.Evaluate(Latv(), Ground(12.8, 8_713, 0.25, 0.35));
        Assert.Equal(DriveLimit.Power, r.Limit);
        Assert.InRange(r.Cap, 0.6, 0.95);
    }

    // The boilers (Systems_Boiler.xml and the Trains of the Rim loadouts): pressure as a share of working pressure, fired in and
    // cooled toward nothing; steam drawn by the load.
    static double HoursToPressure(double firing, double cooling, double target)
    {
        double pressure = 0;
        for (int step = 1; step <= 24 * 60; step++)
        {
            var values = new[] { pressure };
            var flows = new List<FlowSample>
            {
                new() { Quantity = 0, Kind = FlowKind.Into, Rate = firing },
                new() { Quantity = 0, Kind = FlowKind.Toward, Rate = cooling, Target = 0 },
            };
            SystemIntegrator.Advance(values, new[] { 0.0 }, new[] { 1.06 }, flows, 1 / 60.0);
            pressure = values[0];
            if (pressure >= target) return step / 60.0;
        }
        return double.PositiveInfinity;
    }

    [Theory]
    [InlineData(0.4, 0.5, 1.1, 1.6)]    // the tank engine moves after about 1 1/3 hours
    [InlineData(0.4, 1.0, 2.5, 3.1)]    // and is at full pressure in under 3 (the GCR's 3 to 7 hours to be ready)
    [InlineData(0.17, 0.5, 3.0, 3.7)]   // the Frisco moves after about 3 1/3 hours
    [InlineData(0.17, 1.0, 7.5, 8.5)]   // and is at full pressure in about 8 (6 to 10 quoted for big engines)
    public void BoilersRaiseSteamInTheirSourcedTimes(double firing, double target, double earliest, double latest) =>
        Assert.InRange(HoursToPressure(firing, 0.08, target), earliest, latest);

    [Theory]
    [InlineData(0.2, 1.4, 2.0)]   // the tank engine's tanks: about 1 2/3 hours of hard work
    [InlineData(0.11, 2.7, 3.3)]  // the Frisco's tender: about 3 hours
    public void FeedWaterLastsAsTheResearchSays(double feedShare, double earliest, double latest)
    {
        // At full load the glass loses 3 an hour and the injector, keeping it near 0.8, takes that much from the feed water times
        // the share the glass is of it.
        double hours = 1 / (3 * feedShare);
        Assert.InRange(hours, earliest, latest);
    }
}
