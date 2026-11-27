using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Phobos.Radio.Physics;

/// <summary>What the HF tables say for one path at one moment: the band's share of days open, its loss, and the noise at the far end.</summary>
public readonly struct HfConditions
{
    /// <summary>The path's maximum usable frequency, MHz.</summary>
    public readonly double MufMHz;
    /// <summary>The share of days the band is below the path's MUF (VOACAP's MUFday), 0 to 1.</summary>
    public readonly double OpenShare;
    /// <summary>The path's own loss, dB (isotropic antennas at both ends), or null where no usable path exists.</summary>
    public readonly double? LossDb;
    /// <summary>How far the signal falls below its median on one day in ten, dB (VOACAP's SIG LW): how deep it fades.</summary>
    public readonly double FadeDb;
    /// <summary>The noise at the receiving end, dBW in 1 Hz: natural noise only; a colony's own sources are the game's to add.</summary>
    public readonly double NoiseDbwPerHz;

    public HfConditions(double mufMHz, double openShare, double? lossDb, double fadeDb, double noiseDbwPerHz)
    {
        MufMHz = mufMHz;
        OpenShare = openShare;
        LossDb = lossDb;
        FadeDb = fadeDb;
        NoiseDbwPerHz = noiseDbwPerHz;
    }
}

/// <summary>
/// The radio's HF propagation tables (docs/development/radio-hf.md; the owner's cards of 28 Nov 2026: VOACAP, offline, shipped as
/// tables), as scripts/hf-tables.py packs them, format 1. Every figure is from VOACAP (NTIA/ITS), averaged over paths at several
/// longitudes and two directions, as a RimWorld planet has no magnetic field of its own: an approximation. The game only looks values up,
/// between the grid's points in every axis: the time of year and the hour wrap round, the rest are held at their ends.
/// </summary>
public sealed class HfTable
{
    public const int Format = 1;
    const double MufStep = 0.25;
    const double LossFloor = 80;
    const double SpreadStep = 0.1;
    const double NoiseTop = 100;
    /// <summary>The packed byte for "no usable path".</summary>
    const byte NoPath = 255;

    readonly byte[] data;
    readonly int months;
    readonly int hours;
    readonly int pathsAt;
    readonly int noiseAt;
    public IReadOnlyList<int> Sunspots { get; }
    public IReadOnlyList<int> Latitudes { get; }
    public IReadOnlyList<int> DistancesKm { get; }
    public IReadOnlyList<int> BandsKHz { get; }

    HfTable(byte[] data, int months, int[] sunspots, int[] latitudes, int[] distances, int hours, int[] bands, int pathsAt)
    {
        this.data = data;
        this.months = months;
        this.hours = hours;
        Sunspots = sunspots;
        Latitudes = latitudes;
        DistancesKm = distances;
        BandsKHz = bands;
        this.pathsAt = pathsAt;
        noiseAt = pathsAt + months * sunspots.Length * latitudes.Length * distances.Length * hours * (1 + 3 * bands.Length);
    }

    /// <summary>Reads a packed file; throws <see cref="FormatException"/> on anything it does not recognise.</summary>
    public static HfTable Read(byte[] data)
    {
        if (data.Length < 6 || Encoding.ASCII.GetString(data, 0, 4) != "PHFT") throw new FormatException("not an HF table file");
        int at = 4;
        int format = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));
        at += 2;
        if (format != Format) throw new FormatException("HF table format " + format + ", expected " + Format);
        int months = data[at++];
        int[] sunspots = ReadU16(data, ref at);
        int count = data[at++];
        var latitudes = new int[count];
        for (int i = 0; i < count; i++) latitudes[i] = (sbyte)data[at++];
        int[] distances = ReadU16(data, ref at);
        int hours = data[at++];
        int[] bands = ReadU16(data, ref at);
        var table = new HfTable(data, months, sunspots, latitudes, distances, hours, bands, at);
        long size = (long)table.noiseAt + (long)months * sunspots.Length * latitudes.Length * hours * bands.Length;
        if (size != data.Length) throw new FormatException("HF table is " + data.Length + " bytes, its header says " + size);
        return table;
    }

    static int[] ReadU16(byte[] data, ref int at)
    {
        int n = data[at++];
        var values = new int[n];
        for (int i = 0; i < n; i++, at += 2) values[i] = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));
        return values;
    }

    /// <summary>The band's index for this frequency in kHz, or -1 when the tables do not hold it.</summary>
    public int BandIndex(int kHz)
    {
        for (int i = 0; i < BandsKHz.Count; i++)
            if (BandsKHz[i] == kHz) return i;
        return -1;
    }

    /// <summary>
    /// The conditions on a path. <paramref name="yearFraction"/> runs 0 to 1 from Earth's 1 January (the northern midwinter); the game maps
    /// its own year onto it. <paramref name="localHour"/> is the path midpoint's local time, 0 to 24.
    /// </summary>
    public HfConditions Look(int band, double yearFraction, double sunspots, double midLatitude, double km, double localHour)
    {
        if (band < 0 || band >= BandsKHz.Count) throw new ArgumentOutOfRangeException(nameof(band));
        Span(Wrap(yearFraction * months - 0.5, months), months, out int m0, out int m1, out double mt);
        Span(Wrap(localHour, hours), hours, out int h0, out int h1, out double ht);
        Axis(Sunspots, sunspots, out int s0, out int s1, out double st);
        Axis(Latitudes, midLatitude, out int a0, out int a1, out double at);
        Axis(DistancesKm, km, out int d0, out int d1, out double dt);

        double muf = 0, open = 0, loss = 0, fade = 0;
        for (int corner = 0; corner < 32; corner++)
        {
            double w = Weight(corner, 0, mt) * Weight(corner, 1, ht) * Weight(corner, 2, st) * Weight(corner, 3, at) * Weight(corner, 4, dt);
            if (w == 0) continue;
            int m = Bit(corner, 0) ? m1 : m0, h = Bit(corner, 1) ? h1 : h0, s = Bit(corner, 2) ? s1 : s0;
            int a = Bit(corner, 3) ? a1 : a0, d = Bit(corner, 4) ? d1 : d0;
            int cell = pathsAt + ((((m * Sunspots.Count + s) * Latitudes.Count + a) * DistancesKm.Count + d) * hours + h) * (1 + 3 * BandsKHz.Count);
            muf += w * data[cell] * MufStep;
            int b = cell + 1 + 3 * band;
            open += w * data[b] / 250.0;
            // A path with none counts as the most lossy packed path, so the loss rises smoothly towards a closed corner.
            loss += w * (LossFloor + data[b + 1]);
            fade += w * data[b + 2] * SpreadStep;
        }
        double noise = NoiseAt(band, m0, m1, mt, h0, h1, ht, s0, s1, st, a0, a1, at);
        return new HfConditions(muf, Math.Min(1, open), loss >= LossFloor + NoPath ? null : loss, fade, noise);
    }

    /// <summary>The noise at the receiving end: the same look-up without the distance axis, which the noise does not have.</summary>
    double NoiseAt(int band, int m0, int m1, double mt, int h0, int h1, double ht, int s0, int s1, double st, int a0, int a1, double at)
    {
        double noise = 0;
        for (int corner = 0; corner < 16; corner++)
        {
            double w = Weight(corner, 0, mt) * Weight(corner, 1, ht) * Weight(corner, 2, st) * Weight(corner, 3, at);
            if (w == 0) continue;
            int m = Bit(corner, 0) ? m1 : m0, h = Bit(corner, 1) ? h1 : h0, s = Bit(corner, 2) ? s1 : s0, a = Bit(corner, 3) ? a1 : a0;
            noise += w * -(NoiseTop + data[noiseAt + (((m * Sunspots.Count + s) * Latitudes.Count + a) * hours + h) * BandsKHz.Count + band]);
        }
        return noise;
    }

    static bool Bit(int corner, int axis) => (corner >> axis & 1) == 1;
    static double Weight(int corner, int axis, double t) => Bit(corner, axis) ? t : 1 - t;

    static double Wrap(double value, int count) => ((value % count) + count) % count;

    /// <summary>A wrapping axis of whole steps (months, hours): the two points either side and how far between.</summary>
    static void Span(double value, int count, out int i0, out int i1, out double t)
    {
        i0 = (int)Math.Floor(value);
        t = value - i0;
        i0 %= count;
        i1 = (i0 + 1) % count;
    }

    /// <summary>A held axis of listed points (sunspots, latitude, distance): the two points either side and how far between.</summary>
    static void Axis(IReadOnlyList<int> points, double value, out int i0, out int i1, out double t)
    {
        if (value <= points[0])
        {
            i0 = i1 = 0;
            t = 0;
            return;
        }
        int last = points.Count - 1;
        if (value >= points[last])
        {
            i0 = i1 = last;
            t = 0;
            return;
        }
        int i = 0;
        while (points[i + 1] < value) i++;
        i0 = i;
        i1 = i + 1;
        t = (value - points[i]) / (points[i + 1] - points[i]);
    }
}
