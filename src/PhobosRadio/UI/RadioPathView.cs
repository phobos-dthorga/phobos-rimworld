using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The ray line (Radio 0.3.0, the radio visuals record's RV2): while a colonist wearing a hand-held is selected, a line runs from
/// them to the antenna their contact uses, coloured by how good it is, with a mark on everything it met: a filled square on a wall it
/// passed below wall height, which cost signal; a hollow ring on one it passed over; a diamond on rock. The stretch above wall height
/// is drawn thin. Operator adds the signal to spare at the middle of the line, Engineer what each run of things cost. Presentation
/// only: it reads the net's contact and its <see cref="LinkPath"/>, and decides nothing.
/// <para>
/// <b>Cost</b> (the radio's performance rule): a look at the selection each frame; for each selected colonist with a radio (at most
/// <see cref="MaxColonists"/>), one line and a quad a mark; labels only in Operator and Engineer, made once per contact. The path is
/// walked once per contact, about once a second.
/// </para>
/// </summary>
public sealed class RadioPathView : MapComponent
{
    /// <summary>The most selected colonists drawn at once: a crowd selected by box draws only this many lines.</summary>
    public const int MaxColonists = 6;
    /// <summary>The most marks drawn on one line; a line through a mountain stops marking there, as the contact is lost anyway.</summary>
    private const int MaxMarks = 120;
    private const float ThickLine = 0.16f;
    private const float ThinLine = 0.06f;

    private static readonly Color CleanColor = new(1f, 0.91f, 0.66f);
    private static readonly Color WorkableColor = new(0.94f, 0.63f, 0.29f);
    private static readonly Color MarginalColor = new(0.79f, 0.63f, 1f);
    private static readonly Color NoContactColor = new(1f, 0.42f, 0.42f);
    private static readonly Color BuiltColor = new(0.94f, 0.45f, 0.23f);
    private static readonly Color RockColor = new(1f, 0.42f, 0.42f);
    private static readonly Color ClearedColor = new(0.77f, 0.8f, 0.84f);
    private static readonly ConditionalWeakTable<LinkPath, LabelSet> labelsByPath = new();

    /// <summary>A contact's words for one display depth, kept with its path so they are made once.</summary>
    private sealed record LabelSet(DisplayMode mode, List<(Vector3 at, string text, Color color)> labels);

    private readonly List<(LinkPath path, LinkEvaluation link)> shown = new();

    public RadioPathView(Map map) : base(map) { }

    public static Color OutcomeColor(LinkOutcome outcome) => outcome switch
    {
        LinkOutcome.Clean => CleanColor,
        LinkOutcome.Workable => WorkableColor,
        LinkOutcome.Marginal => MarginalColor,
        _ => NoContactColor,
    };

    public static Color CrossingColor(CrossingKind kind) => kind switch
    {
        CrossingKind.Rock => RockColor,
        CrossingKind.Built => BuiltColor,
        _ => ClearedColor,
    };

    public override void MapComponentUpdate()
    {
        shown.Clear();
        if (Find.CurrentMap != map) return;
        var selected = Find.Selector.SelectedObjectsListForReading;
        if (selected.Count == 0) return;
        RadioNet? net = null;
        foreach (var obj in selected)
        {
            // A colonist with a hand-held, or a vehicle with a radio built in (Radio 0.10.0): the net has a best link for either.
            if (obj is not Pawn pawn || pawn.Map != map) continue;
            net ??= RadioNet.For(map);
            if (!net.TryGetBest(pawn, out var link) || link == null) continue;
            var path = LinkPath.For(map, link);
            Draw(path, link);
            shown.Add((path, link));
            if (shown.Count >= MaxColonists) break;
        }
    }

    public override void MapComponentOnGUI()
    {
        if (shown.Count == 0 || Event.current.type != EventType.Repaint) return;
        var mode = RadioMod.Settings.displayMode;
        if (mode == DisplayMode.Plain) return;
        foreach (var (path, link) in shown)
        {
            foreach (var (at, text, color) in Labels(path, link, mode)) GenMapUI.DrawThingLabel(at.MapToUIPosition(), text, color);
        }
    }

    private static Vector3 At(IntVec3 cell) => cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);

    private static Vector3 Along(LinkPath path, float fraction) => Vector3.Lerp(At(path.From), At(path.To), fraction);

    private static void Draw(LinkPath path, LinkEvaluation link)
    {
        var line = SolidColorMaterials.SimpleSolidColorMaterial(OutcomeColor(link.Outcome));
        var from = At(path.From);
        var to = At(path.To);
        if (path.WallHeightCrossing is { } crossing)
        {
            // Thick where walls count, thin where the line is above them.
            var middle = Along(path, crossing);
            var lowAtHandheld = path.BelowWallHeight(0f);
            GenDraw.DrawLineBetween(from, middle, line, lowAtHandheld ? ThickLine : ThinLine);
            GenDraw.DrawLineBetween(middle, to, line, lowAtHandheld ? ThinLine : ThickLine);
        }
        else
        {
            GenDraw.DrawLineBetween(from, to, line, path.BelowWallHeight(0.5f) ? ThickLine : ThinLine);
        }
        var drawn = 0;
        foreach (var mark in path.Marks)
        {
            if (++drawn > MaxMarks) break;
            DrawMark(At(mark.Cell), mark.Kind);
        }
    }

    private static void DrawMark(Vector3 at, CrossingKind kind)
    {
        var material = SolidColorMaterials.SimpleSolidColorMaterial(CrossingColor(kind));
        switch (kind)
        {
            case CrossingKind.Cleared:
                GenDraw.DrawCircleOutline(at, 0.28f, material);
                break;
            case CrossingKind.Rock:
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.Euler(0f, 45f, 0f), new Vector3(0.42f, 1f, 0.42f)), material, 0);
                break;
            default:
                Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(at, Quaternion.identity, new Vector3(0.44f, 1f, 0.44f)), material, 0);
                break;
        }
    }

    /// <summary>The words on the line, made once per contact and depth: the signal to spare, then (Engineer) each run and roof.</summary>
    private static List<(Vector3 at, string text, Color color)> Labels(LinkPath path, LinkEvaluation link, DisplayMode mode)
    {
        if (labelsByPath.TryGetValue(path, out var cached) && cached.mode == mode) return cached.labels;
        var labels = new List<(Vector3, string, Color)>
        {
            (Along(path, 0.5f) + new Vector3(0f, 0f, 0.6f),
                "PhobosRadio_RayMargin".Translate(LinkText.OutcomeLabel(link.Outcome).Named("OUTCOME"), link.MarginDb.ToString("0").Named("MARGIN")),
                OutcomeColor(link.Outcome)),
        };
        if (mode == DisplayMode.Engineer)
        {
            var below = true;
            foreach (var run in path.Runs)
            {
                var at = At(run.MiddleCell) + new Vector3(0f, 0f, below ? -0.7f : 0.7f);
                below = !below;
                labels.Add((at, RunText(run), CrossingColor(run.Kind)));
            }
            if (path.RoofFromDb > 0) labels.Add((At(path.From) + new Vector3(0f, 0f, -0.8f), RoofText(path, path.RoofFromDb, path.RoofFromNatural), ClearedColor));
            if (path.RoofToDb > 0) labels.Add((At(path.To) + new Vector3(0f, 0f, -0.8f), RoofText(path, path.RoofToDb, path.RoofToNatural), ClearedColor));
        }
        labelsByPath.AddOrUpdate(path, new LabelSet(mode, labels));
        return labels;
    }

    public static string RunText(CrossingRun run)
    {
        if (run.Kind == CrossingKind.Cleared) return "PhobosRadio_RayCleared".Translate(run.Label.Named("THING"));
        var count = run.Count > 1 ? "PhobosRadio_RayTimes".Translate(run.Count.Named("COUNT")).ToString() : string.Empty;
        return "PhobosRadio_RayCrossing".Translate(run.Label.Named("THING"), count.Named("TIMES"), run.CostDb.ToString("0").Named("LOSS"));
    }

    public static string RoofText(LinkPath path, float lossDb, bool natural) =>
        "PhobosRadio_RayRoof".Translate(path.RoofCostDb(lossDb, natural).ToString("0").Named("LOSS"));
}
