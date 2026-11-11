using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// When a rising reading will reach a level, from its history, game-free (Framework 0.86.0, the wear trends). Only the samples since the
/// last fall count, because a service or a new part sets wear back; a straight line is fitted to them (least squares), and the time to
/// the level is the gap over the slope. Wear moves only while a vehicle works, so the line is the rate the vehicle has been used at,
/// idle hours included: an estimate of the calendar, not of driving time.
/// </summary>
public static class TrendRule
{
    /// <summary>The first index of the current rise: walking back from the newest sample while each earlier one is no higher (within
    /// <paramref name="tolerance"/>).</summary>
    public static int RiseStart(int count, Func<int, double> at, double tolerance)
    {
        int start = count - 1;
        while (start > 0 && at(start - 1) <= at(start) + tolerance) start--;
        return Math.Max(0, start);
    }

    /// <summary>The least-squares slope per sample of the samples from <paramref name="from"/> to the newest, or null for fewer than two.</summary>
    public static double? Slope(int count, Func<int, double> at, int from)
    {
        int n = count - from;
        if (n < 2) return null;
        double meanX = (n - 1) / 2.0, meanY = 0;
        for (int i = 0; i < n; i++) meanY += at(from + i);
        meanY /= n;
        double covariance = 0, variance = 0;
        for (int i = 0; i < n; i++)
        {
            double dx = i - meanX;
            covariance += dx * (at(from + i) - meanY);
            variance += dx * dx;
        }
        return variance <= 0 ? null : covariance / variance;
    }

    /// <summary>
    /// How many sample intervals until the newest sample's rise reaches <paramref name="level"/>: 0 when it already has, null when the
    /// rise is too short (fewer than <paramref name="minSamples"/>) or not rising (a slope at or below <paramref name="flatSlope"/>).
    /// </summary>
    public static double? SamplesUntil(int count, Func<int, double> at, double level, int minSamples, double tolerance, double flatSlope)
    {
        if (count == 0) return null;
        double now = at(count - 1);
        if (now >= level) return 0;
        int start = RiseStart(count, at, tolerance);
        if (count - start < minSamples) return null;
        double? slope = Slope(count, at, start);
        if (slope is not double rising || rising <= flatSlope) return null;
        return (level - now) / rising;
    }
}
