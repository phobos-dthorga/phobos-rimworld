using System;
using System.Reflection;
using Phobos.Framework.Rules;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// A map's wind as a direction and a speed in knots (D'thorga Framework 0.288.0; docs/development/airdrop.md, "Wind"). The game keeps only a
/// speed. With Combat Extended active its own wind direction is read, by name (it keeps one for its bullets and fire); without it this map's
/// component keeps one by Combat Extended's own rule, so the two read alike. The speed comes from the game's wind by Combat Extended's
/// mapping either way (WindRule). Indoors, under a roof, there is no wind.
/// </summary>
public static class PhobosWind
{
    const string CeTrackerType = "CombatExtended.WeatherTracker";
    const string CeDirectionProperty = "WindDirection";

    static bool ceRead;
    static Type? ceTracker;
    static PropertyInfo? ceDirection;

    /// <summary>Whether Combat Extended's wind is the one read.</summary>
    public static bool FromCombatExtended
    {
        get
        {
            ReadCombatExtended();
            return ceTracker != null && ceDirection != null;
        }
    }

    static void ReadCombatExtended()
    {
        if (ceRead) return;
        ceRead = true;
        if (!OptionalMods.IsActive(OptionalMods.CombatExtended)) return;
        ceTracker = OptionalMods.TypeNamed(CeTrackerType);
        ceDirection = ceTracker?.GetProperty(CeDirectionProperty, BindingFlags.Public | BindingFlags.Instance);
        if (ceTracker != null && ceDirection?.PropertyType != typeof(Vector3)) ceDirection = null;
    }

    /// <summary>Where the wind blows toward over open ground, a flat unit vector (x east, z north); zero in a calm.</summary>
    public static Vector3 Downwind(Map map)
    {
        if (map == null) return Vector3.zero;
        if (FromCombatExtended && Tracker(map) is { } tracker)
        {
            try
            {
                var v = (Vector3)ceDirection!.GetValue(tracker)!;
                v.y = 0f;
                if (v.sqrMagnitude > 1e-6f) return v.normalized;
            }
            catch (Exception)
            {
                // Read by name: a changed Combat Extended falls back to ours, never an error.
                ceDirection = null;
            }
        }
        double bearing = MapComponent_PhobosWind.For(map)?.Bearing ?? 0;
        (double east, double north) = WindRule.Downwind(bearing);
        return new Vector3((float)east, 0f, (float)north);
    }

    /// <summary>The compass bearing the wind blows toward, degrees clockwise from north.</summary>
    public static float DownwindBearing(Map map)
    {
        Vector3 v = Downwind(map);
        return (Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg + 360f) % 360f;
    }

    /// <summary>The wind's speed over open ground, knots.</summary>
    public static float Knots(Map map) => map == null ? 0f : (float)WindRule.KnotsForGameWind(map.windManager.WindSpeed);

    /// <summary>The wind's speed at a cell, knots: none under a roof.</summary>
    public static float KnotsAt(Map map, IntVec3 cell) => map == null || !cell.InBounds(map) || cell.Roofed(map) ? 0f : Knots(map);

    static MapComponent? Tracker(Map map)
    {
        foreach (MapComponent component in map.components)
        {
            if (component != null && component.GetType() == ceTracker) return component;
        }
        return null;
    }
}

/// <summary>
/// The wind direction of a map without Combat Extended (Framework 0.288.0): it wanders toward a random target, up to five degrees each rare
/// tick, as Combat Extended's does, and is saved. With Combat Extended active it stands still and costs nothing. Safe to add mid-game: a map
/// with no saved direction starts from a random one.
/// </summary>
public sealed class MapComponent_PhobosWind : MapComponent
{
    const int StepTicks = 250;

    float bearing = float.NaN, target = float.NaN;

    public MapComponent_PhobosWind(Map map) : base(map) { }

    /// <summary>The bearing this map's wind blows toward, degrees clockwise from north.</summary>
    public float Bearing
    {
        get
        {
            if (float.IsNaN(bearing)) Start();
            return bearing;
        }
    }

    public static MapComponent_PhobosWind? For(Map? map) => map?.GetComponent<MapComponent_PhobosWind>();

    void Start()
    {
        bearing = Rand.Range(0f, 360f);
        target = Rand.Range(0f, 360f);
    }

    public override void MapComponentTick()
    {
        if ((Find.TickManager.TicksGame + map.uniqueID) % StepTicks != 0 || PhobosWind.FromCombatExtended) return;
        if (float.IsNaN(bearing) || float.IsNaN(target)) Start();
        if (WindRule.Reached(bearing, target)) target = Rand.Range(0f, 360f);
        bearing = (float)WindRule.Wander(bearing, target, Rand.Range(0f, (float)WindRule.WanderStepDegrees));
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref bearing, "PhobosWind_Bearing", float.NaN);
        Scribe_Values.Look(ref target, "PhobosWind_Target", float.NaN);
    }
}
