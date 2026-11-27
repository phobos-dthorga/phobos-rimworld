using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One vehicle's systems schematic drawn into a rectangle (Framework 0.363.0 as the window's own drawing; a panel of its own since 0.378.0,
/// the owner's card of 28 Nov 2026, so the window and the vehicle's Schematic tab draw the same page): the engine tabs, the sheet of parts
/// and pipes, the cutaways, and the legend. Pointing at a part tells its reading and lights it on the vehicle; a click opens the Systems
/// tab at its system. Presentation only: it draws a <see cref="SchematicView"/> and delegates every click.
/// </summary>
public sealed class SchematicPanel
{
    /// <summary>Room for the engine tabs (Framework 0.373.0), which the game's tab drawer hangs above the sheet.</summary>
    const float TabHeight = 32f;
    const float LegendHeight = 26f;
    const float Gap = 8f;
    const float PipeWidth = 3f;
    const float BorderWidth = 2f;
    /// <summary>Flow marks: one every this many drawing units, moving this many a second, this big on screen (authored).</summary>
    const float MarkSpacing = 18f;
    const float MarkSpeed = 22f;
    const float MarkSize = 3f;
    /// <summary>How long, in real seconds, a part named by a warning lamp stays outlined (Framework 0.378.0; authored).</summary>
    const float FocusSeconds = 8f;
    static readonly Color Ground = new(0.08f, 0.09f, 0.1f);
    static readonly Color TankInside = new(0.05f, 0.06f, 0.07f);
    static readonly Color Ink = new(0.04f, 0.05f, 0.06f);
    static readonly Color MarkColour = new(1f, 1f, 1f, 0.85f);

    public readonly CompPhobosVehicleSystems Comp;
    public readonly PhobosVehicleSchematicDef Page;
    readonly List<(double x, double y)>[] pipePoints;
    readonly double[] pipeLengths;
    SchematicView? view;
    /// <summary>The engine the per-engine parts show, from 0 (Framework 0.373.0).</summary>
    int engine;
    float phase;
    string? lit;
    /// <summary>The part a warning lamp named, outlined until <see cref="focusUntil"/> (real time) or until the mouse rests on a part.</summary>
    string? focus;
    float focusUntil;
    bool marksNoted;
    bool cutawayNoted;

    public SchematicPanel(CompPhobosVehicleSystems comp, PhobosVehicleSchematicDef page)
    {
        Comp = comp;
        Page = page;
        pipePoints = page.pipes.Select(p => p.points.Select(v => ((double)v.x, (double)v.y)).ToList()).ToArray();
        pipeLengths = pipePoints.Select(SchematicRule.Length).ToArray();
    }

    /// <summary>Whether the engine runs, as last drawn, for the title's word.</summary>
    public bool Running => view?.Running ?? Comp.EngineRunning;

    /// <summary>Outlines a part for a few seconds and lights it on the vehicle: the part a warning lamp named (Framework 0.378.0).</summary>
    public void Focus(string key)
    {
        focus = key;
        focusUntil = Time.realtimeSinceStartup + FocusSeconds;
        lit = null;
        view = null;
    }

    /// <summary>Puts out the light on the vehicle, when the panel is closed or hidden.</summary>
    public void ClearHighlight()
    {
        if (lit != null || focus != null) VehicleHighlight.Clear();
        lit = null;
        focus = null;
    }

    /// <summary>Draws the page into <paramref name="rect"/>; <paramref name="compact"/> leaves out the legend, for the inspect tab.</summary>
    public void Draw(Rect rect, bool compact)
    {
        int now = Find.TickManager?.TicksGame ?? 0;
        List<string> engines = Page.HasEngineTabs ? EngineFigures.Roles(Comp).ToList() : new List<string>();
        if (engine >= engines.Count) engine = 0;
        if (view == null || view.Stale(Comp, now) || view.Engine != engine) view = SchematicView.Build(Comp, Page, engine);
        if (focus != null && Time.realtimeSinceStartup > focusUntil) focus = null;

        // Framework 0.373.0: a tab for each engine where the page has parts of each engine's own and the vehicle more than one engine.
        float top = rect.y;
        if (engines.Count > 1)
        {
            top += TabHeight;
            var tabs = new List<TabRecord>();
            for (int e = 0; e < engines.Count; e++)
            {
                int chosen = e;
                tabs.Add(new TabRecord(EngineLabel(engines[e], e), () => ChooseEngine(chosen), engine == e));
            }
            TabDrawer.DrawTabs(new Rect(rect.x, top, rect.width, 1f), tabs);
        }
        float bottom = compact ? rect.yMax : rect.yMax - LegendHeight - Gap;
        var sheet = new Rect(rect.x, top, rect.width, bottom - top);
        Widgets.DrawBoxSolid(sheet, Ground);
        Widgets.DrawBox(sheet);
        (double scale, double offX, double offY) = SchematicRule.Fit(Page.size.x, Page.size.y, sheet.width - 12f, sheet.height - 12f);
        float s = (float)scale;
        var origin = new Vector2(sheet.x + 6f + (float)offX, sheet.y + 6f + (float)offY);
        Vector2 At(double x, double y) => new(origin.x + (float)x * s, origin.y + (float)y * s);
        Rect RectOf(Rect r) => new(origin.x + r.x * s, origin.y + r.y * s, r.width * s, r.height * s);

        bool marks = VehicleSystemsMod.Settings.schematicFlowMarks;
        // Only once a frame, on the repaint, and never while the game stands still: the machine is not running then either.
        if (marks && Event.current.type == EventType.Repaint && Find.TickManager is { Paused: false }) phase += RealTime.realDeltaTime * MarkSpeed;

        // Pipes first, so the parts sit on their ends.
        if (Event.current.type == EventType.Repaint)
        {
            for (int i = 0; i < Page.pipes.Count; i++)
            {
                (bool shown, bool flowing) = view.Pipes[i];
                if (!shown) continue;
                Color colour = flowing ? MediumColour(Page.pipes[i].medium) : PhobosWidgets.Off;
                List<(double x, double y)> points = pipePoints[i];
                for (int p = 1; p < points.Count; p++) Widgets.DrawLine(At(points[p - 1].x, points[p - 1].y), At(points[p].x, points[p].y), colour, PipeWidth);
                if (!marks || !flowing) continue;
                foreach (double d in SchematicRule.MarkDistances(pipeLengths[i], MarkSpacing, phase))
                {
                    (double x, double y) point = SchematicRule.PointAlong(points, d);
                    Vector2 at = At(point.x, point.y);
                    Widgets.DrawBoxSolid(new Rect(at.x - MarkSize / 2f, at.y - MarkSize / 2f, MarkSize, MarkSize), MarkColour);
                }
                if (!marksNoted)
                {
                    marksNoted = true;
                    Seen.Note("vehicles.schematic.flow-marks", Comp.Vehicle, Page.defName);
                }
            }
        }

        string? hovered = null;
        bool figures = VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain;
        for (int i = 0; i < Page.nodes.Count; i++)
        {
            SchematicNode node = Page.nodes[i];
            SchematicView.NodeView nodeView = view.Nodes[i];
            if (!nodeView.Shown) continue;
            Rect at = RectOf(node.rect);
            if (node.layers.Count > 0) DrawCutaway(node, at);
            DrawNode(node, nodeView, at, figures);
            if (node.key == focus)
            {
                // Framework 0.378.0: the part a warning lamp named, outlined in steady amber (nothing on this page flashes).
                GUI.color = PhobosWidgets.Caution;
                Widgets.DrawBox(at.ExpandedBy(5f), 2);
                GUI.color = Color.white;
            }
            if (Mouse.IsOver(at))
            {
                hovered = node.key;
                Widgets.DrawBox(at.ExpandedBy(2f), 1);
                TooltipHandler.TipRegion(at, () => Tip(node, nodeView), node.key.GetHashCode());
                if (Widgets.ButtonInvisible(at) && nodeView.State != null) OpenSystem(nodeView.State);
            }
        }
        if (hovered != null) focus = null;
        Highlight(hovered ?? focus);
        if (!compact) DrawLegend(new Rect(rect.x, rect.yMax - LegendHeight, rect.width, LegendHeight));
    }

    /// <summary>An engine's tab: its part's name on the vehicle, or "engine N" where it has none.</summary>
    string EngineLabel(string role, int index) =>
        Comp.PartsOf(role).FirstOrDefault()?.props.label?.CapitalizeFirst() ?? "PhobosVehicleSystems_SchematicEngineTab".Translate((index + 1).Named("NUMBER")).Resolve();

    /// <summary>An engine's tab pressed: its parts are shown from the next frame.</summary>
    void ChooseEngine(int index)
    {
        if (index == engine) return;
        engine = index;
        view = null;
        if (Seen.Wants("vehicles.schematic.engine-tab", Comp.Vehicle)) Seen.Note("vehicles.schematic.engine-tab", Comp.Vehicle, Page.defName);
    }

    /// <summary>A part clicked: the Systems tab opens at its system, as a Dashboard gauge does, with the vehicle selected first on a map so
    /// the tab is this vehicle's.</summary>
    void OpenSystem(SystemState state)
    {
        bool onMap = Comp.Vehicle.Spawned;
        if (onMap && !Find.Selector.IsSelected(Comp.Vehicle))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(Comp.Vehicle);
        }
        ITab_VehicleDashboard.OpenSystem(Comp, state, VehicleSystemsMod.Settings.displayMode, !onMap);
    }

    /// <summary>Lights the part under the mouse (or the one a lamp named) on the vehicle, once, and puts the light out when it changes.</summary>
    void Highlight(string? key)
    {
        if (key == lit) return;
        lit = key;
        SchematicView.NodeView? over = view?.NodeOf(key);
        if (over?.Part != null && Comp.Vehicle.Spawned) VehicleHighlight.Set(Comp.Vehicle, over.Part);
        else VehicleHighlight.Clear();
    }

    /// <summary>
    /// A node's cutaway (Framework 0.374.0; docs/development/vehicle-schematic.md, SC2): each white region layer tinted by its reading, fitted
    /// to the node without stretching, then the outline as drawn. Textures are found once and kept; one that is missing draws nothing.
    /// </summary>
    void DrawCutaway(SchematicNode node, Rect rect)
    {
        if (Event.current.type != EventType.Repaint || view == null) return;
        foreach (SchematicLayer layer in node.layers)
        {
            if (Texture(layer.texPath) is not { } tex) continue;
            GUI.color = LayerColour(node, layer);
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit);
        }
        GUI.color = Color.white;
        if (!node.outline.NullOrEmpty() && Texture(node.outline!) is { } edge) GUI.DrawTexture(rect, edge, ScaleMode.ScaleToFit);
        if (!cutawayNoted)
        {
            cutawayNoted = true;
            if (Seen.Wants("vehicles.schematic.cutaway", Comp.Vehicle)) Seen.Note("vehicles.schematic.cutaway", Comp.Vehicle, Page.defName);
        }
    }

    /// <summary>A layer's tint, as its <see cref="SchematicTint"/> says.</summary>
    Color LayerColour(SchematicNode owner, SchematicLayer layer)
    {
        if (layer.tint == SchematicTint.Fixed) return layer.colour;
        SchematicView.NodeView? from = view!.NodeOf(layer.node.NullOrEmpty() ? owner.key : layer.node);
        if (from is not { Shown: true }) return PhobosWidgets.Off;
        if (layer.tint == SchematicTint.Flow)
        {
            return SchematicRule.Flows(view.Running, from.Work, from.Stopped) ? MediumColour(layer.medium) : PhobosWidgets.Off;
        }
        return SchematicView.Colour(from.Tone);
    }

    static readonly Dictionary<string, Texture2D?> Textures = new();

    static Texture2D? Texture(string path)
    {
        if (!Textures.TryGetValue(path, out Texture2D? tex)) Textures[path] = tex = ContentFinder<Texture2D>.Get(path, false);
        return tex;
    }

    static void DrawNode(SchematicNode node, SchematicView.NodeView view, Rect rect, bool figures)
    {
        Color tone = SchematicView.Colour(view.Tone);
        // Framework 0.374.0: a node with a cutaway is drawn by it; only its name and figure go on it.
        bool cutaway = node.layers.Count > 0;
        switch (cutaway ? (SchematicShape?)null : node.shape)
        {
            case SchematicShape.Tank:
                Widgets.DrawBoxSolid(rect, TankInside);
                if (!float.IsNaN(view.Level))
                {
                    float fill = (rect.height - 4f) * Mathf.Clamp01(view.Level);
                    Widgets.DrawBoxSolid(new Rect(rect.x + 2f, rect.yMax - 2f - fill, rect.width - 4f, fill), MediumColour(node.holds));
                }
                Border(rect, tone);
                break;
            case SchematicShape.Pump:
                Widgets.DrawBoxSolid(rect, tone);
                if (Event.current.type == EventType.Repaint)
                {
                    Rect inner = rect.ContractedBy(rect.width * 0.25f);
                    Widgets.DrawLine(new Vector2(inner.x, inner.y), new Vector2(inner.xMax, inner.center.y), Ink, 2f);
                    Widgets.DrawLine(new Vector2(inner.xMax, inner.center.y), new Vector2(inner.x, inner.yMax), Ink, 2f);
                    Widgets.DrawLine(new Vector2(inner.x, inner.yMax), new Vector2(inner.x, inner.y), Ink, 2f);
                }
                Border(rect, Ink);
                break;
            case SchematicShape.Radiator:
            case SchematicShape.Filter:
                Widgets.DrawBoxSolid(rect, TankInside);
                if (Event.current.type == EventType.Repaint)
                {
                    bool fins = node.shape == SchematicShape.Radiator;
                    int count = Mathf.Max(3, Mathf.RoundToInt((fins ? rect.height : rect.width) / 12f));
                    for (int k = 1; k < count; k++)
                    {
                        float f = k / (float)count;
                        if (fins) Widgets.DrawLine(new Vector2(rect.x + 3f, rect.y + rect.height * f), new Vector2(rect.xMax - 3f, rect.y + rect.height * f), tone, 2f);
                        else Widgets.DrawLine(new Vector2(rect.x + rect.width * f, rect.y + 3f), new Vector2(rect.x + rect.width * f, rect.yMax - 3f), tone, 2f);
                    }
                }
                Border(rect, tone);
                break;
            case null:
                break;
            default:
                Widgets.DrawBoxSolid(rect, new Color(tone.r * 0.55f, tone.g * 0.55f, tone.b * 0.55f));
                Border(rect, tone);
                break;
        }

        // Its name inside a block big enough, else beneath; and its figure beneath for those who read figures.
        bool inside = !cutaway && node.shape == SchematicShape.Box && rect.height >= 44f;
        if (cutaway)
        {
            // A cutaway's name and figure sit in its top left corner, clear of the parts beneath it.
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft))
            {
                Widgets.Label(new Rect(rect.x + 4f, rect.y + 2f, rect.width / 3f, 18f), node.label.CapitalizeFirst());
                if (figures && !view.Figure.NullOrEmpty())
                {
                    GUI.color = new Color(0.75f, 0.78f, 0.8f);
                    Widgets.Label(new Rect(rect.x + 4f, rect.y + 16f, rect.width / 3f, 18f), view.Figure);
                    GUI.color = Color.white;
                }
            }
            return;
        }
        using (new TextBlock(GameFont.Tiny, inside ? TextAnchor.MiddleCenter : TextAnchor.UpperCenter))
        {
            float wide = Mathf.Max(rect.width + 40f, 90f);
            var under = new Rect(rect.center.x - wide / 2f, rect.yMax + 1f, wide, 18f);
            Widgets.Label(inside ? rect : under, node.label.CapitalizeFirst());
            if (figures && !view.Figure.NullOrEmpty())
            {
                GUI.color = new Color(0.75f, 0.78f, 0.8f);
                Widgets.Label(inside ? new Rect(rect.x, rect.center.y + 4f, rect.width, 18f) : new Rect(under.x, under.yMax - 4f, under.width, 18f), view.Figure);
                GUI.color = Color.white;
            }
        }
    }

    static void Border(Rect rect, Color colour)
    {
        Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width, BorderWidth), colour);
        Widgets.DrawBoxSolid(new Rect(rect.x, rect.yMax - BorderWidth, rect.width, BorderWidth), colour);
        Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, BorderWidth, rect.height), colour);
        Widgets.DrawBoxSolid(new Rect(rect.xMax - BorderWidth, rect.y, BorderWidth, rect.height), colour);
    }

    static string Tip(SchematicNode node, SchematicView.NodeView view)
    {
        var lines = new List<string> { node.label.CapitalizeFirst() };
        if (!view.Reading.NullOrEmpty()) lines.Add(view.Reading + (view.Stage.NullOrEmpty() ? "" : " (" + view.Stage + ")"));
        if (!float.IsNaN(view.Health))
        {
            bool figures = VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain;
            lines.Add("PhobosVehicleSystems_SchematicPartHealth".Translate((figures ? PhobosWidgets.HealthPercent(view.Health) : HealthWords.Of(view.Health)).Named("HEALTH")));
        }
        if (view.State != null) lines.Add("\n" + "PhobosVehicleSystems_SchematicClick".Translate(view.State.Def.label.Named("SYSTEM")));
        return string.Join("\n", lines);
    }

    static void DrawLegend(Rect rect)
    {
        float x = rect.x;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            foreach (SchematicMedium medium in System.Enum.GetValues(typeof(SchematicMedium)))
            {
                x = Swatch(rect, x, MediumColour(medium), ("PhobosVehicleSystems_SchematicMedium" + medium).Translate());
            }
            Swatch(rect, x, PhobosWidgets.Off, "PhobosVehicleSystems_SchematicMediumNone".Translate());
        }
    }

    static float Swatch(Rect rect, float x, Color colour, string label)
    {
        Widgets.DrawBoxSolid(new Rect(x, rect.center.y - 2f, 18f, 4f), colour);
        float width = Text.CalcSize(label).x;
        Widgets.Label(new Rect(x + 22f, rect.y, width + 4f, rect.height), label);
        return x + 22f + width + 16f;
    }

    /// <summary>What a pipe carries, as its colour (authored: told apart on the dark sheet, and kept off the tones' green, amber and red, so
    /// a pipe's colour says what it carries and a part's says how it is: the mock-up's finding F3).</summary>
    public static Color MediumColour(SchematicMedium medium) => medium switch
    {
        SchematicMedium.Fuel => new Color(0.80f, 0.72f, 0.50f),
        SchematicMedium.Oil => new Color(0.58f, 0.38f, 0.22f),
        SchematicMedium.Coolant => new Color(0.29f, 0.62f, 0.95f),
        SchematicMedium.Air => new Color(0.62f, 0.88f, 0.96f),
        SchematicMedium.Exhaust => new Color(0.66f, 0.62f, 0.66f),
        SchematicMedium.Electric => new Color(0.71f, 0.58f, 1.00f),
        _ => PhobosWidgets.Off,
    };
}
