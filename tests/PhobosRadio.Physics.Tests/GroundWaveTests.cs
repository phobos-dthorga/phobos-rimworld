using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

/// <summary>HF's ground wave (radio-hf.md, slice 1): a small made-up table in format 1, Millington's mixed path, the grounds by biome, the
/// sun's cycle; and the shipped table's physics.</summary>
public class GroundWaveTests
{
    static readonly int[] Heights = { 2, 10 };
    static readonly int[] Distances = { 1, 10, 100 };

    /// <summary>Six grounds, one band (40 m), two polarisations: the loss is the rule's, of ground, polarisation, heights and distance.</summary>
    static byte[] Build(Func<int, int, int, int, int, float> loss)
    {
        var bytes = new List<byte>(Encoding.ASCII.GetBytes("PHGW"));
        void U16(int v) { bytes.Add((byte)(v & 0xff)); bytes.Add((byte)(v >> 8)); }
        void F32(float v) => bytes.AddRange(BitConverter.GetBytes(v));
        U16(GroundWave.Format);
        bytes.Add(6);
        for (int g = 0; g < 6; g++) { F32(10); F32(0.01f); }
        bytes.Add(1);
        U16(7100);
        bytes.Add(2);
        bytes.Add(0);
        bytes.Add(1);
        bytes.Add((byte)Heights.Length);
        foreach (int h in Heights) bytes.Add((byte)h);
        bytes.Add((byte)Distances.Length);
        foreach (int d in Distances) U16(d);
        for (int g = 0; g < 6; g++)
            for (int p = 0; p < 2; p++)
                for (int t = 0; t < Heights.Length; t++)
                    for (int r = 0; r < Heights.Length; r++)
                        for (int d = 0; d < Distances.Length; d++)
                            F32(loss(g, p, Heights[t], Heights[r], Distances[d]));
        return bytes.ToArray();
    }

    // 100 dB at 1 km, 40 dB a decade more; ground g adds 10 g dB; horizontal adds 40 dB; height takes 0.5 dB a metre off each end.
    static readonly GroundWave Table = GroundWave.Read(Build((g, p, ht, hr, km) =>
        (float)(100 + 40 * Math.Log10(km) + 10 * g + (p == 0 ? 40 : 0) - 0.5 * (ht + hr))));

    [Fact]
    public void AGridPointReadsItsLoss() =>
        Assert.Equal(140 - 2, Table.LossDb(0, HfGround.Sea, false, 2, 2, 10), 3);

    [Fact]
    public void DistanceIsLinearInItsLogarithm() =>
        Assert.Equal(100 + 40 * Math.Log10(30) - 2, Table.LossDb(0, HfGround.Sea, false, 2, 2, 30), 3);

    [Fact]
    public void PastTheLastDistanceTheSlopeCarriesOn() =>
        Assert.Equal(100 + 40 * 3 - 2, Table.LossDb(0, HfGround.Sea, false, 2, 2, 1000), 3);

    [Fact]
    public void InsideTheFirstDistanceItFallsAsFreeSpace() =>
        Assert.Equal(100 - 20 - 2, Table.LossDb(0, HfGround.Sea, false, 2, 2, 0.1), 3);

    [Fact]
    public void HeightIsLinearAndHeldAtTheEnds()
    {
        Assert.Equal(140 - 0.5 * (6 + 2), Table.LossDb(0, HfGround.Sea, false, 6, 2, 10), 3);
        Assert.Equal(140 - 0.5 * (10 + 10), Table.LossDb(0, HfGround.Sea, false, 30, 30, 10), 3);
    }

    [Fact]
    public void HorizontalAndGroundReadTheirOwnRows()
    {
        Assert.Equal(40, Table.LossDb(0, HfGround.Sea, true, 2, 2, 10) - Table.LossDb(0, HfGround.Sea, false, 2, 2, 10), 3);
        Assert.Equal(30, Table.LossDb(0, HfGround.Medium, false, 2, 2, 10) - Table.LossDb(0, HfGround.Sea, false, 2, 2, 10), 3);
    }

    [Fact]
    public void AMixedPathOverOneGroundIsThatGround() =>
        Assert.Equal(Table.LossDb(0, HfGround.Wet, false, 2, 2, 50), Table.MixedLossDb(0, HfGround.Wet, HfGround.Wet, false, 2, 2, 25, 50), 6);

    [Fact]
    public void AMixedPathLiesBetweenItsTwoGroundsAndIsTheSameBothWays()
    {
        double sea = Table.LossDb(0, HfGround.Sea, false, 2, 2, 50), dry = Table.LossDb(0, HfGround.Dry, false, 2, 2, 50);
        double mixed = Table.MixedLossDb(0, HfGround.Sea, HfGround.Dry, false, 2, 2, 25, 50);
        Assert.InRange(mixed, sea, dry);
        Assert.Equal(mixed, Table.MixedLossDb(0, HfGround.Dry, HfGround.Sea, false, 2, 2, 25, 50), 6);
    }

    [Theory]
    [InlineData("Ocean", HfGround.Sea)]
    [InlineData("Lake", HfGround.Fresh)]
    [InlineData("IceSheet", HfGround.Ice)]
    [InlineData("SeaIce", HfGround.Ice)]
    [InlineData("ExtremeDesert", HfGround.Dry)]
    [InlineData("AridShrubland", HfGround.Dry)]
    [InlineData("TemperateSwamp", HfGround.Wet)]
    [InlineData("TropicalRainforest", HfGround.Wet)]
    [InlineData("TemperateForest", HfGround.Medium)]
    [InlineData("Tundra", HfGround.Medium)]
    public void ABiomeTakesTheNearestGround(string biome, HfGround ground) => Assert.Equal(ground, GroundWave.GroundOf(biome));

    [Fact]
    public void TheSunsCycleRunsBetweenQuietAndBusyOverItsPeriod()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int day = 0; day < 180; day++)
        {
            double s = SolarCycle.Sunspots(day, 3, 12345);
            min = Math.Min(min, s);
            max = Math.Max(max, s);
        }
        Assert.InRange(min, SolarCycle.QuietSunspots, SolarCycle.QuietSunspots + 1);
        Assert.InRange(max, SolarCycle.BusySunspots - 1, SolarCycle.BusySunspots);
        Assert.Equal(SolarCycle.Sunspots(10, 3, 12345), SolarCycle.Sunspots(190, 3, 12345), 6);
        Assert.NotEqual(SolarCycle.Sunspots(10, 3, 12345), SolarCycle.Sunspots(10, 3, 999), 3);
    }

    [Fact]
    public void AShortPathGoesUpSteeplyAndALongOneLow()
    {
        Assert.InRange(Skywave.TakeoffDegrees(100), 75, 90);
        Assert.InRange(Skywave.TakeoffDegrees(3000), 1, 10);
        Assert.True(Skywave.TakeoffDegrees(500) > Skywave.TakeoffDegrees(1500));
    }

    [Fact]
    public void ALongPathTakesMoreHopsAndRisesAgain() =>
        Assert.True(Skywave.TakeoffDegrees(6000) > Skywave.TakeoffDegrees(3400));

    [Fact]
    public void AQuarterWaveDipoleFavoursTheSkyAndAHalfWaveTheHorizon()
    {
        // 40 m (7.1 MHz): a quarter wave is about 10.6 m, a half wave about 21 m.
        double lowUp = Skywave.ElevationDb(true, 10.6, 7.1, 90), lowFar = Skywave.ElevationDb(true, 10.6, 7.1, 10);
        double highUp = Skywave.ElevationDb(true, 21.1, 7.1, 90), highFar = Skywave.ElevationDb(true, 21.1, 7.1, 10);
        Assert.Equal(20 * Math.Log10(2), lowUp, 1);
        Assert.True(lowUp > lowFar);
        Assert.True(highFar > lowFar);
        Assert.Equal(Skywave.FloorDb, highUp, 1);
    }

    [Fact]
    public void AnUprightWhipLosesAsTheAngleRises()
    {
        Assert.Equal(0, Skywave.ElevationDb(false, 3, 7.1, 0), 3);
        Assert.True(Skywave.ElevationDb(false, 3, 7.1, 60) < Skywave.ElevationDb(false, 3, 7.1, 20));
        Assert.Equal(Skywave.FloorDb, Skywave.ElevationDb(false, 3, 7.1, 90), 3);
    }

    [Fact]
    public void NoiseInABandwidthAddsItsDecibels() => Assert.Equal(-170 + 30 + 10 * Math.Log10(2400), SolarCycle.NoiseDbm(-170, 2400), 6);

    /// <summary>The shipped table keeps LFMF's physics: over sea a vertical antenna loses little more than free space; land loses far more,
    /// 20 m more than 40 m; a low horizontal dipole loses tens of decibels more than a vertical.</summary>
    [Fact]
    public void TheShippedTableKeepsThePhysics()
    {
        string path = Path.Combine(Root(), "mods", "PhobosRadioFramework", "Common", "Data", "HF", "ground-wave.bin");
        GroundWave table = GroundWave.Read(File.ReadAllBytes(path));
        int b40 = table.BandIndex(7100), b20 = table.BandIndex(14200);
        Assert.True(b40 >= 0 && b20 >= 0);
        double freeSpace30 = 32.45 + 20 * Math.Log10(7.1) + 20 * Math.Log10(30);
        Assert.InRange(table.LossDb(b40, HfGround.Sea, false, 2, 2, 30) - freeSpace30, -1, 5);
        Assert.True(table.LossDb(b40, HfGround.Medium, false, 2, 2, 30) > table.LossDb(b40, HfGround.Sea, false, 2, 2, 30) + 30);
        Assert.True(table.LossDb(b20, HfGround.Medium, false, 2, 2, 30) > table.LossDb(b40, HfGround.Medium, false, 2, 2, 30));
        Assert.True(table.LossDb(b40, HfGround.Medium, true, 2, 2, 30) > table.LossDb(b40, HfGround.Medium, false, 2, 2, 30) + 20);
    }

    static string Root()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "AGENTS.md")) || dir != null && !Directory.Exists(Path.Combine(dir, "mods")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new DirectoryNotFoundException("repository root");
    }
}
