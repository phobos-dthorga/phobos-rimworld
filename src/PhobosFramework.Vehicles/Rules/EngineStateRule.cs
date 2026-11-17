using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Why a vehicle's engine is turning or not, as the hub decided it at its last look (Framework 0.130.0, step S1).</summary>
public enum EngineWhy
{
    /// <summary>Nothing asks for it: parked, switched off.</summary>
    Off,
    /// <summary>Something asks for it, but the tank is empty.</summary>
    NoFuel,
    /// <summary>Driving on the battery (the generator set's hybrid): the engine is stopped.</summary>
    OnBattery,
    /// <summary>Running with the vehicle standing and no work asking for it.</summary>
    Idling,
    /// <summary>Running and moving, on the map or with a caravan.</summary>
    Driving,
    /// <summary>Turned by standing work (a pump, a drill, a generator head), whether or not a driver also holds it on.</summary>
    Working,
    /// <summary>An aircraft's engines running.</summary>
    Flying,
}

/// <summary>
/// How a vehicle's engine state is put together (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S1). Game-free.
/// </summary>
public static class EngineStateRule
{
    /// <summary>The engine's whole load: driving's share and the loads' share together, each held to 0 to 1, the sum to 1.</summary>
    public static double Load(double driveLoad, double accessoryShare) =>
        Math.Min(1, Clamp01(driveLoad) + Clamp01(accessoryShare));

    /// <summary>Why the engine turns or not, from what the hub reads, in this order: on the battery, stopped (out of fuel if something
    /// asked for it), flying, driving, standing work, idling.</summary>
    /// <param name="wanted">Whether anything asks for the engine: a driver, a moving caravan, a flight or standing work.</param>
    public static EngineWhy Why(bool onBattery, bool running, bool wanted, bool hasFuel, bool flying, bool moving, bool working)
    {
        if (onBattery) return EngineWhy.OnBattery;
        if (!running) return wanted && !hasFuel ? EngineWhy.NoFuel : EngineWhy.Off;
        if (flying) return EngineWhy.Flying;
        if (moving) return EngineWhy.Driving;
        if (working) return EngineWhy.Working;
        return EngineWhy.Idling;
    }

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}
