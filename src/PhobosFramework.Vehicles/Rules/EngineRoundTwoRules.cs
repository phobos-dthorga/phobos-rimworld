using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Engines lit one after another (Framework 0.250.0, per-engine realism round two, step 9; the owner's card of 23 Nov 2026: game-scaled).
/// Game-free.
/// </summary>
public static class StartSequenceRule
{
    /// <summary>How many of <paramref name="engines"/> are lit <paramref name="hours"/> after the start began, each taking
    /// <paramref name="hoursPerEngine"/>, one after the other.</summary>
    public static int Lit(double hours, double hoursPerEngine, int engines)
    {
        if (engines <= 0) return 0;
        if (hoursPerEngine <= 0) return engines;
        return (int)Math.Min(engines, Math.Floor(Math.Max(0, hours) / hoursPerEngine + 1e-9));
    }
}

/// <summary>
/// Stones and grit thrown into engine intakes on rough ground (Framework 0.250.0, step 10; the owner's card: jets and helicopters). Game-free.
/// </summary>
public static class StripDebrisRule
{
    /// <summary>The chance of a strike over <paramref name="hours"/> in a phase that throws debris: a rate an hour for an exposed intake
    /// on wholly rough ground, times the aircraft's exposure, the phase's weight and the rough share of the ground under it.</summary>
    public static double Chance(double perHour, double exposure, double phaseWeight, double roughShare, double hours)
    {
        double rate = Math.Max(0, perHour) * Math.Max(0, exposure) * Math.Max(0, phaseWeight) * Math.Min(1, Math.Max(0, roughShare));
        return rate <= 0 || hours <= 0 ? 0 : 1 - Math.Exp(-rate * hours);
    }

    /// <summary>The chance of a strike at one lift-off or touch-down on our own flight.</summary>
    public static double EventChance(double perEvent, double exposure, double roughShare, double stoppedByBarrier) =>
        Math.Min(1, Math.Max(0, perEvent) * Math.Max(0, exposure) * Math.Min(1, Math.Max(0, roughShare)) * (1 - Math.Min(1, Math.Max(0, stoppedByBarrier))));
}

/// <summary>
/// A twin's two fuel tanks, one each side, each feeding its own engine, with a cross-feed between them (Framework 0.250.0, step 11; the owner's
/// card: an automatic and a manual mode). The vehicle keeps one fuel figure; the sides are that total and an imbalance, the port side's
/// fuel less the starboard's. Game-free.
/// </summary>
public static class SideTankRule
{
    /// <summary>Each side's fuel: (port, starboard), from the total and the imbalance (clamped so neither is below nothing).</summary>
    public static (double port, double starboard) Sides(double total, double imbalance)
    {
        double t = Math.Max(0, total);
        double d = Math.Max(-t, Math.Min(t, imbalance));
        return ((t + d) / 2, (t - d) / 2);
    }

    /// <summary>The imbalance after the engines burned <paramref name="port"/> and <paramref name="starboard"/>: each from its own side with the
    /// cross-feed shut; with it open, from the fuller side first, so the two come level.</summary>
    public static double AfterBurn(double imbalance, double port, double starboard, bool open)
    {
        double burned = Math.Max(0, port) + Math.Max(0, starboard);
        if (!open) return imbalance - Math.Max(0, port) + Math.Max(0, starboard);
        if (imbalance > 0) return Math.Max(0, imbalance - burned);
        return Math.Min(0, imbalance + burned);
    }

    /// <summary>The imbalance after a leak of <paramref name="amount"/> from <paramref name="side"/> (0 port, 1 starboard): from that side alone
    /// with the cross-feed shut; with it open, the fuel runs between the tanks and the leak takes from both alike.</summary>
    public static double AfterLeak(double imbalance, double amount, int side, bool open)
    {
        if (open || side < 0) return imbalance;
        return side == 0 ? imbalance - Math.Max(0, amount) : imbalance + Math.Max(0, amount);
    }

    /// <summary>The imbalance after <paramref name="amount"/> was put in: the emptier side is filled first, as a fueller would.</summary>
    public static double AfterRefuel(double imbalance, double amount)
    {
        double a = Math.Max(0, amount);
        return imbalance > 0 ? Math.Max(0, imbalance - a) : Math.Min(0, imbalance + a);
    }

    /// <summary>Whether a side is dry (less than <paramref name="dry"/> of fuel in it) while the total still holds some.</summary>
    public static bool SideDry(double total, double imbalance, int side, double dry)
    {
        (double port, double starboard) = Sides(total, imbalance);
        return (side == 0 ? port : starboard) <= Math.Max(0, dry);
    }
}
