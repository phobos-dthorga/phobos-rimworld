using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Circling (D'thorga Framework 0.153.0; docs/development/vehicle-flight-over-the-map.md, slice 5): an aircraft flying round a centre at a
/// radius, steering at the circle's tangent corrected by how far off the radius it is. Angles are degrees clockwise from north, as
/// <see cref="FlightPathRule"/>'s are; distances in cells, speeds in cells a second. Game-free.
/// </summary>
public static class OrbitRule
{
    /// <summary>The most it turns in or out of the tangent to get back onto its circle, degrees (authored: far outside, it flies almost
    /// straight at the centre and curves onto the circle; at the centre, almost straight out).</summary>
    public const double MaxCorrectionDegrees = 70;

    /// <summary>The share of the radius off the circle at which the full correction is reached (authored).</summary>
    public const double CorrectionBandShare = 0.5;

    /// <summary>The circle sizes a player picks from, as multiples of the aircraft's tightest circle (authored presets, owner's rule of
    /// buttons over sliders).</summary>
    public static readonly double[] SizeShares = { 1.0, 1.6, 2.5 };

    /// <summary>
    /// The tightest circle a turn rate allows at a speed, cells: the speed over the turn rate in radians a second (r = v / omega, the
    /// geometry of any steady turn). A Def whose circle is tighter than this could not fly it.
    /// </summary>
    public static double TightestRadius(double speed, double turnDegreesPerSecond) =>
        turnDegreesPerSecond <= 0 ? double.PositiveInfinity : Math.Max(0, speed) / (turnDegreesPerSecond * Math.PI / 180);

    /// <summary>
    /// The heading to steer now to fly round a centre: along the circle's tangent the way round it goes, turned in toward the centre when
    /// outside the circle and out when inside, by up to <see cref="MaxCorrectionDegrees"/>. At the centre itself, the current heading.
    /// </summary>
    public static double Heading(double x, double z, double cx, double cz, double radius, bool clockwise, double current)
    {
        double dx = x - cx, dz = z - cz;
        double r = Math.Sqrt(dx * dx + dz * dz);
        if (r < 1e-6) return FlightPathRule.Normalise(current);
        double bearing = Math.Atan2(dx, dz) * 180 / Math.PI;
        double band = Math.Max(1, radius * CorrectionBandShare);
        double correction = MaxCorrectionDegrees * Math.Min(1, Math.Max(-1, (r - radius) / band));
        return FlightPathRule.Normalise(clockwise ? bearing + 90 + correction : bearing - 90 - correction);
    }

    /// <summary>
    /// The centre of a circle that passes through a point with its tangent along a heading: to the right of the heading going clockwise,
    /// to the left going anticlockwise. Where an aircraft that cannot stop begins to circle without a jink.
    /// </summary>
    public static (double X, double Z) CentreFrom(double x, double z, double heading, double radius, bool clockwise)
    {
        double rad = (heading + (clockwise ? 90 : -90)) * Math.PI / 180;
        return (x + Math.Sin(rad) * radius, z + Math.Cos(rad) * radius);
    }

    /// <summary>The largest radius, no more than the one wanted, whose circle fits inside a rectangle.</summary>
    public static double FitRadius(double radius, double minX, double maxX, double minZ, double maxZ) =>
        Math.Max(0, Math.Min(radius, Math.Min(maxX - minX, maxZ - minZ) / 2));

    /// <summary>A centre moved, as little as it must be, so a circle of this radius round it stays inside a rectangle.</summary>
    public static (double X, double Z) FitCentre(double cx, double cz, double radius, double minX, double maxX, double minZ, double maxZ)
    {
        static double Fit(double c, double r, double min, double max) =>
            max - min <= 2 * r ? (min + max) / 2 : Math.Min(max - r, Math.Max(min + r, c));
        return (Fit(cx, radius, minX, maxX), Fit(cz, radius, minZ, maxZ));
    }
}
