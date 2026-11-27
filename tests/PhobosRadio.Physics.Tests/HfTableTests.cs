using System;
using System.Collections.Generic;
using System.Text;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>The HF tables' reader (radio-hf.md): a small made-up table in format 1, its grid points and the points between them.</summary>
public class HfTableTests
{
    static readonly int[] Sunspots = { 0, 100 };
    static readonly int[] Latitudes = { 0, 30 };
    static readonly int[] Distances = { 100, 1000 };

    /// <summary>A table of 12 months, two sunspot levels, latitudes and distances, 24 hours and one band (40 m), filled by the given rule.</summary>
    static byte[] Build(Func<int, int, int, int, int, (byte Muf, byte Open, byte Loss, byte Fade)> path, byte noise = 60)
    {
        var bytes = new List<byte>(Encoding.ASCII.GetBytes("PHFT"));
        void U16(int v)
        {
            bytes.Add((byte)(v & 0xff));
            bytes.Add((byte)(v >> 8));
        }
        U16(HfTable.Format);
        bytes.Add(12);
        bytes.Add((byte)Sunspots.Length);
        foreach (int s in Sunspots) U16(s);
        bytes.Add((byte)Latitudes.Length);
        foreach (int a in Latitudes) bytes.Add(unchecked((byte)(sbyte)a));
        bytes.Add((byte)Distances.Length);
        foreach (int d in Distances) U16(d);
        bytes.Add(24);
        bytes.Add(1);
        U16(7100);
        for (int m = 0; m < 12; m++)
            for (int s = 0; s < Sunspots.Length; s++)
                for (int a = 0; a < Latitudes.Length; a++)
                    for (int d = 0; d < Distances.Length; d++)
                        for (int h = 0; h < 24; h++)
                        {
                            var c = path(m, s, a, d, h);
                            bytes.Add(c.Muf);
                            bytes.Add(c.Open);
                            bytes.Add(c.Loss);
                            bytes.Add(c.Fade);
                        }
        for (int i = 0; i < 12 * Sunspots.Length * Latitudes.Length * 24; i++) bytes.Add(noise);
        return bytes.ToArray();
    }

    static double June => 5.5 / 12;

    [Fact]
    public void TheHeaderNamesEveryAxis()
    {
        HfTable table = HfTable.Read(Build((_, _, _, _, _) => (0, 0, 0, 0)));
        Assert.Equal(Sunspots, table.Sunspots);
        Assert.Equal(Latitudes, table.Latitudes);
        Assert.Equal(Distances, table.DistancesKm);
        Assert.Equal(0, table.BandIndex(7100));
        Assert.Equal(-1, table.BandIndex(14200));
    }

    [Fact]
    public void AGridPointGivesItsOwnFigures()
    {
        HfTable table = HfTable.Read(Build((m, s, a, d, h) => m == 5 && s == 1 && a == 1 && d == 1 && h == 12 ? ((byte)48, (byte)250, (byte)45, (byte)90) : ((byte)0, (byte)0, (byte)0, (byte)0)));
        HfConditions c = table.Look(0, June, 100, 30, 1000, 12);
        Assert.Equal(12.0, c.MufMHz, 6);
        Assert.Equal(1.0, c.OpenShare, 6);
        Assert.Equal(125.0, c.LossDb!.Value, 6);
        Assert.Equal(9.0, c.FadeDb, 6);
        Assert.Equal(-160.0, c.NoiseDbwPerHz, 6);
    }

    [Fact]
    public void BetweenTwoDistancesTheLossIsBetweenToo()
    {
        HfTable table = HfTable.Read(Build((_, _, _, d, _) => (0, 0, d == 0 ? (byte)20 : (byte)60, 0)));
        Assert.Equal(120.0, table.Look(0, June, 0, 0, 550, 12).LossDb!.Value, 6);
    }

    [Fact]
    public void TheHourWrapsPastMidnight()
    {
        HfTable table = HfTable.Read(Build((_, _, _, _, h) => ((byte)(h * 4), 0, 0, 0)));
        // 23:30 lies half way between 23:00 (23 MHz) and 00:00 (0 MHz).
        Assert.Equal(11.5, table.Look(0, June, 0, 0, 100, 23.5).MufMHz, 6);
        Assert.Equal(1.0, table.Look(0, June, 0, 0, 100, 25).MufMHz, 6);
    }

    [Fact]
    public void TheYearWrapsPastDecember()
    {
        HfTable table = HfTable.Read(Build((m, _, _, _, _) => (m == 11 ? (byte)40 : m == 0 ? (byte)80 : (byte)0, 0, 0, 0)));
        // 1 January lies half way between the middles of December (10 MHz) and January (20 MHz).
        Assert.Equal(15.0, table.Look(0, 0.0, 0, 0, 100, 0).MufMHz, 6);
    }

    [Fact]
    public void BeyondTheGridTheEndsHold()
    {
        HfTable table = HfTable.Read(Build((_, _, a, _, _) => (a == 1 ? (byte)80 : (byte)0, 0, 0, 0)));
        Assert.Equal(20.0, table.Look(0, June, 0, 75, 100, 12).MufMHz, 6);
        Assert.Equal(0.0, table.Look(0, June, 0, -40, 100, 12).MufMHz, 6);
    }

    [Fact]
    public void NoUsablePathHasNoLoss() =>
        Assert.Null(HfTable.Read(Build((_, _, _, _, _) => (0, 0, 255, 0))).Look(0, June, 50, 15, 400, 3).LossDb);

    [Fact]
    public void ANearlyClosedPathIsVeryLossy()
    {
        HfTable table = HfTable.Read(Build((_, _, _, d, _) => (0, 0, d == 0 ? (byte)40 : (byte)255, 0)));
        double? loss = table.Look(0, June, 0, 0, 200, 12).LossDb;
        Assert.NotNull(loss);
        Assert.True(loss > 140);
    }

    [Fact]
    public void AnythingElseIsRefused()
    {
        byte[] good = Build((_, _, _, _, _) => (0, 0, 0, 0));
        byte[] wrongMagic = (byte[])good.Clone();
        wrongMagic[0] = (byte)'X';
        Assert.Throws<FormatException>(() => HfTable.Read(wrongMagic));
        Assert.Throws<FormatException>(() => HfTable.Read(good[..^1]));
    }
}
