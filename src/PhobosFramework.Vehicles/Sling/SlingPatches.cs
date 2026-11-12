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
/// A slung load's drag (sling slice 2, Framework 0.101.0; owner, 11 Nov 2026, with a setting): a helicopter with a load ready on its hook or
/// hanging from it flies slower on the world map (CompVehicleLauncher.FlightSpeed). Postfix; it changes nothing for a helicopter with no load.
/// Vehicle Framework works out the range drawn when choosing a destination from the same figure, so the range shows the load too.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.FlightSpeed), MethodType.Getter)]
public static class Patch_FlightSpeed_Sling
{
    public static void Postfix(CompVehicleLauncher __instance, ref float __result)
    {
        if (__instance.Vehicle?.GetComp<CompPhobosSling>() is { SlungFlightNow: true }) __result *= (float)SlungFlight.Factors.Speed;
    }
}

/// <summary>
/// The same load burns more fuel (CompVehicleLauncher.FuelConsumptionWorldMultiplier, which Vehicle Framework's flight spends by the tick and
/// its range and fuel checks read). Postfix; nothing for a helicopter with no load. SlingRule.SlungFlight turns the per-tile factor into a
/// per-tick one, since the slower flight takes longer.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.FuelConsumptionWorldMultiplier), MethodType.Getter)]
public static class Patch_FlightFuel_Sling
{
    public static void Postfix(CompVehicleLauncher __instance, ref float __result)
    {
        if (__instance.Vehicle?.GetComp<CompPhobosSling>() is { SlungFlightNow: true }) __result *= (float)SlungFlight.Factors.FuelPerTick;
    }
}

/// <summary>The slung flight's two factors, from the rules Def through SlingRule.</summary>
public static class SlungFlight
{
    public static (double Speed, double FuelPerTick) Factors
    {
        get
        {
            PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
            return Phobos.Framework.Rules.SlingRule.SlungFlight(rules.slingSpeedFactor, rules.slingFuelPerTileFactor);
        }
    }
}

/// <summary>
/// An aircraft coming down in a crash on the world map (out of fuel or shot down: AerialVehicleInFlight.InitiateCrashEvent) carries its load
/// down with it. Prefix that only notes it on the sling (sling slice 2): when the helicopter reaches the crash site's map, the load falls beside
/// it and lands hard, rather than being set down. It never skips the original.
/// </summary>
[HarmonyPatch(typeof(AerialVehicleInFlight), nameof(AerialVehicleInFlight.InitiateCrashEvent))]
public static class Patch_CrashEvent_Sling
{
    public static void Prefix(AerialVehicleInFlight __instance) => __instance.Vehicle?.GetComp<CompPhobosSling>()?.NoteCrash();
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
