using System.Collections.Generic;
using Phobos.Framework.Interface;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The damage sheet (Framework 0.78.0; docs/development/vehicle-presentation-design.md, P7a): a vehicle's own picture, seen from above,
/// with every hurt part's cells tinted by its health, as a car-hire firm's damage diagram. A cell shows the worst part covering it; a part
/// at full health leaves its cells clear. Presentation only: it draws what it is given and says which part the mouse is over.
/// </summary>
public static class DamageSheet
{
    /// <summary>How strongly a hurt cell is tinted over the picture (authored).</summary>
    const float TintAlpha = 0.38f;
    /// <summary>A part at or above this share is whole and left clear.</summary>
    const float WholeShare = 0.999f;
    static readonly Color Background = new(0.09f, 0.09f, 0.1f);

    /// <summary>One part's place on the sheet: its key, label and health share.</summary>
    public readonly struct SheetPart
    {
        public readonly string Key;
        public readonly string Label;
        public readonly float Share;

        public SheetPart(string key, string label, float share)
        {
            Key = key;
            Label = label;
            Share = share;
        }
    }

    /// <summary>
    /// Draws the sheet in <paramref name="rect"/> and returns the key of the part under the mouse (the smallest part covering that cell),
    /// or null. The <paramref name="lit"/> part's cells are outlined, so hovering its row elsewhere shows where it sits.
    /// </summary>
    public static string? Draw(Rect rect, VehicleDef def, IReadOnlyList<SheetPart> parts, string? lit)
    {
        Widgets.DrawBoxSolid(rect, Background);
        Widgets.DrawBox(rect);
        Rect area = rect.ContractedBy(6f);
        VehiclePicture.Draw(area, def, facingNorth: true);

        Rect picture = PictureRect(area, def);
        Vector2 drawSize = ((GraphicData?)def.graphicData)?.drawSize ?? Vector2.one;
        float perCell = picture.width / Mathf.Max(0.01f, drawSize.x);
        CellRect footprint = def.VehicleRect(IntVec3.Zero, Rot4.North);
        float centreX = (footprint.minX + footprint.maxX + 1) / 2f;
        float centreZ = (footprint.minZ + footprint.maxZ + 1) / 2f;

        Rect CellRectOf(IntVec2 cell) => new(
            picture.center.x + (cell.x - centreX) * perCell,
            picture.center.y - (cell.z + 1 - centreZ) * perCell,
            perCell, perCell);

        var shares = new Dictionary<string, SheetPart>();
        foreach (SheetPart part in parts) shares[part.Key] = part;

        // The worst part on each cell, and the smallest part on each cell for the mouse.
        var worst = new Dictionary<IntVec2, float>();
        var smallest = new Dictionary<IntVec2, (string key, int size)>();
        foreach (VehicleComponentProperties props in def.components ?? new List<VehicleComponentProperties>())
        {
            if (props.hitbox?.Hitbox is not { Count: > 0 } cells || !shares.TryGetValue(props.key, out SheetPart part)) continue;
            foreach (IntVec2 cell in cells)
            {
                if (!worst.TryGetValue(cell, out float share) || part.Share < share) worst[cell] = part.Share;
                if (!smallest.TryGetValue(cell, out var held) || cells.Count < held.size) smallest[cell] = (props.key, cells.Count);
            }
        }
        foreach (KeyValuePair<IntVec2, float> pair in worst)
        {
            if (pair.Value >= WholeShare) continue;
            Color colour = PhobosWidgets.HealthColour(pair.Value);
            colour.a = TintAlpha;
            Widgets.DrawBoxSolid(CellRectOf(pair.Key), colour);
        }

        string? hovered = null;
        if (Mouse.IsOver(area))
        {
            foreach (KeyValuePair<IntVec2, (string key, int size)> pair in smallest)
            {
                if (!Mouse.IsOver(CellRectOf(pair.Key))) continue;
                hovered = pair.Value.key;
                break;
            }
        }
        string? outline = hovered ?? lit;
        if (outline != null)
        {
            foreach (VehicleComponentProperties props in def.components ?? new List<VehicleComponentProperties>())
            {
                if (props.key != outline || props.hitbox?.Hitbox is not { } cells) continue;
                foreach (IntVec2 cell in cells) Widgets.DrawBox(CellRectOf(cell), 1);
            }
        }
        if (hovered != null && shares.TryGetValue(hovered, out SheetPart over))
        {
            bool figures = VehicleSystemsMod.Settings.displayMode != Phobos.Framework.DisplayMode.Plain;
            TooltipHandler.TipRegion(area, over.Label + ": " + (figures ? over.Share.ToStringPercent() : HealthWords.Of(over.Share)));
        }
        return hovered;
    }

    /// <summary>Where Vehicle Framework puts the picture in a rect (facing north): fitted to the drawn size and the Def's icon scale,
    /// centred, then moved by the Def's display offset, as its own picture drawing does (VehicleDef.ScaleDrawRatio, VehicleGui.GetBlitData).</summary>
    static Rect PictureRect(Rect rect, VehicleDef def)
    {
        Vector2 drawSize = ((GraphicData?)def.graphicData)?.drawSize ?? Vector2.one;
        float width = rect.width * def.uiIconScale, height = rect.height * def.uiIconScale;
        if (width < height) height = width * (drawSize.y / Mathf.Max(0.01f, drawSize.x));
        else width = height * (drawSize.x / Mathf.Max(0.01f, drawSize.y));
        Vector3 offset = VehiclePicture.DisplayOffsetNorth(def);
        return new Rect(rect.x + (rect.width - width) / 2f + offset.x * rect.width, rect.y + (rect.height - height) / 2f + offset.y * rect.height, width, height);
    }
}
