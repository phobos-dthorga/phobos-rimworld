using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>One part that feeds a vehicle's speed, as the mobility rule sees it.</summary>
public readonly struct SpeedPart
{
    public readonly double Efficiency;
    public readonly bool Track;
    public readonly bool Key;

    /// <param name="efficiency">What the part gives its stat now, 0 to 1.</param>
    /// <param name="track">A track, which stops the vehicle when thrown.</param>
    /// <param name="key">A part that can stop the stat by itself (Vehicle Framework's priority parts: an engine).</param>
    public SpeedPart(double efficiency, bool track, bool key)
    {
        Efficiency = efficiency;
        Track = track;
        Key = key;
    }
}

/// <summary>
/// Mobility kills (owner, 16 and 17 Nov 2026; docs/development/vehicle-knocked-out-systems.md, M1 and M2). Game-free.
/// </summary>
public static class MobilityRule
{
    /// <summary>Whether a part is a track: its key ends with one of the suffixes (case does not matter) and it feeds the vehicle's speed.</summary>
    public static bool IsTrack(string key, bool feedsSpeed, IReadOnlyList<string> suffixes)
    {
        if (!feedsSpeed || string.IsNullOrEmpty(key)) return false;
        foreach (string suffix in suffixes)
        {
            if (!string.IsNullOrEmpty(suffix) && key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    /// <summary>A track is thrown, and the vehicle cannot move, at or under this share of its health.</summary>
    public static bool Thrown(double healthShare, double thrownAtOrBelow) => healthShare <= thrownAtOrBelow;

    /// <summary>
    /// The part to name when a vehicle cannot move: a thrown track first; then a key part (an engine) giving nothing; then the weakest part.
    /// -1 when no part is short of full.
    /// </summary>
    public static int Cause(IReadOnlyList<SpeedPart> parts, double thrownAtOrBelow)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].Track && parts[i].Efficiency <= thrownAtOrBelow) return i;
        }
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].Key && parts[i].Efficiency <= 0) return i;
        }
        int weakest = -1;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i].Efficiency >= 1) continue;
            if (weakest < 0 || parts[i].Efficiency < parts[weakest].Efficiency) weakest = i;
        }
        return weakest;
    }
}
