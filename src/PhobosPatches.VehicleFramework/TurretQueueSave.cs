using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vehicles;
using Verse;
using TurretData = Vehicles.CompVehicleTurrets.TurretData;

namespace Phobos.VehicleFrameworkPatches;

/// <summary>
/// TEMPORARY (docs/development/temporary-patches.md, row 22): a vehicle saved while one of its weapons is part-way through a burst keeps
/// all of itself in the save (Patches 0.31.1). Vehicle Framework saves CompVehicleTurrets.turretQueue, the bursts under way, by reference,
/// but its entries (TurretData) cannot be saved by reference: the save throws there, and everything the vehicle would write after it is
/// lost, its health, cargo and the crew and passengers inside it. On loading, the cut-short entry throws again and the game drops the
/// vehicle and everyone in it (the owner's Mongoose and its two crew, saved mid-strike on 22 Nov 2026). A prefix sets the bursts aside for
/// the save, and clears each weapon's queuedToFire flag with them, since only leaving the queue clears it and a weapon loaded with it set
/// never reloads or looks for a target again; a finalizer puts both back whatever happened, and lets any error through unchanged. The save
/// then holds an empty queue, which Vehicle Framework writes and reads correctly, and on loading the burst has simply ended: the weapon
/// aims again at its saved target. Stands down by itself: it applies only while Vehicle Framework's own code still saves the queue by
/// reference, read from that code at start-up. Saved games already cut short cannot be mended by it.
/// </summary>
[HarmonyPatch(typeof(CompVehicleTurrets), nameof(CompVehicleTurrets.PostExposeData))]
public static class TurretQueueSavePatch
{
    /// <summary>Vehicle Framework's field, and the label it saves the field under.</summary>
    const string QueueField = "turretQueue";

    static AccessTools.FieldRef<CompVehicleTurrets, List<TurretData>>? queue;

    /// <summary>Applies only while Vehicle Framework's save code still asks for the queue by reference: the label, then that mode.</summary>
    public static bool Prepare()
    {
        if (AccessTools.Field(typeof(CompVehicleTurrets), QueueField)?.FieldType != typeof(List<TurretData>)) return false;
        MethodInfo? save = AccessTools.Method(typeof(CompVehicleTurrets), nameof(CompVehicleTurrets.PostExposeData));
        if (save == null) return false;
        List<CodeInstruction> code = PatchProcessor.GetOriginalInstructions(save);
        bool byReference = false;
        for (int i = 0; i + 1 < code.Count; i++)
        {
            if (code[i].opcode != OpCodes.Ldstr || code[i].operand as string != QueueField) continue;
            byReference = code[i + 1].LoadsConstant((long)LookMode.Reference);
            break;
        }
        if (!byReference)
        {
            VehicleFrameworkPatchesMod.Logger.Info("Vehicle Framework no longer saves its turret queue by reference; the save guard for it stands down.");
            return false;
        }
        queue = AccessTools.FieldRefAccess<CompVehicleTurrets, List<TurretData>>(QueueField);
        VehicleFrameworkPatchesMod.TurretQueueGuardOn = true;
        return true;
    }

    public static void Prefix(CompVehicleTurrets __instance, out List<TurretData>? __state)
    {
        __state = null;
        if (Scribe.mode != LoadSaveMode.Saving || queue == null) return;
        List<TurretData>? underWay = queue(__instance);
        if (underWay == null || underWay.Count == 0) return;
        __state = underWay;
        queue(__instance) = new List<TurretData>();
        foreach (TurretData data in underWay)
        {
            if (data.turret != null) data.turret.queuedToFire = false;
        }
    }

    public static void Finalizer(CompVehicleTurrets __instance, List<TurretData>? __state)
    {
        if (__state == null || queue == null) return;
        queue(__instance) = __state;
        foreach (TurretData data in __state)
        {
            if (data.turret != null) data.turret.queuedToFire = true;
        }
        Thing vehicle = __instance.parent;
        if (VehicleFrameworkPatchesMod.Logger.DebugEnabled)
            VehicleFrameworkPatchesMod.Logger.Debug(vehicle.LabelShort + " was saved mid-burst; " + __state.Count + " burst(s) left out of the save, kept in play");
        if (Seen.Wants("patches.turret-queue-saved", vehicle)) Seen.Note("patches.turret-queue-saved", vehicle, __state.Count + " burst(s) under way");
    }
}
