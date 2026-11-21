using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Electronic jamming fields and our systems (Framework 0.225.0; docs/development/ea6b-prowler.md, step 2): whether a point lies inside a field,
/// and whether a capability is one a jammer blinds. Game-free and tested.
/// </summary>
public static class JamFieldRule
{
    /// <summary>Whether a point this far across and along from a field's centre lies inside it (on the ground plane; height is ignored).</summary>
    public static bool Inside(double dx, double dz, double radius) => radius > 0 && dx * dx + dz * dz <= radius * radius;

    /// <summary>Whether a jammer blinds this capability: only those the rules Def lists (a radar jammer floods radar frequencies, so a radar and
    /// a warning receiver; not an infrared night sight or an autopilot's gyros).</summary>
    public static bool Blinds(IReadOnlyCollection<string> jammed, string quantity)
    {
        foreach (string name in jammed)
        {
            if (name == quantity) return true;
        }
        return false;
    }
}
