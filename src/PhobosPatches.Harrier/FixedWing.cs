using System;
using System.Collections.Generic;
using System.Reflection;
using Vehicles;

namespace Phobos.VehiclePatches;

/// <summary>Whether a vehicle is one of the fixed-wing framework's aircraft, read from its own IsFixedWing property by name, so this
/// assembly never references that framework.</summary>
public static class FixedWing
{
    static readonly Dictionary<Type, PropertyInfo?> properties = new();

    public static bool Is(VehiclePawn? vehicle)
    {
        if (vehicle == null) return false;
        Type type = vehicle.GetType();
        if (!properties.TryGetValue(type, out PropertyInfo? property))
        {
            property = type.GetProperty("IsFixedWing", BindingFlags.Public | BindingFlags.Instance);
            properties[type] = property;
        }
        return property?.GetValue(vehicle) is true;
    }
}
