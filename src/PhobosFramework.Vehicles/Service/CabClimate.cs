using System.Collections.Generic;
using Phobos.Framework.Environment;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The temperature inside each closed cab, as the Cabin system works it out, given to the game through the Framework's held
/// temperature registry (docs/development/vehicle-world-map-design.md): a passenger's holder is the vehicle's role handler, so the
/// game reads the cab instead of the cell or the world tile, on maps and in caravans alike. Open cabs give nothing and the game
/// carries on to the weather. Derived, never saved: the comp writes it on every update.
/// </summary>
[StaticConstructorOnStartup]
public static class CabClimate
{
    static readonly Dictionary<VehiclePawn, float> cabs = new();

    static CabClimate()
    {
        HeldTemperature.Register<VehicleRoleHandler>(handler => handler.vehicle != null && cabs.TryGetValue(handler.vehicle, out float cab) ? cab : null);
    }

    public static void Set(VehiclePawn vehicle, float temperature) => cabs[vehicle] = temperature;

    public static void Forget(VehiclePawn vehicle) => cabs.Remove(vehicle);

    public static bool TryGet(VehiclePawn vehicle, out float temperature) => cabs.TryGetValue(vehicle, out temperature);

    /// <summary>How many cabs are known, for the load line and dev actions.</summary>
    public static int Count => cabs.Count;
}
