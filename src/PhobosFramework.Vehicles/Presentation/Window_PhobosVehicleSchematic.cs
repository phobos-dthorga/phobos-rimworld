using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The live systems schematic (Framework 0.363.0; docs/development/vehicle-schematic.md, the owner's answers of 28 Nov 2026: a first
/// slice, its own window, the flow sheet first, flow marks a preference that starts off): a vehicle's parts as plain symbols joined by
/// pipes in the colour of what they carry, grey when nothing flows, each part in the tone of its reading and health. Pointing at a part
/// tells its reading and lights it on the vehicle; clicking opens the Systems tab at its system. Presentation only: it draws a
/// <see cref="SchematicView"/> and delegates every click.
/// </summary>
public class Window_PhobosVehicleSchematic : Window
{
    const float TitleHeight = 30f;
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
    static readonly Color Ground = new(0.08f, 0.09f, 0.1f);
    static readonly Color TankInside = new(0.05f, 0.06f, 0.07f);
    static readonly Color Ink = new(0.04f, 0.05f, 0.06f);
    static readonly Color MarkColour = new(1f, 1f, 1f, 0.85f);
    const string GuardKey = "The vehicle systems schematic";

    readonly CompPhobosVehicleSystems comp;
    readonly PhobosVehicleSchematicDef page;
    readonly List<(double x, double y)>[] pipePoints;
    readonly double[] pipeLengths;
    SchematicView? view;
    /// <summary>The engine the per-engine parts show, from 0 (Framework 0.373.0).</summary>
    int engine;
    float phase;
    string? lit;
    bool marksNoted;

    public override Vector2 InitialSize => new(820f, 520f);

    public Window_PhobosVehicleSchematic(CompPhobosVehicleSystems comp, PhobosVehicleSchematicDef page)
    {
        this.comp = comp;
        this.page = page;
        doCloseX = true;
        draggable = true;
        resizeable = true;
        preventCameraMotion = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
        pipePoints = page.pipes.Select(p => p.points.Select(v => ((double)v.x, (double)v.y)).ToList()).ToArray();
        pipeLengths = pipePoints.Select(SchematicRule.Length).ToArray();
    }

    /// <summary>The page this vehicle's button opens, or null for none: a page for its kind, on a player's vehicle followed in full.</summary>
    public static PhobosVehicleSchematicDef? PageFor(CompPhobosVehicleSystems comp) =>
        comp.Tier == DetailTier.Full && comp.Vehicle.Faction == Faction.OfPlayer ? PhobosVehicleSchematicDef.For(comp) : null;

    static Window_PhobosVehicleSchematic? OpenFor(CompPhobosVehicleSystems comp)
    {
        foreach (Window window in Find.WindowStack.Windows)
        {
            if (window is Window_PhobosVehicleSchematic page && page.comp == comp) return page;
        }
        return null;
    }

    /// <summary>Whether this vehicle's page is open, for its button's light.</summary>
    public static bool IsOpenFor(CompPhobosVehicleSystems comp) => OpenFor(comp) != null;

    /// <summary>The Dashboard's button: opens this vehicle's page, or closes it when it is open.</summary>
    public static void Toggle(CompPhobosVehicleSystems comp)
    {
        Window_PhobosVehicleSchematic? open = OpenFor(comp);
        if (open != null)
        {
            open.Close();
            return;
        }
        if (PageFor(comp) is not { } page) return;
        Find.WindowStack.Add(new Window_PhobosVehicleSchematic(comp, page));
        Seen.Note("vehicles.schematic.opened", comp.Vehicle, page.defName);
    }

    public override void PostClose()
    {
        base.PostClose();
        if (lit != null) VehicleHighlight.Clear();
        lit = null;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { DrawInner(inRect); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    void DrawInner(Rect inRect)
    {
        if (comp.Vehicle == null || comp.Vehicle.Destroyed || !comp.Usable)
        {
            Close();
            return;
        }
        int now = Find.TickManager?.TicksGame ?? 0;
        List<string> engines = page.HasEngineTabs ? EngineFigures.Roles(comp).ToList() : new List<string>();
        if (engine >= engines.Count) engine = 0;
        if (view == null || view.Stale(comp, now) || view.Engine != engine) view = SchematicView.Build(comp, page, engine);

        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, TitleHeight),
                "PhobosVehicleSystems_SchematicTitle".Translate(comp.Vehicle.LabelShortCap.Named("VEHICLE")));
        }
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleRight))
        {
            GUI.color = view.Running ? PhobosWidgets.Good : PhobosWidgets.Off;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 24f, TitleHeight),
                (view.Running ? "PhobosVehicleSystems_SchematicRunning" : "PhobosVehicleSystems_SchematicEngineOff").Translate());
            GUI.color = Color.white;
        }

        // Framework 0.373.0: a tab for each engine where the page has parts of each engine's own and the vehicle more than one engine.
        float top = inRect.y + TitleHeight + Gap;
        if (engines.Count > 1)
        {
            top += TabHeight;
            var tabs = new List<TabRecord>();
            for (int e = 0; e < engines.Count; e++)
            {
                int chosen = e;
                tabs.Add(new TabRecord(EngineLabel(engines[e], e), () => ChooseEngine(chosen), engine == e));
            }
            TabDrawer.DrawTabs(new Rect(inRect.x, top, inRect.width, 1f), tabs);
        }
        var sheet = new Rect(inRect.x, top, inRect.width, inRect.yMax - LegendHeight - Gap - top);
        Widgets.DrawBoxSolid(sheet, Ground);
        Widgets.DrawBox(sheet);
        (double scale, double offX, double offY) = SchematicRule.Fit(page.size.x, page.size.y, sheet.width - 12f, sheet.height - 12f);
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
            for (int i = 0; i < page.pipes.Count; i++)
            {
                (bool shown, bool flowing) = view.Pipes[i];
                if (!shown) continue;
                Color colour = flowing ? MediumColour(page.pipes[i].medium) : PhobosWidgets.Off;
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
                    Seen.Note("vehicles.schematic.flow-marks", comp.Vehicle, page.defName);
                }
            }
        }

        string? hovered = null;
        bool figures = VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain;
        for (int i = 0; i < page.nodes.Count; i++)
        {
            SchematicNode node = page.nodes[i];
            SchematicView.NodeView nodeView = view.Nodes[i];
            if (!nodeView.Shown) continue;
            Rect rect = RectOf(node.rect);
            DrawNode(node, nodeView, rect, figures);
            if (Mouse.IsOver(rect))
            {
                hovered = node.key;
                Widgets.DrawBox(rect.ExpandedBy(2f), 1);
                TooltipHandler.TipRegion(rect, () => Tip(node, nodeView), node.key.GetHashCode());
                if (Widgets.ButtonInvisible(rect) && nodeView.State != null) OpenSystem(nodeView.State);
            }
        }
        Highlight(hovered);
        DrawLegend(new Rect(inRect.x, inRect.yMax - LegendHeight, inRect.width, LegendHeight));
    }

    /// <summary>An engine's tab: its part's name on the vehicle, or "engine N" where it has none.</summary>
    string EngineLabel(string role, int index) =>
        comp.PartsOf(role).FirstOrDefault()?.props.label?.CapitalizeFirst() ?? "PhobosVehicleSystems_SchematicEngineTab".Translate((index + 1).Named("NUMBER")).Resolve();

    /// <summary>An engine's tab pressed: its parts are shown from the next frame.</summary>
    void ChooseEngine(int index)
    {
        if (index == engine) return;
        engine = index;
        view = null;
        if (Seen.Wants("vehicles.schematic.engine-tab", comp.Vehicle)) Seen.Note("vehicles.schematic.engine-tab", comp.Vehicle, page.defName);
    }

    /// <summary>A part clicked: the Systems tab opens at its system, as a Dashboard gauge does, with the vehicle selected first on a map so
    /// the tab is this vehicle's.</summary>
    void OpenSystem(SystemState state)
    {
        bool onMap = comp.Vehicle.Spawned;
        if (onMap && !Find.Selector.IsSelected(comp.Vehicle))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(comp.Vehicle);
        }
        ITab_VehicleDashboard.OpenSystem(comp, state, VehicleSystemsMod.Settings.displayMode, !onMap);
    }

    /// <summary>Lights the part under the mouse on the vehicle, once, and puts the light out when the mouse leaves.</summary>
    void Highlight(string? hovered)
    {
        if (hovered == lit) return;
        lit = hovered;
        SchematicView.NodeView? over = view?.NodeOf(hovered);
        if (over?.Part != null && comp.Vehicle.Spawned) VehicleHighlight.Set(comp.Vehicle, over.Part);
        else VehicleHighlight.Clear();
    }

    static void DrawNode(SchematicNode node, SchematicView.NodeView view, Rect rect, bool figures)
    {
        Color tone = SchematicView.Colour(view.Tone);
        switch (node.shape)
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
            default:
                Widgets.DrawBoxSolid(rect, new Color(tone.r * 0.55f, tone.g * 0.55f, tone.b * 0.55f));
                Border(rect, tone);
                break;
        }

        // Its name inside a block big enough, else beneath; and its figure beneath for those who read figures.
        bool inside = node.shape == SchematicShape.Box && rect.height >= 44f;
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

    string Tip(SchematicNode node, SchematicView.NodeView view)
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
