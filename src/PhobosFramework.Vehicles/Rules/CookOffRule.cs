using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Ammunition aboard a burning vehicle cooking off (cross-links round three, G13; Framework 0.113.0; owner, 13 Nov 2026: on by default, only
/// where Combat Extended is detected and its ammunition system is on), game-free. Rounds heated in a fire go off; a stowed load does not
/// all go at once, so each blast takes a share of what is aboard and the fire may set off more. All figures are authored, a gameplay choice:
/// how often a blast comes at full fire, how much of the load each blast uses, and a blast's size from the mass it uses (radius and damage
/// grow with the cube root of the mass, as a blast's reach does with its charge; the base and caps keep a crate of rifle rounds a small pop
/// and a truck full of shells a bad day).
/// </summary>
public static class CookOffRule
{
    /// <summary>The mean hours between blasts: <paramref name="mtbHoursAtFullFire"/> at a full-grown fire, longer for a smaller one, none
    /// without fire or ammunition.</summary>
    public static double MeanHoursBetween(double fireShare, double ammoKg, double mtbHoursAtFullFire)
    {
        if (fireShare <= 0 || ammoKg <= 0) return double.PositiveInfinity;
        return Math.Max(0.001, mtbHoursAtFullFire) / Math.Min(1, fireShare);
    }

    /// <summary>The kilograms one blast uses: <paramref name="share"/> of what is aboard, at least <paramref name="leastKg"/> (or all of it).</summary>
    public static double KilogramsPerBlast(double ammoKg, double share, double leastKg) =>
        Math.Min(Math.Max(0, ammoKg), Math.Max(Math.Max(0, leastKg), Math.Max(0, ammoKg) * Math.Max(0, Math.Min(1, share))));

    /// <summary>A blast's radius in cells for the kilograms it uses.</summary>
    public static double Radius(double kilograms, double baseRadius, double perCubeRootKg, double maxRadius) =>
        Math.Min(maxRadius, baseRadius + perCubeRootKg * Math.Cbrt(Math.Max(0, kilograms)));

    /// <summary>A blast's damage for the kilograms it uses.</summary>
    public static int Damage(double kilograms, double baseDamage, double perCubeRootKg, double maxDamage) =>
        (int)Math.Round(Math.Min(maxDamage, baseDamage + perCubeRootKg * Math.Cbrt(Math.Max(0, kilograms))));
}
