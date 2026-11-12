using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Where a trailer's axle stands and which way the trailer points, in map units (x east, z north) and degrees (0 north, 90 east).</summary>
public readonly struct TrailerPose
{
    public readonly double AxleX;
    public readonly double AxleZ;
    public readonly double Heading;

    public TrailerPose(double axleX, double axleZ, double heading)
    {
        AxleX = axleX;
        AxleZ = axleZ;
        Heading = heading;
    }
}

/// <summary>
/// How a towed trailer follows its hitch, game-free (owner, 11 Nov 2026: our own hitch, smoother than RimThunder's eight steps;
/// docs/development/vehicle-hitch-design.md). One rule: the trailer's axle stays one drawbar length behind the hitch and is dragged
/// toward it, so a long trailer straightens slowly, a short one quickly, and a trailer cuts the inside of a corner (the curve is a
/// tractrix; this form is our simplification and has not been checked against a reference). The jack-knife limit is an authored
/// angle, not a force; and the same rule is used in reverse, where a real trailer is unstable.
/// </summary>
public static class HitchRule
{
    /// <summary>The trailer after the hitch moved: its axle slid along the line toward the hitch until one drawbar length from it.</summary>
    public static TrailerPose Follow(double hitchX, double hitchZ, TrailerPose before, double drawbarLength)
    {
        double dx = hitchX - before.AxleX, dz = hitchZ - before.AxleZ;
        double distance = Math.Sqrt(dx * dx + dz * dz);
        if (distance < 1e-9) return AxleFor(hitchX, hitchZ, before.Heading, drawbarLength);
        double heading = HeadingOf(dx, dz);
        double length = Math.Max(0, drawbarLength);
        return new TrailerPose(hitchX - dx / distance * length, hitchZ - dz / distance * length, heading);
    }

    /// <summary>Holds the trailer within the jack-knife angle of the vehicle's own heading, its axle placed back behind the hitch.</summary>
    public static TrailerPose Limit(double hitchX, double hitchZ, TrailerPose pose, double vehicleHeading, double maxAngle, double drawbarLength)
    {
        double difference = Difference(pose.Heading, vehicleHeading);
        double limit = Math.Max(0, maxAngle);
        if (Math.Abs(difference) <= limit) return pose;
        return AxleFor(hitchX, hitchZ, Normalize(vehicleHeading + Math.Sign(difference) * limit), drawbarLength);
    }

    /// <summary>The trailer pointing a given way with its axle one drawbar length behind the hitch.</summary>
    public static TrailerPose AxleFor(double hitchX, double hitchZ, double heading, double drawbarLength)
    {
        double radians = heading * Math.PI / 180.0;
        double length = Math.Max(0, drawbarLength);
        return new TrailerPose(hitchX - Math.Sin(radians) * length, hitchZ - Math.Cos(radians) * length, Normalize(heading));
    }

    /// <summary>The heading of a direction: 0 north (+z), 90 east (+x), within [0, 360).</summary>
    public static double HeadingOf(double dx, double dz) => Normalize(Math.Atan2(dx, dz) * 180.0 / Math.PI);

    /// <summary>The signed difference a minus b, within (-180, 180].</summary>
    public static double Difference(double a, double b)
    {
        double d = Normalize(a - b);
        return d > 180 ? d - 360 : d;
    }

    public static double Normalize(double degrees)
    {
        double d = degrees % 360;
        return d < 0 ? d + 360 : d;
    }

    // ---- Placing the trailer on the map (slice 2, Framework 0.101.0) ----

    /// <summary>The middle of the trailer's picture: <paramref name="axleToCentre"/> ahead of its axle along its heading.</summary>
    public static (double X, double Z) CentreOf(TrailerPose pose, double axleToCentre)
    {
        double radians = pose.Heading * Math.PI / 180.0;
        return (pose.AxleX + Math.Sin(radians) * axleToCentre, pose.AxleZ + Math.Cos(radians) * axleToCentre);
    }

    /// <summary>The way a thing faces nearest a heading, as the game counts it: 0 north, 1 east, 2 south, 3 west.</summary>
    public static int Cardinal(double heading) => (int)Math.Round(Normalize(heading) / 90.0) % 4;

    /// <summary>What is left of a heading after its facing: the extra turn the picture is drawn with, within [-45, 45] degrees.</summary>
    public static double Remainder(double heading) => Difference(heading, Cardinal(heading) * 90.0);

    /// <summary>
    /// The middle of a footprint standing on a cell, as Vehicle Framework works it out (VehiclePawn.TrueCenter): the cell's middle, moved
    /// half a cell where the footprint's width or length is even. A footprint turned east or west swaps its width and length.
    /// </summary>
    public static (double X, double Z) TrueCentre(int cellX, int cellZ, int sizeX, int sizeZ, int facing)
    {
        bool sideways = facing == 1 || facing == 3;
        int wide = sideways ? sizeZ : sizeX, deep = sideways ? sizeX : sizeZ;
        double x = cellX + 0.5, z = cellZ + 0.5;
        if (wide % 2 == 0) x += 0.5;
        if (deep % 2 == 0) z += sideways ? -0.5 : 0.5;
        return (x, z);
    }

    /// <summary>The cell to stand a footprint on so that its middle is as near a point as the grid allows.</summary>
    public static (int X, int Z) CellFor(double centreX, double centreZ, int sizeX, int sizeZ, int facing)
    {
        (double offX, double offZ) = TrueCentre(0, 0, sizeX, sizeZ, facing);
        return ((int)Math.Round(centreX - offX), (int)Math.Round(centreZ - offZ));
    }

    /// <summary>
    /// A tow without the drivetrain model (a vehicle of another author's with no drivetrain of ours, on the player's setting): the share of
    /// its speed a vehicle keeps is its own mass's share of the whole train, never below <paramref name="floor"/>. A gameplay
    /// simplification, not physics: it ignores the engine, the ground and the grade.
    /// </summary>
    public static double PlainCap(double vehicleKg, double towedKg, double floor)
    {
        if (towedKg <= 0) return 1;
        if (vehicleKg <= 0) return Math.Max(0, floor);
        return Math.Max(floor, vehicleKg / (vehicleKg + towedKg));
    }
}
