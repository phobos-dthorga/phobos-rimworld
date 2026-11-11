using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What drives the wheels (or the propeller, or nothing on the ground).</summary>
public enum EngineKind
{
    Piston,
    Electric,
    Turboshaft,
    Turbofan,
    /// <summary>Thrusters that hold a craft up on thrust alone (Vehicle Framework's drop pods): lift from a thrust-to-weight figure,
    /// no spool or turbine temperature.</summary>
    Thruster,
}

/// <summary>How engine speed relates to road speed.</summary>
public enum TransmissionKind
{
    /// <summary>Stepped gears chosen automatically by an up and down rpm pair (manual boxes follow the same rule: players don't shift).</summary>
    Automatic,
    /// <summary>One fixed reduction: electric motors.</summary>
    Direct,
    /// <summary>A fixed-pitch propeller: absorbed power rises with the cube of shaft speed.</summary>
    Propeller,
    /// <summary>No drive to the ground at all: aircraft engines, whose load comes from the flight phase.</summary>
    None,
    /// <summary>
    /// A diesel engine driving a generator, and traction motors on the axles: no gearbox. Engine speed follows the power asked of it
    /// (the throttle's notches), tractive effort is capped by the motors at low speed and by the engine's power above that, and the
    /// motors heat with their current (diesel-electric locomotives).
    /// </summary>
    DieselElectric,
}

/// <summary>Why the model is holding a vehicle below the speed Vehicle Framework intends.</summary>
public enum DriveLimit
{
    None,
    Power,
    Traction,
}

/// <summary>One vehicle's drivetrain in SI units, built from its Def, its loadout's overrides and its own mass. Game-free.</summary>
public sealed class DrivetrainSpec
{
    public EngineKind Engine = EngineKind.Piston;
    public TransmissionKind Transmission = TransmissionKind.Automatic;
    public double IdleRpm = 700, LaunchRpm = 1100, RatedRpm = 3000;
    public double RatedPowerKw = 80;
    /// <summary>Normalised torque against rpm ÷ rated rpm: x and y points of a piecewise-linear curve.</summary>
    public double[] TorqueX = { 0, 0.25, 0.5, 0.65, 0.85, 1, 1.1 };
    public double[] TorqueY = { 0, 0.6, 0.95, 1, 0.95, 0.85, 0 };
    public double FrictionConstant = 0.03, FrictionLinear = 0.04, FrictionQuadratic = 0.08;
    public double IndicatedEfficiency = 0.40;
    public double CoolantHeatShare = 0.28;
    /// <summary>For turbines: the fuel used at idle as a share of full-power fuel (a Willans shape with a high floor).</summary>
    public double IdleFuelShare;
    public double[] Gears = { 4.0, 2.4, 1.5, 1.0, 0.8 };
    public double FinalDrive = 4.1;
    public double UpshiftRpm = 2400, DownshiftRpm = 1300;
    public double Efficiency = 0.88;
    public double WheelRadius = 0.38;
    public double DrivenShare = 0.5;
    public double DragArea = 2.4;
    /// <summary>The speed in m/s Vehicle Framework gives at full health on good ground: boats run 85% of rated rpm there.</summary>
    public double DesignSpeed = 10;
    /// <summary>Diesel-electric: the most the traction motors can pull, in newtons (starting tractive effort); 0 for no cap.</summary>
    public double MaxTractiveEffortN;
    /// <summary>Diesel-electric: the pull the traction motors can hold indefinitely, in newtons; above it they heat (0: not rated).</summary>
    public double ContinuousTractiveEffortN;

    /// <summary>Torque at rated rpm such that power there equals rated power.</summary>
    public double ReferenceTorque => RatedPowerKw * 1000 / (RatedRpm * Math.PI / 30) / Math.Max(0.01, Curve(TorqueX, TorqueY, 1));

    public static double Curve(double[] x, double[] y, double at)
    {
        if (at <= x[0]) return y[0];
        for (int i = 1; i < x.Length; i++)
        {
            if (at <= x[i]) return y[i - 1] + (y[i] - y[i - 1]) * (at - x[i - 1]) / (x[i] - x[i - 1]);
        }
        return y[y.Length - 1];
    }
}

/// <summary>What the vehicle is doing at the moment of a sample.</summary>
public struct DriveConditions
{
    /// <summary>The speed Vehicle Framework intends, in m/s (measured speed ÷ the cap already applied).</summary>
    public double Speed;
    public double Acceleration;
    public double MassKg;
    /// <summary>A trailer's mass behind the hitch (docs/development/vehicle-hitch-design.md): it must be rolled, speeded up and lifted
    /// with the vehicle, but puts no weight on the driven wheels (a full trailer stands on its own axles). Air drag is unchanged, a
    /// simplification: the trailer runs in the vehicle's wake.</summary>
    public double TowedMassKg;
    public double RollingResistance;
    /// <summary>Peak tyre-ground friction coefficient.</summary>
    public double Grip;
    /// <summary>Rise over run, for caravans in hills.</summary>
    public double Grade;
    public bool EngineRunning;
    /// <summary>The gear in use since the last sample (1-based), for hysteresis; 0 for none.</summary>
    public int CurrentGear;
    /// <summary>Gears the gearbox can still use (damage takes the top ones away).</summary>
    public int UsableGears;
    /// <summary>The share of rated torque the clutch can still pass, 0 to 1.</summary>
    public double ClutchCapacity;
    /// <summary>The share of the driven wheels still driving, 0 to 1.</summary>
    public double DrivenWheelsWorking;
}

/// <summary>What the model works out from a sample. Derived, never saved.</summary>
public struct DriveResult
{
    public int Gear;
    public double Rpm;
    public double Load;
    public double EnginePowerKw;
    public double DrivePowerKw;
    public double FuelPowerKw;
    public double HeatKw;
    /// <summary>The share of Vehicle Framework's speed the vehicle can hold, from the crawl floor to 1.</summary>
    public double Cap;
    public DriveLimit Limit;
    public bool ClutchSlipping;
    /// <summary>The pull at the driven wheels, newtons.</summary>
    public double TractiveForceN;
}

/// <summary>
/// The quasi-static (backward-facing) drivetrain: from the speed a vehicle is moving at, back through the wheels, the gearbox and
/// the engine to gear, rpm, load, fuel power and heat, as in Guzzella and Sciarretta's method and NREL's ADVISOR. Nothing is
/// integrated, so it cannot go unstable and costs the same at any game speed. Design: docs/development/vehicle-drivetrain-design.md.
/// Game-free: compiled into the tests without the game. Every default figure is authored unless the design names a source.
/// </summary>
public static class DrivetrainModel
{
    public const double Gravity = 9.81;
    public const double AirDensity = 1.2;
    /// <summary>A vehicle that cannot get traction still crawls at this share of its speed (owner, 8 Nov 2026: crawl, never stop).</summary>
    public const double CrawlFloor = 0.15;
    /// <summary>Torque in hand a gear must keep to be chosen (10%, as the design says).</summary>
    public const double TorqueReserve = 1.1;
    /// <summary>The highest rpm an engine is held at before the cap binds (10% over rated).</summary>
    public const double RevLimitShare = 1.1;

    /// <summary>The force the road takes at a speed, in newtons.</summary>
    public static double RoadLoad(DrivetrainSpec spec, in DriveConditions c, double speed) =>
        c.RollingResistance * TrainKg(c) * Gravity + 0.5 * AirDensity * spec.DragArea * speed * speed + TrainKg(c) * (c.Acceleration + Gravity * c.Grade);

    /// <summary>The whole train's mass: the vehicle and anything it tows.</summary>
    public static double TrainKg(in DriveConditions c) => c.MassKg + Math.Max(0, c.TowedMassKg);

    public static double WheelRpm(DrivetrainSpec spec, double speed) => speed / (2 * Math.PI * Math.Max(0.05, spec.WheelRadius)) * 60;

    public static double EngineRpm(DrivetrainSpec spec, double speed, int gear) =>
        WheelRpm(spec, speed) * spec.FinalDrive * (spec.Transmission == TransmissionKind.Automatic ? spec.Gears[gear - 1] : 1);

    /// <summary>Power the engine can give at an rpm, in kW (electric: constant torque to rated rpm, then constant power).</summary>
    public static double AvailablePowerKw(DrivetrainSpec spec, double rpm)
    {
        if (rpm <= 0) return 0;
        if (spec.Engine == EngineKind.Electric) return spec.RatedPowerKw * Math.Min(1, rpm / spec.RatedRpm);
        double torque = spec.ReferenceTorque * DrivetrainSpec.Curve(spec.TorqueX, spec.TorqueY, rpm / spec.RatedRpm);
        return torque * rpm * Math.PI / 30 / 1000;
    }

    /// <summary>Friction and pumping power at an rpm, kW: a constant, a linear and a quadratic share of rated power (authored).</summary>
    public static double FrictionKw(DrivetrainSpec spec, double rpm)
    {
        double x = rpm / spec.RatedRpm;
        return spec.RatedPowerKw * (spec.FrictionConstant + spec.FrictionLinear * x + spec.FrictionQuadratic * x * x);
    }

    /// <summary>Fuel power for an engine power at an rpm: the Willans line (pistons) or an idle floor (turbines), kW.</summary>
    public static double FuelPowerKw(DrivetrainSpec spec, double engineKw, double rpm)
    {
        if (spec.Engine == EngineKind.Electric) return engineKw / Math.Max(0.5, spec.IndicatedEfficiency);
        if (spec.IdleFuelShare > 0)
        {
            double fullFuel = spec.RatedPowerKw / Math.Max(0.05, spec.IndicatedEfficiency);
            return fullFuel * (spec.IdleFuelShare + (1 - spec.IdleFuelShare) * Math.Min(1, Math.Max(0, engineKw / spec.RatedPowerKw)));
        }
        return (Math.Max(0, engineKw) + FrictionKw(spec, rpm)) / Math.Max(0.05, spec.IndicatedEfficiency);
    }

    /// <summary>Works the sample through. A stopped vehicle with its engine running idles; with it off, everything is zero.</summary>
    public static DriveResult Evaluate(DrivetrainSpec spec, in DriveConditions c)
    {
        var result = new DriveResult { Cap = 1, Limit = DriveLimit.None };
        if (!c.EngineRunning)
        {
            result.Cap = 1;
            return result;
        }
        if (spec.Transmission == TransmissionKind.None || c.Speed <= 0.05)
        {
            Idle(spec, ref result);
            return result;
        }
        double speed = c.Speed;
        double force = RoadLoad(spec, c, speed);
        double wheelKw = Math.Max(0, force * speed / 1000);
        double requiredKw = wheelKw / Math.Max(0.1, spec.Efficiency);
        result.DrivePowerKw = wheelKw;

        // Traction: the driven wheels can push at most grip × their share of the weight.
        double tractionLimit = c.Grip * spec.DrivenShare * Clamp01(c.DrivenWheelsWorking) * c.MassKg * Gravity;
        double tractionCap = 1;
        if (force > tractionLimit)
        {
            double rolling = c.RollingResistance * TrainKg(c) * Gravity + TrainKg(c) * (c.Acceleration + Gravity * c.Grade);
            double spare = tractionLimit - rolling;
            tractionCap = spare <= 0 ? 0 : Math.Sqrt(spare / (0.5 * AirDensity * Math.Max(0.01, spec.DragArea))) / speed;
        }

        result.TractiveForceN = Math.Min(force, tractionLimit);
        if (spec.Transmission == TransmissionKind.DieselElectric)
        {
            DieselElectric(spec, c, speed, force, requiredKw, tractionCap, ref result);
            return result;
        }

        if (spec.Transmission == TransmissionKind.Propeller)
        {
            double share = speed / Math.Max(0.1, spec.DesignSpeed) * 0.85;
            result.Gear = 0;
            result.Rpm = Math.Max(spec.IdleRpm, share * spec.RatedRpm);
            result.Load = Math.Min(1, share * share * share);
            result.EnginePowerKw = result.Load * spec.RatedPowerKw;
            result.Cap = share > 1 ? 1 / share : 1;
            if (result.Cap < 1) result.Limit = DriveLimit.Power;
            Finish(spec, ref result);
            return result;
        }

        // Gear and rpm.
        int gears = spec.Transmission == TransmissionKind.Automatic ? Math.Max(1, Math.Min(c.UsableGears <= 0 ? spec.Gears.Length : c.UsableGears, spec.Gears.Length)) : 1;
        double clutch = c.ClutchCapacity <= 0 ? 1 : Clamp01(c.ClutchCapacity);
        int gear = ChooseGear(spec, speed, requiredKw, gears, c.CurrentGear, clutch);
        double rpm = EngineRpm(spec, speed, gear);
        if (spec.Transmission == TransmissionKind.Automatic && gear == 1 && rpm < spec.LaunchRpm)
        {
            rpm = spec.LaunchRpm;
            result.ClutchSlipping = true;
        }
        result.Gear = spec.Transmission == TransmissionKind.Automatic ? gear : 0;
        result.Rpm = Math.Max(spec.Engine == EngineKind.Electric ? 0 : spec.IdleRpm, rpm);
        double availableKw = AvailablePowerKw(spec, result.Rpm) * clutch;
        result.EnginePowerKw = Math.Min(requiredKw, availableKw);
        result.Load = availableKw <= 0 ? 1 : Math.Min(1, requiredKw / availableKw);

        // Power: the highest speed at which some gear can supply the road load.
        double powerCap = 1;
        if (requiredKw > BestAvailableKw(spec, speed, gears) * clutch)
        {
            powerCap = PowerLimitedSpeed(spec, c, speed, gears, clutch) / speed;
            if (clutch < 1 && powerCap < 1) result.ClutchSlipping = true;
        }
        double cap = Math.Min(powerCap, tractionCap);
        if (cap < 1)
        {
            result.Limit = tractionCap < powerCap ? DriveLimit.Traction : DriveLimit.Power;
            result.Cap = Math.Max(CrawlFloor, cap);
            result.Load = 1;
            result.EnginePowerKw = Math.Min(availableKw, requiredKw * result.Cap);
        }
        Finish(spec, ref result);
        return result;
    }

    /// <summary>
    /// The diesel-electric case: the wheels get the engine's rated power less the generator's and motors' losses at any speed, but no
    /// more pull than the traction motors' maximum; the engine runs at the power asked of it, its speed rising with the throttle from
    /// idle to rated (a straight line, authored: real throttles step through notches).
    /// </summary>
    static void DieselElectric(DrivetrainSpec spec, in DriveConditions c, double speed, double force, double requiredKw, double tractionCap,
        ref DriveResult result)
    {
        double usableKw = spec.RatedPowerKw * spec.Efficiency;
        double forceLimit = spec.MaxTractiveEffortN > 0 ? spec.MaxTractiveEffortN : double.MaxValue;
        double powerCap = 1;
        if (force * speed / 1000 > usableKw || force > forceLimit) powerCap = DieselElectricSpeed(spec, c, speed, usableKw, forceLimit) / speed;
        double cap = Math.Min(powerCap, tractionCap);
        result.Gear = 0;
        result.TractiveForceN = Math.Min(result.TractiveForceN, forceLimit);
        result.EnginePowerKw = Math.Min(spec.RatedPowerKw, requiredKw);
        if (cap < 1)
        {
            result.Limit = tractionCap < powerCap ? DriveLimit.Traction : DriveLimit.Power;
            result.Cap = Math.Max(CrawlFloor, cap);
            result.EnginePowerKw = Math.Min(spec.RatedPowerKw, Math.Max(requiredKw * result.Cap, result.TractiveForceN * speed * result.Cap / 1000 / Math.Max(0.1, spec.Efficiency)));
        }
        result.Load = Clamp01(result.EnginePowerKw / Math.Max(0.1, spec.RatedPowerKw));
        if (result.Limit != DriveLimit.None) result.Load = 1;
        result.Rpm = spec.IdleRpm + (spec.RatedRpm - spec.IdleRpm) * result.Load;
        Finish(spec, ref result);
    }

    /// <summary>A bisection (16 steps) for the highest speed a diesel-electric can hold within its power and its motors' pull.</summary>
    static double DieselElectricSpeed(DrivetrainSpec spec, in DriveConditions c, double intended, double usableKw, double forceLimit)
    {
        double low = 0, high = intended;
        for (int i = 0; i < 16; i++)
        {
            double mid = (low + high) / 2;
            double load = RoadLoad(spec, c, mid);
            if (load <= forceLimit && load * mid / 1000 <= usableKw) low = mid;
            else high = mid;
        }
        return low;
    }

    /// <summary>The traction motors' load: pull ÷ their continuous rating (above 1 they heat), 0 when not rated.</summary>
    public static double TractionMotorLoad(DrivetrainSpec spec, double tractiveForceN) =>
        spec.ContinuousTractiveEffortN > 0 ? Math.Max(0, tractiveForceN) / spec.ContinuousTractiveEffortN : 0;

    static void Idle(DrivetrainSpec spec, ref DriveResult result)
    {
        result.Rpm = spec.Engine == EngineKind.Electric ? 0 : spec.IdleRpm;
        result.Gear = 0;
        result.Load = 0;
        result.EnginePowerKw = 0;
        Finish(spec, ref result);
    }

    static void Finish(DrivetrainSpec spec, ref DriveResult result)
    {
        result.FuelPowerKw = FuelPowerKw(spec, result.EnginePowerKw, result.Rpm);
        result.HeatKw = result.FuelPowerKw * spec.CoolantHeatShare;
    }

    /// <summary>The highest usable gear with rpm at or above the downshift line, within the rev limit and with torque in hand; the
    /// current gear is kept while it still qualifies and stays below the upshift line (hysteresis, as Jolt's up and down pair).</summary>
    public static int ChooseGear(DrivetrainSpec spec, double speed, double requiredKw, int gears, int current, double clutch = 1)
    {
        if (spec.Transmission != TransmissionKind.Automatic) return 1;
        bool Ok(int g, bool keeping)
        {
            double rpm = EngineRpm(spec, speed, g);
            if (rpm > spec.RatedRpm * RevLimitShare) return false;
            if (g > 1 && rpm < spec.DownshiftRpm) return false;
            if (keeping && g < gears && rpm > spec.UpshiftRpm) return false;
            return AvailablePowerKw(spec, Math.Max(rpm, spec.LaunchRpm)) * clutch >= requiredKw * TorqueReserve;
        }
        if (current >= 1 && current <= gears && Ok(current, keeping: true)) return current;
        for (int g = gears; g >= 1; g--)
        {
            if (Ok(g, keeping: false)) return g;
        }
        // Nothing has torque in hand: the gear with the most power at this speed.
        int best = 1;
        double bestKw = -1;
        for (int g = 1; g <= gears; g++)
        {
            double rpm = EngineRpm(spec, speed, g);
            if (rpm > spec.RatedRpm * RevLimitShare) continue;
            double kw = AvailablePowerKw(spec, Math.Max(rpm, spec.LaunchRpm));
            if (kw > bestKw) { bestKw = kw; best = g; }
        }
        return best;
    }

    static double BestAvailableKw(DrivetrainSpec spec, double speed, int gears)
    {
        if (spec.Transmission != TransmissionKind.Automatic) return AvailablePowerKw(spec, EngineRpm(spec, speed, 1));
        double best = 0;
        for (int g = 1; g <= gears; g++)
        {
            double rpm = EngineRpm(spec, speed, g);
            if (rpm > spec.RatedRpm * RevLimitShare) continue;
            best = Math.Max(best, AvailablePowerKw(spec, Math.Max(rpm, spec.LaunchRpm)));
        }
        return best;
    }

    /// <summary>A bisection (16 steps) for the highest speed up to the intended one that some gear can hold.</summary>
    public static double PowerLimitedSpeed(DrivetrainSpec spec, in DriveConditions c, double intended, int gears, double clutch = 1)
    {
        double low = 0, high = intended;
        for (int i = 0; i < 16; i++)
        {
            double mid = (low + high) / 2;
            double need = Math.Max(0, RoadLoad(spec, c, mid) * mid / 1000) / Math.Max(0.1, spec.Efficiency);
            if (BestAvailableKw(spec, mid, gears) * clutch >= need) low = mid;
            else high = mid;
        }
        return low;
    }

    /// <summary>The cap moves halfway to its target each sample (a sample is a second), so the vehicle never jerks.</summary>
    public static double SmoothCap(double current, double target) => current + (target - current) * 0.5;

    /// <summary>Rolling resistance from a terrain's path cost: 0.012 on built floors to 0.35 in marsh (authored from secondary tables).</summary>
    public static double RollingResistanceForPathCost(double pathCost)
    {
        double[] x = { 0, 2, 4, 14, 30 };
        double[] y = { 0.012, 0.05, 0.12, 0.25, 0.35 };
        return DrivetrainSpec.Curve(x, y, pathCost);
    }

    /// <summary>Peak grip from a terrain's path cost: 0.8 on built floors, 0.6 on gravel and soil, 0.55 on firm sand, 0.35 in soft
    /// sand and mud (secondary tables, rounded; 0.35 is the figure the design's worked example used for loose sand).</summary>
    public static double GripForPathCost(double pathCost)
    {
        double[] x = { 0, 2, 4, 14, 30 };
        double[] y = { 0.8, 0.6, 0.55, 0.35, 0.3 };
        return DrivetrainSpec.Curve(x, y, pathCost);
    }

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
