using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicle's inspect lines, by display mode (presentation only). Plain names only the stages worth a look and a service that is
/// due; Operator adds each quantity's figure; Engineer adds the engine's load, the electrical draw and the air temperature.
/// </summary>
public static class InspectText
{
    public static string? For(CompPhobosVehicleSystems comp)
    {
        DisplayMode mode = VehicleSystemsMod.Settings.displayMode;
        var parts = new List<string>();
        foreach (SystemState state in comp.States)
        {
            if (VehicleSystemsMod.Settings.disabledSystems.Contains(state.Def.defName)) continue;
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                StageSpec? stage = state.CurrentStage(q);
                if (mode == DisplayMode.Plain || quantity.hidden)
                {
                    if (stage != null && stage.notable) parts.Add("PhobosVehicleSystems_InspectStage".Translate(quantity.label.Named("QUANTITY"), stage.label.Named("STAGE")));
                    continue;
                }
                string figure = Format(quantity, state.values[q]);
                parts.Add(stage != null && (stage.notable || mode == DisplayMode.Engineer)
                    ? "PhobosVehicleSystems_InspectFigureStage".Translate(quantity.label.Named("QUANTITY"), figure.Named("VALUE"), stage.label.Named("STAGE")).Resolve()
                    : "PhobosVehicleSystems_InspectFigure".Translate(quantity.label.Named("QUANTITY"), figure.Named("VALUE")).Resolve());
            }
        }
        var lines = new List<string>();
        if (parts.Count > 0) lines.Add(string.Join(", ", parts).CapitalizeFirst());
        string? waiting = WaitingLine(comp);
        if (waiting != null) lines.Add(waiting);
        string? engine = EngineLine(comp, mode);
        if (engine != null) lines.Add(engine);
        if (mode == DisplayMode.Engineer)
        {
            lines.Add("PhobosVehicleSystems_InspectEngineer".Translate(comp.EngineLoad.ToStringPercent().Named("LOAD"),
                comp.ActiveLoadAmps().ToString("0").Named("AMPS"), comp.Ambient.ToStringTemperature("F0").Named("AIR")).Resolve());
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    /// <summary>The one thing the vehicle waits for: a mechanic, a supply, water, or the reason it will not move; null when nothing.</summary>
    public static string? WaitingLine(CompPhobosVehicleSystems comp)
    {
        if (comp.MoveRefusal != null) return comp.MoveRefusal;
        SystemState? due = comp.DueService();
        if (due?.Def.service == null) return null;
        return comp.MissingSupply(out ThingDef? missing, out int count) && missing != null
            ? "PhobosVehicleSystems_InspectServiceNeeds".Translate(due.Def.service.label.Named("SERVICE"), count.Named("COUNT"), missing.label.Named("SUPPLY")).Resolve()
            : !comp.WaterReady(out string? waterReason) && waterReason != null
                ? "PhobosVehicleSystems_InspectServiceWater".Translate(due.Def.service.label.Named("SERVICE"), waterReason.Named("REASON")).Resolve()
                : "PhobosVehicleSystems_InspectServiceDue".Translate(due.Def.service.label.Named("SERVICE")).Resolve();
    }

    /// <summary>The engine's line: Plain only when it labours or the wheels spin; Operator gear and rpm (or spool and turbine
    /// temperature); Engineer adds load and kilowatts.</summary>
    static string? EngineLine(CompPhobosVehicleSystems comp, DisplayMode mode)
    {
        Powertrain? train = comp.Powertrain;
        if (train == null || VehicleSystemsMod.Settings.drivetrainMode == DrivetrainMode.Off || !comp.EngineRunning) return null;
        Phobos.Framework.Vehicles.Rules.DriveResult r = train.Result;
        bool spinning = r.Limit == Phobos.Framework.Vehicles.Rules.DriveLimit.Traction && train.Full;
        if (mode == DisplayMode.Plain)
        {
            if (spinning) return "PhobosVehicleSystems_InspectWheelsSpinning".Translate();
            if (r.Load > 0.9) return "PhobosVehicleSystems_InspectLabouring".Translate();
            return null;
        }
        string line;
        if (train.Turbine != null)
        {
            line = "PhobosVehicleSystems_InspectTurbine".Translate(train.TurbineNow.SpeedPercent.ToString("0").Named("SPOOL"),
                ((float)train.TurbineNow.Temperature).ToStringTemperature("F0").Named("TEMPERATURE")).Resolve();
            if (train.IsAircraft && train.PhaseSpec != null && !train.PhaseSpec.label.NullOrEmpty()) line += " " + "PhobosVehicleSystems_InspectPhase".Translate(train.PhaseSpec.label.Named("PHASE")).Resolve();
        }
        else if (r.Gear > 0)
        {
            line = "PhobosVehicleSystems_InspectGear".Translate(r.Gear.Named("GEAR"), r.Rpm.ToString("N0").Named("RPM")).Resolve();
        }
        else
        {
            line = "PhobosVehicleSystems_InspectRpm".Translate(r.Rpm.ToString("N0").Named("RPM")).Resolve();
        }
        if (spinning) line += " " + "PhobosVehicleSystems_InspectWheelsSpinning".Translate().Resolve();
        if (mode == DisplayMode.Engineer)
        {
            line += " " + "PhobosVehicleSystems_InspectPower".Translate(((float)r.Load).ToStringPercent().Named("LOAD"), r.EnginePowerKw.ToString("0").Named("ENGINE"),
                r.FuelPowerKw.ToString("0").Named("FUEL"), r.HeatKw.ToString("0").Named("HEAT")).Resolve();
            if (train.Cap < 0.99) line += " " + "PhobosVehicleSystems_InspectCap".Translate(((float)train.Cap).ToStringPercent().Named("CAP")).Resolve();
        }
        return line;
    }

    public static string Format(QuantitySpec quantity, double value) => quantity.unit switch
    {
        QuantityUnit.Celsius => ((float)value).ToStringTemperature("F0"),
        QuantityUnit.Fraction => ((float)value).ToStringPercent(),
        QuantityUnit.Hours => "PhobosVehicleSystems_HoursValue".Translate(value.ToString("0").Named("HOURS")).Resolve(),
        _ => value.ToString("0.##"),
    };
}

/// <summary>What a tab's gauge shows for a quantity: its label, how full the bar is and the figure; for a quantity with a
/// remainingLabel, what is left rather than what is used.</summary>
public readonly struct GaugeReading
{
    public readonly string Label;
    public readonly float Fill;
    public readonly string Figure;

    GaugeReading(string label, float fill, string figure)
    {
        Label = label;
        Fill = fill;
        Figure = figure;
    }

    public static GaugeReading For(SystemState state, int q)
    {
        QuantitySpec quantity = state.Def.quantities[q];
        double min = state.min[q], max = state.max[q], value = state.values[q];
        double share = (value - min) / System.Math.Max(1e-6, max - min);
        if (!quantity.remainingLabel.NullOrEmpty())
        {
            return new GaugeReading(quantity.remainingLabel!.CapitalizeFirst(), (float)(1 - share), InspectText.Format(quantity, max - (value - min)));
        }
        return new GaugeReading(quantity.label.CapitalizeFirst(), (float)share, InspectText.Format(quantity, value));
    }
}
