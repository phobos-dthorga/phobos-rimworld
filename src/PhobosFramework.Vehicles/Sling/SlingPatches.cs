using System.Collections.Generic;
using HarmonyLib;
using Vehicles;
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
