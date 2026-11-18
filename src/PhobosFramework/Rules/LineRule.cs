using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Ropes and a hoist from a hovering aircraft (D'thorga Framework 0.155.0; docs/development/vehicle-flight-over-the-map.md, slice 6): how
/// long a lift takes, who the hoist can take, and what a fall from a line does. Heights in metres, speeds in metres a second. Game-free.
/// </summary>
public static class LineRule
{
    /// <summary>A pound in kilograms (the international pound).</summary>
    public const double KilogramsPerPound = 0.45359237;

    /// <summary>Feet a minute in metres a second.</summary>
    public static double FeetPerMinute(double feetPerMinute) => feetPerMinute * 0.3048 / 60;

    /// <summary>The share of its flight height it comes down to for line work: the work height over the flight height, at most 1.</summary>
    public static double WorkClimb(double workMetres, double flightMetres) =>
        flightMetres <= 0 ? 1 : Math.Min(1, Math.Max(0.05, workMetres / flightMetres));

    /// <summary>Whether the hoist takes a load: at or under its rated load.</summary>
    public static bool HoistTakes(double kilograms, double ratedKilograms) => kilograms <= ratedKilograms;

    /// <summary>
    /// How long one lift takes, seconds: the hook lowered empty, the hook-up, the load raised. A person lowered is the same the other way
    /// round (lowered loaded, raised empty), so the figure serves both.
    /// </summary>
    public static double LiftSeconds(double heightMetres, double emptyMetresPerSecond, double loadedMetresPerSecond, double hookSeconds) =>
        Math.Max(0, heightMetres) / Math.Max(0.01, emptyMetresPerSecond) + Math.Max(0, hookSeconds)
        + Math.Max(0, heightMetres) / Math.Max(0.01, loadedMetresPerSecond);

    /// <summary>How high above the ground a person on the hoist is: the aircraft's height less the cable paid out.</summary>
    public static double HeightOnCable(double aircraftMetres, double cableOutMetres) => Math.Max(0, aircraftMetres - Math.Max(0, cableOutMetres));

    /// <summary>The harm points a fall per metre does (authored: real consequences, owner's choice of 19 Nov 2026; a fall from the work height
    /// of 20 m does 40, enough to break bones and sometimes kill, as such a fall can).</summary>
    public const double FallHarmPerMetre = 2;

    /// <summary>Falls shorter than this do nothing: a step or a jump (authored).</summary>
    public const double HarmlessFallMetres = 1.5;

    /// <summary>The blunt harm a fall from a line does, in the game's damage points, spread over <see cref="FallBlows"/> blows.</summary>
    public static double FallHarm(double metres) => metres <= HarmlessFallMetres ? 0 : (metres - HarmlessFallMetres) * FallHarmPerMetre;

    /// <summary>How many blows a fall's harm is dealt as, so it lands on several parts as a fall does (authored).</summary>
    public const int FallBlows = 3;
}
