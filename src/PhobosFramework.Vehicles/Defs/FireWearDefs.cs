using System.Collections.Generic;
using Phobos.Framework.Defs;

namespace Phobos.VehicleSystems;

/// <summary>One part role a fire wears: its share of health lost per hour at full fire, and whether it is soft (rubber, hoses, belts).</summary>
public class PhobosFireWearRole
{
    public string role = "";
    public float sharePerHour;
    public bool soft;
}

/// <summary>
/// Fire wearing a vehicle's parts (Framework 0.221.0; docs/development/vehicle-fire.md, FR2, FR3, FR5, FR6; owner, 22 Nov 2026): which
/// roles burn and how fast at full fire, the least share of that rate a burning vehicle wears at, and the wiring a fire shorts. The first
/// loaded instance wins; another mod may patch it in XML. Every figure is authored.
/// </summary>
public class PhobosFireWearRulesDef : PhobosRulesDef
{
    public List<PhobosFireWearRole> roles = new();
    public float leastShareOfRate = 0.1f;
    /// <summary>The part role a fire's shorted wiring is put on, and its severity an hour at full fire.</summary>
    public string wiringRole = "Battery";
    public float wiringPerHour;
    public PhobosVehicleConditionDef? wiringCondition;
}
