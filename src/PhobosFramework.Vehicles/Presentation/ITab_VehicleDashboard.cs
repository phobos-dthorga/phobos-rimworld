using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Dashboard tab: does anything need me now? Gauges for the quantities marked for it, a warning light for every notable stage,
/// what the vehicle waits for, the switches, and (Operator and Engineer) the tachometer and gear. A gauge click opens the Systems tab
/// at that system. Presentation only: every click delegates to the comp.
/// </summary>
public class ITab_VehicleDashboard : ITab
{
    const float Margin = 10f;
    const float GaugeHeight = 36f;
    const float LightHeight = 18f;
    const float SwitchSize = 32f;

    public ITab_VehicleDashboard()
    {
        size = new Vector2(460f, 300f);
        labelKey = "PhobosVehicleSystems_TabDashboard";
    }

    static CompPhobosVehicleSystems? CompOf(Thing? thing) => thing is VehiclePawn vehicle ? vehicle.GetComp<CompPhobosVehicleSystems>() : null;

    public override bool IsVisible => CompOf(SelThing)?.Usable == true;

    protected override void FillTab()
    {
        CompPhobosVehicleSystems? comp = CompOf(SelThing);
        if (comp == null || !comp.Usable) return;
        Draw(comp, new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin), onWorld: false);
    }

    /// <summary>
    /// Draws one vehicle's dashboard in <paramref name="outer"/>: for this tab on a map, and for the world map's tab on a caravan or an
    /// aircraft in flight (<paramref name="onWorld"/>), where a gauge click opens the world's Systems tab instead.
    /// </summary>
    public static void Draw(CompPhobosVehicleSystems comp, Rect outer, bool onWorld)
    {
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        bool figures = mode != DisplayMode.Plain;
        float y = outer.y;

        // What it waits for; or, for another faction's vehicle, that its systems are not followed closely.
        string waiting = comp.Tier != DetailTier.Full
            ? "PhobosVehicleSystems_DashLiteModel".Translate()
            : InspectText.WaitingLine(comp) ?? "PhobosVehicleSystems_DashReady".Translate();
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft))
        {
            float height = Mathf.Max(24f, Text.CalcHeight(waiting, outer.width));
            Widgets.Label(new Rect(outer.x, y, outer.width, height), waiting);
            y += height + 4f;
        }

        // The gauges.
        List<(SystemState state, int q)> gauges = comp.States.Where(comp.IsEnabled)
            .SelectMany(s => Enumerable.Range(0, s.values.Length).Where(q => s.Def.quantities[q].showOnDashboard).Select(q => (s, q))).ToList();
        if (gauges.Count > 0)
        {
            float width = (outer.width - 6f * (gauges.Count - 1)) / gauges.Count;
            float x = outer.x;
            foreach ((SystemState state, int q) in gauges)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                StageSpec? stage = state.CurrentStage(q);
                var rect = new Rect(x, y, width, GaugeHeight);
                GaugeReading reading = GaugeReading.For(state, q);
                PhobosWidgets.Gauge(rect, reading.Fill, StageColour(stage), reading.Label, figures ? reading.Figure : null,
                    stage != null ? reading.Label + ": " + stage.label + (figures ? " (" + reading.Figure + ")" : "") : reading.Label);
                if (Widgets.ButtonInvisible(rect))
                {
                    ITab_VehicleSystems.ScrollTo(comp.Vehicle.thingIDNumber, state.Def.defName);
                    if (mode != DisplayMode.Plain)
                    {
                        if (onWorld) PhobosTabs.OpenWorldTab(typeof(WITab_PhobosVehicleSystems));
                        else InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));
                    }
                }
                x += width + 6f;
            }
            y += GaugeHeight + 6f;
        }

        // The warning lights: every notable stage, red when it refuses or damages.
        float lightX = outer.x;
        bool anyLight = false;
        foreach (SystemState state in comp.States)
        {
            if (!comp.IsEnabled(state)) continue;
            for (int q = 0; q < state.values.Length; q++)
            {
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null || !stage.notable) continue;
                string label = state.Def.quantities[q].label.CapitalizeFirst() + ": " + stage.label;
                float needed;
                using (new TextBlock(GameFont.Tiny)) needed = Text.CalcSize(label).x + 24f;
                if (lightX + needed > outer.xMax && lightX > outer.x)
                {
                    lightX = outer.x;
                    y += LightHeight;
                }
                lightX += PhobosWidgets.WarningLight(new Rect(lightX, y, outer.xMax - lightX, LightHeight), StageColour(stage), label) + 10f;
                anyLight = true;
            }
        }
        if (anyLight) y += LightHeight + 6f;

        // The engine, for those who read figures.
        Powertrain? train = comp.Powertrain;
        if (figures && train != null && VehicleSystemsMod.Settings.drivetrainMode != DrivetrainMode.Off && comp.EngineRunning)
        {
            DriveResult r = train.Result;
            if (train.Turbine != null)
            {
                PhobosWidgets.Gauge(new Rect(outer.x, y, outer.width, GaugeHeight), (float)(train.TurbineNow.SpeedPercent / 110.0),
                    train.TurbineNow.OverTop > 0 ? PhobosWidgets.Bad : train.TurbineNow.OverContinuous > 0 ? PhobosWidgets.Caution : PhobosWidgets.Good,
                    "PhobosVehicleSystems_SysTurbine".Translate(),
                    "PhobosVehicleSystems_InspectTurbine".Translate(train.TurbineNow.SpeedPercent.ToString("0").Named("SPOOL"),
                        train.ShownTurbineTemperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve());
            }
            else
            {
                string figure = r.Gear > 0
                    ? "PhobosVehicleSystems_InspectGear".Translate(r.Gear.Named("GEAR"), r.Rpm.ToString("N0").Named("RPM")).Resolve()
                    : "PhobosVehicleSystems_InspectRpm".Translate(r.Rpm.ToString("N0").Named("RPM")).Resolve();
                PhobosWidgets.Tachometer(new Rect(outer.x, y, outer.width, GaugeHeight), (float)r.Rpm, (float)train.Spec.RatedRpm,
                    (float)DrivetrainModel.RevLimitShare, "PhobosVehicleSystems_SysDrivetrain".Translate(), figure);
            }
            y += GaugeHeight + 6f;
        }

        // The switches and the service button.
        float switchX = outer.x;
        PhobosVehicleLoadoutDef? loadout = comp.Props.loadout;
        if (loadout != null)
        {
            foreach (ElectricalLoadSpec load in loadout.loads.Where(l => l.toggle && comp.LoadAvailable(l)))
            {
                var rect = new Rect(switchX, y, SwitchSize, SwitchSize);
                bool on = comp.IsOn(load);
                Widgets.DrawBoxSolid(rect, on ? (comp.LoadActive(load.name) ? PhobosWidgets.Good : PhobosWidgets.Caution) : PhobosWidgets.Off);
                if (Widgets.ButtonImage(rect.ContractedBy(3f), SystemIcons.For(load.iconPath))) comp.SetSwitch(load, !on);
                Widgets.DrawBox(rect);
                TooltipHandler.TipRegion(rect, load.label.CapitalizeFirst() + ": " + (on ? "PhobosVehicleSystems_DashSwitchOn" : "PhobosVehicleSystems_DashSwitchOff").Translate()
                    + (load.description.NullOrEmpty() ? "" : "\n" + load.description));
                switchX += SwitchSize + 6f;
            }
        }
        var service = new Rect(switchX, y, SwitchSize, SwitchSize);
        Widgets.DrawBoxSolid(service, comp.ServiceRequested ? PhobosWidgets.Caution : PhobosWidgets.Off);
        if (Widgets.ButtonImage(service.ContractedBy(3f), SystemIcons.Service)) comp.SetServiceRequested(!comp.ServiceRequested);
        Widgets.DrawBox(service);
        TooltipHandler.TipRegion(service, "PhobosVehicleSystems_RequestService".Translate() + "\n" + "PhobosVehicleSystems_RequestServiceDesc".Translate());
    }

    /// <summary>A stage's colour: green when nothing is notable, amber when it is, red when the stage refuses or damages.</summary>
    public static Color StageColour(StageSpec? stage)
    {
        if (stage == null || !stage.notable) return PhobosWidgets.Good;
        foreach (VehicleSystemEffect effect in stage.effects)
        {
            if (effect is Effect_RefuseDraft or Effect_RefuseMove or Effect_DamagePart or Effect_Explosion or Effect_ForceUndraft) return PhobosWidgets.Bad;
        }
        return PhobosWidgets.Caution;
    }
}
