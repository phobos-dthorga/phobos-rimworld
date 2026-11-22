using System;
using Phobos.Framework.Rules;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The engine-load ledger's arithmetic (Framework 0.131.0; docs/development/vehicle-engine-loads.md): what each thing the engine
/// drives takes at the shaft, what the alternator takes for the electricity it actually makes, and how much of what it is asked
/// to carry a drive belt can pass. Game-free; every figure it is given is a Def's.
/// </summary>
public static class ShaftLoadRule
{
    /// <summary>
    /// A driven load's shaft power now, kW: its size at the engine's rated speed (a fixed figure plus a share of rated power), by its law
    /// with engine speed (0 steady, 1 with speed, 3 with its cube as pumps and fans go), times what its inputs give (a fan's
    /// engagement, a pump's condition). Never negative; nothing with the engine stopped.
    /// </summary>
    public static double Kilowatts(double kilowatts, double shareOfRated, double ratedKilowatts, double rpm, double ratedRpm, double speedExponent,
        double inputs)
    {
        if (rpm <= 0 || ratedRpm <= 0 || inputs <= 0) return 0;
        double size = Math.Max(0, kilowatts) + Math.Max(0, shareOfRated) * Math.Max(0, ratedKilowatts);
        double speed = speedExponent == 0 ? 1 : Math.Pow(rpm / ratedRpm, speedExponent);
        return size * speed * inputs;
    }

    /// <summary>
    /// The amps the alternator makes now: what the loads draw and what the batteries are taking, and never more than it can give at this
    /// speed. What the loads ask beyond that comes off the battery and costs the engine nothing.
    /// </summary>
    public static double AlternatorOutputAmps(double supplyAmps, double loadAmps, double chargingAmps) =>
        Math.Max(0, Math.Min(Math.Max(0, supplyAmps), Math.Max(0, loadAmps) + Math.Max(0, chargingAmps)));

    /// <summary>
    /// What an alternator drags while it turns, kW, whether or not it makes anything (Framework 0.243.0): its bearings, brushes and fan, a share
    /// of its rated electrical watts. Nothing without one.
    /// </summary>
    public static double AlternatorDragKilowatts(double ratedAmps, double volts, double dragShare) =>
        PowerUnits.Watts(Math.Max(0, ratedAmps), Math.Max(0, volts)) * Math.Max(0, dragShare) / 1000.0;

    /// <summary>
    /// The engine's idle speed with what it drives now (Framework 0.243.0, idle-up): its control raises idle as the loads beyond its standing
    /// ones grow, by up to <paramref name="idleUpShare"/> of itself once they reach <paramref name="atLoadShare"/> of rated power, in proportion
    /// below that. Never below the plain idle.
    /// </summary>
    public static double IdleRpm(double idleRpm, double extraKilowatts, double ratedKilowatts, double idleUpShare, double atLoadShare)
    {
        if (idleRpm <= 0 || ratedKilowatts <= 0 || atLoadShare <= 0) return Math.Max(0, idleRpm);
        double share = Math.Min(1, Math.Max(0, extraKilowatts) / ratedKilowatts / atLoadShare);
        return idleRpm * (1 + Math.Max(0, idleUpShare) * share);
    }

    /// <summary>
    /// The share of the fuel burned now that goes to some shaft power (Framework 0.243.0, the Systems tab's fuel line): on the Willans line each
    /// shaft kilowatt costs one over the indicated efficiency, friction being counted once in the whole. 0 to 1.
    /// </summary>
    public static double FuelShare(double kilowatts, double fuelKilowatts, double indicatedEfficiency)
    {
        if (kilowatts <= 0 || fuelKilowatts <= 0) return 0;
        return Math.Min(1, kilowatts / Math.Max(0.05, indicatedEfficiency) / fuelKilowatts);
    }

    /// <summary>
    /// What an electric vehicle's converter takes from its drive battery, as a share of the pack an hour (Framework 0.243.0): the 12 V side's
    /// output in watts over the converter's efficiency, against the pack's size. 0 with nothing drawn.
    /// </summary>
    public static double ConverterPackSharePerHour(double outputAmps, double volts, double converterEfficiency, double packKilowattHours)
    {
        if (outputAmps <= 0 || volts <= 0 || packKilowattHours <= 0) return 0;
        return PowerUnits.Watts(outputAmps, volts) / Math.Max(0.5, converterEfficiency) / 1000.0 / packKilowattHours;
    }

    /// <summary>The shaft power the alternator takes to make those amps, kW.</summary>
    public static double AlternatorKilowatts(double outputAmps, double volts, double efficiency) =>
        AccessoryRules.ShaftKilowatts(PowerUnits.Watts(Math.Max(0, outputAmps), Math.Max(0, volts)), efficiency);

    /// <summary>
    /// The share of what it is asked to carry that a drive belt passes: all of it while the load is within what the belt can carry
    /// (its capacity as a share of the engine's rated power, times the belt's condition), otherwise only its capacity, slipping.
    /// A snapped belt passes nothing; with nothing asked of it, it reads as gripping.
    /// </summary>
    public static double BeltGrip(double requestedKilowatts, double capacityShare, double ratedKilowatts, double beltCondition)
    {
        if (beltCondition <= 0) return 0;
        if (requestedKilowatts <= 0) return 1;
        double capacity = Math.Max(0, capacityShare) * Math.Max(0, ratedKilowatts) * Math.Min(1, beltCondition);
        return Math.Min(1, capacity / requestedKilowatts);
    }
}
