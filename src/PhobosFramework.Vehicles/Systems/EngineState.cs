using Phobos.Framework.Vehicles.Rules;

namespace Phobos.VehicleSystems;

/// <summary>
/// The one picture of a vehicle's engine (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S1): whether it turns and
/// why, how fast, and the shares of its rated power that driving and the loads take. Worked out once per update by
/// <see cref="CompPhobosVehicleSystems"/> and refreshed by its once-a-second drive sample; everything that shows or uses the engine reads
/// this, so two figures for the same thing can no longer disagree. Derived, never saved.
/// </summary>
public readonly struct EngineState
{
    public readonly EngineWhy Why;
    /// <summary>Engine speed now, rpm; 0 when stopped.</summary>
    public readonly float Rpm;
    /// <summary>The share of rated power driving, flying or standing work asks for: 0 to 1.</summary>
    public readonly float DriveLoad;
    /// <summary>The share the electrical loads take through the alternator (crosslink 5): 0 to 1.</summary>
    public readonly float AccessoryShare;
    /// <summary>The whole load, what the systems and the sound read: driving's and the loads' shares together, at most 1.</summary>
    public readonly float Load;

    public EngineState(EngineWhy why, float rpm, float driveLoad, float accessoryShare)
    {
        Why = why;
        Rpm = rpm;
        DriveLoad = driveLoad;
        AccessoryShare = accessoryShare;
        Load = (float)EngineStateRule.Load(driveLoad, accessoryShare);
    }

    public bool Running => Why >= EngineWhy.Idling;

    /// <summary>The same engine with a new drive sample (the once-a-second refresh), the loads' share kept.</summary>
    public EngineState WithDrive(float rpm, float driveLoad) => new(Why, rpm, driveLoad, AccessoryShare);
}
