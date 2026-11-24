using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// A map's wind as a direction and a speed in knots (D'thorga Framework 0.288.0; docs/development/airdrop.md, "Wind"). The game keeps only
/// a speed (0.04 to 2, a share of an ordinary breeze); Combat Extended adds a direction that wanders and reads the speed as a Beaufort force.
/// This rule follows Combat Extended's mapping so both read alike. Game-free.
/// </summary>
public static class WindRule
{
    /// <summary>
    /// The midpoint of each Beaufort band in knots, force 0 to 9 (the bands are the World Meteorological Organization's; the midpoint is
    /// authored).
    /// </summary>
    public static readonly double[] BeaufortKnots = { 0, 2, 5, 8.5, 13.5, 19, 24.5, 30.5, 37, 44 };

    /// <summary>A Beaufort force, which may lie between two (Combat Extended keeps it as a fraction), in knots: read between the midpoints.</summary>
    public static double KnotsForBeaufort(double force)
    {
        if (double.IsNaN(force) || force <= 0) return 0;
        int top = BeaufortKnots.Length - 1;
        if (force >= top) return BeaufortKnots[top];
        int low = (int)Math.Floor(force);
        double share = force - low;
        return BeaufortKnots[low] + (BeaufortKnots[low + 1] - BeaufortKnots[low]) * share;
    }

    /// <summary>Combat Extended's mapping of the game's wind speed to a Beaufort force: three times it, at most nine.</summary>
    public static double BeaufortForGameWind(double gameWindSpeed) => Math.Min(9, Math.Max(0, 3 * gameWindSpeed));

    /// <summary>The game's wind speed in knots, by Combat Extended's mapping.</summary>
    public static double KnotsForGameWind(double gameWindSpeed) => KnotsForBeaufort(BeaufortForGameWind(gameWindSpeed));

    /// <summary>The most a wandering direction turns in one step, degrees (Combat Extended's figure: five, each rare tick).</summary>
    public const double WanderStepDegrees = 5;

    /// <summary>
    /// One step of a wandering direction toward its target, by Combat Extended's rule: within <paramref name="stepDegrees"/> (a random share of
    /// <see cref="WanderStepDegrees"/>), the short way round. Degrees clockwise from north, 0 to 360.
    /// </summary>
    public static double Wander(double bearing, double target, double stepDegrees)
    {
        double turn = ((target - bearing) % 360 + 540) % 360 - 180;
        double step = Math.Max(0, stepDegrees);
        double moved = Math.Abs(turn) <= step ? turn : Math.Sign(turn) * step;
        return ((bearing + moved) % 360 + 360) % 360;
    }

    /// <summary>Whether a wandering direction has reached its target, so a new one is drawn (within a degree, as Combat Extended's).</summary>
    public static bool Reached(double bearing, double target) => Math.Abs(((target - bearing) % 360 + 540) % 360 - 180) < 1;

    /// <summary>The downwind unit vector for a bearing (where the wind blows toward), east and north components.</summary>
    public static (double East, double North) Downwind(double bearingDegrees)
    {
        double rad = bearingDegrees * Math.PI / 180;
        return (Math.Sin(rad), Math.Cos(rad));
    }
}
