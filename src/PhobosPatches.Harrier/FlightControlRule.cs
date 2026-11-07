using System.Collections.Generic;
using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Flight controls that matter. Vehicle Framework's FlightControl stat is the worst of the parts that carry it (avionics, hydraulics,
/// wiring on the Harrier, from D'thorga Patches 0.11.0), but nothing in Vehicle Framework 1.6 or the fixed-wing framework reads it
/// (read 7 Nov 2026): it was a figure on the stat card and no more. This rule gives it one effect: a jet whose flight controls are
/// below the floor cannot take off until they are repaired. Only vehicles with a part that carries the stat are affected. The floor
/// is authored; the refusal names it.
/// </summary>
public static class FlightControlRule
{
    /// <summary>Flight control below this grounds the aircraft (authored: a control part at roughly a third of its health).</summary>
    public const float MinimumFlightControl = 0.35f;

    static readonly Dictionary<VehicleDef, bool> carries = new();

    /// <summary>Whether this vehicle has any part whose damage lowers flight control.</summary>
    public static bool Applies(VehicleDef def)
    {
        if (carries.TryGetValue(def, out bool result)) return result;
        result = false;
        if (def.components != null)
        {
            foreach (VehicleComponentProperties component in def.components)
            {
                if (component.categories != null && component.categories.Contains(VehicleStatDefOf.FlightControl)) { result = true; break; }
            }
        }
        carries[def] = result;
        return result;
    }

    public static bool Grounded(VehiclePawn vehicle) => Applies(vehicle.VehicleDef) && vehicle.GetStatValue(VehicleStatDefOf.FlightControl) < MinimumFlightControl;
}

/// <summary>The launch button (and so the sortie button) refuses with the reason while the flight controls are too damaged.</summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanLaunchWithCargoCapacity))]
public static class FlightControlLaunchPatch
{
    public static void Postfix(CompVehicleLauncher __instance, ref bool __result, ref string disableReason)
    {
        if (!__result || __instance.Vehicle == null || !FlightControlRule.Grounded(__instance.Vehicle)) return;
        __result = false;
        disableReason = "PhobosVehiclePatches_FlightControlsDamaged".Translate(__instance.Vehicle.LabelShort.Named("VEHICLE"),
            (FlightControlRule.MinimumFlightControl * 100f).ToString("0").Named("PERCENT")).Resolve();
    }
}
