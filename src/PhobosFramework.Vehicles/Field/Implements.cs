using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// An implement behind a vehicle (docs/development/farming-batch-two.md, "The linkage"): a plough or a cultivator on the three-point
/// linkage, a drill on the drawbar. It is its own vehicle, held by the vehicle in front through our hitch. It is lowered, and works and
/// drags, only while the vehicle holding it drives a row of a field run; at a row's end, on a road and parked it is lifted. Nothing saved:
/// whether it is lowered is read from the vehicle in front each time.
/// </summary>
public static class Implements
{
    /// <summary>The vehicle holding this implement through its hitch, or null.</summary>
    public static VehiclePawn? HeldBy(VehiclePawn implement) => Hitches.Towing(implement)?.Vehicle;

    /// <summary>Whether this implement is lowered and working: held, and the vehicle holding it drives a row of a field run.</summary>
    public static bool Lowered(VehiclePawn implement) => HeldBy(implement)?.GetComp<CompPhobosFieldRun>()?.WorkingRow == true;
}
