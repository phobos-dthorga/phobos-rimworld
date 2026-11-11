using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.Interface;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Systems tab: what exactly is going on, and why. Each system is a card; each reading one row with its picture, bar, stage word in
/// its colour and a small line of its last day, which opens on a click to its full graph and what is moving it (Framework 0.93.0; owner,
/// 11 Nov 2026: with every graph and rate line open at once nothing could be told apart). A system's parts are a strip of small bars;
/// hovering one lights it up on the vehicle. A filter leaves only what needs attention. Engineer mode adds the hidden readings and the
/// figures. Hidden in Plain mode. Presentation only.
/// </summary>
public class ITab_VehicleSystems : ITab
{
    public sealed class State
    {
        public Vector2 Scroll;
        public string? PendingSection;
        /// <summary>The readings opened to their graph, by system and quantity.</summary>
        public readonly HashSet<string> Open = new();
        /// <summary>The height of everything drawn last frame: rows open and close, so the height is measured, not worked out twice.</summary>
        public float DrawnHeight;
    }

    const float Margin = 10f;
    const float Row = 24f;
    const float Graph = 64f;
    const float FilterHeight = 24f;
    const float IconSize = 18f;
    const float ArrowSize = 12f;
    const float ChipBar = 34f;
    const float ChipHeight = 20f;
    static readonly Color DetailColour = new(0.72f, 0.72f, 0.72f);

    /// <summary>Only what needs attention: kept while the game runs, for every vehicle.</summary>
    static bool attentionOnly;
    static readonly TabState<State> states = new();

    public ITab_VehicleSystems()
    {
        size = new Vector2(560f, 450f);
        labelKey = "PhobosVehicleSystems_TabSystems";
    }

    static CompPhobosVehicleSystems? CompOf(Thing? thing) => thing is VehiclePawn vehicle ? vehicle.GetComp<CompPhobosVehicleSystems>() : null;

    public override bool IsVisible => VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain && CompOf(SelThing)?.Usable == true;

    /// <summary>Asks the tab to open scrolled to a system (a Dashboard gauge was clicked).</summary>
    public static void ScrollTo(int thingId, string systemDefName) => states.Get(thingId).PendingSection = systemDefName;

    /// <summary>How many vehicles' tab state is held, for the dev action.</summary>
    public static int StateCount => states.Count;

    protected override void CloseTab()
    {
        base.CloseTab();
        ClearState();
    }

    /// <summary>Forgets every vehicle's scroll and pending section and stops lighting a part (this tab or the world's closed).</summary>
    public static void ClearState()
    {
        states.Clear();
        VehicleHighlight.Clear();
    }

    protected override void FillTab()
    {
        CompPhobosVehicleSystems? comp = CompOf(SelThing);
        if (comp == null || !comp.Usable) return;
        Draw(comp, new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin), onWorld: false);
    }

    /// <summary>
    /// Draws one vehicle's systems in <paramref name="outer"/>, scrolling: for this tab on a map, and for the world map's tab on a caravan
    /// or an aircraft in flight (<paramref name="onWorld"/>), where nothing is lit up on the vehicle (it is not on a map).
    /// </summary>
    public static void Draw(CompPhobosVehicleSystems comp, Rect outer, bool onWorld)
    {
        // Guarded: a throw is logged once and the tab rests, where the game would log it every frame (UiGuard).
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(outer); return; }
        try { DrawInner(comp, outer, onWorld); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    internal const string GuardKey = "The vehicle Systems tab";

    static void DrawInner(CompPhobosVehicleSystems comp, Rect outer, bool onWorld)
    {
        using Phobos.Framework.Debug.PhobosTiming.Scope timing = Phobos.Framework.Debug.PhobosTiming.Measure("systems tab");
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        bool engineer = mode == DisplayMode.Engineer;
        State state = states.Get(comp.Vehicle.thingIDNumber);

        var filter = new Rect(outer.x, outer.y, 240f, FilterHeight);
        Widgets.CheckboxLabeled(filter, "PhobosVehicleSystems_SysFilterAttention".Translate(), ref attentionOnly);
        TooltipHandler.TipRegion(filter, "PhobosVehicleSystems_SysFilterAttentionDesc".Translate());
        outer.yMin += FilterHeight + 4f;

        float viewWidth = outer.width - 16f;
        var view = new Rect(0f, 0f, viewWidth, state.DrawnHeight > 0f ? state.DrawnHeight : outer.height);
        Widgets.BeginScrollView(outer, ref state.Scroll, view);
        float y = 0f;
        bool anything = false;
        VehicleComponent? hovered = null;
        foreach (SystemState system in comp.States)
        {
            if (!comp.IsEnabled(system)) continue;
            // What this card shows: its readings (the hidden ones for engineers), and its parts; under the filter, only what is wrong.
            rowScratch.Clear();
            for (int q = 0; q < system.values.Length; q++)
            {
                QuantitySpec quantity = system.Def.quantities[q];
                if (quantity.hidden && !engineer) continue;
                if (attentionOnly && system.CurrentStage(q) is not { notable: true }) continue;
                rowScratch.Add(q);
            }
            partScratch.Clear();
            foreach (string role in system.Def.roles)
            {
                if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
                foreach (string key in keys)
                {
                    VehicleComponent? part = comp.Vehicle.statHandler?.GetComponent(key);
                    if (part == null || (attentionOnly && part.HealthPercent >= WholeShare)) continue;
                    partScratch.Add((role, part));
                }
            }
            if (rowScratch.Count == 0 && partScratch.Count == 0) continue;
            anything = true;
            if (state.PendingSection == system.Def.defName)
            {
                state.Scroll.y = y;
                state.PendingSection = null;
            }
            float top = y;
            PhobosWidgets.CardHeader(ref y, 0f, viewWidth, system.Def.label.CapitalizeFirst(), SystemColour(system));
            foreach (int q in rowScratch) DrawReading(comp, system, q, state, ref y, viewWidth, engineer);
            if (partScratch.Count > 0) DrawParts(ref y, viewWidth, ref hovered);
            PhobosWidgets.CardEnd(top, ref y, 0f, viewWidth);
        }
        Powertrain? train = comp.Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off)
        {
            DriveResult r = train.Result;
            if (train.Ground && (!attentionOnly || train.Cap < 0.99))
            {
                anything = true;
                float top = y;
                PhobosWidgets.CardHeader(ref y, 0f, viewWidth, "PhobosVehicleSystems_SysDrivetrain".Translate(), train.Cap < 0.99 ? PhobosWidgets.Caution : PhobosWidgets.Good);
                string gear = r.Gear > 0 ? "PhobosVehicleSystems_SysGear".Translate(r.Gear.Named("GEAR")).Resolve() + ", " : "";
                Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), gear + "PhobosVehicleSystems_SysRpm".Translate(r.Rpm.ToString("N0").Named("RPM")).Resolve()
                    + ", " + "PhobosVehicleSystems_SysLoad".Translate(((float)r.Load).ToStringPercent().Named("LOAD")).Resolve());
                y += Row;
                if (engineer)
                {
                    Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_InspectPower".Translate(((float)r.Load).ToStringPercent().Named("LOAD"),
                        r.EnginePowerKw.ToString("0").Named("ENGINE"), r.FuelPowerKw.ToString("0").Named("FUEL"), r.HeatKw.ToString("0").Named("HEAT")).Resolve());
                    y += Row;
                    Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysMass".Translate(train.MassKg().ToString("N0").Named("MASS")).Resolve());
                    y += Row;
                }
                if (train.Cap < 0.99)
                {
                    string limit = (r.Limit == DriveLimit.Traction ? "PhobosVehicleSystems_LimitTraction" : "PhobosVehicleSystems_LimitPower").Translate();
                    Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysCap".Translate(((float)train.Cap).ToStringPercent().Named("CAP"), limit.Named("LIMIT")).Resolve());
                    y += Row;
                }
                PhobosWidgets.CardEnd(top, ref y, 0f, viewWidth);
            }
            if (train.Turbine != null && !attentionOnly)
            {
                anything = true;
                float top = y;
                PhobosWidgets.CardHeader(ref y, 0f, viewWidth, "PhobosVehicleSystems_SysTurbine".Translate(), null);
                TurbineState t = train.TurbineNow;
                Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_InspectTurbine".Translate(t.SpeedPercent.ToString("0").Named("SPOOL"),
                    train.ShownTurbineTemperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve()
                    + (train.PhaseSpec != null && !train.PhaseSpec.label.NullOrEmpty() ? " " + "PhobosVehicleSystems_InspectPhase".Translate(train.PhaseSpec.label.Named("PHASE")).Resolve() : ""));
                y += Row;
                (int working, int engines) = train.Engines();
                Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysRating".Translate(train.RatingLabel.Named("RATING")).Resolve() + ", "
                    + "PhobosVehicleSystems_SysEnginesWorking".Translate(working.Named("WORKING"), engines.Named("ENGINES")).Resolve());
                y += Row;
                if (engineer && train.BleedShare > 0)
                {
                    Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysBleed".Translate(((float)train.BleedShare).ToStringPercent("0.#").Named("SHARE")).Resolve());
                    y += Row;
                }
                if (engineer && train.JudgesHover)
                {
                    (bool dry, bool wet, double margin) = train.HoverCheck();
                    Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysHoverMargin".Translate(((float)margin).ToStringPercent().Named("MARGIN"),
                        (dry ? "PhobosVehicleSystems_HoverDry" : wet ? "PhobosVehicleSystems_HoverWet" : "PhobosVehicleSystems_HoverNone").Translate().Named("HOW")).Resolve());
                    y += Row;
                }
                PhobosWidgets.CardEnd(top, ref y, 0f, viewWidth);
            }
        }
        if (!anything)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
            {
                Widgets.Label(new Rect(6f, y, viewWidth - 12f, Row), "PhobosVehicleSystems_SysNothingNeedsAttention".Translate().Colorize(PhobosWidgets.Good));
            }
            y += Row;
        }
        Widgets.EndScrollView();
        state.DrawnHeight = y + 4f;
        if (!onWorld) VehicleHighlight.Set(comp.Vehicle, hovered);
    }

    // ---- The pieces ----

    /// <summary>Scratch lists for one card, reused every frame.</summary>
    static readonly List<int> rowScratch = new();
    static readonly List<(string role, VehicleComponent part)> partScratch = new();

    /// <summary>A part at or above this share of its health is whole (the filter leaves it out).</summary>
    const float WholeShare = 0.999f;

    /// <summary>The worst stage among a system's readings, as a colour.</summary>
    static Color SystemColour(SystemState system)
    {
        Color colour = PhobosWidgets.Good;
        for (int q = 0; q < system.values.Length; q++)
        {
            if (system.CurrentStage(q) is not { notable: true } stage) continue;
            if (stage.Severe) return PhobosWidgets.Bad;
            colour = PhobosWidgets.Caution;
        }
        return colour;
    }

    /// <summary>
    /// One reading as one row: an arrow where it can open, its picture, name, bar with the figure in it (or the figure alone, for a count
    /// or a switch), its stage in the stage's colour, and a small line of its last day. A click opens its full graph and what moves it.
    /// </summary>
    static void DrawReading(CompPhobosVehicleSystems comp, SystemState system, int q, State state, ref float y, float width, bool engineer)
    {
        QuantitySpec quantity = system.Def.quantities[q];
        StageSpec? stage = system.CurrentStage(q);
        GaugeReading reading = GaugeReading.For(system, q);
        Color colour = ITab_VehicleDashboard.StageColour(stage);
        HistoryRing? ring = comp.History.For(system, q);
        bool hasGraph = ring != null && ring.Count >= 2;
        bool hasFlows = false;
        foreach (FlowSample sample in system.samples)
        {
            if (sample.Quantity == q) { hasFlows = true; break; }
        }
        bool canOpen = hasGraph || hasFlows;
        string key = system.Def.defName + "/" + quantity.name;
        bool open = canOpen && state.Open.Contains(key);

        var row = new Rect(0f, y, width, Row);
        Widgets.DrawHighlightIfMouseover(row);
        float x = 4f;
        if (canOpen) GUI.DrawTexture(new Rect(x, y + (Row - ArrowSize) / 2f, ArrowSize, ArrowSize), open ? TexButton.Collapse : TexButton.Reveal);
        x += ArrowSize + 4f;
        if (SystemIcons.IfExistsCached(quantity.telltale) is { } icon)
        {
            Color before = GUI.color;
            GUI.color = stage is { notable: true } ? colour : PhobosWidgets.Off;
            GUI.DrawTexture(new Rect(x, y + (Row - IconSize) / 2f, IconSize, IconSize), icon);
            GUI.color = before;
        }
        x += IconSize + 4f;
        float nameEnd = width * 0.38f, barEnd = width * 0.66f, stageEnd = width * 0.86f;
        using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(x, y, nameEnd - x - 4f, Row), reading.Label.Truncate(nameEnd - x - 4f, Truncated(nameCache, width)));
        }
        var bar = new Rect(nameEnd, y + 4f, barEnd - nameEnd - 6f, Row - 8f);
        if (quantity.unit is QuantityUnit.Fraction or QuantityUnit.Celsius) PhobosWidgets.Gauge(bar, reading.Fill, colour, figure: reading.Figure);
        else using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft)) Widgets.Label(new Rect(nameEnd, y, barEnd - nameEnd - 6f, Row), reading.Figure);
        if (stage != null)
        {
            using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
            {
                Widgets.Label(new Rect(barEnd, y, stageEnd - barEnd - 4f, Row), stage.label.Truncate(stageEnd - barEnd - 4f, Truncated(stageCache, width)).Colorize(colour));
            }
        }
        if (hasGraph && !open)
        {
            bool remaining = !quantity.remainingLabel.NullOrEmpty();
            float top = GraphTop(quantity, ring!);
            PhobosWidgets.Sparkline(new Rect(stageEnd, y + 4f, width - stageEnd - 4f, Row - 8f), ring!, remaining ? top : quantity.min, remaining ? quantity.min : top, colour);
        }
        if (Mouse.IsOver(row))
        {
            // Built only for the row under the mouse, not for every row every frame.
            string tip = stage != null
                ? "PhobosVehicleSystems_InspectFigureStage".Translate(reading.Label.Named("QUANTITY"), reading.Figure.Named("VALUE"), stage.label.Named("STAGE")).Resolve()
                : "PhobosVehicleSystems_InspectFigure".Translate(reading.Label.Named("QUANTITY"), reading.Figure.Named("VALUE")).Resolve();
            if (canOpen) tip += "\n\n" + "PhobosVehicleSystems_SysRowTip".Translate().Resolve();
            TooltipHandler.TipRegion(row, tip);
        }
        if (canOpen && Widgets.ButtonInvisible(row))
        {
            if (!state.Open.Remove(key)) state.Open.Add(key);
        }
        y += Row;
        if (!open) return;

        if (hasGraph)
        {
            // The marks are read only when the graph is painted, as the graph itself is.
            List<CurveMark>? marks = Event.current.type == EventType.Repaint ? VehicleEventText.Marks(comp, system.Def.defName, quantity.name, QuantityHistory.Hours) : null;
            PhobosWidgets.HistoryGraph(new Rect(ArrowSize + 8f, y, width - ArrowSize - 12f, Graph), ring!, quantity.min, GraphTop(quantity, ring!), colour, QuantityHistory.Hours, marks: marks);
            y += Graph + 2f;
        }
        DrawFlows(system, q, quantity, ref y, width, engineer);
        y += 4f;
    }

    /// <summary>Shortened names remembered, so a row's text is measured once and not every frame; forgotten when the tab's width changes
    /// (the map's tab and the world's differ).</summary>
    static readonly Dictionary<string, string> nameCache = new();
    static readonly Dictionary<string, string> stageCache = new();
    static float cachedWidth;

    static Dictionary<string, string> Truncated(Dictionary<string, string> cache, float width)
    {
        if (width != cachedWidth)
        {
            nameCache.Clear();
            stageCache.Clear();
            cachedWidth = width;
        }
        return cache;
    }

    /// <summary>A graph's top: a count that can run to a huge figure (hours since service, to 100,000) is scaled to what was recorded.</summary>
    static float GraphTop(QuantitySpec quantity, HistoryRing ring) =>
        quantity.unit is QuantityUnit.Hours or QuantityUnit.Plain ? (float)GraphScaleRule.Top(RingMax(ring), quantity.min, quantity.max, GraphFloorSpan) : quantity.max;

    /// <summary>
    /// What is moving a reading, in plain words: where it is heading ("cooling toward 18C"), and whether what flows in and out leaves it
    /// rising, falling or steady. Engineer mode adds the figures.
    /// </summary>
    static void DrawFlows(SystemState system, int q, QuantitySpec quantity, ref float y, float width, bool engineer)
    {
        double value = system.values[q], span = System.Math.Max(1e-6, quantity.max - quantity.min);
        double net = 0;
        bool anyRate = false;
        foreach (FlowSample sample in system.samples)
        {
            if (sample.Quantity != q) continue;
            if (sample.Kind != FlowKind.Toward)
            {
                net += sample.Kind == FlowKind.From ? -sample.Rate : sample.Rate;
                anyRate = true;
                continue;
            }
            bool celsius = quantity.unit == QuantityUnit.Celsius;
            string target = InspectText.Format(quantity, sample.Target);
            double gap = sample.Target - value;
            string line = System.Math.Abs(gap) < span * HoldingShare ? "PhobosVehicleSystems_FlowHolding".Translate(target.Named("TARGET")).Resolve()
                : gap < 0 ? (celsius ? "PhobosVehicleSystems_FlowCooling".Translate(target.Named("TARGET")).Resolve() : "PhobosVehicleSystems_FlowFallingToward".Translate(target.Named("TARGET")).Resolve())
                : celsius ? "PhobosVehicleSystems_FlowWarming".Translate(target.Named("TARGET")).Resolve() : "PhobosVehicleSystems_FlowRisingToward".Translate(target.Named("TARGET")).Resolve();
            if (engineer) line = "PhobosVehicleSystems_SysFlowDetail".Translate(line.Named("LINE"), "PhobosVehicleSystems_SysFlowGapRate".Translate(sample.Rate.ToString("0.##").Named("RATE")).Named("FIGURE")).Resolve();
            FlowLine(line, ref y, width);
        }
        if (!anyRate) return;
        string trend = System.Math.Abs(net) < span * SteadyShare ? "PhobosVehicleSystems_FlowSteady".Translate().Resolve()
            : net > 0 ? "PhobosVehicleSystems_FlowRising".Translate().Resolve() : "PhobosVehicleSystems_FlowFalling".Translate().Resolve();
        if (engineer) trend = "PhobosVehicleSystems_SysFlowDetail".Translate(trend.Named("LINE"), "PhobosVehicleSystems_SysFlowRate".Translate(RateText(quantity, net).Named("RATE")).Named("FIGURE")).Resolve();
        FlowLine(trend, ref y, width);
    }

    /// <summary>Within this share of a reading's range of where it is heading, it is holding there (authored).</summary>
    const double HoldingShare = 0.005;
    /// <summary>Below this share of a reading's range an hour, it is steady (authored).</summary>
    const double SteadyShare = 0.0005;

    static void FlowLine(string text, ref float y, float width)
    {
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(ArrowSize + IconSize + 12f, y, width - ArrowSize - IconSize - 16f, Row - 6f), text.CapitalizeFirst().Colorize(DetailColour));
        }
        y += Row - 6f;
    }

    /// <summary>A rate an hour in the reading's own unit: a share as a percentage, a temperature as degrees.</summary>
    static string RateText(QuantitySpec quantity, double rate) => quantity.unit switch
    {
        QuantityUnit.Fraction => (rate >= 0 ? "+" : "") + ((float)rate).ToStringPercent("0.#"),
        QuantityUnit.Celsius or QuantityUnit.CelsiusOffset => ((float)rate).ToStringTemperatureOffset("F1"),
        _ => rate.ToString("+0.##;-0.##"),
    };

    /// <summary>
    /// A system's parts as a strip of small bars, wrapping as they fill the width (the scratch list holds them): name, then a bar of its
    /// health. Hovering one lights it up on the vehicle.
    /// </summary>
    static void DrawParts(ref float y, float width, ref VehicleComponent? hovered)
    {
        float x = 6f;
        y += 2f;
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleLeft))
        {
            foreach ((string role, VehicleComponent part) in partScratch)
            {
                // No part icon and no Vehicle Framework colour helper: both pass Unity types, which a 1.7 build cannot use
                // through Vehicle Framework's 1.6 assembly. The health colour is the core's own.
                string label = PartLabel(role, part.props.label);
                float chip = Mathf.Min(width - 12f, Text.CalcSize(label).x + ChipBar + 14f);
                if (x + chip > width - 6f && x > 6f)
                {
                    x = 6f;
                    y += ChipHeight + 2f;
                }
                var rect = new Rect(x, y, chip, ChipHeight);
                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                    hovered = part;
                }
                float health = part.HealthPercent;
                Widgets.Label(new Rect(x + 2f, y, chip - ChipBar - 8f, ChipHeight), label.Truncate(chip - ChipBar - 8f));
                PhobosWidgets.Gauge(new Rect(rect.xMax - ChipBar - 2f, y + 5f, ChipBar, ChipHeight - 10f), health, PhobosWidgets.HealthColour(health));
                TooltipHandler.TipRegion(rect, "PhobosVehicleSystems_SysPartTip".Translate(label.Named("PART"), health.ToStringPercent().Named("HEALTH")));
                x += chip + 6f;
            }
        }
        y += ChipHeight + 2f;
    }

    /// <summary>"Wheels: front left wheel", or only the part's name when it says the same as its role ("Battery", not "Battery:
    /// Battery").</summary>
    public static string PartLabel(string role, string part) =>
        string.Equals(role.Trim(), part.Trim(), System.StringComparison.OrdinalIgnoreCase)
            ? part.CapitalizeFirst()
            : "PhobosVehicleSystems_SysPart".Translate(role.Named("ROLE"), part.Named("PART")).Resolve();

    /// <summary>The least span a scaled graph shows, so a reading near nothing is not blown up into noise (authored: a day of hours).</summary>
    const float GraphFloorSpan = 24f;

    static float RingMax(HistoryRing ring)
    {
        float max = float.MinValue;
        for (int i = 0; i < ring.Count; i++) max = Mathf.Max(max, ring[i]);
        return max;
    }
}
