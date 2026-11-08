using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.Debug;

/// <summary>What the player has selected, for every Phobos mod's dev actions: things on a map, or objects on the world map.</summary>
public static class DebugSelection
{
    /// <summary>The selected things of a type on the current map.</summary>
    public static IEnumerable<T> SelectedThings<T>() where T : Thing => Find.Selector.SelectedObjects.OfType<T>();

    /// <summary>The selected comps of a type on the selected things, skipping things without one.</summary>
    public static IEnumerable<TComp> SelectedComps<TComp>() where TComp : ThingComp =>
        SelectedThings<ThingWithComps>().Select(t => t.GetComp<TComp>()).Where(c => c != null)!;

    /// <summary>The selected world objects of a type (caravans, sites) on the world map.</summary>
    public static IEnumerable<T> SelectedWorldObjects<T>() where T : WorldObject =>
        Find.World == null ? Enumerable.Empty<T>() : Find.WorldSelector.SelectedObjects.OfType<T>();
}
