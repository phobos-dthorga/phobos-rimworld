using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Vehicles.Rendering;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

// Our own flight over the map (Framework 0.147.0; docs/development/vehicle-flight-over-the-map.md, "The patches as they now stand"). Each acts
// only for an aircraft of ours in the air, and asks FlightRegistry.Any first, so each costs one empty check while none flies. None is on a
// method every pawn calls each tick.

/// <summary>
/// Where an aircraft in the air is drawn: its true position, on the Skyfaller layer over buildings, people and smoke and under the weather.
/// Vehicle Framework's drawn position springs toward the cell's centre and would trail a moving aircraft, and its height offset is applied
/// unevenly to the body, turrets and seat pawns; everything drawn or aimed reads this one figure, so all agree (finding 1). A postfix.
/// </summary>
[HarmonyPatch(typeof(VehicleDrawTracker), nameof(VehicleDrawTracker.DrawPos), MethodType.Getter)]
public static class Patch_VehicleDrawTracker_DrawPos_Flight
{
    static readonly float SkyfallerAltitude = AltitudeLayer.Skyfaller.AltitudeFor();

    public static void Postfix(VehiclePawn ___vehicle, ref Vector3 __result)
    {
        if (!FlightRegistry.Any || FlightRegistry.Get(___vehicle) is not { } flight) return;
        Vector3 centre = flight.TrueCenter;
        __result = new Vector3(centre.x, flight.Climb > 0f ? SkyfallerAltitude : __result.y, centre.z);
    }
}

/// <summary>
/// A ground order given to an aircraft in the air becomes "fly here" (finding 7): Vehicle Framework's "go here" (one vehicle or a group)
/// ends here, and a group order skips each vehicle's own refusal. A prefix that skips the original, and must: the original would hand the
/// aircraft to the path follower that drives it on the ground. It skips only for an aircraft of ours in the air.
/// </summary>
[HarmonyPatch]
public static class Patch_PawnGotoAction_Flight
{
    static MethodBase? TargetMethod() => AccessTools.Method("Vehicles.FloatMenuOptionProvider_OrderVehicle:PawnGotoAction");

    public static bool Prefix(IntVec3 clickCell, VehiclePawn vehicle)
    {
        if (FlightRegistry.Get(vehicle) is not { } flight) return true;
        // Framework 0.174.0: several aircraft sent to one spot together spread round it.
        flight.FlyTo(FlightGroupOrders.Spread(vehicle, clickCell));
        return false;
    }
}

/// <summary>Nobody boards an aircraft in the air (finding 9). A prefix that skips the original for one of ours in the air, refusing and saying
/// why; boarding has no check to add to.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.TryAddPawn), typeof(Pawn), typeof(VehicleRoleHandler))]
public static class Patch_TryAddPawn_Flight
{
    public static bool Prefix(VehiclePawn __instance, ref bool __result)
    {
        if (!FlightRegistry.InAir(__instance) || VehicleAccess.Lines == __instance) return true;
        VehicleAccess.RefuseInAir(__instance, "PhobosFlight_RefuseBoard");
        __result = false;
        return false;
    }
}

/// <summary>
/// Fire on the ground does not catch an aircraft of ours in the air (fault-read W2, Framework 0.167.0): its flammable body stands on the cell
/// beneath, where a grass fire would otherwise attach to it. A prefix that skips the original for one of ours in the air only, as VTOL Vehicle's
/// and the Harrier's frameworks do for theirs; burning in the air from hits is not modelled.
/// </summary>
[HarmonyPatch(typeof(FireUtility), nameof(FireUtility.TryAttachFire))]
public static class Patch_TryAttachFire_Flight
{
    public static bool Prefix(Thing t) => !FlightRegistry.Any || t is not VehiclePawn vehicle || !FlightRegistry.InAir(vehicle);
}

/// <summary>
/// Vehicle Framework rebuilds its list of comps to tick whenever a comp is added or removed (an upgrade), dropping every comp that ticks by
/// request (fault-read W11, Framework 0.167.0): an aircraft in the air would stop flying. A postfix: each comp of ours that still needs its
/// tick asks for it again.
/// </summary>
[HarmonyPatch(typeof(VehiclePawn), "RecacheCompTickers")]
public static class Patch_RecacheCompTickers_Flight
{
    public static void Postfix(VehiclePawn __instance)
    {
        foreach (ThingComp comp in __instance.AllComps)
        {
            if (comp is IKeepsTicking { WantsTicks: true } && comp is VehicleComp vehicleComp) __instance.RequestTickStart(vehicleComp);
        }
    }
}

/// <summary>A comp of ours that ticks by request and can say whether it still needs to (fault-read W11): asked when Vehicle Framework drops it.</summary>
public interface IKeepsTicking
{
    bool WantsTicks { get; }
}

/// <summary>Nothing is loaded into an aircraft in the air (finding 9). A prefix that skips the original for one of ours in the air.</summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.AddOrTransfer), typeof(Thing), typeof(int))]
public static class Patch_AddOrTransfer_Flight
{
    public static bool Prefix(VehiclePawn __instance, Thing thing, ref int __result)
    {
        if (!FlightRegistry.InAir(__instance) || VehicleAccess.Lines == __instance) return true;
        // Fault-read F1 (Framework 0.163.0): Vehicle Framework moves someone dying aboard into the cargo; refused, the game could not leave
        // a body and lost it with their gear. Nothing else is loaded in the air.
        if (thing is Pawn or Corpse) return true;
        VehicleAccess.RefuseInAir(__instance, "PhobosFlight_RefuseLoad");
        __result = 0;
        return false;
    }
}

/// <summary>Nothing is unloaded from an aircraft in the air: Vehicle Framework's drop helper, by the container's owner (finding 9). A prefix
/// that skips the original for one of ours in the air.</summary>
[HarmonyPatch(typeof(Ext_Vehicles), nameof(Ext_Vehicles.TryDropOutsideVehicle))]
public static class Patch_DropOutsideVehicle_Flight
{
    public static bool Prefix(ThingOwner container, ref bool __result) => VehicleAccess.MayDrop(container, ref __result);
}

/// <summary>The same for its drop-everything helper. A prefix that skips the original for one of ours in the air.</summary>
[HarmonyPatch(typeof(Ext_Vehicles), nameof(Ext_Vehicles.TryDropAllOutsideVehicle))]
public static class Patch_DropAllOutsideVehicle_Flight
{
    public static bool Prefix(ThingOwner container, ref bool __result) => VehicleAccess.MayDrop(container, ref __result);
}

/// <summary>No work on an aircraft in the air: repairs and the like (finding 9). A prefix that skips the original for one of ours in the air;
/// a work giver finding no job is the game's own way of saying there is none.</summary>
[HarmonyPatch(typeof(VehicleWorkGiver), nameof(VehicleWorkGiver.JobOnThing))]
public static class Patch_VehicleWorkGiver_Flight
{
    public static bool Prefix(Thing t, ref Job? __result)
    {
        if (!FlightRegistry.Any || t is not VehiclePawn vehicle || !FlightRegistry.InAir(vehicle)) return true;
        __result = null;
        return false;
    }
}

/// <summary>
/// Nobody takes apart an aircraft in the air (Framework 0.174.0; the audit of 19 Nov 2026, finding 3, confirmed in code): Vehicle Framework's
/// disassembly is the game's building-removal work giver, not one of its vehicle work givers, so the guard above never saw it, and an aircraft
/// marked for disassembly could be taken apart while it hovered. A postfix: no job while it is in the air on our flight.
/// </summary>
[HarmonyPatch(typeof(WorkGiver_DisassembleVehicle), nameof(WorkGiver_DisassembleVehicle.HasJobOnThing))]
public static class Patch_DisassembleVehicle_Flight
{
    public static void Postfix(Thing thing, ref bool __result)
    {
        if (!__result || !FlightRegistry.Any || thing is not VehiclePawn vehicle || !FlightRegistry.InAir(vehicle)) return;
        __result = false;
        Seen.Note("aircraft.no-disassembly-in-air", vehicle);
    }
}

/// <summary>The same for loading, refuelling and upgrade work, which look for vehicles through this list (finding 9). A postfix that only
/// takes aircraft in the air out.</summary>
[HarmonyPatch(typeof(VehicleReservationManager), nameof(VehicleReservationManager.VehicleListers))]
public static class Patch_VehicleListers_Flight
{
    public static void Postfix(ref IEnumerable<VehiclePawn> __result)
    {
        if (!FlightRegistry.Any) return;
        __result = __result.Where(vehicle => !FlightRegistry.InAir(vehicle));
    }
}

/// <summary>
/// Notes a vehicle being destroyed, so the guards let its people out: Vehicle Framework puts everyone out through DisembarkPawn as it destroys
/// a vehicle, and a guard that refused them took them with it (row CR8: a submarine destroyed while under). A prefix and a finalizer that
/// change nothing else.
/// </summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Destroy))]
public static class Patch_VehiclePawn_Destroy_Access
{
    public static void Prefix(VehiclePawn __instance)
    {
        VehicleAccess.Destroying.Add(__instance);
        // Framework 0.155.0: whoever is on its ropes or hoist falls before it is gone.
        if (FlightRegistry.Any && FlightRegistry.InAir(__instance)) CompPhobosFlight.OnDestroying(__instance);
    }

    public static Exception? Finalizer(VehiclePawn __instance, Exception? __exception)
    {
        VehicleAccess.Destroying.Remove(__instance);
        return __exception;
    }
}

/// <summary>
/// The one rule for what may reach a vehicle and come and go from it now (Framework 0.147.0): a hull that is under, or an aircraft in the air.
/// Asked from the shared prefixes on DisembarkPawn and ApplyDamage and from the flight's own guards.
/// </summary>
public static class VehicleAccess
{
    /// <summary>Vehicles being destroyed now: their people are let out whatever else holds.</summary>
    public static readonly HashSet<VehiclePawn> Destroying = new();

    /// <summary>The aircraft whose hoist is putting someone aboard now (Framework 0.155.0): its own boarding and loading guards let it.</summary>
    public static VehiclePawn? Lines;

    /// <summary>Whether a hit of this kind reaches an aircraft in the air: shots and blasts do, blows, fire on the ground and the like do not.</summary>
    public static bool ReachesInAir(DamageInfo dinfo) => dinfo.Def != null && (dinfo.Def.isRanged || dinfo.Def.isExplosive);

    /// <summary>For the drop helpers' prefixes: true to let the drop run; false, with the result set to a refusal, for a container of an aircraft
    /// of ours in the air.</summary>
    public static bool MayDrop(ThingOwner container, ref bool result)
    {
        if (!FlightRegistry.Any || container?.Owner is not Pawn_InventoryTracker { pawn: VehiclePawn vehicle } || !FlightRegistry.InAir(vehicle)) return true;
        RefuseInAir(vehicle, "PhobosFlight_RefuseUnload");
        result = false;
        return false;
    }

    /// <summary>Says once in a while, to the player, why something could not happen to an aircraft in the air: never a silent refusal.</summary>
    public static void RefuseInAir(VehiclePawn vehicle, string key)
    {
        if (vehicle.Faction != Faction.OfPlayer) return;
        if (!MessagesRepeatAvoider.MessageShowAllowed(key + vehicle.thingIDNumber, 5f)) return;
        Messages.Message(key.Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), vehicle, MessageTypeDefOf.RejectInput, historical: false);
    }
}
