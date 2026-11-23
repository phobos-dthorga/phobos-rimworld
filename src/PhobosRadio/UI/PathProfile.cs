using Phobos.Radio.Services;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The side view of a contact's path (Radio 0.3.0, the radio visuals record's RV4), drawn in the link-budget window: the station's
/// antenna on the left at its height, the hand-held on the right, the ground, wall height as a dashed line, and between them each
/// thing the straight line met: a wall it passed below in orange, one it passed over as an outline, rock full height. Roofs sit over
/// each end. The line itself is coloured by how good the contact is. Heights are in metres in every mode; Engineer adds what each run
/// cost. Presentation only, from the contact's <see cref="LinkPath"/>; it costs nothing while the window is shut.
/// </summary>
public static class PathProfile
{
    public const float Height = 170f;
    private const float LeftMargin = 46f;
    private const float RightMargin = 14f;
    private const float TopMargin = 12f;
    private const float BottomMargin = 26f;
    private const float MinScaleMetres = 6f;

    private static readonly Color Background = new(0.05f, 0.06f, 0.08f);
    private static readonly Color Grid = new(0.18f, 0.22f, 0.27f);
    private static readonly Color Ground = new(0.16f, 0.19f, 0.14f);
    private static readonly Color RockFill = new(0.18f, 0.19f, 0.22f);
    private static readonly Color BuiltRoof = new(0.55f, 0.54f, 0.49f);
    private static readonly Color Muted = new(0.58f, 0.63f, 0.70f);
    private static readonly Color Antenna = Color.white;
    private static readonly Color Handheld = new(0.90f, 0.83f, 0.68f);

    public static void Draw(Rect rect, LinkPath path, LinkEvaluation link, DisplayMode depth)
    {
        Widgets.DrawBoxSolid(rect, Background);
        var plot = new Rect(rect.x + LeftMargin, rect.y + TopMargin, rect.width - LeftMargin - RightMargin,
            rect.height - TopMargin - BottomMargin);
        var topMetres = Mathf.Max(MinScaleMetres, Mathf.Max(path.HeightFrom, path.HeightTo) + 2f);
        float Y(float metres) => plot.yMax - metres / topMetres * plot.height;
        // The station's antenna on the left: a fraction along the path from the hand-held is drawn from the right.
        float X(float fraction) => plot.xMax - fraction * plot.width;
        var steps = path.Marks.Count > 0 ? path.Marks[0].Steps : 1;
        var cellWidth = Mathf.Max(2f, plot.width / Mathf.Max(1, steps));

        Text.Font = GameFont.Tiny;
        Text.Anchor = TextAnchor.MiddleRight;
        foreach (var metres in new[] { 0f, path.WallHeightMetres, Mathf.Round(topMetres / 2f), Mathf.Floor(topMetres) })
        {
            var y = Y(metres);
            var isWall = Mathf.Approximately(metres, path.WallHeightMetres);
            if (isWall) DashedLine(plot.x, plot.xMax, y, RadioPathView.CrossingColor(CrossingKind.Built));
            else Widgets.DrawLine(new Vector2(plot.x, y), new Vector2(plot.xMax, y), Grid, 1f);
            GUI.color = isWall ? RadioPathView.CrossingColor(CrossingKind.Built) : Muted;
            Widgets.Label(new Rect(rect.x, y - 9f, LeftMargin - 6f, 18f), "PhobosRadio_ProfileMetres".Translate(metres.ToString("0.#").Named("METRES")));
        }
        GUI.color = Color.white;
        Widgets.DrawBoxSolid(new Rect(plot.x, plot.yMax, plot.width, 4f), Ground);

        DrawRoof(path.RoofToDb, path.RoofToNatural, X(1f), cellWidth, plot, Y);
        DrawRoof(path.RoofFromDb, path.RoofFromNatural, X(0f), cellWidth, plot, Y);
        foreach (var mark in path.Marks)
        {
            var x = X(mark.Fraction) - cellWidth / 2f;
            var color = RadioPathView.CrossingColor(mark.Kind);
            switch (mark.Kind)
            {
                case CrossingKind.Rock:
                    Widgets.DrawBoxSolid(new Rect(x, plot.y, cellWidth - 1f, plot.height), RockFill);
                    Widgets.DrawBoxSolid(new Rect(x, plot.y, cellWidth - 1f, 3f), color);
                    break;
                case CrossingKind.Built:
                    Widgets.DrawBoxSolid(new Rect(x, Y(path.WallHeightMetres), cellWidth - 1f, plot.yMax - Y(path.WallHeightMetres)), color);
                    break;
                default:
                    GUI.color = color;
                    Widgets.DrawBox(new Rect(x, Y(path.WallHeightMetres), cellWidth - 1f, plot.yMax - Y(path.WallHeightMetres)));
                    GUI.color = Color.white;
                    break;
            }
        }

        // The two ends, then the line between them.
        var antennaTop = new Vector2(X(1f), Y(path.HeightTo));
        Widgets.DrawLine(new Vector2(X(1f), plot.yMax), antennaTop, Antenna, 2f);
        Widgets.DrawBoxSolid(new Rect(antennaTop.x - 3f, antennaTop.y - 3f, 6f, 6f), Antenna);
        var handheldTop = new Vector2(X(0f), Y(path.HeightFrom));
        Widgets.DrawLine(new Vector2(X(0f), plot.yMax), handheldTop, Handheld, 4f);
        Widgets.DrawLine(antennaTop, handheldTop, RadioPathView.OutcomeColor(link.Outcome), 2f);

        if (depth == DisplayMode.Engineer) DrawRunLabels(path, plot, X);

        Text.Anchor = TextAnchor.LowerLeft;
        GUI.color = Muted;
        var labels = new Rect(plot.x, rect.yMax - 20f, plot.width, 18f);
        Widgets.Label(labels, "PhobosRadio_ProfileAntenna".Translate(path.HeightTo.ToString("0.#").Named("HEIGHT")));
        Text.Anchor = TextAnchor.LowerCenter;
        Widgets.Label(labels, "PhobosRadio_ProfileDistance".Translate(path.DistanceMetres.ToString("0").Named("DISTANCE")));
        Text.Anchor = TextAnchor.LowerRight;
        Widgets.Label(labels, "PhobosRadio_ProfileHandheld".Translate(path.HeightFrom.ToString("0.#").Named("HEIGHT")));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Small;
    }

    private static void DrawRoof(float lossDb, bool natural, float x, float cellWidth, Rect plot, System.Func<float, float> y)
    {
        if (lossDb <= 0) return;
        var width = cellWidth * 3f;
        var left = Mathf.Clamp(x - width / 2f, plot.x, plot.xMax - width);
        if (natural)
        {
            Widgets.DrawBoxSolid(new Rect(left, plot.y, width, y(2.9f) - plot.y), RockFill);
        }
        else
        {
            Widgets.DrawBoxSolid(new Rect(left, y(2.9f), width, 3f), BuiltRoof);
        }
    }

    /// <summary>Engineer: what each run cost, above the plot, skipping any that would overlap the one before.</summary>
    private static void DrawRunLabels(LinkPath path, Rect plot, System.Func<float, float> x)
    {
        Text.Anchor = TextAnchor.UpperCenter;
        var lastRight = float.NegativeInfinity;
        // Left to right on screen is antenna to hand-held, so walk the runs from the antenna end.
        for (var i = path.Runs.Count - 1; i >= 0; i--)
        {
            var run = path.Runs[i];
            var text = RadioPathView.RunText(run);
            var width = Text.CalcSize(text).x + 6f;
            var centre = x((run.FirstFraction + run.LastFraction) / 2f);
            var left = Mathf.Clamp(centre - width / 2f, plot.x, plot.xMax - width);
            if (left < lastRight) continue;
            Widgets.DrawBoxSolid(new Rect(left, plot.y + 2f, width, 16f), new Color(0f, 0f, 0f, 0.6f));
            GUI.color = RadioPathView.CrossingColor(run.Kind);
            Widgets.Label(new Rect(left, plot.y + 2f, width, 16f), text);
            GUI.color = Color.white;
            lastRight = left + width + 4f;
        }
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static void DashedLine(float from, float to, float y, Color color)
    {
        const float Dash = 6f;
        for (var x = from; x < to; x += Dash * 2f) Widgets.DrawLine(new Vector2(x, y), new Vector2(Mathf.Min(x + Dash, to), y), color, 1f);
    }
}
