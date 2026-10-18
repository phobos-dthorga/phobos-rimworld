using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// Feedline loss. Coax loss per metre rises roughly with the square root of frequency (the usual
/// skin-effect law, an approximation), so a cable is rated at a reference frequency and scaled.
/// </summary>
public static class Coax
{
    public static double LossDb(double metres, double lossDbPer100mAtReference, double referenceMegahertz, double megahertz)
    {
        if (referenceMegahertz <= 0) throw new ArgumentOutOfRangeException(nameof(referenceMegahertz), "Reference frequency must be positive.");
        if (megahertz <= 0) throw new ArgumentOutOfRangeException(nameof(megahertz), "Frequency must be positive.");
        if (metres <= 0) return 0;
        return metres / 100.0 * lossDbPer100mAtReference * Math.Sqrt(megahertz / referenceMegahertz);
    }
}
