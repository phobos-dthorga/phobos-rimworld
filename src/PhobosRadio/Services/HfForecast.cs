using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// VOACAP's forecast for a colony's HF bands (Radio 0.62.0; docs/development/radio-hf.md, slice 1, the owner's cards 13 and 18: the
/// station's band panel and the world map's band rose, in dev mode only until skywave is built). Read-only look-ups in the shipped tables;
/// nothing here decides a contact.
/// <para>Cost: worked out for one map at most once a game hour, and only while dev mode is on and a station or the colony is selected: a
/// few dozen table look-ups.</para>
/// </summary>
public static class HfForecast
{
    /// <summary>The distances the panel lists and the rose reaches to, in km (agent's choice).</summary>
    public static readonly int[] DistancesKm = { 500, 1000, 2000, 3000, 5000, 8000, 12000 };
    /// <summary>A band counts as open to a distance on at least this share of days (agent's choice: more often than not).</summary>
    public const double OpenShare = 0.5;
    public const int Bearings = 16;

    public sealed record Reach(int Km, double OpenShare, double MufMHz, bool HasPath);
    public sealed record Outlook(BandDef Band, double NoiseDbm, double BandwidthHz, IReadOnlyList<Reach> ByDistance);
    /// <summary>For one band, how far it is open along each bearing (km; 0 when it is open to none of the distances), clockwise from north.</summary>
    public sealed record Rose(BandDef Band, IReadOnlyList<int> FarthestKm);

    static readonly Dictionary<Map, (int hour, List<Outlook> outlooks, List<Rose> roses)> kept = new();

    /// <summary>The HF bands the map's stations work, in their Def order.</summary>
    public static List<BandDef> BandsOf(Map map) =>
        RadioNet.For(map).Stations.SelectMany(s => s.Bands).Where(b => b.path == BandPath.Hf).Distinct().OrderBy(b => b.centreMHz).ToList();

    public static IReadOnlyList<Outlook> Outlooks(Map map) => For(map).outlooks;
    public static IReadOnlyList<Rose> Roses(Map map) => For(map).roses;

    static (int hour, List<Outlook> outlooks, List<Rose> roses) For(Map map)
    {
        int hour = Find.TickManager.TicksAbs / GenDate.TicksPerHour;
        if (kept.TryGetValue(map, out var held) && held.hour == hour) return held;
        if (kept.Count > 8) kept.Clear();
        var outlooks = new List<Outlook>();
        var roses = new List<Rose>();
        if (HfService.Sky is { } table && map.Tile.Valid)
        {
            Vector2 home = Find.WorldGrid.LongLatOf(map.Tile);
            long now = Find.TickManager.TicksAbs;
            double year = HfService.YearFraction(now, home.x), sun = HfService.SunspotsNow();
            ModeDef ssb = DefDatabase<ModeDef>.GetNamedSilentFail("PhobosRadio_VoiceSSB") ?? DefDatabase<ModeDef>.AllDefsListForReading[0];
            foreach (BandDef band in BandsOf(map))
            {
                int index = table.BandIndex(HfService.KHz(band));
                if (index < 0) continue;
                double localHour = GenDate.HourFloat(now, home.x);
                var reach = DistancesKm.Select(km =>
                {
                    HfConditions c = table.Look(index, year, sun, home.y, km, localHour);
                    return new Reach(km, c.OpenShare, c.MufMHz, c.LossDb != null);
                }).ToList();
                outlooks.Add(new Outlook(band, HfService.NoiseDbm(band, map.Tile, ssb.bandwidthHz) ?? double.NaN, ssb.bandwidthHz, reach));
                var farthest = new List<int>(Bearings);
                for (int b = 0; b < Bearings; b++)
                {
                    int best = 0;
                    foreach (int km in DistancesKm)
                    {
                        Vector2 mid = PointAlong(map.Tile, b * 360f / Bearings, km / 2.0);
                        HfConditions c = table.Look(index, year, sun, mid.y, km, GenDate.HourFloat(now, mid.x));
                        if (c.LossDb != null && c.OpenShare >= OpenShare) best = km;
                    }
                    farthest.Add(best);
                }
                roses.Add(new Rose(band, farthest));
            }
        }
        var result = (hour, outlooks, roses);
        kept[map] = result;
        return result;
    }

    /// <summary>The point this far from a tile along a bearing (degrees clockwise from north), at HF's scale, as longitude and latitude in
    /// degrees, the game's own way (PlanetLayer.LongLatOf).</summary>
    public static Vector2 PointAlong(PlanetTile from, float bearingDegrees, double km)
    {
        Vector3 p = WorldPointAlong(from, bearingDegrees, km);
        return new Vector2(Mathf.Atan2(p.x, -p.z) * Mathf.Rad2Deg, Mathf.Asin(Mathf.Clamp(p.y / p.magnitude, -1f, 1f)) * Mathf.Rad2Deg);
    }

    /// <summary>The same as a point on the globe, at the tile's radius.</summary>
    public static Vector3 WorldPointAlong(PlanetTile from, float bearingDegrees, double km)
    {
        Vector3 centre = Find.WorldGrid.GetTileCenter(from);
        float radius = centre.magnitude;
        Vector3 h = centre / radius;
        Vector3 north = (Vector3.up - h * h.y).normalized;
        if (north.sqrMagnitude < 0.5f) north = Vector3.forward;
        Vector3 east = Vector3.Cross(h, north);
        float beta = bearingDegrees * Mathf.Deg2Rad;
        Vector3 dir = north * Mathf.Cos(beta) + east * Mathf.Sin(beta);
        double tiles = km * 1000.0 / RadioRulesDef.Current.hfMetresPerWorldTile;
        float theta = (float)(tiles * Find.WorldGrid.AverageTileSize / radius);
        return (h * Mathf.Cos(theta) + dir * Mathf.Sin(theta)) * radius;
    }
}
