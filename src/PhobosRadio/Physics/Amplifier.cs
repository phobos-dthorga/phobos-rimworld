using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// A linear amplifier in a station's feedline (Radio 0.5.0): it raises what the station sends by its gain, never above its rated
/// output, and costs a little on receive, when it is switched out of the line. Game-free; compiled into the unit tests.
/// </summary>
public static class Amplifier
{
    /// <summary>What comes out, in watts: the drive raised by the gain, capped at the rating.</summary>
    public static double OutputWatts(double driveWatts, double gainDb, double ratedWatts)
    {
        if (driveWatts <= 0) return 0;
        return Math.Min(driveWatts * Math.Pow(10.0, gainDb / 10.0), Math.Max(ratedWatts, 0));
    }

    /// <summary>
    /// What a station sends through the amplifiers in its cable run: the strongest powered one decides (they are not chained, an
    /// authored simplification); with none powered, the station's own power, less each idle amplifier's loss on the way through.
    /// </summary>
    public static double ThroughWatts(double driveWatts, ReadOnlySpan<(double gainDb, double ratedWatts, double lossDb, bool powered)> amplifiers)
    {
        var best = 0.0;
        var anyPowered = false;
        var idleLossDb = 0.0;
        foreach (var (gainDb, ratedWatts, lossDb, powered) in amplifiers)
        {
            if (powered)
            {
                anyPowered = true;
                best = Math.Max(best, OutputWatts(driveWatts, gainDb, ratedWatts));
            }
            else
            {
                idleLossDb += lossDb;
            }
        }
        return anyPowered ? best : driveWatts * Math.Pow(10.0, -idleLossDb / 10.0);
    }
}
