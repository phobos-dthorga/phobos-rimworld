using System.Collections.Generic;
using System.Linq;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// One engine's own figure of a quantity, wherever the vehicle keeps it for each engine (Framework 0.373.0; the systems schematic's engine
/// tabs, docs/development/vehicle-schematic.md): each engine's fuel feed, oil level and oil pressure (<see cref="EngineFeeds"/>), its metal
/// in the oil (<see cref="OilMetal"/>), and any quantity kept per part for the engines (<see cref="PartFigures"/>, such as hot-section life).
/// A vehicle that keeps one figure for all its engines has none here, and its readers fall back to that figure. Reads only.
/// </summary>
public static class EngineFigures
{
    /// <summary>The engine roles in order (Engine1, Engine2...), or the one Engine.</summary>
    public static IReadOnlyList<string> Roles(CompPhobosVehicleSystems comp) => comp.Powertrain?.EngineRoles ?? Powertrain.EngineRolesOf(comp);

    /// <summary>Engine <paramref name="engine"/> (from 0)'s own figure of <paramref name="quantity"/>; false where none is kept for it.</summary>
    public static bool TryRead(CompPhobosVehicleSystems comp, string quantity, int engine, out float figure)
    {
        figure = 0f;
        IReadOnlyList<string> roles = Roles(comp);
        if (engine < 0 || engine >= roles.Count || roles.Count < 2) return false;
        if (EngineFeeds.IsReading(quantity)) return comp.EngineFeeds.TryReading(quantity, engine, out figure);
        if (comp.PartsOf(roles[engine]).FirstOrDefault() is not VehicleComponent part) return false;
        if (quantity == OilMetal.EngineQuantity)
        {
            // An engine that has shed nothing reads clean, where the vehicle keeps the figure at all.
            if (!comp.TryGetQuantity(quantity, out _)) return false;
            if (!comp.OilMetal.TryOf(part, out figure)) figure = 0f;
            return true;
        }
        return comp.partFigures != null && comp.partFigures.TryGetValue(quantity + "|" + part.props.key, out figure);
    }
}
