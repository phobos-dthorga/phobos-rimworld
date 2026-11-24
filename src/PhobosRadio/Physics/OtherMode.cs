using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// A second mode on a path already worked out (Radio 0.15.0; docs/development/autonomous-vehicles.md, step 6: "the picture mode reuses the
/// control mode's walk, with its own bandwidth and threshold"). The path loss, antennas and walls are the same for both; only the noise floor
/// (it rises with the bandwidth) and the signal-to-noise ratio the mode asks differ. So the second margin follows from the first with no
/// second walk across the map.
/// <para>
/// An approximation, labelled as such: it counts the whole floor as rising with the bandwidth. That is exact where thermal noise rules. The
/// colony's own noise is set per source and does not rise, so near a noisy building the wider mode does a little better than this says.
/// </para>
/// </summary>
public static class OtherMode
{
    /// <summary>The margin, in dB, of another mode on the same path, from this mode's margin.</summary>
    public static double MarginDb(double marginDb, double bandwidthHz, double requiredSnrDb, double otherBandwidthHz, double otherRequiredSnrDb)
    {
        if (bandwidthHz <= 0 || otherBandwidthHz <= 0) throw new ArgumentOutOfRangeException(nameof(bandwidthHz), "Bandwidths must be positive.");
        return marginDb - 10.0 * Math.Log10(otherBandwidthHz / bandwidthHz) - (otherRequiredSnrDb - requiredSnrDb);
    }
}
