using System;
using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A value for RimHUD's pawn display (Jaxe's RimHUD, optional): which vehicle a person is aboard and in what role, with the fuel,
/// the cab and what the vehicle waits for in the tooltip; a click selects the vehicle on its map. RimHUD finds this class through
/// the CustomValueDef in Mods/VehiclesRimHUD (loaded only with both Vehicle Framework and RimHUD) and calls GetParameters by
/// reflection, so nothing here references RimHUD. Its wiki fixes the return type exactly (read 8 Nov 2026). Presentation only.
/// </summary>
public static class RimHudVehicleValue
{
    public static (string? label, string? value, Func<string?>? tooltip, Action? onHover, Action? onClick) GetParameters(Pawn pawn)
    {
        string label = "PhobosVehicleSystems_HudVehicle".Translate();
        VehiclePawn? vehicle = pawn?.GetVehicle();
        if (vehicle == null || pawn!.ParentHolder is not VehicleRoleHandler handler)
        {
            return (label, "PhobosVehicleSystems_HudNone".Translate(), null, null, null);
        }
        string role = handler.role?.label ?? "";
        string value = "PhobosVehicleSystems_HudAboard".Translate(vehicle.LabelShort.Named("VEHICLE"), role.Named("ROLE")).Resolve();
        Action? onClick = vehicle.Spawned ? () => CameraJumper.TryJumpAndSelect(vehicle) : null;
        return (label, value, () => Tooltip(vehicle), null, onClick);
    }

    static string? Tooltip(VehiclePawn vehicle)
    {
        var lines = new List<string>();
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel != null && fuel.FuelCapacity > 0)
        {
            lines.Add("PhobosVehicleSystems_HudFuel".Translate((fuel.Fuel / fuel.FuelCapacity).ToStringPercent().Named("FUEL")).Resolve());
        }
        if (CabClimate.TryGet(vehicle, out float cab))
        {
            lines.Add("PhobosVehicleSystems_HudCab".Translate(cab.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve());
        }
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (comp != null && comp.Usable && InspectText.WaitingLine(comp) is { } waiting) lines.Add(waiting);
        if (vehicle.Spawned) lines.Add("PhobosVehicleSystems_HudClick".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }
}
