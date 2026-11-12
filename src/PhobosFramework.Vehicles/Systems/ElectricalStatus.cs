namespace Phobos.VehicleSystems;

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

    public ElectricalStatus(ElectricalTrend trend, float amps, string loads)
    {
        Trend = trend;
        Amps = amps;
        Loads = loads;
    }
}
