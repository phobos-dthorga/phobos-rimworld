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

    /// <summary>A graph of a ring's samples over the last hours, on the game's curve drawer, scaled to min..max.</summary>
    public static void HistoryGraph(Rect rect, HistoryRing ring, float min, float max, Color colour, int hours, string valueFormat = "0.#")
    {
        if (ring.Count < 2) return;
        var curve = new SimpleCurve();
        float hoursPerSample = (float)hours / ring.Capacity;
        for (int i = 0; i < ring.Count; i++)
        {
            curve.Add(new CurvePoint(-(ring.Count - 1 - i) * hoursPerSample, ring[i]), sort: false);
        }
        var style = new SimpleCurveDrawerStyle
        {
            DrawBackground = true,
            DrawBackgroundLines = true,
            DrawMeasures = true,
            DrawLegend = false,
            DrawCurveMousePoint = true,
            UseFixedScale = true,
            FixedScale = new Vector2(min, max),
            UseFixedSection = true,
            FixedSection = new FloatRange(-hours, 0f),
            UseAntiAliasedLines = true,
            MeasureLabelsXCount = 4,
            MeasureLabelsYCount = 3,
            XIntegersOnly = true,
        };
        var curves = new List<SimpleCurveDrawInfo> { new() { curve = curve, color = colour, valueFormat = valueFormat } };
        SimpleCurveDrawer.DrawCurves(rect, curves, style);
    }

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
    public static void HistoryGraphs(Rect rect, IReadOnlyList<GraphSeries> series, float min, float max, int hours, int capacity, string valueFormat = "0.#")
    {
        const float LegendHeight = 18f;
        var curves = new List<SimpleCurveDrawInfo>();
        float x = rect.x;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            foreach (GraphSeries line in series)
            {
                if (line.Samples.Count < 2) continue;
                var curve = new SimpleCurve();
                float hoursPerSample = (float)hours / capacity;
                for (int i = 0; i < line.Samples.Count; i++)
                {
                    curve.Add(new CurvePoint(-(line.Samples.Count - 1 - i) * hoursPerSample, line.Samples[i]), sort: false);
                }
                curves.Add(new SimpleCurveDrawInfo { curve = curve, color = line.Colour, label = line.Label, valueFormat = valueFormat });
                float width = Text.CalcSize(line.Label).x + 18f;
                Widgets.DrawBoxSolid(new Rect(x, rect.y + 6f, 10f, 6f), line.Colour);
                Widgets.Label(new Rect(x + 13f, rect.y, width, LegendHeight), line.Label);
                x += width + 6f;
            }
        }
        if (curves.Count == 0) return;
        var style = new SimpleCurveDrawerStyle
        {
            DrawBackground = true,
            DrawBackgroundLines = true,
            DrawMeasures = true,
            DrawLegend = false,
            DrawCurveMousePoint = true,
            UseFixedScale = true,
            FixedScale = new Vector2(min, max),
            UseFixedSection = true,
            FixedSection = new FloatRange(-hours, 0f),
            UseAntiAliasedLines = true,
            MeasureLabelsXCount = 4,
            MeasureLabelsYCount = 3,
            XIntegersOnly = true,
        };
        SimpleCurveDrawer.DrawCurves(new Rect(rect.x, rect.y + LegendHeight, rect.width, rect.height - LegendHeight), curves, style);
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
}
