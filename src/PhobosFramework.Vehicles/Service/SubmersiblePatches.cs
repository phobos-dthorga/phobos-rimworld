using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

// Vehicle Framework has no depth and no hook for any of these (docs/development/submarine-framework.md lists what was read). Each patch asks one
// question of Submersibles, and each costs one empty check while no boat is under.

/// <summary>Nobody picks a hull that is under as a target until it is spotted, and then only someone whose weapon can reach it. Vehicle Framework's own postfix counts any crewed vehicle as a threat, and its comp hook can only add to that, so this has to be a patch. A postfix.</summary>
[HarmonyPatch(typeof(Pawn), nameof(Pawn.ThreatDisabled))]
public static class SubmersibleNotAThreat
{
    public static void Postfix(Pawn __instance, IAttackTargetSearcher disabledFor, ref bool __result)
    {
        if (!__result && Submersibles.AnyHidden && __instance is VehiclePawn vehicle && Submersibles.IsHidden(vehicle) && Submersibles.OutOfReach(vehicle, disabledFor)) __result = true;
    }
}

/// <summary>
/// Draws a hull that is under as a shadow instead of itself. A prefix that skips the original, and needs to: Vehicle Framework draws the body, its turrets, its overlays and any crew on show
/// from this one method and offers no way to leave them out. It skips only for a hull that is under.
/// </summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.DynamicDrawPhaseAt))]
public static class SubmersibleDrawnAsShadow
{
    public static bool Prefix(VehiclePawn __instance, DrawPhase phase)
    {
        if (!Submersibles.AnyHidden || !Submersibles.IsHidden(__instance)) return true;
        if (phase == DrawPhase.Draw) SubmersibleDraw.Draw(__instance);
        return false;
    }
}

/// <summary>
/// Keeps what cannot reach a hull that is under from hurting it, and shrinks what can. A prefix that skips the original, and needs to: every hit on a vehicle, from any weapon framework, ends in
/// this one method, and a vehicle offers no way to refuse one. It skips only for a hull that is under, and only for a hit that does not carry through water.
/// </summary>
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.ApplyDamage))]
public static class SubmersibleDamageBelow
{
    public static bool Prefix(VehiclePawn ___vehicle, ref DamageInfo dinfo)
    {
        if (!Submersibles.AnyHidden || !Submersibles.IsHidden(___vehicle)) return true;
        return !Submersibles.AbsorbHit(___vehicle, ref dinfo);
    }
}

/// <summary>A turret holds fire while its boat is under, unless the design names it as one that fires from below. A postfix; it only ever turns a turret off.</summary>
[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.TurretDisabled), MethodType.Getter)]
public static class SubmersibleTurretsHeld
{
    public static void Postfix(VehicleTurret __instance, ref bool __result)
    {
        if (!__result && Submersibles.AnyHidden && Submersibles.TurretHeld(__instance)) __result = true;
    }
}

/// <summary>Nobody steps out of a hull that is under. A prefix that skips the original, and needs to: it would put the person in the water beside the boat, and disembarking has no check to add to.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.DisembarkPawn))]
public static class SubmersibleNoLandingBelow
{
    public static bool Prefix(VehiclePawn __instance)
    {
        if (!Submersibles.AnyHidden || !Submersibles.IsHidden(__instance)) return true;
        Submersibles.RefuseLanding(__instance);
        return false;
    }
}

/// <summary>A caravan that is wholly under is no target for ambushes, demands or meetings: the storyteller picks targets by these tags. A postfix; it only ever takes tags away.</summary>
[HarmonyPatch(typeof(WorldObject), nameof(WorldObject.IncidentTargetTags))]
public static class SubmersibleCaravanNotATarget
{
    public static void Postfix(WorldObject __instance, ref IEnumerable<IncidentTargetTagDef> __result)
    {
        if (__instance is VehicleCaravan caravan && Submersibles.CaravanHidden(caravan)) __result = Enumerable.Empty<IncidentTargetTagDef>();
    }
}
