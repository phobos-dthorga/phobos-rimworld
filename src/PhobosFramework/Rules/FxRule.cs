using System;

namespace Phobos.Framework.Rules;

/// <summary>How dense a thing's visual effects are drawn (Framework 0.321.0; the owner's card of 26 Nov 2026: Off, Light or Full, Full to start).</summary>
public enum FxLevel
{
    Off,
    Light,
    Full,
}

/// <summary>
/// The arithmetic of the visual effects emitter, game-free (Framework 0.321.0; docs/development/vehicle-effects.md). Effects are asked for as
/// a rate per game hour and spread evenly over the emitter's short passes, so a puff a minute never comes as a clump every four seconds;
/// places on a thing are given along its heading (forward and to its right) and turned into map offsets here.
/// </summary>
public static class FxRule
{
    /// <summary>Game ticks in an hour.</summary>
    public const int TicksPerHour = 2500;

    /// <summary>
    /// How many to make in this pass: the whole number expected over <paramref name="ticks"/> at <paramref name="perHour"/>, and one more when
    /// <paramref name="roll"/> (0 to 1) falls under the fraction left over, so the long-run rate is exact.
    /// </summary>
    public static int Count(double perHour, int ticks, double roll)
    {
        if (perHour <= 0 || ticks <= 0) return 0;
        double expected = perHour * ticks / TicksPerHour;
        int whole = (int)Math.Floor(expected);
        return whole + (roll < expected - whole ? 1 : 0);
    }

    /// <summary>The map offset (x east, z north) of a place <paramref name="forward"/> cells ahead and <paramref name="right"/> cells to the
    /// right of a thing facing <paramref name="headingDegrees"/> (the game's angles: 0 north, clockwise).</summary>
    public static (double x, double z) Offset(double headingDegrees, double forward, double right)
    {
        double h = headingDegrees * Math.PI / 180.0;
        double sin = Math.Sin(h), cos = Math.Cos(h);
        return (forward * sin + right * cos, forward * cos - right * sin);
    }

    /// <summary>The share of a moving thing's effects drawn at this level: none off, <paramref name="lightShare"/> at Light, all at Full. A
    /// fault's own sign is never thinned (<paramref name="isSign"/>): Light thins only what moving vehicles throw up.</summary>
    public static double Density(FxLevel level, bool isSign, double lightShare) => level switch
    {
        FxLevel.Off => 0,
        FxLevel.Light => isSign ? 1 : Math.Clamp(lightShare, 0, 1),
        _ => 1,
    };

    /// <summary>How much a rotor's downwash still reaches the ground: full on the ground with the rotor at speed, falling straight to nothing
    /// at <paramref name="reachMetres"/> above it, and in step with the rotor's speed.</summary>
    public static double Downwash(double rotorShare, double heightMetres, double reachMetres)
    {
        if (rotorShare <= 0 || reachMetres <= 0 || heightMetres >= reachMetres) return 0;
        return Math.Clamp(rotorShare, 0, 1) * (1 - Math.Max(0, heightMetres) / reachMetres);
    }

    /// <summary>The exhaust's puffs an hour at this engine load: <paramref name="cruise"/> from a light load, rising to <paramref name="hard"/>
    /// at full load, nothing below the idle load (an idling engine shows nothing).</summary>
    public static double ExhaustRate(double load, double idleLoad, double cruise, double hard)
    {
        if (load <= idleLoad) return 0;
        double share = Math.Clamp((load - idleLoad) / Math.Max(1e-6, 1 - idleLoad), 0, 1);
        return cruise + (hard - cruise) * share * share;
    }

    /// <summary>
    /// Whether a lamp is lit at this tick (Framework 0.326.0, slice 3): a steady lamp always; a flashing one for the first
    /// <paramref name="litShare"/> of each period of <paramref name="periodTicks"/>, started <paramref name="phase"/> (0 to 1) of a period
    /// late, so two lamps half a period apart take turns (a recovery truck's pair).
    /// </summary>
    public static bool LampLit(bool flashing, long ticks, double periodTicks, double phase, double litShare)
    {
        if (!flashing) return true;
        if (periodTicks <= 0 || litShare <= 0) return false;
        double at = (ticks / periodTicks - phase) % 1.0;
        if (at < 0) at += 1;
        return at < Math.Min(1, litShare);
    }

    /// <summary>Whether a vehicle is slowing hard enough to light its brake lamps (Framework 0.326.0): it was moving at
    /// <paramref name="minSpeed"/> or more and has lost <paramref name="dropShare"/> of that speed or more since the last look, stopping
    /// included.</summary>
    public static bool Slowing(double previousSpeed, double speed, double minSpeed, double dropShare) =>
        previousSpeed >= minSpeed && speed <= previousSpeed * (1 - Math.Clamp(dropShare, 0, 1));

    /// <summary>What is left of a thrown thing's speed after <paramref name="seconds"/> in air that slows it by a time constant of
    /// <paramref name="timeConstantSeconds"/> (a flare thrown from an aircraft, Framework 0.326.0).</summary>
    public static double Drag(double seconds, double timeConstantSeconds) =>
        timeConstantSeconds <= 0 ? 0 : Math.Exp(-Math.Max(0, seconds) / timeConstantSeconds);
}
