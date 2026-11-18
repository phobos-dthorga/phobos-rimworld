using System;

namespace Phobos.Framework.Rules;

/// <summary>What the engines leave an aircraft able to do in the air, from its hover margin (1 is just enough to hover).</summary>
public enum FlightHold
{
    /// <summary>It can hover: lift straight up, hold still, settle straight down.</summary>
    Hover,
    /// <summary>It cannot hover but can stay up moving forward: it must keep flying and come down on the move.</summary>
    ForwardOnly,
    /// <summary>It cannot stay up at all: it comes down where it is.</summary>
    Falling,
}

/// <summary>
/// Our own flight over a map, the hover half (D'thorga Framework 0.147.0; docs/development/vehicle-flight-over-the-map.md): turning, speeding up
/// and braking toward a point, the eight headings, what the hover margin leaves the aircraft able to do, and what a forced landing costs.
/// Angles are degrees clockwise from north, as the game's and Vehicle Framework's are; distances and speeds in cells. Game-free.
/// </summary>
public static class FlightPathRule
{
    /// <summary>An angle brought into [0, 360).</summary>
    public static double Normalise(double degrees)
    {
        double a = degrees % 360;
        return a < 0 ? a + 360 : a;
    }

    /// <summary>The signed shortest turn from one heading to another, in (-180, 180]: positive is clockwise.</summary>
    public static double Turn(double from, double to)
    {
        double d = Normalise(to - from);
        return d > 180 ? d - 360 : d;
    }

    /// <summary>The heading from one point to another (east is +x, north is +z), clockwise from north; the current heading where they meet.</summary>
    public static double HeadingTo(double dx, double dz, double current)
    {
        if (Math.Abs(dx) < 1e-9 && Math.Abs(dz) < 1e-9) return current;
        return Normalise(Math.Atan2(dx, dz) * 180 / Math.PI);
    }

    /// <summary>Turns toward a heading by at most <paramref name="maxStep"/> degrees.</summary>
    public static double TurnToward(double current, double target, double maxStep)
    {
        double d = Turn(current, target);
        if (Math.Abs(d) <= maxStep) return Normalise(target);
        return Normalise(current + Math.Sign(d) * maxStep);
    }

    /// <summary>The nearest of the eight headings, as a multiple of 45 in [0, 360).</summary>
    public static double NearestOfEight(double heading) => Normalise(Math.Round(Normalise(heading) / 45) * 45);

    /// <summary>The nearest of the four headings, as a multiple of 90 in [0, 360).</summary>
    public static double NearestOfFour(double heading) => Normalise(Math.Round(Normalise(heading) / 90) * 90);

    /// <summary>
    /// The speed for the next step toward a point: speeding up by <paramref name="accelerationStep"/> to <paramref name="maxSpeed"/>, but never
    /// faster than lets it stop in the distance left at that rate (v = sqrt(2 a d), per step), so it arrives at rest.
    /// <paramref name="acceleration"/> is per second squared; speeds per second; the step is <paramref name="seconds"/> long.
    /// </summary>
    public static double NextSpeed(double current, double distanceLeft, double maxSpeed, double acceleration, double seconds)
    {
        if (distanceLeft <= 0 || maxSpeed <= 0) return 0;
        double canStopFrom = Math.Sqrt(2 * Math.Max(0.01, acceleration) * distanceLeft);
        double wanted = Math.Min(maxSpeed, canStopFrom);
        double change = Math.Max(0.01, acceleration) * seconds;
        return current < wanted ? Math.Min(wanted, current + change) : Math.Max(wanted, current - change);
    }

    /// <summary>
    /// A speed share of the full speed while the aircraft is still turning onto its course: full when within 15 degrees, a quarter when
    /// facing away (authored, so a hovering aircraft turns on the spot before it runs off sideways).
    /// </summary>
    public static double TurnSpeedShare(double turnLeft)
    {
        double a = Math.Abs(turnLeft);
        if (a <= 15) return 1;
        if (a >= 90) return 0.25;
        return 1 - 0.75 * (a - 15) / 75;
    }

    /// <summary>What the hover margin leaves the aircraft able to do: hover at 1 or more, fly on down to the forward-flight share, else fall.</summary>
    public static FlightHold Judge(double margin, double forwardFlightShare)
    {
        if (double.IsNaN(margin)) return FlightHold.Hover;
        if (margin >= 1) return FlightHold.Hover;
        return margin >= Math.Min(1, Math.Max(0, forwardFlightShare)) ? FlightHold.ForwardOnly : FlightHold.Falling;
    }

    /// <summary>The least share of a part's full health a forced landing takes (authored: a firm arrival with the engines still turning).</summary>
    public const double ForcedLandingLeastShare = 0.1;

    /// <summary>The most it takes (authored: a fall with nothing left to hold it).</summary>
    public const double ForcedLandingMostShare = 0.6;

    /// <summary>
    /// The share of one part's full health a forced landing on open ground takes from the undercarriage and the airframe: the least when it
    /// had almost enough to hover, rising in a straight line to the most when the engines gave nothing (authored, from the owner's "real
    /// consequences" of 18 Nov 2026: "by how far short of a hover it was").
    /// </summary>
    public static double ForcedLandingShare(double margin)
    {
        double shortfall = double.IsNaN(margin) ? 1 : Math.Min(1, Math.Max(0, 1 - margin));
        return ForcedLandingLeastShare + (ForcedLandingMostShare - ForcedLandingLeastShare) * shortfall;
    }

    /// <summary>The blunt harm, in the game's damage points, a forced landing does each person aboard: a jolt that grows with the shortfall
    /// (authored: 2 to 10, bruises at worst for a healthy adult).</summary>
    public static double ForcedLandingHarm(double margin)
    {
        double shortfall = double.IsNaN(margin) ? 1 : Math.Min(1, Math.Max(0, 1 - margin));
        return 2 + 8 * shortfall;
    }

    /// <summary>
    /// Where a line from a point along a heading meets the edge of a rectangle (Framework 0.149.0: where an aircraft leaves a map toward its
    /// destination, and where one coming from a place enters it). The point is taken inside the rectangle; the heading clockwise from north.
    /// </summary>
    public static (double X, double Z) EdgePoint(double x, double z, double heading, double minX, double maxX, double minZ, double maxZ)
    {
        (x, z) = Clamp(x, z, minX, maxX, minZ, maxZ);
        double rad = heading * Math.PI / 180;
        double dx = Math.Sin(rad), dz = Math.Cos(rad);
        double t = double.PositiveInfinity;
        if (dx > 1e-9) t = Math.Min(t, (maxX - x) / dx);
        else if (dx < -1e-9) t = Math.Min(t, (minX - x) / dx);
        if (dz > 1e-9) t = Math.Min(t, (maxZ - z) / dz);
        else if (dz < -1e-9) t = Math.Min(t, (minZ - z) / dz);
        if (double.IsInfinity(t)) return (x, z);
        return Clamp(x + dx * t, z + dz * t, minX, maxX, minZ, maxZ);
    }

    /// <summary>A point brought inside a rectangle (for the true position held inside the map less half the aircraft's longest side).</summary>
    public static (double X, double Z) Clamp(double x, double z, double minX, double maxX, double minZ, double maxZ) =>
        (Math.Min(maxX, Math.Max(minX, x)), Math.Min(maxZ, Math.Max(minZ, z)));

    /// <summary>
    /// Where the shadow falls from the body, in cells: it slides south-east and away as the aircraft climbs, as a low sun would cast it
    /// (authored, for the eye: a quarter of the flight height in cells, split between east and south). <paramref name="climb"/> is 0 on
    /// the ground to 1 at flight height.
    /// </summary>
    public static (double X, double Z) ShadowOffset(double climb, double heightCells)
    {
        double c = Math.Min(1, Math.Max(0, climb)) * Math.Max(0, heightCells) * 0.25;
        return (c * 0.6, -c * 0.8);
    }
}
