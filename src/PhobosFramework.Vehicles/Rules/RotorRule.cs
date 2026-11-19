using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How fast an aircraft's drawn rotors turn on our own flight (Framework 0.173.0; the owner's report of 19 Nov 2026: the Cherokee's blades
/// stood still on our flight). Vehicle Framework turns a rotor overlay only inside its own take-off and landing animation, which our flight
/// replaces, so we turn it: up to the aircraft's own full rate while its engines run, and down to rest after they stop. Game-free. The rates are
/// the aircraft author's; the spin-up and run-down times are authored.
/// </summary>
public static class RotorRule
{
    /// <summary>
    /// The rotor's rate after <paramref name="seconds"/> more, in the same unit as <paramref name="full"/> (degrees a tick): rising evenly to
    /// <paramref name="full"/> over <paramref name="upSeconds"/> while <paramref name="running"/>, falling evenly to rest over
    /// <paramref name="downSeconds"/> after. A time of 0 or less jumps at once.
    /// </summary>
    public static double Next(double rate, double full, bool running, double seconds, double upSeconds, double downSeconds)
    {
        if (full <= 0) return 0;
        rate = Math.Max(0, Math.Min(full, rate));
        if (running)
        {
            if (upSeconds <= 0) return full;
            return Math.Min(full, rate + full * seconds / upSeconds);
        }
        if (downSeconds <= 0) return 0;
        return Math.Max(0, rate - full * seconds / downSeconds);
    }
}
