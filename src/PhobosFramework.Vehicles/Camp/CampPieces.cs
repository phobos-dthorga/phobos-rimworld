using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Takes the camp's canvas roof off the cells given, only where it is still the canvas roof (another roof built since is left).</summary>
internal static class CanvasRoofs
{
    internal static void Lift(Map? map, List<IntVec3> cells)
    {
        if (map == null) return;
        foreach (IntVec3 cell in cells)
        {
            if (cell.InBounds(map) && map.roofGrid.RoofAt(cell) == VehicleCampDefOf.Phobos_CanvasRoof) map.roofGrid.SetRoof(cell, null);
        }
    }

    internal static void Lay(Map map, IEnumerable<IntVec3> cells, List<IntVec3> laid)
    {
        foreach (IntVec3 cell in cells)
        {
            if (!cell.InBounds(map) || map.roofGrid.RoofAt(cell) != null) continue;
            map.roofGrid.SetRoof(cell, VehicleCampDefOf.Phobos_CanvasRoof);
            map.areaManager.BuildRoof[cell] = false;
            map.areaManager.NoRoof[cell] = false;
            laid.Add(cell);
        }
    }
}

/// <summary>
/// A pole of a vehicle's awning (Framework 0.212.0; the record's Y9). The awning is a canvas roof over the cells along the vehicle's side, two
/// deep: rain and snow do not fall there, but it is no room. The first pole draws the canvas over those cells, see-through so the people
/// beneath still show. Every pole holds the list of the awning's roof cells and takes the canvas off before it goes, shot down or struck, so
/// a camp left standing by a lost vehicle stays whole until a pole is taken down. No Harmony.
/// </summary>
public class Building_PhobosAwningPole : Building
{
    const string CanvasPath = "Phobos/VehicleSystems/Camp/AwningCanvas";
    /// <summary>How much of the canvas shows over what is beneath it (authored): enough to see the awning, little enough to see who is under it.</summary>
    const float CanvasOpacity = 0.6f;

    static Material? canvasMaterial;

    /// <summary>The awning's cells against the vehicle, one a row: the canvas is drawn from each across both rows. Only on the pole that draws it.</summary>
    internal List<IntVec3> canvasRows = new();
    /// <summary>The direction out from the vehicle, as the game counts rotations.</summary>
    internal int outRotation;
    /// <summary>The awning's roof cells, on every pole.</summary>
    internal List<IntVec3> roof = new();

    /// <summary>The canvas picture, or a plain see-through canvas colour while the picture is missing (authored: a sand-coloured canvas).</summary>
    static Material Canvas => canvasMaterial ??= ContentFinder<Texture2D>.Get(CanvasPath, reportFailure: false) != null
        ? MaterialPool.MatFrom(CanvasPath, ShaderDatabase.Transparent, new Color(1f, 1f, 1f, CanvasOpacity))
        : SolidColorMaterials.NewSolidColorMaterial(new Color(0.69f, 0.64f, 0.5f, CanvasOpacity * 0.7f), ShaderDatabase.Transparent);

    public override void Print(SectionLayer layer)
    {
        base.Print(layer);
        if (canvasRows.Count == 0) return;
        Rot4 outward = new(outRotation);
        Vector3 half = outward.FacingCell.ToVector3() * 0.5f;
        float altitude = AltitudeLayer.MetaOverlays.AltitudeFor();
        foreach (IntVec3 row in canvasRows)
        {
            Vector3 centre = row.ToVector3Shifted() + half;
            centre.y = altitude;
            // The picture is drawn with its top away from the vehicle: one cell along, the awning's depth out.
            Printer_Plane.PrintPlane(layer, centre, new Vector2(1f, Phobos.Framework.Vehicles.Rules.CampLayoutRule.AwningDepth), Canvas, outward.AsAngle);
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        CanvasRoofs.Lift(Map, roof);
        roof.Clear();
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref canvasRows, "PhobosAwning_Canvas", LookMode.Value);
        Scribe_Collections.Look(ref roof, "PhobosAwning_Roof", LookMode.Value);
        Scribe_Values.Look(ref outRotation, "PhobosAwning_Out");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            canvasRows ??= new List<IntVec3>();
            roof ??= new List<IntVec3>();
        }
    }
}

/// <summary>
/// A roof tent, pitched (Framework 0.212.0; the record's B1): a two-person bed under its own canvas roof beside the vehicle. It takes its roof off
/// before it goes, as the awning's poles do. The vehicle's camp packs it away; if the vehicle is lost it stays as a bed.
/// </summary>
public class Building_PhobosRoofTent : Building_Bed
{
    internal List<IntVec3> roof = new();

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        CanvasRoofs.Lift(Map, roof);
        roof.Clear();
        base.DeSpawn(mode);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref roof, "PhobosRoofTent_Roof", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) roof ??= new List<IntVec3>();
    }
}
