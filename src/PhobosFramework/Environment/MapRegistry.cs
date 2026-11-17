using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// The things of one kind standing on each map (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S8): a component joins
/// its map's list when its thing spawns and leaves it when the thing despawns, so finding them never walks every building the colony owns.
/// The owner's recording of 18 Nov 2026 found the game's own building lookup (ListerBuildings.AllBuildingsColonistOfDef, which walks every
/// colony building) behind most of D'thorga Framework's time, asked every tick by a vehicle repair looking for a tuning rig.
/// Derived, never saved: spawning on load fills it again. A map that goes away takes its list with it.
/// </summary>
public static class MapRegistry<T> where T : class
{
    static readonly ConditionalWeakTable<Map, List<T>> Lists = new();
    static readonly List<T> Empty = new();

    /// <summary>Call from the component's PostSpawnSetup.</summary>
    public static void Add(Map? map, T item)
    {
        if (map == null) return;
        List<T> list = Lists.GetOrCreateValue(map);
        if (!list.Contains(item)) list.Add(item);
    }

    /// <summary>Call from the component's PostDeSpawn, with the map it is leaving.</summary>
    public static void Remove(Map? map, T item)
    {
        if (map != null && Lists.TryGetValue(map, out List<T>? list)) list.Remove(item);
    }

    /// <summary>Everything of this kind on the map, in the order it spawned. Do not change the list while walking it.</summary>
    public static IReadOnlyList<T> On(Map? map) => map != null && Lists.TryGetValue(map, out List<T>? list) ? list : Empty;
}
