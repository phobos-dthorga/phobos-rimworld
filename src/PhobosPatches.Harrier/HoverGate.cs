using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.VehicleSystems;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// The hover limit on the Harrier's pad landings (owner, 9 Nov 2026: a player setting, default "refuse the pad and use the runway";
/// docs/development/vehicle-turbine-design.md). The Harrier asks its static Harrier.CanHover(vehicle, props) before every pad landing:
/// the vertical-landing button, the order, each tick of the approach and hover, and the automatic return to a parking spot, which
/// falls back to the runway when it is false (read in the Harrier's assembly, 9 Nov 2026). D'thorga Framework's turbine model says
/// whether the engine can hold the jet in a hover at its weight in the day's heat; when the player chose to refuse, CanHover answers
/// no. The Harrier's own refusal text speaks of a damaged engine, so the button, the order and the abort carry our reason instead.
/// "Allow with damage" and "warn only" need nothing here: the Framework's engine model handles them while the jet hovers.
/// Every member is reached by name: this assembly does not reference the Harrier's.
/// </summary>
public static class HoverGate
{
    /// <summary>Whether the last CanHover answered no because of the hover limit alone (the engine itself was fine).</summary>
    internal static bool refusedByLimit;

    internal static bool Refuses(object vehicle) => vehicle is VehiclePawn pawn && HoverLimits.RefusesPad(pawn);
}

/// <summary>A postfix on Harrier.CanHover: no when the hover is beyond the engine and the player chose to refuse the pad.</summary>
[HarmonyPatch]
public static class HoverGateCanHoverPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("AV8BHarrier.Harrier:CanHover");

    public static void Postfix(ref bool __result, object vehicle)
    {
        HoverGate.refusedByLimit = false;
        if (!__result || !HoverGate.Refuses(vehicle)) return;
        __result = false;
        HoverGate.refusedByLimit = true;
    }
}

/// <summary>
/// A prefix on the vertical-landing order: when the hover limit refuses it, say why and skip the order. It skips the original
/// because the original would refuse with the Harrier's damaged-engine message, which would be untrue (recorded reason).
/// </summary>
[HarmonyPatch]
public static class HoverGateOrderPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("AV8BHarrier.VerticalLanding:Order");

    public static bool Prefix(object vehicle)
    {
        if (!HoverGate.Refuses(vehicle)) return true;
        VehiclePawn pawn = (VehiclePawn)vehicle;
        Messages.Message(HoverLimits.RefusalText(pawn), pawn, MessageTypeDefOf.RejectInput, false);
        return false;
    }
}

/// <summary>A postfix on the vertical-landing buttons: a button the hover limit disabled gives the limit's reason.</summary>
[HarmonyPatch]
public static class HoverGateGizmoPatch
{
    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("AV8BHarrier.VerticalLanding:Gizmos");

    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, object vehicle)
    {
        foreach (Gizmo gizmo in __result)
        {
            // The Harrier asks CanHover just before it yields the landing button, so the flag belongs to this button.
            if (HoverGate.refusedByLimit && gizmo.Disabled && gizmo is Command && vehicle is VehiclePawn pawn)
            {
                gizmo.Disable(HoverLimits.RefusalText(pawn));
                HoverGate.refusedByLimit = false;
            }
            yield return gizmo;
        }
    }
}

/// <summary>A prefix on the landing's cancel: an approach or hover the limit stopped says so, not that the engine is damaged.</summary>
[HarmonyPatch]
public static class HoverGateCancelPatch
{
    const string DamagedEngineKey = "Harrier_HoverAbortedEngine";

    static bool Prepare() => TargetMethod() != null;

    static MethodBase? TargetMethod() => AccessTools.Method("AV8BHarrier.VerticalLanding:Cancel");

    public static void Prefix(object vehicle, ref string messageKey)
    {
        if (messageKey != DamagedEngineKey || !HoverGate.refusedByLimit || vehicle is not VehiclePawn pawn) return;
        HoverGate.refusedByLimit = false;
        messageKey = null!;
        Messages.Message(HoverLimits.RefusalText(pawn), pawn, MessageTypeDefOf.NegativeEvent, false);
    }
}
