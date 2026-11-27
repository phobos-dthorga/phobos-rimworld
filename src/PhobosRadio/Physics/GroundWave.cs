using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>The grounds of the ground-wave table, in its order (ITU-R P.527-6, Figure 24, read by eye at HF).</summary>
public enum HfGround
{
    Sea,
    Wet,
    Fresh,
    Medium,
    Dry,
    Ice,
}

/// <summary>
/// HF's ground wave (Radio 0.62.0; docs/development/radio-hf.md, slice 1): the basic transmission loss over a smooth Earth from NTIA's
/// LF/MF model, the program behind ITU-R P.368-10, packed offline by <c>scripts/hf-tables.py ground</c>. Game-free: the game passes the
/// band, the grounds, the heights and the distance in km.
/// <para>Between the table's points the loss is linear in height and in the logarithm of distance; past its last distance it carries on
/// along the last stretch's slope, and inside its first (1 km) it falls as free space does, 20 dB a decade (agent's choices).</para>
/// </summary>
public sealed class GroundWave
{
    public const int Format = 1;
    static readonly byte[] Magic = { (byte)'P', (byte)'H', (byte)'G', (byte)'W' };

    readonly float[] losses;
    readonly int[] khz;
    readonly int[] heights;
    readonly int[] distances;
    readonly int grounds;
    readonly int pols;

    public IReadOnlyList<int> BandsKHz => khz;
    public IReadOnlyList<int> HeightsMetres => heights;
    public IReadOnlyList<int> DistancesKm => distances;

    GroundWave(float[] losses, int grounds, int[] khz, int pols, int[] heights, int[] distances)
    {
        this.losses = losses;
        this.grounds = grounds;
        this.khz = khz;
        this.pols = pols;
        this.heights = heights;
        this.distances = distances;
    }

    public static GroundWave Read(byte[] data)
    {
        for (int i = 0; i < 4; i++)
            if (data.Length < 6 || data[i] != Magic[i]) throw new FormatException("not a ground-wave table");
        int at = 4;
        int format = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));
        at += 2;
        if (format != Format) throw new FormatException($"ground-wave table format {format}, expected {Format}");
        int grounds = data[at++];
        at += 8 * grounds;
        int nb = data[at++];
        var khz = new int[nb];
        for (int i = 0; i < nb; i++, at += 2) khz[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));
        int pols = data[at++];
        at += pols;
        int nh = data[at++];
        var heights = new int[nh];
        for (int i = 0; i < nh; i++) heights[i] = data[at++];
        int nd = data[at++];
        var distances = new int[nd];
        for (int i = 0; i < nd; i++, at += 2) distances[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));
        int count = grounds * nb * pols * nh * nh * nd;
        if (data.Length < at + 4 * count) throw new FormatException("ground-wave table cut short");
        var losses = new float[count];
        for (int i = 0; i < count; i++, at += 4) losses[i] = BinaryPrimitives.ReadSingleLittleEndian(data.AsSpan(at));
        return new GroundWave(losses, grounds, khz, pols, heights, distances);
    }

    /// <summary>The band's index for this frequency in kHz, or -1.</summary>
    public int BandIndex(int kHz) => Array.IndexOf(khz, kHz);

    /// <summary>The loss over one ground, in dB: <paramref name="horizontal"/> when both antennas lie flat (dipoles), else vertical.</summary>
    public double LossDb(int band, HfGround ground, bool horizontal, double heightTxMetres, double heightRxMetres, double km)
    {
        if (band < 0 || band >= khz.Length) throw new ArgumentOutOfRangeException(nameof(band));
        int g = Math.Min((int)ground, grounds - 1);
        // LFMF's polarisation order: 0 horizontal, 1 vertical.
        int p = horizontal ? 0 : Math.Min(1, pols - 1);
        Axis(heights, heightTxMetres, out int t0, out int t1, out double tt);
        Axis(heights, heightRxMetres, out int r0, out int r1, out double rt);
        double sum = 0;
        for (int corner = 0; corner < 4; corner++)
        {
            int t = (corner & 1) == 1 ? t1 : t0, r = (corner & 2) == 2 ? r1 : r0;
            double w = ((corner & 1) == 1 ? tt : 1 - tt) * ((corner & 2) == 2 ? rt : 1 - rt);
            if (w == 0) continue;
            sum += w * AlongDistance(g, band, p, t, r, km);
        }
        return sum;
    }

    /// <summary>
    /// The loss over two grounds, the first <paramref name="kmFirst"/> from the transmitter over <paramref name="first"/> and the rest over
    /// <paramref name="second"/>: ITU-R P.368-10's Annex 2 (Millington's method, the mean of the two directions), in losses rather than
    /// field strengths, which differ only in sign and a constant.
    /// </summary>
    public double MixedLossDb(int band, HfGround first, HfGround second, bool horizontal, double heightTxMetres, double heightRxMetres,
        double kmFirst, double km)
    {
        if (first == second || kmFirst <= 0 || kmFirst >= km) return LossDb(band, kmFirst <= 0 ? second : first, horizontal, heightTxMetres, heightRxMetres, km);
        double kmSecond = km - kmFirst;
        double L(HfGround g, double d) => LossDb(band, g, horizontal, heightTxMetres, heightRxMetres, d);
        double forward = L(first, kmFirst) - L(second, kmFirst) + L(second, km);
        double back = L(second, kmSecond) - L(first, kmSecond) + L(first, km);
        return (forward + back) / 2;
    }

    double AlongDistance(int g, int band, int p, int t, int r, double km)
    {
        int nh = heights.Length, nd = distances.Length;
        int row = ((((g * khz.Length + band) * pols + p) * nh + t) * nh + r) * nd;
        double first = distances[0];
        if (km <= first) return losses[row] + 20 * Math.Log10(Math.Max(km, 0.001) / first);
        int last = nd - 1;
        int i;
        if (km >= distances[last]) i = last - 1;
        else
        {
            i = 0;
            while (distances[i + 1] < km) i++;
        }
        double x0 = Math.Log10(distances[i]), x1 = Math.Log10(distances[i + 1]);
        double s = (Math.Log10(km) - x0) / (x1 - x0);
        return losses[row + i] + s * (losses[row + i + 1] - losses[row + i]);
    }

    /// <summary>The two listed points either side of a value, and how far between; held at the ends.</summary>
    static void Axis(int[] points, double value, out int i0, out int i1, out double t)
    {
        if (value <= points[0]) { i0 = i1 = 0; t = 0; return; }
        int last = points.Length - 1;
        if (value >= points[last]) { i0 = i1 = last; t = 0; return; }
        int i = 0;
        while (points[i + 1] < value) i++;
        i0 = i;
        i1 = i + 1;
        t = (value - points[i]) / (points[i + 1] - points[i]);
    }

    /// <summary>
    /// A tile's ground for HF by its biome's name, so other mods' biomes take the nearest (agent's mapping): ocean and sea water; lakes
    /// fresh water; ice sheets and sea ice ice; deserts and arid land very dry; swamps, marshes, bogs and rainforests wet; everything else
    /// medium dry.
    /// </summary>
    public static HfGround GroundOf(string biomeName)
    {
        string n = biomeName.ToLowerInvariant();
        if (n.Contains("ice") || n.Contains("glacier")) return HfGround.Ice;
        if (n.Contains("ocean") || n.Contains("sea")) return HfGround.Sea;
        if (n.Contains("lake") || n.Contains("river")) return HfGround.Fresh;
        if (n.Contains("desert") || n.Contains("arid") || n.Contains("dune") || n.Contains("salt")) return HfGround.Dry;
        if (n.Contains("swamp") || n.Contains("marsh") || n.Contains("bog") || n.Contains("rainforest") || n.Contains("jungle")
            || n.Contains("mangrove") || n.Contains("wetland")) return HfGround.Wet;
        return HfGround.Medium;
    }
}

/// <summary>
/// The sun's cycle for HF (Radio 0.62.0; the owner's card of 28 Nov 2026: a cycle a few game years long, starting at a random point on a
/// new world): the smoothed sunspot number from the game's time, its phase from the world's seed, so nothing is saved. The shape, a
/// raised cosine between a quiet and a busy sun, and its two figures are authored (real cycles run from near 0 to between about 100 and
/// 200).
/// </summary>
public static class SolarCycle
{
    public const double QuietSunspots = 5;
    public const double BusySunspots = 180;

    /// <summary>The sunspot number at this many days since the game began, for a cycle this many years long (a game year is 60 days).</summary>
    public static double Sunspots(double days, double cycleYears, int worldSeed)
    {
        double period = Math.Max(1.0, cycleYears * 60.0);
        double offset = (uint)(worldSeed * 2654435761u) / (double)uint.MaxValue;
        double phase = days / period + offset;
        return QuietSunspots + (BusySunspots - QuietSunspots) * (1 - Math.Cos(2 * Math.PI * phase)) / 2;
    }

    /// <summary>Noise in a receiver's bandwidth, in dBm, from a noise density in dBW per hertz.</summary>
    public static double NoiseDbm(double noiseDbwPerHz, double bandwidthHz) => noiseDbwPerHz + 30 + 10 * Math.Log10(Math.Max(1, bandwidthHz));
}
