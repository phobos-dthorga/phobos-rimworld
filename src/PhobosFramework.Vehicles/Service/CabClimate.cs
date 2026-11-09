using System.Linq;
using System.Runtime.CompilerServices;
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
    sealed class Cab
    {
        public float degrees;
    }

    // Weak: loading a game despawns nothing, and a plain dictionary would keep the last game's vehicles.
    static readonly ConditionalWeakTable<VehiclePawn, Cab> cabs = new();

    static CabClimate()
    {
        HeldTemperature.Register<VehicleRoleHandler>(handler => handler.vehicle != null && cabs.TryGetValue(handler.vehicle, out Cab? cab) ? cab.degrees : null);
    }

    public static void Set(VehiclePawn vehicle, float temperature) => cabs.GetOrCreateValue(vehicle).degrees = temperature;

    public static void Forget(VehiclePawn vehicle) => cabs.Remove(vehicle);

    public static bool TryGet(VehiclePawn vehicle, out float temperature)
    {
        bool found = cabs.TryGetValue(vehicle, out Cab? cab);
        temperature = found ? cab!.degrees : 0f;
        return found;
    }

    /// <summary>How many cabs are known, for the load line and dev actions.</summary>
    public static int Count => cabs.Count();
}
