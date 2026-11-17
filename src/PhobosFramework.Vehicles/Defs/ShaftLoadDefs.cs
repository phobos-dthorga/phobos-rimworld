using System.Collections.Generic;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Something a vehicle's engine drives besides its wheels (Framework 0.131.0; docs/development/vehicle-engine-loads.md, the engine-load
/// ledger): a water pump, a radiator fan, an air-conditioner compressor, an air compressor, a pump on a power take-off. Each Def is one
/// load, and it applies to every vehicle with a piston or turbine drivetrain on the ground whose <see cref="available"/> holds, so another
/// mod adds a load by adding a Def, with no C#. What it takes comes off the power left for driving, and is burned and turned to heat with
/// the rest. The alternator is not a Def: it is worked out from the vehicle's electrics.
/// </summary>
public class PhobosVehicleShaftLoadDef : Def
{
    /// <summary>Its size at the engine's rated speed, kW: for a load whose size does not follow the engine's (a cab's air conditioner).</summary>
    public float kilowatts;
    /// <summary>Its size at the engine's rated speed as a share of the engine's rated power: for a load that grows with the engine (its fan,
    /// its pumps). Added to <see cref="kilowatts"/>.</summary>
    public float shareOfRated;
    /// <summary>How it follows engine speed: 0 steady, 1 with speed, 2 or 3 for pumps and fans (the affinity laws give the cube).</summary>
    public float speedExponent;
    /// <summary>Turned by the drive belt: a slipping belt passes less, a snapped one nothing, and it counts toward what the belt carries.</summary>
    public bool beltDriven;
    /// <summary>Part of the standing set a rated power figure already includes (a water pump always turns): it is counted in the fuel at the
    /// nominal cruise, so a cruise still burns the author's rate, and it takes nothing from the power left for driving.</summary>
    public bool baseline;
    /// <summary>Whether this vehicle has it at all (a fan only on a liquid-cooled engine). Null: every vehicle.</summary>
    public VehicleSystemCondition? available;
    /// <summary>Whether it is working now. Null: whenever the engine runs.</summary>
    public VehicleSystemCondition? when;
    /// <summary>Multiplied together with its size: a fan's engagement by coolant temperature, a pump's condition. Empty: 1.</summary>
    public List<VehicleSystemInput> inputs = new();
    /// <summary>Where it stands in the Systems tab's list, lowest first.</summary>
    public int order;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (kilowatts <= 0f && shareOfRated <= 0f) yield return "has no size: give kilowatts or shareOfRated";
        if (speedExponent < 0f) yield return "speedExponent is below 0";
        if (baseline && (when != null || inputs.Count > 0)) yield return "a baseline load is steady: it may have an available condition, but no when or inputs";
    }
}

/// <summary>One load on a vehicle's ledger at the last look: which, and the shaft power it took. Derived, never saved.</summary>
public readonly struct ShaftLoadNow
{
    public readonly string Label;
    public readonly float Kilowatts;

    public ShaftLoadNow(string label, float kilowatts)
    {
        Label = label;
        Kilowatts = kilowatts;
    }
}
