using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How whole a vehicle is, for Vehicle Framework's rule that destroys it when the figure falls under 1 % (owner, 17 Nov 2026). Game-free.
/// Vehicle Framework adds up every part's health over every part's full health. The parts we append joined that sum, so a vehicle
/// with our systems took more destroying than its author made it. A part that does a job on the vehicle still counts (a battery, a
/// pump, brakes), since a real vehicle has them; an empty mounting point for a fitting (a lamp bracket, a tank cradle, a winch mount,
/// tie-downs, a hook mount) does not: it stays hittable and breakable, but nobody has to smash a bracket to finish a wreck.
/// </summary>
public sealed class IntegrityRule
{
    double health, full, allHealth, allFull;

    /// <summary>Adds one part: its health now, its full health, and whether it counts toward the vehicle's destruction.</summary>
    public void Add(double partHealth, double partFull, bool counts)
    {
        allHealth += partHealth;
        allFull += partFull;
        if (!counts) return;
        health += partHealth;
        full += partFull;
    }

    /// <summary>
    /// The share of health left among the counted parts, 0 to 1, rounded to a thousandth as Vehicle Framework rounds its own. A vehicle
    /// with no counted part at all (every part marked as a mount: an authoring mistake) falls back to all its parts, and one with no
    /// health to speak of reads as whole rather than dividing by zero.
    /// </summary>
    public double Fraction()
    {
        double top = full > 0 ? health : allHealth;
        double bottom = full > 0 ? full : allFull;
        if (bottom <= 0) return 1;
        return Math.Round(Math.Min(1, Math.Max(0, top / bottom)) * 1000) / 1000;
    }
}
