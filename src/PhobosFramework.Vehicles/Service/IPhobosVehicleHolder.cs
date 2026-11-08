using System.Collections.Generic;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>A world object that holds vehicles of its own, such as a garage: its vehicles appear in the world-map vehicle tabs.</summary>
public interface IPhobosVehicleHolder
{
    IEnumerable<VehiclePawn> HeldVehicles { get; }
}
