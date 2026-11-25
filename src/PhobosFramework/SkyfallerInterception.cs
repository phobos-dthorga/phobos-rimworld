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
