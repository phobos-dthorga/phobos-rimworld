using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>How a dipole is strung (Radio 0.62.0, the owner's card of 28 Nov 2026: all three offered).</summary>
public enum DipoleShape
{
    /// <summary>Flat between two masts at its ends.</summary>
    Flat,
    /// <summary>An inverted V: one mast in the middle, the wires sloping to pegs at the ends.</summary>
    Vee,
    /// <summary>Shortened: two masts at its ends, a loading coil in each leg.</summary>
    Short,
}

/// <summary>One piece of a dipole's picture: which part, its centre and size in cells from the building's centre (x east, z north), its
/// turn in degrees anticlockwise, and its order (higher drawn over lower).</summary>
public readonly record struct DipolePiece(string Part, double X, double Z, double Width, double Height, double AngleDegrees, int Order);

/// <summary>
/// Where a dipole's parts go (Radio 0.64.0; docs/development/radio-hf.md, the masts): the masts drawn upright as the game draws trees, the
/// wire from their tops with a little sag, the parts along it. The same layout as the placement ghost's picture
/// (assets/phobos-radio/compose_dipoles.py). Game-free, so it is tested; the game prints the pieces into the map's mesh.
/// </summary>
public static class DipoleLayout
{
    /// <summary>The share of a guyed mast picture's height from its top down to its footing (measured from the pictures); a lattice
    /// tower stands on the foot of its picture.</summary>
    public const double Foot = 0.75;
    public const double TowerFoot = 0.97;
    public const double WireCells = 0.05;
    public const double CoaxCells = 0.04;
    const int SagSteps = 6;

    /// <summary>The pieces for a dipole <paramref name="length"/> cells long, its mast pictures <paramref name="mastCells"/> tall and
    /// <paramref name="mastWidthCells"/> wide, running along x (north and south facings) or along z (east and west).</summary>
    public static List<DipolePiece> Pieces(DipoleShape shape, int length, double mastCells, double mastWidthCells, double mastFoot, bool alongZ,
        (double w, double h) feed, (double w, double h) coil, (double w, double h) insulator, (double w, double h) peg)
    {
        var pieces = new List<DipolePiece>();
        (double x, double z) Ground(double u) => alongZ ? (0, u) : (u, 0);
        (double x, double z) Top(double u)
        {
            var (x, z) = Ground(u);
            return (x, z + mastCells * mastFoot - mastCells * 0.03);
        }
        void Mast(double u, int order)
        {
            var (x, z) = Ground(u);
            pieces.Add(new DipolePiece("mast", x, z + mastCells * (mastFoot - 0.5), mastWidthCells, mastCells, 0, order));
        }
        void Wire(List<(double x, double z)> line, int order)
        {
            for (int i = 1; i < line.Count; i++)
                pieces.Add(Segment("wire", line[i - 1], line[i], WireCells, order));
        }
        void On(string part, (double w, double h) size, List<(double x, double z)> line, double t, int order)
        {
            var (p, angle) = Along(line, t);
            pieces.Add(new DipolePiece(part, p.x, p.z, size.w, size.h, angle, order));
        }

        double half = length / 2.0;
        if (shape == DipoleShape.Vee)
        {
            var apex = Top(0);
            Mast(0, 0);
            var foot = Ground(0);
            pieces.Add(Segment("coax", apex, (foot.x, foot.z + mastCells * 0.05), CoaxCells, 1));
            foreach (double end in new[] { -half + 0.35, half - 0.35 })
            {
                var g = Ground(end);
                var line = Sagged(apex, (g.x, g.z + 0.06), 0.04);
                Wire(line, 2);
                On("insulator", insulator, line, 0.9, 3);
                pieces.Add(new DipolePiece("peg", g.x, g.z + peg.h * 0.25, peg.w, peg.h, 0, 3));
            }
            pieces.Add(new DipolePiece("feed", apex.x, apex.z - feed.h * 0.4, feed.w, feed.h, 0, 4));
            return pieces;
        }
        double a = -half + 0.5, b = half - 0.5;
        var wire = Sagged(Top(a), Top(b), shape == DipoleShape.Flat ? 0.12 : 0.08);
        var mid = Along(wire, 0.5).p;
        var centre = Ground(0);
        // Along z (east and west) the wire runs over the far mast, so the masts are drawn over it there.
        int mastOrder = alongZ ? 3 : 0;
        Mast(a, mastOrder);
        Mast(b, mastOrder);
        pieces.Add(Segment("coax", mid, centre, CoaxCells, 1));
        Wire(wire, 2);
        On("insulator", insulator, wire, 0.04, 4);
        On("insulator", insulator, wire, 0.96, 4);
        if (shape == DipoleShape.Short)
        {
            On("coil", coil, wire, 0.3, 4);
            On("coil", coil, wire, 0.7, 4);
        }
        pieces.Add(new DipolePiece("feed", mid.x, mid.z - feed.h * 0.2, feed.w, feed.h, 0, 5));
        return pieces;
    }

    /// <summary>A wire from a to b hanging by <paramref name="sag"/> cells at its middle (a parabola, near enough at this size).</summary>
    public static List<(double x, double z)> Sagged((double x, double z) a, (double x, double z) b, double sag)
    {
        var points = new List<(double, double)>(SagSteps + 1);
        for (int i = 0; i <= SagSteps; i++)
        {
            double t = i / (double)SagSteps;
            points.Add((a.x + (b.x - a.x) * t, a.z + (b.z - a.z) * t - sag * 4 * t * (1 - t)));
        }
        return points;
    }

    /// <summary>The point a share t of the way along a line, and its angle there in degrees anticlockwise from east.</summary>
    public static ((double x, double z) p, double angle) Along(List<(double x, double z)> line, double t)
    {
        double total = 0;
        for (int i = 1; i < line.Count; i++) total += Dist(line[i - 1], line[i]);
        double goal = t * total, run = 0;
        for (int i = 1; i < line.Count; i++)
        {
            double d = Dist(line[i - 1], line[i]);
            if (run + d >= goal || i == line.Count - 1)
            {
                double s = d > 0 ? (goal - run) / d : 0;
                var (x0, z0) = line[i - 1];
                var (x1, z1) = line[i];
                return ((x0 + (x1 - x0) * s, z0 + (z1 - z0) * s), Math.Atan2(z1 - z0, x1 - x0) * 180 / Math.PI);
            }
            run += d;
        }
        return (line[0], 0);
    }

    static DipolePiece Segment(string part, (double x, double z) a, (double x, double z) b, double thickness, int order) =>
        new(part, (a.x + b.x) / 2, (a.z + b.z) / 2, Math.Max(Dist(a, b), 0.001), thickness, Math.Atan2(b.z - a.z, b.x - a.x) * 180 / Math.PI, order);

    static double Dist((double x, double z) a, (double x, double z) b) => Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
}
