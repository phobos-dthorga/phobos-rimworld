using System;
using System.IO;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// HF in play (Radio 0.62.0; docs/development/radio-hf.md, slice 1): the shipped tables, the sun's cycle, the atmosphere's noise at a
/// receiver and the ground wave between two tiles. The rules are game-free in <see cref="GroundWave"/>, <see cref="HfTable"/> and
/// <see cref="SolarCycle"/>; this reads the game's time, tiles and biomes.
/// <para>Cost: the two tables are read from the mod's folder once, the first time an HF band is weighed (3.3 MB and 14 KB); after that
/// each link a few table look-ups. Nothing per tick, nothing saved: the sun's phase comes from the world's seed.</para>
/// </summary>
public static class HfService
{
    const string Folder = "Common/Data/HF";
    static bool tried;
    static GroundWave? ground;
    static HfTable? sky;

    /// <summary>The ground-wave table, or null when it could not be read (logged once).</summary>
    public static GroundWave? Ground
    {
        get
        {
            Load();
            return ground;
        }
    }

    /// <summary>VOACAP's skywave and noise tables, or null when they could not be read (logged once).</summary>
    public static HfTable? Sky
    {
        get
        {
            Load();
            return sky;
        }
    }

    static void Load()
    {
        if (tried) return;
        tried = true;
        string? root = RadioMod.ContentPack?.RootDir;
        if (root == null) return;
        try
        {
            ground = GroundWave.Read(File.ReadAllBytes(Path.Combine(root, Folder, "ground-wave.bin")));
            sky = HfTable.Read(File.ReadAllBytes(Path.Combine(root, Folder, "hf-propagation.bin")));
        }
        catch (Exception e)
        {
            RadioMod.Logger.Warn("HF tables could not be read, so HF bands reach nobody between tiles: " + e.Message);
            ground = null;
            sky = null;
        }
    }

    /// <summary>The band's frequency as the tables hold it, in kHz.</summary>
    public static int KHz(BandDef band) => (int)Math.Round(band.centreMHz * 1000f);

    /// <summary>The sunspot number now, from the sun's cycle and the world's seed.</summary>
    public static double SunspotsNow() =>
        SolarCycle.Sunspots(Find.TickManager.TicksAbs / (double)GenDate.TicksPerDay, RadioRulesDef.Current.hfSolarCycleYears, Find.World?.info?.Seed ?? 0);

    /// <summary>Earth's year fraction for the HF tables at this tick (the rules' start of the game's year on Earth's).</summary>
    public static double YearFraction(long ticksAbs, float longitude) =>
        (RadioRulesDef.Current.hfYearStartFraction + GenDate.DayOfYear(ticksAbs, longitude) / (double)GenDate.DaysPerYear) % 1.0;

    /// <summary>The atmosphere's noise in a receiver's bandwidth at this tile now, in dBm, or null without the tables or the band.</summary>
    public static double? NoiseDbm(BandDef band, PlanetTile tile, double bandwidthHz)
    {
        if (Sky is not { } table || !tile.Valid || table.BandIndex(KHz(band)) is var index && index < 0) return null;
        Vector2 longLat = Find.WorldGrid.LongLatOf(tile);
        long now = Find.TickManager.TicksAbs;
        HfConditions here = table.Look(index, YearFraction(now, longLat.x), SunspotsNow(), longLat.y, table.DistancesKm[0],
            GenDate.HourFloat(now, longLat.x));
        return SolarCycle.NoiseDbm(here.NoiseDbwPerHz, bandwidthHz);
    }

    /// <summary>A receiver's floor with the atmosphere's noise added (power sum), or the floor as it was without it.</summary>
    public static double FloorWithAtmosphereDbm(double floorDbm, BandDef band, PlanetTile tile, double bandwidthHz) =>
        NoiseDbm(band, tile, bandwidthHz) is { } noise ? 10 * Math.Log10(Math.Pow(10, floorDbm / 10) + Math.Pow(10, noise / 10)) : floorDbm;

    /// <summary>A tile's ground for HF, by its biome; medium dry ground where the tile cannot be read.</summary>
    public static HfGround GroundAt(PlanetTile tile) =>
        BiomeGround.TryRead(tile, out TileGround read) && read.Biome != null ? GroundWave.GroundOf(read.Biome.defName) : HfGround.Medium;

    /// <summary>
    /// The ground wave between two tiles, in dB, or null without the table or the band: half the path over each end's ground (P.368's
    /// mixed-path method), each antenna's own height, horizontal only when both lie flat and the mean of the two polarisations in dB when
    /// one does (an agent's simplification).
    /// </summary>
    public static double? GroundWaveDb(BandDef band, PlanetTile tileA, PlanetTile tileB, double km, double heightA, double heightB, bool flatA,
        bool flatB)
    {
        if (Ground is not { } table || table.BandIndex(KHz(band)) is var index && index < 0) return null;
        HfGround first = GroundAt(tileA), second = GroundAt(tileB);
        double Loss(bool flat) => table.MixedLossDb(index, first, second, flat, heightA, heightB, km / 2, km);
        return flatA == flatB ? Loss(flatA) : (Loss(true) + Loss(false)) / 2;
    }
}
