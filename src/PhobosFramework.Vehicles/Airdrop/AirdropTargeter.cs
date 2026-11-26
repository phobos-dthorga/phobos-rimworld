using System;
using System.Collections.Generic;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Choosing where a drop lands (Framework 0.288.0; docs/development/airdrop.md): a point on the map, with the wind's drift drawn as a line
/// from where the aircraft lets go to the point, a ring for the scatter, and the wind and any warning in words by the mouse. Presentation
/// only: it hands the chosen cell to one order.
/// </summary>
public static class AirdropTargeter
{
    static readonly TargetingParameters Cells = new() { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false, canTargetItems = false };

    /// <summary>Why a drop may not aim at this cell, or null: off the map, or under a mountain's roof.</summary>
    public static string? PointRefusal(Map map, IntVec3 cell)
    {
        if (map == null || !cell.InBounds(map)) return "PhobosAirdrop_RefuseOffMap".Translate().Resolve();
        if (cell.GetRoof(map) is { isThickRoof: true }) return "PhobosAirdrop_RefuseThickRoof".Translate().Resolve();
        return null;
    }

    /// <summary>Opens the targeter on <paramref name="map"/> for a drop of mainly <paramref name="main"/> loads.</summary>
    public static void Begin(Map map, ParachuteLoad main, Action<IntVec3> pick)
    {
        float feet = (float)ParachuteRule.For(main).PlannedFeet;
        // Framework 0.306.0: the view opens on a drop zone marker, where the drop most likely goes.
        if (DropZoneMarkers.First(map) is { } marker) CameraJumper.TryJump(marker);
        Find.Targeter.BeginTargeting(Cells,
            target => pick(target.Cell),
            highlightAction: target => Draw(map, main, feet, target.Cell),
            targetValidator: target => PointRefusal(map, target.Cell) == null,
            mouseAttachment: SystemIcons.For("Phobos/VehicleSystems/Airdrop/AirdropOn"),
            onGuiAction: target => Widgets.MouseAttachedLabel(Label(map, main, feet, target.Cell)));
    }

    static void Draw(Map map, ParachuteLoad main, float feet, IntVec3 cell)
    {
        if (!cell.InBounds(map)) return;
        float drift = PhobosParachuteFaller.DriftCells(map, main, feet);
        Vector3 point = cell.ToVector3Shifted();
        Vector3 release = point - PhobosWind.Downwind(map) * drift;
        float y = AltitudeLayer.MetaOverlays.AltitudeFor();
        GenDraw.DrawLineBetween(new Vector3(release.x, y, release.z), new Vector3(point.x, y, point.z), SimpleColor.White, 0.1f);
        GenDraw.DrawRadiusRing(cell, Mathf.Min(GenRadial.MaxRadialPatternRadius - 1f, (float)ParachuteRule.ScatterCells(drift) + 0.5f));
    }

    static string Label(Map map, ParachuteLoad main, float feet, IntVec3 cell)
    {
        if (PointRefusal(map, cell) is { } why) return why;
        string text = WindWords(map, main, feet);
        foreach (string warning in Warnings(map, main, feet)) text += "\n" + warning;
        return text;
    }

    /// <summary>The wind and the drift in words: the speed, which way it blows, and how far a load drifts, in real metres and in cells.</summary>
    public static string WindWords(Map map, ParachuteLoad main, float feet)
    {
        float knots = PhobosWind.Knots(map);
        if (knots < 0.5f) return "PhobosAirdrop_WindCalm".Translate().Resolve();
        return "PhobosAirdrop_Wind".Translate(knots.ToString("0").Named("KNOTS"), Compass(PhobosWind.DownwindBearing(map)).Named("TOWARD"),
            PhobosParachuteFaller.DriftMetres(map, main, feet).ToString("0").Named("METRES"),
            PhobosParachuteFaller.DriftCells(map, main, feet).ToString("0").Named("CELLS")).Resolve();
    }

    /// <summary>What a drop should be warned of now: wind past the limit, night, a height below the minimum or too low to open.</summary>
    public static IEnumerable<string> Warnings(Map map, ParachuteLoad main, float feet)
    {
        ParachuteRule.Figures figures = ParachuteRule.For(main);
        float knots = PhobosWind.Knots(map);
        if (knots > figures.WindLimitKnots)
            yield return "PhobosAirdrop_WarnWind".Translate(knots.ToString("0").Named("KNOTS"), figures.WindLimitKnots.ToString("0").Named("LIMIT")).Resolve();
        if (map.skyManager.CurSkyGlow < 0.3f) yield return "PhobosAirdrop_WarnNight".Translate().Resolve();
        if (!ParachuteRule.Opens(feet)) yield return "PhobosAirdrop_WarnNoOpen".Translate(feet.ToString("0").Named("FEET")).Resolve();
        else if (feet < figures.MinimumFeet)
            yield return "PhobosAirdrop_WarnLow".Translate(feet.ToString("0").Named("FEET"), figures.MinimumFeet.ToString("0").Named("MINIMUM")).Resolve();
    }

    /// <summary>A bearing as one of eight compass points, in the player's language.</summary>
    public static string Compass(float bearing)
    {
        int index = Mathf.RoundToInt(((bearing % 360f) + 360f) % 360f / 45f) % 8;
        return index switch
        {
            0 => "PhobosAirdrop_CompassN".Translate().Resolve(),
            1 => "PhobosAirdrop_CompassNE".Translate().Resolve(),
            2 => "PhobosAirdrop_CompassE".Translate().Resolve(),
            3 => "PhobosAirdrop_CompassSE".Translate().Resolve(),
            4 => "PhobosAirdrop_CompassS".Translate().Resolve(),
            5 => "PhobosAirdrop_CompassSW".Translate().Resolve(),
            6 => "PhobosAirdrop_CompassW".Translate().Resolve(),
            _ => "PhobosAirdrop_CompassNW".Translate().Resolve(),
        };
    }
}
