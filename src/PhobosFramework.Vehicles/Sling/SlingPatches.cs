using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A slung load is held in the helicopter's sling comp, and the game finds held things through Pawn.GetChildHolders, which lists a pawn's
/// inventory, carried thing, equipment and apparel but never its comps (RimWorld 1.7 Verse.Pawn.cs:4736); Vehicle Framework's in-flight
/// holders pass nothing on either. Without this the people riding in a slung vehicle would be lost track of in flight. Postfix: it adds the
/// sling only while it holds something, and never skips the original (sling loads, Framework 0.92.0). Cherokee Sling makes the same kind of
/// postfix for its own comp; each adds only its own.
/// </summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.GetChildHolders))]
public static class Patch_PawnChildHolders_Sling
{
    public static void Postfix(Pawn __instance, List<IThingHolder> outChildren)
    {
        if (__instance is VehiclePawn vehicle && vehicle.GetComp<CompPhobosSling>() is { Carried: not null } sling && !outChildren.Contains(sling)) outChildren.Add(sling);
    }
}

/// <summary>
/// Vehicle Framework's launch check (CompVehicleLauncher.CanLaunchWithCargoCapacity) also refuses a take-off whose chosen sling load is not
/// hooked up yet, has moved out of reach or is too heavy today, with the reason on the launch button. Postfix; it only ever refuses.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanLaunchWithCargoCapacity))]
public static class Patch_CanLaunch_Sling
{
    public static void Postfix(CompVehicleLauncher __instance, ref bool __result, ref string disableReason)
    {
        if (!__result || __instance.Vehicle?.GetComp<CompPhobosSling>()?.LaunchRefusal() is not { } refusal) return;
        __result = false;
        disableReason = refusal;
    }
}

/// <summary>
/// Vehicle Framework draws a helicopter taking off from its own stand-in on the map (VehicleSkyfaller_Leaving.DrawAt), which knows nothing
/// of a sling. Postfix: after the helicopter is drawn, its slung load is drawn below it (SlingDrawing). It never skips the original and
/// draws nothing for a helicopter with no load (Framework 0.99.0).
/// </summary>
[HarmonyPatch(typeof(VehicleSkyfaller_Leaving), "DrawAt", new[] { typeof(Vector3), typeof(bool) })]
public static class Patch_LeavingDraw_Sling
{
    public static void Postfix(VehicleSkyfaller_Leaving __instance) => SlingDrawing.Draw(__instance);
}

/// <summary>The same for a helicopter coming in to land (VehicleSkyfaller_Arriving.DrawAt). Postfix; it never skips the original.</summary>
[HarmonyPatch(typeof(VehicleSkyfaller_Arriving), "DrawAt", new[] { typeof(Vector3), typeof(bool) })]
public static class Patch_ArrivingDraw_Sling
{
    public static void Postfix(VehicleSkyfaller_Arriving __instance) => SlingDrawing.Draw(__instance);
}

/// <summary>
/// An aircraft in flight on the world map lists only itself as its vehicles (AerialVehicleInFlight.Vehicles), and Vehicle Framework's
/// passengers tab is drawn from that list, so a slung vehicle and the people in it could not be seen or chosen there. Postfix: the slung
/// load follows its helicopter in the list. Only that tab reads the list in Vehicle Framework (checked with query-code, 12 Nov 2026). It
/// never skips the original and changes nothing for an aircraft with no load (Framework 0.99.0).
/// </summary>
[HarmonyPatch(typeof(AerialVehicleInFlight), nameof(AerialVehicleInFlight.Vehicles), MethodType.Getter)]
public static class Patch_AerialVehicles_Sling
{
    public static void Postfix(AerialVehicleInFlight __instance, ref IEnumerable<VehiclePawn> __result)
    {
        if (SlingLoads.Of(__instance.Vehicle) != null) __result = SlingLoads.WithLoads(__result);
    }
}
