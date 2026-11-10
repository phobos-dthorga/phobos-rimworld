using Vehicles;
using Vehicles.World;

namespace Phobos.VehicleSystems;

/// <summary>Whether a vehicle is under way, on a map or in a caravan on the world map. The one place this is asked, so every system agrees on it.</summary>
public static class VehicleMotion
{
    public static bool Moving(VehiclePawn vehicle) =>
        vehicle.GetVehicleCaravan() is { } caravan ? caravan.vehiclePather is { MovingNow: true } : vehicle.vehiclePather is { Moving: true };
}
