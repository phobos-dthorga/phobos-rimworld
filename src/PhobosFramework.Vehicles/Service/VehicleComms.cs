using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>What a vehicle's radios read as on its Comms system: none fitted, fitted but dead (no power, its bay shot away), or working.</summary>
public enum CommsRadio
{
    None = 0,
    Dead = 1,
    Working = 2,
}

/// <summary>A vehicle's link home, as its Comms system reads it: no working radio, jammed, out of touch, faint, or in touch.</summary>
public enum CommsLink
{
    NoRadio = 0,
    Jammed = 1,
    OutOfTouch = 2,
    Faint = 3,
    InTouch = 4,
    /// <summary>Not on a map (in a caravan, in flight between places): the Comms system cannot say; a convoy's own radio button does.</summary>
    Away = 5,
}

/// <summary>Answers for a vehicle's radios; D'thorga Radio Framework's Vehicle Framework tie-in registers one.</summary>
public interface IVehicleCommsSource
{
    CommsRadio RadioOf(VehiclePawn vehicle);
    CommsLink LinkOf(VehiclePawn vehicle);
}

/// <summary>
/// The Comms system's seam (Framework 0.356.0; docs/development/vehicle-standard.md, VS1; the owner's card of 27 Nov 2026: a Comms system on
/// every powered vehicle). D'thorga Framework knows nothing of radio: the radio's tie-in registers a source, and the Comms system's readings
/// ask it through <see cref="Input_Comms"/>. With none registered (no radio installed), every vehicle reads "no radio". The Comms system's
/// own Def ships with the radio, so a game without it has no Comms system to read.
/// </summary>
public static class VehicleComms
{
    static IVehicleCommsSource? source;

    /// <summary>Registers the radio's answers; called once, at start-up.</summary>
    public static void Register(IVehicleCommsSource commsSource) => source = commsSource;

    public static CommsRadio RadioOf(VehiclePawn vehicle) => source?.RadioOf(vehicle) ?? CommsRadio.None;

    public static CommsLink LinkOf(VehiclePawn vehicle) => source?.LinkOf(vehicle) ?? CommsLink.NoRadio;
}
