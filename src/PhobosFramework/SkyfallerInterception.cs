using System;
using Verse;

namespace Phobos.Framework;

/// <summary>
/// Whether a falling thing on a map has been shot at by another mod's interceptor (D'thorga Framework 0.293.0; docs/development/airdrop.md,
/// "Air defence against drops"). A tie-in fills <see cref="Check"/> (D'thorga Patches does for InterRim Ballistic Missile's CIWS, whose own
/// effect runs only in the game's skyfaller impact, which our parachute loads never call); a falling thing of ours reads <see cref="Of"/>
/// as it lands. Nothing fills it, nothing is intercepted.
/// </summary>
public static class SkyfallerInterception
{
    /// <summary>A tie-in's test: true when this falling thing was intercepted. Set once at start-up.</summary>
    public static Func<Thing, bool>? Check;

    /// <summary>Whether this falling thing was intercepted; false when no tie-in answers, or the answer fails.</summary>
    public static bool Of(Thing thing)
    {
        if (Check == null || thing == null) return false;
        try
        {
            return Check(thing);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// A falling thing of ours brought down by another mod's interceptor, handed our own outcome rather than that mod's (Framework 0.297.0; the
/// owner's ask of 26 Nov 2026 for a softer outcome than Combat Extended's, which kills everyone inside). D'thorga Framework's vehicle part
/// fills <see cref="Handler"/> for its parachute loads; D'thorga Patches' Combat Extended tie-in calls <see cref="TryShootDown"/>.
/// </summary>
public static class ShotDownFallers
{
    /// <summary>Lands a falling thing of ours as shot down and returns true; false for anything else.</summary>
    public static Func<Thing, bool>? Handler;

    /// <summary>Whether ours took it: when true, the other mod's own outcome must not run.</summary>
    public static bool TryShootDown(Thing? thing)
    {
        if (Handler == null || thing == null) return false;
        try
        {
            return Handler(thing);
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Enemy aircraft in the air over a map, for another mod's air defence to aim at (Framework 0.297.0; the owner's ask of 26 Nov 2026 that
/// InterRim Ballistic Missile's CIWS also fire at the aircraft on a drop pass). D'thorga Framework's vehicle part fills the three
/// functions; a tie-in reads them. Nothing filled, there are none.
/// </summary>
public static class AirTargets
{
    /// <summary>Aircraft hostile to the player in the air over this map now.</summary>
    public static Func<Map, System.Collections.Generic.IEnumerable<Thing>>? Over;

    /// <summary>Whether this is still an aircraft hostile to the player in the air.</summary>
    public static Func<Thing, bool>? IsHostileInAir;

    /// <summary>Hits an aircraft in the air with this much bullet damage.</summary>
    public static Action<Thing, float>? Hit;
}
