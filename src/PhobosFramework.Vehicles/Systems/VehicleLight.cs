using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One light a vehicle casts on the map (Framework 0.104.0): the one place our vehicles register, move and clear light, used by the work
/// lamps and the headlights. It goes through the game's own light grid as a custom glower: a pool of light round one cell, with no thing
/// behind it, so nothing is added to a vehicle's Def and nothing is saved. The handle lives only as long as the map's light grid does; a
/// loaded game starts with every light out, and the owners light them again from their own state.
/// </summary>
public sealed class VehicleLight
{
    Map? map;
    int handle;
    IntVec3 cell = IntVec3.Invalid;

    /// <summary>Whether the light is on a map now.</summary>
    public bool Lit => map != null;

    /// <summary>The cell the light is centred on, or Invalid when out.</summary>
    public IntVec3 Cell => cell;

    /// <summary>Lights the pool at <paramref name="at"/>, moving it there if it was elsewhere. Does nothing if it is already there; a cell
    /// off the map puts it out.</summary>
    public void Place(Map on, IntVec3 at, float radius, ColorInt colour)
    {
        if (map == on && cell == at) return;
        PutOut();
        if (on == null || on.Disposed || !at.InBounds(on)) return;
        handle = on.glowGrid.RegisterCustomGlower(at, radius, colour);
        map = on;
        cell = at;
    }

    /// <summary>Takes the light off its map, if it is on one.</summary>
    public void PutOut()
    {
        if (map == null) return;
        if (!map.Disposed) map.glowGrid.DeregisterCustomGlower(handle);
        map = null;
        cell = IntVec3.Invalid;
    }
}
