using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Drop zone markers (D'thorga Framework 0.306.0; docs/development/airdrop.md, "Drop zone markers"; the owner's cards of 26 Nov 2026: "Aim point
/// and less scatter", then "Panel plus smoke"): a pathfinder's panel laid on the ground, as real pathfinder teams mark a drop zone, which puts
/// up coloured smoke while an aircraft drops on it by day and shows its lights by night. A drop aimed within reach of one scatters half as
/// much, and less again by day, when the crew can read the surface wind off its smoke; the drop targeter opens on it; an ally's supply drop
/// aims at it.
/// </summary>
public static class DropZoneMarkers
{
    /// <summary>How far from a marker a drop's point may be for the marker to help, cells (authored).</summary>
    public const float ReachCells = 15f;

    /// <summary>The share of the usual scatter for a drop aimed near a marker: the panel as an aim point (authored: half).</summary>
    public const float MarkedScatterShare = 0.5f;

    /// <summary>
    /// The share of that again by day, when the marker's smoke shows the crew the wind at the ground (authored). Our crews already let go upwind
    /// of the point by the drift worked out from the wind (the targeter's release point), as a real crew does; the smoke makes that wind truer,
    /// so the wind-born scatter is less.
    /// </summary>
    public const float SmokeScatterShare = 0.7f;

    /// <summary>The sun's glow at and above which it counts as day: smoke can be seen; below it, the lights are on (authored).</summary>
    public const float DaylightGlow = 0.3f;

    /// <summary>Ticks between puffs of a marker's smoke while an aircraft drops on it.</summary>
    public const int SmokeEveryTicks = 20;

    /// <summary>The smoke's colour: violet, one of the US M18 smoke grenade's four colours [S], and unlike any fire or the game's own smoke.</summary>
    static readonly Color SmokeColour = new(0.62f, 0.38f, 0.82f);

    /// <summary>The markers spawned on this map.</summary>
    public static IEnumerable<Thing> On(Map? map) =>
        map == null ? Enumerable.Empty<Thing>() : map.listerThings.ThingsOfDef(AirdropDefOf.Phobos_DropZoneMarker);

    /// <summary>The marker nearest this cell within reach, or null.</summary>
    public static Thing? Near(Map? map, IntVec3 cell)
    {
        Thing? best = null;
        float bestSq = ReachCells * ReachCells;
        foreach (Thing marker in On(map))
        {
            float sq = (marker.Position - cell).LengthHorizontalSquared;
            if (sq > bestSq) continue;
            bestSq = sq;
            best = marker;
        }
        return best;
    }

    /// <summary>Whether it is light enough on this map for a marker's smoke to be seen from the air.</summary>
    public static bool Daylight(Map map) => GenCelestial.CurCelestialSunGlow(map) >= DaylightGlow;

    /// <summary>The share of the usual scatter for a drop aimed at this cell: half near a marker, and less again by day; else all of it.</summary>
    public static float ScatterShare(Map? map, IntVec3 cell)
    {
        if (map == null || !cell.IsValid || Near(map, cell) == null) return 1f;
        return Daylight(map) ? MarkedScatterShare * SmokeScatterShare : MarkedScatterShare;
    }

    /// <summary>The marker a drop should aim at on this map: the first laid, or null when there is none.</summary>
    public static Thing? First(Map? map) => On(map).OrderBy(t => t.thingIDNumber).FirstOrDefault();

    /// <summary>
    /// While an aircraft drops on a point near a marker, by day: a puff of the marker's smoke every so often, carried downwind, as a pathfinder
    /// pops smoke for the run-in. Looks only: never the game's blinding smoke.
    /// </summary>
    public static void Smoke(Map map, IntVec3 target)
    {
        if (Find.TickManager.TicksGame % SmokeEveryTicks != 0 || !target.IsValid || !Daylight(map)) return;
        if (Near(map, target) is not { } marker) return;
        Vector3 loc = marker.TrueCenter();
        if (!loc.ShouldSpawnMotesAt(map)) return;
        Vector3 downwind = PhobosWind.Downwind(map);
        FleckCreationData data = FleckMaker.GetDataStatic(loc, map, FleckDefOf.Smoke, Rand.Range(1.2f, 2f));
        data.rotationRate = Rand.Range(-30f, 30f);
        data.instanceColor = SmokeColour;
        bool calm = downwind.sqrMagnitude < 0.01f;
        data.velocityAngle = calm ? Rand.Range(-15f, 15f) : downwind.AngleFlat() + Rand.Range(-10f, 10f);
        data.velocitySpeed = calm ? Rand.Range(0.3f, 0.5f) : Rand.Range(0.7f, 1.1f);
        map.flecks.CreateFleck(data);
        Seen.Note("airdrop.marker-smoke", marker);
    }
}

/// <summary>
/// A drop zone marker's lights (Framework 0.306.0; the owner's card of 26 Nov 2026, "Panel plus smoke", lights at night with it): lit only while
/// it is too dark for its smoke to be seen, as a real drop zone is marked by lights at night. Needs no power (battery beacons, authored);
/// checked every rare tick, so a marker costs next to nothing.
/// </summary>
public class CompPhobosMarkerLights : CompGlower
{
    // Derived, never saved: whether it was dark at the last look.
    bool wasDark;

    protected override bool ShouldBeLitNow => parent.Spawned && !DropZoneMarkers.Daylight(parent.Map);

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!parent.Spawned) return;
        bool dark = !DropZoneMarkers.Daylight(parent.Map);
        if (dark == wasDark) return;
        wasDark = dark;
        UpdateLit(parent.Map);
        if (dark) Seen.Note("airdrop.marker-lights", parent);
    }
}
