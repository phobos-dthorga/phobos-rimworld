namespace Phobos.VehicleSystems;

/// <summary>What one electrical load is doing now (Framework 0.117.0, for the Systems tab).</summary>
public enum LoadState
{
    /// <summary>This vehicle does not have it (its fitted condition does not hold).</summary>
    NotFitted,
    /// <summary>Its switch is off.</summary>
    SwitchedOff,
    /// <summary>Switched off to save the battery while the charging fails.</summary>
    Shed,
    /// <summary>Ready, and not drawing because its work is not going on.</summary>
    Idle,
    /// <summary>Drawing on the battery.</summary>
    Running,
    /// <summary>Wanted, but the battery is flat.</summary>
    NoPower,
    /// <summary>Its circuit is shorted by a hit to the wiring, until the battery or alternator is repaired (Framework 0.123.0, cross-links G12).</summary>
    Shorted,
}

/// <summary>Which way a vehicle's battery is going, for the inspect line (Framework 0.104.0). None: steady, full, or nothing worth saying.</summary>
public enum ElectricalTrend
{
    None,
    RunningDown,
    Charging,
    ChargingFromGrid,
}

/// <summary>
/// The battery's state in words (Framework 0.104.0): its trend, the current in or out of it in amperes, and the switched loads running
/// (their labels, joined). Worked out by the vehicle's systems at each update; the inspect line only reads it.
/// </summary>
public readonly struct ElectricalStatus
{
    public readonly ElectricalTrend Trend;
    public readonly float Amps;
    public readonly string Loads;
    /// <summary>The loads switched off to save the battery (Framework 0.106.0), joined; empty when none.</summary>
    public readonly string Shed;
    /// <summary>Game hours the charge lasts at this drain (Framework 0.106.0); infinity when not running down.</summary>
    public readonly float HoursLeft;

    public ElectricalStatus(ElectricalTrend trend, float amps, string loads, string shed = "", float hoursLeft = float.PositiveInfinity)
    {
        Trend = trend;
        Amps = amps;
        Loads = loads;
        Shed = shed;
        HoursLeft = hoursLeft;
    }
}
