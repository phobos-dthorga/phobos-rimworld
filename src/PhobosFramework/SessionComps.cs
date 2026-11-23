using Verse;

namespace Phobos.Framework;

/// <summary>
/// A comp given to one thing for this session only, for a test before anything carries it for real (Framework 0.278.0; first used by the
/// Radio Framework's vehicle radio and the remote control of vehicles). The comp is not in the thing's def, so the game does not build
/// it again when the save is loaded: it is gone then, and what it saved is ignored. Nothing else is touched.
/// </summary>
public static class SessionComps
{
    /// <summary>
    /// Adds a comp built from these properties, unless the thing has none of its own (a thing with no comps shares the game's empty
    /// list, which must never be added to) or already has one of that class. Returns the new comp, or null.
    /// </summary>
    public static ThingComp? Add(ThingWithComps thing, CompProperties props)
    {
        var comps = thing.AllComps;
        if (comps.Count == 0 || props.compClass == null) return null;
        foreach (var comp in comps) if (comp.GetType() == props.compClass) return null;
        if (System.Activator.CreateInstance(props.compClass) is not ThingComp added) return null;
        added.parent = thing;
        comps.Add(added);
        added.Initialize(props);
        return added;
    }
}
