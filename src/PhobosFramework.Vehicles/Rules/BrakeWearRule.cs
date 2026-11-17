using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Brake and clutch wear that follow what the move did (cross-links round two T4, built in Framework 0.123.0; owner, 17 Nov 2026), game-free.
/// Braking turns the vehicle's motion into heat in the pads, so the work grows with the mass being stopped and with the square of the speed
/// it is stopped from (kinetic energy, half m v squared); a clutch takes up the drive at each take-off, harder the heavier the load. The
/// trailer's mass counts, since the tractor brakes the whole train alone (a simplification already in the hitch design). The real basis is
/// physics; every limit and the clutch's slip curve are authored, a gameplay choice.
/// </summary>
public static class BrakeWearRule
{
    /// <summary>What the mass being moved does to the wear: everything aboard and behind against the vehicle's empty mass, never below 1
    /// (an empty vehicle wears as it always did) and never above <paramref name="most"/>. No empty mass known reads 1.</summary>
    public static double MassFactor(double totalKg, double towedKg, double kerbKg, double most)
    {
        if (kerbKg <= 0 || double.IsNaN(totalKg)) return 1;
        double ratio = (Math.Max(0, totalKg) + Math.Max(0, towedKg)) / kerbKg;
        return Math.Max(1, Math.Min(most, ratio));
    }

    /// <summary>What the speed it is stopped from does to the wear: the square of that speed against the vehicle's design speed, between
    /// <paramref name="least"/> and <paramref name="most"/>. An unknown speed or design speed reads 1.</summary>
    public static double SpeedFactor(double speed, double designSpeed, double least, double most)
    {
        if (speed <= 0 || designSpeed <= 0 || double.IsNaN(speed)) return 1;
        double share = speed / designSpeed;
        return Math.Max(least, Math.Min(most, share * share));
    }

    /// <summary>Where a clutch begins to slip, as a share of its wear (authored).</summary>
    public const double SlipsFrom = 0.7;
    /// <summary>What a clutch worn through still holds (authored): enough to creep, not to pull.</summary>
    public const double WornThroughHolds = 0.25;

    /// <summary>What a worn clutch can hold against a sound one: all of it up to <see cref="SlipsFrom"/>, then falling in a straight line
    /// to half at 0.95 worn and <see cref="WornThroughHolds"/> worn through.</summary>
    public static double ClutchHolds(double wear)
    {
        if (double.IsNaN(wear) || wear <= SlipsFrom) return 1;
        if (wear >= 1) return WornThroughHolds;
        if (wear <= 0.95) return 1 - 0.5 * (wear - SlipsFrom) / (0.95 - SlipsFrom);
        return 0.5 - (0.5 - WornThroughHolds) * (wear - 0.95) / 0.05;
    }
}

/// <summary>
/// Which of a vehicle's switched loads a hit to its wiring shorts out (cross-links round three G12, Framework 0.123.0), game-free: a pick
/// that looks random but is worked out again the same at every look from a seed (the vehicle and the part), so nothing about it is saved
/// and a loaded game shorts the same circuit.
/// </summary>
public static class WiringRule
{
    /// <summary><paramref name="howMany"/> different indices below <paramref name="candidates"/>, the same for the same seed; fewer when
    /// there are not that many candidates, none when there are none.</summary>
    public static int[] Pick(int seed, int candidates, int howMany)
    {
        if (candidates <= 0 || howMany <= 0) return Array.Empty<int>();
        howMany = Math.Min(howMany, candidates);
        var picked = new int[howMany];
        uint state = unchecked((uint)seed) ^ 0x9E3779B9u;
        int count = 0;
        while (count < howMany)
        {
            // A xorshift step: enough to spread small seeds over the candidates.
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            int index = (int)(state % (uint)candidates);
            if (Array.IndexOf(picked, index, 0, count) >= 0) continue;
            picked[count++] = index;
        }
        return picked;
    }
}

/// <summary>
/// How fast dust clogs an engine's air filter where the vehicle is (cross-links round three G8, with round two's T7; Framework 0.123.0),
/// game-free: dry country is dusty, so the rate rises as the yearly rainfall falls, and a sandstorm (weather with a sand rate) is far worse.
/// Dusty and sandy running shortens an air cleaner's life (US Army TM 9-2350-222-10-2, as cited in docs/development/vehicle-crosslinks.md);
/// the curve and the storm's factor are authored.
/// </summary>
public static class DustRule
{
    /// <summary>The rate's multiple in a sandstorm, on top of the dry country's (authored).</summary>
    public const double SandstormFactor = 4;

    /// <summary>The multiple of the ordinary rate: 3 in a desert with no rain, 1 from 1,000 mm a year up; times
    /// <see cref="SandstormFactor"/> while sand blows, unless the vehicle stands under a roof.</summary>
    public static double Factor(double rainfallMm, double sandRate, bool roofed)
    {
        double dry = rainfallMm <= 0 ? 3 : rainfallMm >= 1000 ? 1 : rainfallMm <= 250 ? 3 - 0.5 * rainfallMm / 250 : 2.5 - 1.5 * (rainfallMm - 250) / 750;
        return sandRate > 0 && !roofed ? dry * SandstormFactor : dry;
    }
}
