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
/// The Systems tab: what exactly is going on, and why. Every quantity with its figure, stage and bar; in Engineer mode the last
/// day's graph and the flows per hour; each role's parts with their health (hovering one lights it up on the vehicle, as Vehicle
/// Framework's Health tab does); the drivetrain and turbine figures. Hidden in Plain mode. Presentation only.
/// </summary>
public class ITab_VehicleSystems : ITab
{
    public sealed class State
    {
        public Vector2 Scroll;
        public string? PendingSection;
    }

    const float Margin = 10f;
    const float Row = 22f;
    const float Graph = 64f;
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
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        bool engineer = mode == DisplayMode.Engineer;
        State state = states.Get(comp.Vehicle.thingIDNumber);
        float viewWidth = outer.width - 16f;
        float height = ContentHeight(comp, engineer, viewWidth);
        var view = new Rect(0f, 0f, viewWidth, height);
        Widgets.BeginScrollView(outer, ref state.Scroll, view);
        float y = 0f;
        VehicleComponent? hovered = null;
        foreach (SystemState system in comp.States)
        {
            if (!comp.IsEnabled(system) || !HasRows(comp, system, engineer)) continue;
            if (state.PendingSection == system.Def.defName)
            {
                state.Scroll.y = y;
                state.PendingSection = null;
            }
            PhobosWidgets.Section(ref y, 0f, viewWidth, system.Def.label.CapitalizeFirst());
            for (int q = 0; q < system.values.Length; q++)
            {
                QuantitySpec quantity = system.Def.quantities[q];
                if (quantity.hidden && !engineer) continue;
                StageSpec? stage = system.CurrentStage(q);
                GaugeReading reading = GaugeReading.For(system, q);
                string text = stage != null
                    ? "PhobosVehicleSystems_InspectFigureStage".Translate(reading.Label.Named("QUANTITY"), reading.Figure.Named("VALUE"), stage.label.Named("STAGE")).Resolve()
                    : "PhobosVehicleSystems_InspectFigure".Translate(reading.Label.Named("QUANTITY"), reading.Figure.Named("VALUE")).Resolve();
                Widgets.Label(new Rect(0f, y, viewWidth * 0.55f, Row), text);
                if (quantity.unit is QuantityUnit.Fraction or QuantityUnit.Celsius)
                {
                    PhobosWidgets.Gauge(new Rect(viewWidth * 0.58f, y + 3f, viewWidth * 0.42f, Row - 6f), reading.Fill, ITab_VehicleDashboard.StageColour(stage));
                }
                y += Row;
                if (engineer)
                {
                    HistoryRing? ring = comp.History.For(system, q);
                    if (ring != null && ring.Count >= 2)
                    {
                        // The marks are read only when the graph is painted, as the graph itself is.
                        List<CurveMark>? marks = Event.current.type == EventType.Repaint ? VehicleEventText.Marks(comp, system.Def.defName, quantity.name, QuantityHistory.Hours) : null;
                        // A count that can run to a huge figure (hours since service, to 100,000) is scaled to what was recorded, or its
                        // line lies flat on the floor (owner's screenshot, 11 Nov 2026).
                        float top = quantity.unit is QuantityUnit.Hours or QuantityUnit.Plain ? (float)GraphScaleRule.Top(RingMax(ring), quantity.min, quantity.max, GraphFloorSpan) : quantity.max;
                        PhobosWidgets.HistoryGraph(new Rect(0f, y, viewWidth, Graph), ring, quantity.min, top, ITab_VehicleDashboard.StageColour(stage), QuantityHistory.Hours, marks: marks);
                        y += Graph + 4f;
                    }
                    foreach (FlowSample sample in system.samples)
                    {
                        if (sample.Quantity != q) continue;
                        string flow = sample.Kind == FlowKind.Toward
                            ? "PhobosVehicleSystems_SysFlowToward".Translate(InspectText.Format(quantity, sample.Target).Named("TARGET"), sample.Rate.ToString("0.##").Named("RATE")).Resolve()
                            : "PhobosVehicleSystems_SysFlowRate".Translate(((sample.Kind == FlowKind.From ? -sample.Rate : sample.Rate)).ToString("+0.##;-0.##").Named("RATE")).Resolve();
                        using (new TextBlock(GameFont.Tiny))
                        {
                            Widgets.Label(new Rect(12f, y, viewWidth - 12f, Row - 4f), flow);
                        }
                        y += Row - 4f;
                    }
                }
            }
            // The parts the system reads, by role.
            foreach (string role in system.Def.roles)
            {
                if (!comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) continue;
                foreach (string key in keys)
                {
                    VehicleComponent? part = comp.Vehicle.statHandler?.GetComponent(key);
                    if (part == null) continue;
                    var row = new Rect(0f, y, viewWidth, Row);
                    if (Mouse.IsOver(row))
                    {
                        Widgets.DrawHighlight(row);
                        hovered = part;
                    }
                    // No part icon and no Vehicle Framework colour helper: both pass Unity types, which a 1.7 build cannot use
                    // through Vehicle Framework's 1.6 assembly. The health colour is the core's own.
                    Widgets.Label(new Rect(8f, y, viewWidth * 0.55f, Row), PartLabel(role, part.props.label));
                    using (new TextBlock(TextAnchor.MiddleRight))
                    {
                        Widgets.Label(new Rect(viewWidth * 0.58f, y, viewWidth * 0.42f, Row),
                            part.HealthPercent.ToStringPercent().Colorize(PhobosWidgets.HealthColour(part.HealthPercent)));
                    }
                    y += Row;
                }
            }
            y += 6f;
        }
        Powertrain? train = comp.Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off)
        {
            DriveResult r = train.Result;
            if (train.Ground)
            {
                PhobosWidgets.Section(ref y, 0f, viewWidth, "PhobosVehicleSystems_SysDrivetrain".Translate());
                string gear = r.Gear > 0 ? "PhobosVehicleSystems_SysGear".Translate(r.Gear.Named("GEAR")).Resolve() + ", " : "";
                Widgets.Label(new Rect(0f, y, viewWidth, Row), gear + "PhobosVehicleSystems_SysRpm".Translate(r.Rpm.ToString("N0").Named("RPM")).Resolve()
                    + ", " + "PhobosVehicleSystems_SysLoad".Translate(((float)r.Load).ToStringPercent().Named("LOAD")).Resolve());
                y += Row;
                if (engineer)
                {
                    Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_InspectPower".Translate(((float)r.Load).ToStringPercent().Named("LOAD"),
                        r.EnginePowerKw.ToString("0").Named("ENGINE"), r.FuelPowerKw.ToString("0").Named("FUEL"), r.HeatKw.ToString("0").Named("HEAT")).Resolve());
                    y += Row;
                    Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_SysMass".Translate(train.MassKg().ToString("N0").Named("MASS")).Resolve());
                    y += Row;
                }
                if (train.Cap < 0.99)
                {
                    string limit = (r.Limit == DriveLimit.Traction ? "PhobosVehicleSystems_LimitTraction" : "PhobosVehicleSystems_LimitPower").Translate();
                    Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_SysCap".Translate(((float)train.Cap).ToStringPercent().Named("CAP"), limit.Named("LIMIT")).Resolve());
                    y += Row;
                }
                y += 6f;
            }
            if (train.Turbine != null)
            {
                PhobosWidgets.Section(ref y, 0f, viewWidth, "PhobosVehicleSystems_SysTurbine".Translate());
                TurbineState t = train.TurbineNow;
                Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_InspectTurbine".Translate(t.SpeedPercent.ToString("0").Named("SPOOL"),
                    train.ShownTurbineTemperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve()
                    + (train.PhaseSpec != null && !train.PhaseSpec.label.NullOrEmpty() ? " " + "PhobosVehicleSystems_InspectPhase".Translate(train.PhaseSpec.label.Named("PHASE")).Resolve() : ""));
                y += Row;
                (int working, int engines) = train.Engines();
                Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_SysRating".Translate(train.RatingLabel.Named("RATING")).Resolve() + ", "
                    + "PhobosVehicleSystems_SysEnginesWorking".Translate(working.Named("WORKING"), engines.Named("ENGINES")).Resolve());
                y += Row;
                if (engineer && train.BleedShare > 0)
                {
                    Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_SysBleed".Translate(((float)train.BleedShare).ToStringPercent("0.#").Named("SHARE")).Resolve());
                    y += Row;
                }
                if (engineer && train.JudgesHover)
                {
                    (bool dry, bool wet, double margin) = train.HoverCheck();
                    Widgets.Label(new Rect(0f, y, viewWidth, Row), "PhobosVehicleSystems_SysHoverMargin".Translate(((float)margin).ToStringPercent().Named("MARGIN"),
                        (dry ? "PhobosVehicleSystems_HoverDry" : wet ? "PhobosVehicleSystems_HoverWet" : "PhobosVehicleSystems_HoverNone").Translate().Named("HOW")).Resolve());
                    y += Row;
                }
            }
        }
        Widgets.EndScrollView();
        if (!onWorld) VehicleHighlight.Set(comp.Vehicle, hovered);
    }

    /// <summary>"Wheels: front left wheel", or only the part's name when it says the same as its role ("Battery", not "Battery:
    /// Battery").</summary>
    public static string PartLabel(string role, string part) =>
        string.Equals(role.Trim(), part.Trim(), System.StringComparison.OrdinalIgnoreCase)
            ? part.CapitalizeFirst()
            : "PhobosVehicleSystems_SysPart".Translate(role.Named("ROLE"), part.Named("PART")).Resolve();

    /// <summary>Whether a system has anything to show in this mode: a quantity that is not hidden, or a bound part. A system with
    /// neither (one switched off by a setting, such as fuel in the cold) is left out rather than shown as an empty heading.</summary>
    /// <summary>The least span a scaled graph shows, so a reading near nothing is not blown up into noise (authored: a day of hours).</summary>
    const float GraphFloorSpan = 24f;

    static float RingMax(HistoryRing ring)
    {
        float max = float.MinValue;
        for (int i = 0; i < ring.Count; i++) max = Mathf.Max(max, ring[i]);
        return max;
    }

    static bool HasRows(CompPhobosVehicleSystems comp, SystemState system, bool engineer)
    {
        for (int q = 0; q < system.values.Length; q++)
        {
            if (engineer || !system.Def.quantities[q].hidden) return true;
        }
        foreach (string role in system.Def.roles)
        {
            if (comp.Props.roleKeys.TryGetValue(role, out List<string>? keys) && keys.Count > 0) return true;
        }
        return false;
    }

    /// <summary>The scroll view's height, laid out the same way as FillTab draws it.</summary>
    static float ContentHeight(CompPhobosVehicleSystems comp, bool engineer, float width)
    {
        float y = 0f;
        foreach (SystemState system in comp.States)
        {
            if (!comp.IsEnabled(system) || !HasRows(comp, system, engineer)) continue;
            y += 28f;
            for (int q = 0; q < system.values.Length; q++)
            {
                if (system.Def.quantities[q].hidden && !engineer) continue;
                y += Row;
                if (engineer)
                {
                    HistoryRing? ring = comp.History.For(system, q);
                    if (ring != null && ring.Count >= 2) y += Graph + 4f;
                    foreach (FlowSample sample in system.samples)
                    {
                        if (sample.Quantity == q) y += Row - 4f;
                    }
                }
            }
            foreach (string role in system.Def.roles)
            {
                if (comp.Props.roleKeys.TryGetValue(role, out List<string>? keys)) y += keys.Count * Row;
            }
            y += 6f;
        }
        Powertrain? train = comp.Powertrain;
        if (train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off)
        {
            if (train.Ground) y += 28f + Row + (engineer ? 2 * Row : 0f) + (train.Cap < 0.99 ? Row : 0f) + 6f;
            if (train.Turbine != null) y += 28f + 2 * Row + (engineer && train.BleedShare > 0 ? Row : 0f) + (engineer && train.JudgesHover ? Row : 0f);
        }
        return y + Row;
    }
}
