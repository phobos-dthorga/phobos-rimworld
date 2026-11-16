using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A powered turret with no power is turned and laid by hand (owner, 16 Nov 2026: "hand-cranked", never dead; docs/development/vehicle-knocked-out-systems.md,
/// T3 and T4). Game-free.
/// </summary>
public static class TurretPowerRule
{
    /// <summary>Whether a weapon is swung by hand anyway (a pintle or ring-mounted machine gun), so it loses nothing without power: its key
    /// contains one of the patterns, case aside.</summary>
    public static bool HandSwung(string key, IReadOnlyList<string> patterns)
    {
        if (string.IsNullOrEmpty(key)) return false;
        foreach (string pattern in patterns)
        {
            if (!string.IsNullOrEmpty(pattern) && key.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    /// <summary>Whether the drive has power: its load is running and the battery is above flat.</summary>
    public static bool Powered(bool loadRunning, double batteryShare, double flatBelow) => loadRunning && batteryShare >= flatBelow;

    /// <summary>The traverse and aiming paces of a drive: full with power, the hand-cranked figures without.</summary>
    public static void Paces(bool powered, double crankTraverse, double crankAim, out double traverse, out double aim)
    {
        traverse = powered ? 1 : Math.Max(TurretPaceRule.FloorPace, Math.Min(1, crankTraverse));
        aim = powered ? 1 : Math.Max(TurretPaceRule.FloorPace, Math.Min(1, crankAim));
    }
}
