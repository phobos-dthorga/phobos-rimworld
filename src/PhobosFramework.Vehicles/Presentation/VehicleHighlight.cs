using System.Reflection;
using HarmonyLib;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// Lights a part up on the vehicle, as Vehicle Framework's own Health tab does: its HighlightedComponent property is internal, so
/// the one reflection accessor every Phobos tab shares lives here (a rename in Vehicle Framework 1.7 is then one fix). Clearing
/// is idempotent; nothing is lit off a map.
/// </summary>
public static class VehicleHighlight
{
    static readonly MethodInfo? setter = AccessTools.PropertySetter(typeof(VehiclePawn), "HighlightedComponent");
    static VehiclePawn? highlighted;

    /// <summary>Lights the part on its vehicle, or clears the light when the part is null.</summary>
    public static void Set(VehiclePawn? vehicle, VehicleComponent? part)
    {
        if (setter == null) return;
        if (part == null || vehicle == null)
        {
            Clear();
            return;
        }
        setter.Invoke(vehicle, new object?[] { part });
        highlighted = vehicle;
    }

    public static void Clear()
    {
        if (setter == null || highlighted == null) return;
        setter.Invoke(highlighted, new object?[] { null });
        highlighted = null;
    }
}
