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
        SystemState? due = comp.DueService();
        if (due?.Def.service != null)
        {
            lines.Add(comp.MissingSupply(out ThingDef? missing, out int count) && missing != null
                ? "PhobosVehicleSystems_InspectServiceNeeds".Translate(due.Def.service.label.Named("SERVICE"), count.Named("COUNT"), missing.label.Named("SUPPLY")).Resolve()
                : "PhobosVehicleSystems_InspectServiceDue".Translate(due.Def.service.label.Named("SERVICE")).Resolve());
        }
        if (mode == DisplayMode.Engineer)
        {
            lines.Add("PhobosVehicleSystems_InspectEngineer".Translate(comp.EngineLoad.ToStringPercent().Named("LOAD"),
                comp.ActiveLoadAmps().ToString("0").Named("AMPS"), comp.Ambient.ToStringTemperature("F0").Named("AIR")).Resolve());
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    public static string Format(QuantitySpec quantity, double value) => quantity.unit switch
    {
        QuantityUnit.Celsius => ((float)value).ToStringTemperature("F0"),
        QuantityUnit.Fraction => ((float)value).ToStringPercent(),
        QuantityUnit.Hours => "PhobosVehicleSystems_HoursValue".Translate(value.ToString("0").Named("HOURS")).Resolve(),
        _ => value.ToString("0.##"),
    };
}
