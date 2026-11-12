using System.Collections.Generic;
using Phobos.Framework.Rules;
using UnityEngine;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// The little displays every Phobos tab draws the same way: a gauge, a warning light, a history graph, a section heading, a
/// tachometer. Presentation only, on the game's own widgets; callers pass what to show and handle any click themselves.
/// </summary>
public static class PhobosWidgets
{
    public static readonly Color Good = new(0.36f, 0.72f, 0.36f);
    public static readonly Color Caution = new(0.92f, 0.70f, 0.28f);
    public static readonly Color Bad = new(0.85f, 0.32f, 0.28f);
    public static readonly Color Off = new(0.45f, 0.45f, 0.48f);

    /// <summary>Health at or above this share reads as good (authored).</summary>
    public const float HealthGoodShare = 0.7f;
    /// <summary>Health at or above this share, and below good, reads as caution; below it, bad (authored).</summary>
    public const float HealthCautionShare = 0.35f;

    /// <summary>
    /// The colour for a share of health (0 to 1): good, caution, bad, or off when nothing is left. Our own colours, so a tab
    /// never needs another mod's colour helper (on RimWorld 1.7, a 1.6 mod's method that returns a Unity type cannot be called
    /// from a 1.7 build).
    /// </summary>
    public static Color HealthColour(float share) =>
        share <= 0f ? Off : share >= HealthGoodShare ? Good : share >= HealthCautionShare ? Caution : Bad;
    static readonly Color GaugeBackground = new(0.12f, 0.12f, 0.13f);

    /// <summary>A horizontal gauge: a bar filled to a share, in a colour, with an optional label above and figure inside.</summary>
    public static void Gauge(Rect rect, float fill, Color colour, string? label = null, string? figure = null, string? tooltip = null)
    {
        float barHeight = label == null ? rect.height : Mathf.Max(10f, rect.height - 18f);
        var bar = new Rect(rect.x, rect.yMax - barHeight, rect.width, barHeight);
        if (label != null)
        {
            using (new TextBlock(GameFont.Tiny, TextAnchor.LowerLeft))
            {
                Widgets.Label(new Rect(rect.x, rect.y, rect.width, rect.height - barHeight), label);
            }
        }
        Widgets.DrawBoxSolid(bar, GaugeBackground);
        Widgets.DrawBoxSolid(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(fill), bar.height), colour);
        Widgets.DrawBox(bar);
        if (figure != null)
        {
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter))
            {
                Widgets.Label(bar, figure);
            }
        }
        if (tooltip != null) TooltipHandler.TipRegion(rect, tooltip);
    }

    /// <summary>A warning light: a coloured square with its label beside it. Returns the width used.</summary>
    public static float WarningLight(Rect rect, Color colour, string label)
    {
        float size = Mathf.Min(rect.height, 14f);
        var light = new Rect(rect.x, rect.y + (rect.height - size) / 2f, size, size);
        Widgets.DrawBoxSolid(light, colour);
        Widgets.DrawBox(light);
        float textWidth;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            textWidth = Mathf.Min(Text.CalcSize(label).x + 4f, rect.width - size - 6f);
            Widgets.Label(new Rect(light.xMax + 4f, rect.y, textWidth, rect.height), label);
        }
        return size + 4f + textWidth;
    }

    /// <summary>A section heading with a rule under it, advancing the caller's y. Returns the heading's rect.</summary>
    public static Rect Section(ref float curY, float x, float width, string label)
    {
        var rect = new Rect(x, curY, width, 24f);
        using (new TextBlock(GameFont.Small, TextAnchor.LowerLeft))
        {
            Widgets.Label(rect, label);
        }
        Widgets.DrawLineHorizontal(x, rect.yMax, width);
        curY = rect.yMax + 4f;
        return rect;
    }

    /// <summary>
    /// A graph of a ring's samples over the last hours, on the game's curve drawer, scaled to min..max. <paramref name="unit"/> follows the
    /// value where the pointer rests (a degree sign, a percent sign; empty for none).
    /// </summary>
    public static void HistoryGraph(Rect rect, HistoryRing ring, float min, float max, Color colour, int hours, string unit = "", List<CurveMark>? marks = null)
    {
        // The drawer draws on Repaint only, so the curve is built only then (Layout and every mouse event used to build it for nothing).
        if (ring.Count < 2 || Event.current.type != EventType.Repaint) return;
        SimpleCurve curve = GraphCurve(ring.Count, i => ring[i], (float)hours / ring.Capacity);
        var curves = new List<SimpleCurveDrawInfo> { new() { curve = curve, color = colour, valueFormat = GraphValueFormat(unit) } };
        if (marks is { Count: 0 }) marks = null;
        DrawGraphPanel(rect, hours, marks != null);
        SimpleCurveDrawer.DrawCurves(rect, curves, GraphStyle(min, max, hours), marks);
    }

    /// <summary>The most points a history graph draws per line, however many samples it holds (authored: a day's 240 averaged in twos and threes).</summary>
    public const int GraphPoints = 96;

    /// <summary>The faint grid over a graph's dark panel, at the hour labels and the value labels.</summary>
    static readonly Color GraphGrid = new(1f, 1f, 1f, 0.07f);

    /// <summary>The space the curve drawer keeps for its labels: a column on the left and a band below, and a band above for marks when
    /// it has any (the game's own figures).</summary>
    const float GraphLabelsWidth = 60f, GraphLabelsHeight = 30f, GraphMarksHeight = 15f;

    /// <summary>
    /// A line of samples (oldest first) as a curve ending at hour 0, averaged into at most <see cref="GraphPoints"/> points, each placed at
    /// its last sample's hour: a day's line shows the same shape for less than half the drawing (the owner's profiler, 11 Nov 2026: the
    /// Systems tab took about a millisecond a frame with its graphs open).
    /// </summary>
    static SimpleCurve GraphCurve(int count, System.Func<int, float> sample, float hoursPerSample)
    {
        var curve = new SimpleCurve();
        int points = Mathf.Min(GraphPoints, count);
        for (int p = 0; p < points; p++)
        {
            int from = p * count / points, to = Mathf.Max(from + 1, (p + 1) * count / points);
            float sum = 0f;
            for (int i = from; i < to; i++) sum += sample(i);
            curve.Add(new CurvePoint(-(count - to) * hoursPerSample, sum / (to - from)), sort: false);
        }
        return curve;
    }

    /// <summary>
    /// The panel behind a graph: the gauges' dark ground instead of the drawer's grey-violet one, which washed out coloured lines (owner,
    /// 11 Nov 2026), and faint upright lines at the hour labels (the drawer itself rules the value labels across). The hour labels'
    /// places follow the drawer's own rule for whole numbers: start at the first whole hour, step by the span over the label count.
    /// </summary>
    static void DrawGraphPanel(Rect rect, int hours, bool marks)
    {
        float top = marks ? GraphMarksHeight : 0f;
        var plot = new Rect(rect.x + GraphLabelsWidth, rect.y + top, rect.width - GraphLabelsWidth, rect.height - GraphLabelsHeight - top);
        if (plot.width <= 0f || plot.height <= 0f) return;
        Widgets.DrawBoxSolid(plot, GaugeBackground);
        int step = Mathf.Max(1, Mathf.CeilToInt((hours + 1f) / GraphHourLabels));
        Color before = GUI.color;
        GUI.color = GraphGrid;
        for (int hour = -hours; hour <= 0; hour += step)
        {
            Widgets.DrawLineVertical(plot.x + plot.width * (hour + hours) / hours, plot.y, plot.height);
        }
        GUI.color = before;
    }

    /// <summary>How many hour labels a graph asks the drawer for.</summary>
    const int GraphHourLabels = 4;

    /// <summary>
    /// The one look of every history graph of ours: our own dark panel (DrawGraphPanel) under the drawer's labels and value rules, a line
    /// with no dot per sample (the drawer's default puts one on every point, which turned a day of samples into a beaded rope; owner's
    /// screenshot, 11 Nov 2026), and the value where the pointer rests labelled by the hour.
    /// </summary>
    static SimpleCurveDrawerStyle GraphStyle(float min, float max, int hours) => new()
    {
        DrawBackground = false,
        DrawBackgroundLines = false,
        DrawMeasures = true,
        DrawPoints = false,
        DrawLegend = false,
        DrawCurveMousePoint = true,
        UseFixedScale = true,
        FixedScale = new Vector2(min, max),
        UseFixedSection = true,
        FixedSection = new FloatRange(-hours, 0f),
        UseAntiAliasedLines = true,
        MeasureLabelsXCount = GraphHourLabels,
        MeasureLabelsYCount = 3,
        XIntegersOnly = true,
        LabelX = "PhobosFramework_GraphHour".Translate(),
    };

    /// <summary>The drawer's value pattern (it fills {0} with the value): the value and its unit, as the language file orders them.</summary>
    static string GraphValueFormat(string unit) =>
        unit.NullOrEmpty() ? "{0}" : "PhobosFramework_GraphValue".Translate("{0}".Named("VALUE"), unit.Named("UNIT")).Resolve();

    /// <summary>One line on a graph: samples oldest first, spread evenly over the graph's hours, with a label and a colour.</summary>
    public readonly struct GraphSeries
    {
        public readonly IReadOnlyList<float> Samples;
        public readonly string Label;
        public readonly Color Colour;

        public GraphSeries(IReadOnlyList<float> samples, string label, Color colour)
        {
            Samples = samples;
            Label = label;
            Colour = colour;
        }
    }

    /// <summary>
    /// Several lines over the same last hours on one graph, on the game's curve drawer, with a small legend above it in each line's colour
    /// (Framework 0.78.0: the diagnosis report's last day). Each series is spread over the hours by its own count, so a series with fewer
    /// samples (a vehicle that was away) still ends at now. Draws only what it is given, on the frame; it records nothing.
    /// </summary>
    public static void HistoryGraphs(Rect rect, IReadOnlyList<GraphSeries> series, float min, float max, int hours, int capacity, string unit = "", List<CurveMark>? marks = null)
    {
        const float LegendHeight = 18f;
        // Everything here, legend included, is drawn on Repaint only, as the drawer does.
        if (Event.current.type != EventType.Repaint) return;
        var curves = new List<SimpleCurveDrawInfo>();
        string valueFormat = GraphValueFormat(unit);
        float hoursPerSample = (float)hours / capacity;
        float x = rect.x;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            foreach (GraphSeries line in series)
            {
                if (line.Samples.Count < 2) continue;
                IReadOnlyList<float> samples = line.Samples;
                SimpleCurve curve = GraphCurve(samples.Count, i => samples[i], hoursPerSample);
                curves.Add(new SimpleCurveDrawInfo { curve = curve, color = line.Colour, label = line.Label, valueFormat = valueFormat });
                float width = Text.CalcSize(line.Label).x + 18f;
                Widgets.DrawBoxSolid(new Rect(x, rect.y + 6f, 10f, 6f), line.Colour);
                Widgets.Label(new Rect(x + 13f, rect.y, width, LegendHeight), line.Label);
                x += width + 6f;
            }
        }
        if (curves.Count == 0) return;
        var graph = new Rect(rect.x, rect.y + LegendHeight, rect.width, rect.height - LegendHeight);
        if (marks is { Count: 0 }) marks = null;
        DrawGraphPanel(graph, hours, marks != null);
        SimpleCurveDrawer.DrawCurves(graph, curves, GraphStyle(min, max, hours), marks);
    }

    /// <summary>The most segments a sparkline draws, however many samples it holds (authored: enough for a trend, cheap every frame).</summary>
    public const int SparklineSegments = 48;

    /// <summary>
    /// A bare line of a ring's samples over its whole span, scaled to min..max, with no axes, figures or legend (Framework 0.79.0, the
    /// Dashboard's last day under each gauge): a trend seen at a glance. Long rings are thinned to <see cref="SparklineSegments"/> segments,
    /// each the average of its samples, so a spike shows as a bump rather than vanishing. Draws on repaint only; records nothing.
    /// </summary>
    public static void Sparkline(Rect rect, HistoryRing ring, float min, float max, Color colour)
    {
        if (ring.Count < 2 || Event.current.type != EventType.Repaint) return;
        Widgets.DrawBoxSolid(rect, GaugeBackground);
        int segments = Mathf.Min(SparklineSegments, ring.Count - 1);
        // min above max turns the line over, for a gauge that shows what is left.
        float span = Mathf.Abs(max - min) < 1e-6f ? 1e-6f : max - min;
        float perSegment = (float)ring.Count / (segments + 1);
        // Where the newest sample ends: a ring not yet full draws only its share of the strip, ending at the right edge (now).
        float filled = (float)ring.Count / ring.Capacity;
        float left = rect.xMax - rect.width * filled;
        Vector2 previous = default;
        for (int s = 0; s <= segments; s++)
        {
            int from = Mathf.FloorToInt(s * perSegment), to = Mathf.Max(from + 1, Mathf.FloorToInt((s + 1) * perSegment));
            float sum = 0f;
            for (int i = from; i < to && i < ring.Count; i++) sum += ring[i];
            float value = sum / Mathf.Max(1, Mathf.Min(to, ring.Count) - from);
            float x = left + (rect.xMax - left) * s / segments;
            float y = rect.yMax - 1f - (rect.height - 2f) * Mathf.Clamp01((value - min) / span);
            var point = new Vector2(x, y);
            if (s > 0) Widgets.DrawLine(previous, point, colour, 1f);
            previous = point;
        }
    }

    /// <summary>A tachometer as a bar: fill against the redline, green to the rated speed and red past it.</summary>
    public static void Tachometer(Rect rect, float rpm, float rated, float redlineShare = 1.1f, string? label = null, string? figure = null)
    {
        float redline = Mathf.Max(1f, rated * redlineShare);
        Gauge(rect, rpm / redline, rpm > rated ? Bad : Good, label, figure);
    }

    // ---- The shared kit (Framework 0.94.0): pieces first built inside one screen, moved here so every screen of ours looks alike ----

    /// <summary>A card's heading strip is this tall.</summary>
    public const float CardHeaderHeight = 24f;
    /// <summary>The space left under a card before the next.</summary>
    public const float CardGap = 8f;
    static readonly Color CardEdge = new(1f, 1f, 1f, 0.14f);

    /// <summary>
    /// Opens a card: its name on a shaded strip, and a small square in the colour of the worst thing in it (none when
    /// <paramref name="status"/> is null). Keep the y it began at for <see cref="CardEnd"/>.
    /// </summary>
    public static void CardHeader(ref float y, float x, float width, string label, Color? status)
    {
        var header = new Rect(x, y, width, CardHeaderHeight);
        Widgets.DrawLightHighlight(header);
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(x + 6f, y, width - 30f, CardHeaderHeight), label);
        if (status is Color colour) Widgets.DrawBoxSolid(new Rect(x + width - 18f, y + (CardHeaderHeight - 10f) / 2f, 10f, 10f), colour);
        y += CardHeaderHeight + 2f;
    }

    /// <summary>Closes a card: a faint outline round everything since <paramref name="top"/>, and the gap to the next.</summary>
    public static void CardEnd(float top, ref float y, float x, float width)
    {
        y += 2f;
        Color before = GUI.color;
        GUI.color = CardEdge;
        Widgets.DrawBox(new Rect(x, top, width, y - top));
        GUI.color = before;
        y += CardGap;
    }

    /// <summary>A line in the tiny font, wrapped to the width given; returns its height, so the row under it can grow to fit.</summary>
    public static float WrappedLine(float x, float y, float width, string text)
    {
        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft))
        {
            float height = Text.CalcHeight(text, width);
            Widgets.Label(new Rect(x, y, width, height), text);
            return height;
        }
    }

    /// <summary>How wide a choice's button is: square (<paramref name="size"/>) with a picture, as wide as its word without one.</summary>
    public static float ChoiceWidth(Texture2D? icon, string label, float size)
    {
        if (icon != null) return size;
        using (new TextBlock(GameFont.Tiny)) return Mathf.Max(size, Text.CalcSize(label).x + 10f);
    }

    /// <summary>
    /// One choice among several as a picture button: lit green along its foot when it is what is in force, amber when it is work already
    /// ordered, dimmed when it cannot be chosen yet. Returns whether it was clicked to choose it; <paramref name="over"/> says whether the
    /// mouse rests on it, so the caller can show its details only then.
    /// </summary>
    public static bool ChoiceButton(Rect rect, Texture2D? icon, string label, bool lit, bool refused, bool ordered, out bool over)
    {
        Widgets.DrawOptionBackground(rect, lit);
        if (lit) Widgets.DrawBoxSolid(new Rect(rect.x + 2f, rect.yMax - 4f, rect.width - 4f, 3f), Good);
        else if (ordered) Widgets.DrawBoxSolid(new Rect(rect.x + 2f, rect.yMax - 4f, rect.width - 4f, 3f), Caution);
        Color before = GUI.color;
        if (refused) GUI.color = new Color(1f, 1f, 1f, 0.35f);
        if (icon != null) GUI.DrawTexture(rect.ContractedBy(4f), icon);
        else using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleCenter)) Widgets.Label(rect, label);
        GUI.color = before;
        over = Mouse.IsOver(rect);
        return !lit && !ordered && !refused && Widgets.ButtonInvisible(rect);
    }

    /// <summary>
    /// A details strip along a window's foot: what the mouse rests on, in place of a tooltip that would cover the rows above; a grey hint
    /// when it rests on nothing.
    /// </summary>
    public static void DetailsStrip(Rect rect, string? details, string hint)
    {
        Widgets.DrawMenuSection(rect);
        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft))
        {
            Color before = GUI.color;
            GUI.color = details == null ? Color.gray : Color.white;
            Widgets.Label(rect.ContractedBy(6f), details ?? hint);
            GUI.color = before;
        }
    }
}

/// <summary>
/// A details strip that keeps what it last showed (owner, 12 Nov 2026: the fittings window's strip cut long text off, and the text went
/// the moment the mouse left a button). It shows what the mouse rests on; when the mouse rests on nothing, it keeps the last thing shown,
/// so the player can move down to it and scroll it with the mouse wheel. It changes only when the mouse rests on something else, and
/// then starts again at the top. One per window, held in a field; it saves nothing.
/// </summary>
public sealed class PhobosStickyDetails
{
    string? shown;
    Vector2 scroll;
    float lastWidth = -1f;
    float textHeight;

    /// <summary>Forgets what was shown, for a window whose rows have changed under it.</summary>
    public void Clear()
    {
        shown = null;
        scroll = Vector2.zero;
    }

    /// <param name="hovered">What the mouse rests on this frame, or null.</param>
    /// <param name="hint">The grey line shown before anything has been rested on.</param>
    public void Draw(Rect rect, string? hovered, string hint)
    {
        if (hovered != null && hovered != shown)
        {
            shown = hovered;
            scroll = Vector2.zero;
            lastWidth = -1f;
        }
        Widgets.DrawMenuSection(rect);
        Rect inner = rect.ContractedBy(6f);
        using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft))
        {
            if (shown == null)
            {
                Color before = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(inner, hint);
                GUI.color = before;
                return;
            }
            // Only the text's height is measured, and only when the text or the width changes.
            float width = inner.width - GenUI.ScrollBarWidth - 2f;
            if (width != lastWidth)
            {
                lastWidth = width;
                textHeight = Text.CalcHeight(shown, width);
            }
            if (textHeight <= inner.height)
            {
                Widgets.Label(inner, shown);
                return;
            }
            var view = new Rect(0f, 0f, width, textHeight);
            Widgets.BeginScrollView(inner, ref scroll, view);
            Widgets.Label(view, shown);
            Widgets.EndScrollView();
        }
    }
}
