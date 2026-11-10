using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Phobos.Framework.Sounds;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Sound;

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
    const float TellTaleSize = 24f;
    const float SparklineHeight = 16f;
    const float CheckEngineBelow = 0.5f;
    const float CheckEngineBadBelow = 0.25f;
    const string CheckEngineIcon = "Phobos/VehicleSystems/Dash/CheckEngine";
    const string EngineKeyIcon = "Phobos/VehicleSystems/Dash/EngineKey";

    public ITab_VehicleDashboard()
    {
        size = new Vector2(460f, 368f);
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
            bool sparklines = false;
            foreach ((SystemState state, int q) in gauges)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                StageSpec? stage = state.CurrentStage(q);
                var rect = new Rect(x, y, width, GaugeHeight);
                GaugeReading reading = GaugeReading.For(state, q);
                PhobosWidgets.Gauge(rect, reading.Fill, StageColour(stage), reading.Label, figures ? reading.Figure : null,
                    stage != null ? reading.Label + ": " + stage.label + (figures ? " (" + reading.Figure + ")" : "") : reading.Label);
                if (Widgets.ButtonInvisible(rect)) OpenSystem(comp, state, mode, onWorld);
                // The last day under the gauge, for those who read figures (0.79.0, G2): a climbing coolant is seen before it is red.
                if (figures && comp.History.For(state, q) is { Count: >= 2 } ring)
                {
                    sparklines = true;
                    // Upside down for a gauge that shows what is left (tread), so the line falls as the gauge does.
                    bool remaining = !quantity.remainingLabel.NullOrEmpty();
                    float low = (float)state.min[q], high = (float)state.max[q];
                    PhobosWidgets.Sparkline(new Rect(x, y + GaugeHeight + 2f, width, SparklineHeight), ring, remaining ? high : low, remaining ? low : high, StageColour(stage));
                    TooltipHandler.TipRegion(new Rect(x, y + GaugeHeight + 2f, width, SparklineHeight), "PhobosVehicleSystems_DashSparklineTip".Translate(reading.Label.Named("QUANTITY"), QuantityHistory.Hours.Named("HOURS")));
                }
                x += width + 6f;
            }
            y += GaugeHeight + 6f + (sparklines ? SparklineHeight + 2f : 0f);
        }

        // The warning lights: every notable stage, red when it refuses or damages.
        float lightX = outer.x;
        float rowHeight = LightHeight;
        bool anyLight = false;

        // A picture and its tint where the quantity has one; the warning in words where it does not.
        void Light(Color colour, string label, Texture2D? icon, System.Action? clicked)
        {
            float needed;
            if (icon != null)
            {
                needed = TellTaleSize + 6f;
            }
            else
            {
                using (new TextBlock(GameFont.Tiny)) needed = Text.CalcSize(label).x + 24f;
            }
            if (lightX + needed > outer.xMax && lightX > outer.x)
            {
                lightX = outer.x;
                y += rowHeight;
                rowHeight = LightHeight;
            }
            if (icon != null)
            {
                var rect = new Rect(lightX, y, TellTaleSize, TellTaleSize);
                GUI.color = colour;
                GUI.DrawTexture(rect, icon);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(rect, label);
                if (clicked != null && Widgets.ButtonInvisible(rect)) clicked();
                lightX += needed;
                rowHeight = TellTaleSize;
            }
            else
            {
                lightX += PhobosWidgets.WarningLight(new Rect(lightX, y, outer.xMax - lightX, LightHeight), colour, label) + 10f;
            }
            anyLight = true;
        }

        float engineHealth = comp.Tier == DetailTier.Full ? comp.EngineHealthShare() : 1f;
        if (engineHealth < CheckEngineBelow)
        {
            Light(engineHealth < CheckEngineBadBelow ? PhobosWidgets.Bad : PhobosWidgets.Caution,
                "PhobosVehicleSystems_DashCheckEngine".Translate(engineHealth.ToStringPercent().Named("HEALTH")).Resolve(), SystemIcons.IfExists(CheckEngineIcon), null);
        }
        foreach (SystemState state in comp.States)
        {
            if (!comp.IsEnabled(state)) continue;
            for (int q = 0; q < state.values.Length; q++)
            {
                StageSpec? stage = state.CurrentStage(q);
                if (stage == null || !stage.notable) continue;
                string label = state.Def.quantities[q].label.CapitalizeFirst() + ": " + stage.label;
                SystemState lit = state;
                Light(StageColour(stage), label, SystemIcons.IfExists(state.Def.quantities[q].telltale), () => OpenSystem(comp, lit, mode, onWorld));
            }
        }
        if (anyLight) y += rowHeight + 6f;

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

        Rect NextSwitch()
        {
            if (switchX + SwitchSize > outer.xMax)
            {
                switchX = outer.x;
                y += SwitchSize + 6f;
            }
            var rect = new Rect(switchX, y, SwitchSize, SwitchSize);
            switchX += SwitchSize + 6f;
            return rect;
        }

        bool yours = !onWorld && comp.Tier == DetailTier.Full && comp.Vehicle.Faction == Faction.OfPlayer && comp.Vehicle.Spawned;
        if (yours)
        {
            // The engine key: Vehicle Framework's draft, which is its ignition.
            Rect key = NextSwitch();
            bool running = comp.Vehicle.Drafted;
            Widgets.DrawBoxSolid(key, running ? PhobosWidgets.Good : PhobosWidgets.Off);
            if (Widgets.ButtonImage(key.ContractedBy(3f), SystemIcons.For(EngineKeyIcon))) comp.SetIgnition(!running);
            Widgets.DrawBox(key);
            TooltipHandler.TipRegion(key, "PhobosVehicleSystems_DashEngine".Translate() + ": " + (running ? "PhobosVehicleSystems_DashSwitchOn" : "PhobosVehicleSystems_DashSwitchOff").Translate()
                + "\n" + "PhobosVehicleSystems_DashEngineDesc".Translate());

            // The vehicle's own working buttons: set up, pack up, overdrive and the rest, the same commands the bottom bar shows.
            foreach (CompPhobosSetUp setUp in comp.Vehicle.AllComps.OfType<CompPhobosSetUp>())
            {
                foreach (Gizmo gizmo in setUp.CompGetGizmosExtra())
                {
                    if (gizmo is not Command command || command.icon is not Texture2D picture || command.defaultLabel.NullOrEmpty() || command.defaultLabel.StartsWith("DEV")) continue;
                    Rect rect = NextSwitch();
                    bool active = command is Command_Toggle { isActive: not null } toggle && toggle.isActive();
                    Widgets.DrawBoxSolid(rect, active ? PhobosWidgets.Good : PhobosWidgets.Off);
                    if (Widgets.ButtonImage(rect.ContractedBy(3f), picture))
                    {
                        if (command.Disabled) Messages.Message(command.disabledReason, comp.Vehicle, MessageTypeDefOf.RejectInput, historical: false);
                        else command.ProcessInput(Event.current);
                    }
                    Widgets.DrawBox(rect);
                    TooltipHandler.TipRegion(rect, command.defaultLabel.CapitalizeFirst() + "\n" + command.defaultDesc + (command.Disabled ? "\n" + command.disabledReason : ""));
                }
            }
        }
        PhobosVehicleLoadoutDef? loadout = comp.Props.loadout;
        if (loadout != null)
        {
            foreach (ElectricalLoadSpec load in loadout.loads.Where(l => l.toggle && comp.LoadAvailable(l)))
            {
                Rect rect = NextSwitch();
                bool on = comp.IsOn(load);
                Widgets.DrawBoxSolid(rect, on ? (comp.LoadActive(load.name) ? PhobosWidgets.Good : PhobosWidgets.Caution) : PhobosWidgets.Off);
                if (Widgets.ButtonImage(rect.ContractedBy(3f), SystemIcons.For(load.iconPath)))
                {
                    // The same click as the switch's button: the dashboard's switches are the same switches.
                    (on ? PhobosSoundDefOf.PhobosFramework_SwitchOff : PhobosSoundDefOf.PhobosFramework_SwitchOn).PlayOneShotOnCamera();
                    comp.SetSwitch(load, !on);
                }
                Widgets.DrawBox(rect);
                TooltipHandler.TipRegion(rect, load.label.CapitalizeFirst() + ": " + (on ? "PhobosVehicleSystems_DashSwitchOn" : "PhobosVehicleSystems_DashSwitchOff").Translate()
                    + (load.description.NullOrEmpty() ? "" : "\n" + load.description));
            }
        }
        Rect service = NextSwitch();
        Widgets.DrawBoxSolid(service, comp.ServiceRequested ? PhobosWidgets.Caution : PhobosWidgets.Off);
        if (Widgets.ButtonImage(service.ContractedBy(3f), SystemIcons.Service)) comp.SetServiceRequested(!comp.ServiceRequested);
        Widgets.DrawBox(service);
        TooltipHandler.TipRegion(service, "PhobosVehicleSystems_RequestService".Translate() + "\n" + "PhobosVehicleSystems_RequestServiceDesc".Translate());
        if (comp.Tier == DetailTier.Full && comp.Vehicle.Faction == Faction.OfPlayer)
        {
            Rect fittings = NextSwitch();
            if (Widgets.ButtonImage(fittings.ContractedBy(3f), SystemIcons.Fittings)) Find.WindowStack.Add(new Dialog_PhobosVehicleFittings(comp));
            Widgets.DrawBox(fittings);
            TooltipHandler.TipRegion(fittings, "PhobosVehicleSystems_ButtonFittings".Translate() + "\n" + "PhobosVehicleSystems_ButtonFittingsDesc".Translate());
        }
    }

    /// <summary>A gauge or a warning clicked: the Systems tab opens at that system (in Plain mode there is no Systems tab to open).</summary>
    static void OpenSystem(CompPhobosVehicleSystems comp, SystemState state, DisplayMode mode, bool onWorld)
    {
        ITab_VehicleSystems.ScrollTo(comp.Vehicle.thingIDNumber, state.Def.defName);
        if (mode == DisplayMode.Plain) return;
        if (onWorld) PhobosTabs.OpenWorldTab(typeof(WITab_PhobosVehicleSystems));
        else InspectPaneUtility.OpenTab(typeof(ITab_VehicleSystems));
    }

    /// <summary>A stage's colour: green when nothing is notable, amber when it is, red when the stage refuses or damages.</summary>
    public static Color StageColour(StageSpec? stage)
    {
        if (stage == null || !stage.notable) return PhobosWidgets.Good;
        return stage.Severe ? PhobosWidgets.Bad : PhobosWidgets.Caution;
    }
}
