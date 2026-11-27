using System;

namespace Phobos.Radio.Physics;

/// <summary>
/// A jammer as noise (Radio 0.45.0; docs/development/electronic-warfare.md, EW1): its power spread evenly over every band it covers
/// (barrage jamming), arriving by free space like any other signal. A receiver hears the share that falls inside its own bandwidth, so a
/// narrow mode hears less of it than a wide one, and a close, strong link still gets through ("burn-through", which needs no rule of its own).
/// <b>Simplifications, labelled:</b> no walls, rock or roofs between jammer and receiver (as the colony's own noise; EW8 is later), an
/// omnidirectional jamming antenna, and free space at the band's centre.
/// </summary>
public static class Jamming
{
    /// <summary>
    /// What reaches a 0 dBi antenna at a distance, in dBm within the receiver's bandwidth: the jammer's radiated power, spread over the
    /// span it covers, the receiver's share of that span, less free space at the band's centre. A receiver as wide as the whole span hears
    /// all of it, never more.
    /// </summary>
    public static double AtAntennaDbm(double radiatedDbm, double spanHz, double bandwidthHz, double distanceMetres, double centreMHz)
    {
        if (spanHz <= 0) throw new ArgumentOutOfRangeException(nameof(spanHz), "A jammer covers some width of band.");
        if (bandwidthHz <= 0) throw new ArgumentOutOfRangeException(nameof(bandwidthHz), "Bandwidth must be positive.");
        var share = 10.0 * Math.Log10(Math.Min(bandwidthHz, spanHz) / spanHz);
        return radiatedDbm + share - Propagation.FreeSpacePathLossDb(distanceMetres, centreMHz);
    }

    /// <summary>
    /// The same at the receiver's input: through its antenna's gain, less its cable's loss and anything only in its receive path. This is
    /// where a signal is compared with its floor, so jamming meets a link on the same terms as the signal it drowns.
    /// </summary>
    public static double AtInputDbm(double atAntennaDbm, double antennaGainDbi, double receiveLossDb) =>
        double.IsNegativeInfinity(atAntennaDbm) ? double.NegativeInfinity : atAntennaDbm + antennaGainDbi - receiveLossDb;
}
