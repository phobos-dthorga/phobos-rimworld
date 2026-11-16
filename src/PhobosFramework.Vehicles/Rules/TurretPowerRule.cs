using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>How a weapon is mounted (Framework 0.117.2).</summary>
public enum WeaponMountKind
{
    /// <summary>Fixed in place: no drive (smoke launchers).</summary>
    Fixed,
    /// <summary>A turret with a powered drive of its own.</summary>
    Powered,
    /// <summary>Swung by hand on its own ring or pintle: needs no power.</summary>
    HandSwung,
    /// <summary>Rides on a turret and goes where it goes (a coaxial gun): shares that turret's drive.</summary>
    WithTurret,
}

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

    /// <summary>
    /// How a weapon is mounted, and so what turns it (Framework 0.117.2; the owner's audit of every vehicle's weapons, 17 Nov 2026):
    /// <list type="bullet">
    /// <item>a fixed weapon (smoke launchers) has no drive;</item>
    /// <item>a weapon riding on a turret either goes where the turret goes (a coaxial gun, a launcher on the turret's side, worked by
    /// the turret's own crew) or stands on its own ring on top of it (a commander's or loader's machine gun): the mount words in its
    /// key say so, and otherwise a weapon worked by a different crew member from its turret's is on its own mount;</item>
    /// <item>a weapon standing alone is swung by hand when its key or turret Def names a machine gun or a mount for one, and powered
    /// otherwise.</item>
    /// </list>
    /// </summary>
    public static WeaponMountKind Classify(bool isFixed, bool ridesTurret, string key, string turretDefName, bool sameCrewAsTurret,
        IReadOnlyList<string> handSwung, IReadOnlyList<string> coaxial, IReadOnlyList<string> ownMount)
    {
        if (isFixed) return WeaponMountKind.Fixed;
        if (ridesTurret)
        {
            if (HandSwung(key, coaxial) || HandSwung(turretDefName, coaxial)) return WeaponMountKind.WithTurret;
            if (HandSwung(key, ownMount)) return WeaponMountKind.HandSwung;
            return sameCrewAsTurret ? WeaponMountKind.WithTurret : WeaponMountKind.HandSwung;
        }
        return HandSwung(key, handSwung) || HandSwung(turretDefName, handSwung) ? WeaponMountKind.HandSwung : WeaponMountKind.Powered;
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
