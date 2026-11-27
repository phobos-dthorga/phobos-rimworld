using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>How a part of a schematic reads at a glance: nothing to show, sound, worth a look, or stopping the machine.</summary>
public enum SchematicTone
{
    Off,
    Good,
    Caution,
    Bad,
}

/// <summary>
/// The arithmetic behind a live systems schematic (Framework 0.363.0; docs/development/vehicle-schematic.md, SC1 and SC9), game-free so
/// any game's page can share it: fitting a drawing into the space it is given, where along a pipe a drifting mark sits, and the tone of a
/// part from its reading and its health. Points are drawing units, x to the right and y down.
/// </summary>
public static class SchematicRule
{
    /// <summary>A part's health below this share reads red, as on the game's Health tab.</summary>
    public const double HealthBadBelow = 0.4;
    /// <summary>A part's health below this share, and not red, reads as worth a look.</summary>
    public const double HealthCautionBelow = 0.7;
    /// <summary>A part doing less than this share of its work stops what flows through it (the fuel feed's "none" line, 0.1).</summary>
    public const double StopsFlowBelow = 0.1;

    /// <summary>How a drawing of <paramref name="width"/> by <paramref name="height"/> units fits a space: one scale for both directions, so
    /// nothing is stretched, and the offset that centres it.</summary>
    public static (double scale, double x, double y) Fit(double width, double height, double spaceWidth, double spaceHeight)
    {
        if (width <= 0 || height <= 0 || spaceWidth <= 0 || spaceHeight <= 0) return (0, 0, 0);
        double scale = Math.Min(spaceWidth / width, spaceHeight / height);
        return (scale, (spaceWidth - width * scale) / 2, (spaceHeight - height * scale) / 2);
    }

    /// <summary>A pipe's length: the sum of its straight runs.</summary>
    public static double Length(IReadOnlyList<(double x, double y)> points)
    {
        double length = 0;
        for (int i = 1; i < points.Count; i++) length += Distance(points[i - 1], points[i]);
        return length;
    }

    /// <summary>The point <paramref name="distance"/> along a pipe from its first point, held at its ends.</summary>
    public static (double x, double y) PointAlong(IReadOnlyList<(double x, double y)> points, double distance)
    {
        if (points.Count == 0) return (0, 0);
        if (distance <= 0 || points.Count == 1) return points[0];
        for (int i = 1; i < points.Count; i++)
        {
            double run = Distance(points[i - 1], points[i]);
            if (distance <= run && run > 0)
            {
                double t = distance / run;
                return (points[i - 1].x + (points[i].x - points[i - 1].x) * t, points[i - 1].y + (points[i].y - points[i - 1].y) * t);
            }
            distance -= run;
        }
        return points[points.Count - 1];
    }

    /// <summary>
    /// Where the drifting marks sit along a pipe of <paramref name="length"/>: one every <paramref name="spacing"/>, all moved on by
    /// <paramref name="phase"/>, so a phase that grows with time carries them along the pipe and one that stops holds them still.
    /// </summary>
    public static IEnumerable<double> MarkDistances(double length, double spacing, double phase)
    {
        if (length <= 0 || spacing <= 0) yield break;
        double first = phase % spacing;
        if (first < 0) first += spacing;
        for (double d = first; d < length; d += spacing) yield return d;
    }

    /// <summary>A part's tone from its health share: red below <see cref="HealthBadBelow"/>, worth a look below
    /// <see cref="HealthCautionBelow"/>, else sound.</summary>
    public static SchematicTone FromHealth(double share) =>
        share < HealthBadBelow ? SchematicTone.Bad : share < HealthCautionBelow ? SchematicTone.Caution : SchematicTone.Good;

    /// <summary>The worse of two tones; off counts as the least.</summary>
    public static SchematicTone Worst(SchematicTone a, SchematicTone b) => a >= b ? a : b;

    /// <summary>Whether something flows through a pipe: the machine running, and the part that drives it doing at least
    /// <see cref="StopsFlowBelow"/> of its work and not in a stage that stops it.</summary>
    public static bool Flows(bool running, double driverWork, bool driverStopped) => running && driverWork >= StopsFlowBelow && !driverStopped;

    static double Distance((double x, double y) a, (double x, double y) b)
    {
        double dx = b.x - a.x, dy = b.y - a.y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
