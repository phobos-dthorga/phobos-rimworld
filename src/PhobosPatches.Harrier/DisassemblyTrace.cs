using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Logging;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehiclePatches;

/// <summary>
/// TEMPORARY diagnostic (Patches 0.18.0, owner's choice of 12 Nov 2026; docs/development/temporary-patches.md row 15,
/// docs/development/vehicle-patches.md "The Harrier cannot be taken apart"). The Harrier cannot be disassembled: a colonist's work stops
/// near the end and the jet stays, and in god mode each try logs "Attempted to load a graphic off the main thread" and a job fault in
/// VehiclePawn.GenerateGraphic. Read from the code (12 Nov 2026), not observed: VehiclePawn.Destroy blanks the picture before it calls each
/// comp's OnDestroy and Pawn.Destroy; if anything between those throws, the jet stays spawned with no picture, and the game's parallel
/// pre-draw then asks for one on a worker thread, which the game refuses. Which step throws is not known, so this logs each stage of
/// the destroy step and every exception on the way, for a vehicle being taken apart only. It never skips an original and never swallows
/// an exception: every finalizer returns void, so Harmony rethrows. Off with the setting; remove the whole file once the cause is
/// found and fixed or reported.
/// </summary>
public static class DisassemblyTrace
{
    static readonly PhobosLog Logger = VehiclePatchesMod.Logger;
    static readonly object gate = new();
    /// <summary>The vehicles being taken apart, by thing ID, with the last stage each reached. A vehicle stays listed after a failed
    /// destroy, so a later picture fault can be tied to it; the list is cleared when one finishes.</summary>
    static readonly Dictionary<int, string> lastStage = new();
    static readonly HashSet<int> pictureFaultLogged = new();
    /// <summary>The vehicle whose destroy step is running now (main thread only), so comps and turrets can be tied to it.</summary>
    static VehiclePawn? destroying;
    static readonly HashSet<Type> patchedComps = new();
    static Harmony? harmony;

    public static bool On => VehiclePatchesMod.Settings.traceDisassembly;

    /// <summary>Patches applied by reflection: the fixed-wing framework's own Destroy (read by name, so this assembly never references
    /// it). Runs once the game has loaded every assembly.</summary>
    public static void Apply(Harmony instance)
    {
        harmony = instance;
        Type? vtol = AccessTools.TypeByName("FixedWingAircraftFramework.VtolVehiclePawn");
        MethodInfo? vtolDestroy = vtol == null ? null : AccessTools.DeclaredMethod(vtol, "Destroy", new[] { typeof(DestroyMode) });
        if (vtolDestroy == null)
        {
            Logger.Info("Disassembly trace: the fixed-wing framework's Destroy was not found, so only Vehicle Framework's steps are traced.");
            return;
        }
        harmony.Patch(vtolDestroy,
            prefix: new HarmonyMethod(typeof(DisassemblyTrace), nameof(FixedWingDestroyPrefix)),
            finalizer: new HarmonyMethod(typeof(DisassemblyTrace), nameof(FixedWingDestroyFinalizer)));
    }

    static string Name(Thing thing) => thing.LabelShort + " (" + thing.ThingID + ")";

    static bool Watched(Thing? thing)
    {
        if (thing == null) return false;
        lock (gate) return lastStage.ContainsKey(thing.thingIDNumber);
    }

    internal static void Stage(VehiclePawn vehicle, string stage)
    {
        lock (gate) lastStage[vehicle.thingIDNumber] = stage;
        Logger.Info("Disassembly trace, " + Name(vehicle) + ": " + stage);
    }

    static void Watch(VehiclePawn vehicle, string why)
    {
        bool first;
        lock (gate) first = !lastStage.ContainsKey(vehicle.thingIDNumber);
        Stage(vehicle, why);
        if (first) PatchCompOverrides(vehicle);
    }

    static void Fault(VehiclePawn vehicle, string where, Exception exception)
    {
        string stage;
        lock (gate) stage = lastStage.TryGetValue(vehicle.thingIDNumber, out string? s) ? s : "none";
        Logger.Error("Disassembly trace, " + Name(vehicle) + ": " + where + " threw after the stage \"" + stage + "\". Spawned " + vehicle.Spawned
            + ", destroyed " + vehicle.Destroyed + ".", exception);
    }

    /// <summary>Patches, once per type, every vehicle comp of this vehicle that overrides OnDestroy, so the trace says which one ran
    /// last. Harmony patches the override, not the base, so each is found here at run time.</summary>
    static void PatchCompOverrides(VehiclePawn vehicle)
    {
        if (harmony == null) return;
        var names = new List<string>();
        foreach (ThingComp comp in vehicle.AllComps)
        {
            if (comp is not VehicleComp) continue;
            Type type = comp.GetType();
            MethodInfo? method = type.GetMethod(nameof(VehicleComp.OnDestroy), BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (method == null || method.DeclaringType == typeof(VehicleComp)) continue;
            names.Add(method.DeclaringType!.FullName ?? method.DeclaringType.Name);
            if (!patchedComps.Add(method.DeclaringType)) continue;
            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(DisassemblyTrace), nameof(CompOnDestroyPrefix)),
                    finalizer: new HarmonyMethod(typeof(DisassemblyTrace), nameof(CompOnDestroyFinalizer)));
            }
            catch (Exception e)
            {
                Logger.Warn("Disassembly trace: could not watch " + method.DeclaringType.FullName + ".OnDestroy. " + e.Message);
            }
        }
        Logger.Info("Disassembly trace, " + Name(vehicle) + ": comps with their own OnDestroy: " + (names.Count == 0 ? "none" : string.Join(", ", names)) + ".");
    }

    static void Finished(VehiclePawn vehicle)
    {
        lock (gate)
        {
            lastStage.Remove(vehicle.thingIDNumber);
            pictureFaultLogged.Remove(vehicle.thingIDNumber);
        }
    }

    // ---- The fixed-wing framework's Destroy (it cleans up its flight, then calls Vehicle Framework's) ----

    public static void FixedWingDestroyPrefix(VehiclePawn __instance, DestroyMode mode)
    {
        if (!On || mode != DestroyMode.Deconstruct) return;
        Watch(__instance, "the fixed-wing framework's Destroy entered (its flight clean-up and crew out come first)");
    }

    public static void FixedWingDestroyFinalizer(VehiclePawn __instance, Exception? __exception)
    {
        if (!Watched(__instance)) return;
        if (__exception != null) Fault(__instance, "the fixed-wing framework's Destroy", __exception);
        else Stage(__instance, "the fixed-wing framework's Destroy returned");
    }

    // ---- Vehicle Framework's Destroy and the steps inside it ----

    public static void DestroyPrefix(VehiclePawn __instance, DestroyMode mode)
    {
        if (!On || (mode != DestroyMode.Deconstruct && !Watched(__instance))) return;
        destroying = __instance;
        Watch(__instance, "Vehicle Framework's Destroy entered (crew out, sounds, destroyed events, materials, picture cleared, comps, then the pawn's Destroy); "
            + __instance.AllPawnsAboard.Count + " aboard, map " + (__instance.Map?.ToString() ?? "none"));
    }

    public static void DestroyFinalizer(VehiclePawn __instance, Exception? __exception)
    {
        if (destroying == __instance) destroying = null;
        if (!Watched(__instance)) return;
        if (__exception != null)
        {
            Fault(__instance, "Vehicle Framework's Destroy", __exception);
            return;
        }
        Stage(__instance, "Vehicle Framework's Destroy returned; destroyed " + __instance.Destroyed + ", spawned " + __instance.Spawned);
        if (__instance.Destroyed) Finished(__instance);
    }

    public static void DisembarkAllPostfix(VehiclePawn __instance)
    {
        if (destroying == __instance) Stage(__instance, "crew put out");
    }

    public static void ReleasePrefix(object target)
    {
        if (destroying == null) return;
        if (ReferenceEquals(target, destroying)) Stage(destroying, "destroyed events done; the body's materials being released (the picture is cleared next)");
    }

    public static void CompOnDestroyPrefix(object __instance)
    {
        if (destroying != null) Stage(destroying, "comp " + __instance.GetType().FullName + ".OnDestroy entered");
    }

    public static void CompOnDestroyFinalizer(object __instance, Exception? __exception)
    {
        if (destroying == null) return;
        if (__exception != null) Fault(destroying, "comp " + __instance.GetType().FullName + ".OnDestroy", __exception);
        else Stage(destroying, "comp " + __instance.GetType().FullName + ".OnDestroy returned");
    }

    public static void TurretOnDestroyFinalizer(VehicleTurret __instance, Exception? __exception)
    {
        if (destroying == null) return;
        if (__exception != null) Fault(destroying, "turret " + __instance.def?.defName + " OnDestroy", __exception);
        else Stage(destroying, "turret " + __instance.def?.defName + " OnDestroy returned");
    }

    public static void PawnDestroyPrefix(Pawn __instance)
    {
        if (__instance is VehiclePawn vehicle && destroying == vehicle) Stage(vehicle, "the pawn's own Destroy entered (despawn, world pawns, inventory)");
    }

    public static void PawnDestroyFinalizer(Pawn __instance, Exception? __exception)
    {
        if (__instance is not VehiclePawn vehicle || destroying != vehicle) return;
        if (__exception != null) Fault(vehicle, "the pawn's own Destroy", __exception);
        else Stage(vehicle, "the pawn's own Destroy returned");
    }

    public static void DeSpawnFinalizer(VehiclePawn __instance, Exception? __exception)
    {
        if (destroying != __instance) return;
        if (__exception != null) Fault(__instance, "DeSpawn", __exception);
        else Stage(__instance, "despawned");
    }

    /// <summary>The picture fault itself. Logged once per vehicle, since the game asks for the picture every frame; says whether it
    /// was the main thread that asked.</summary>
    public static void GenerateGraphicFinalizer(VehiclePawn __instance, Exception? __exception)
    {
        if (__exception == null || !Watched(__instance)) return;
        lock (gate)
        {
            if (!pictureFaultLogged.Add(__instance.thingIDNumber)) return;
        }
        Fault(__instance, "rebuilding the picture (" + (UnityData.IsInMainThread ? "main thread" : "a worker thread") + ")", __exception);
    }

    // ---- The disassembly job ----

    static readonly AccessTools.FieldRef<JobDriver_DisassembleVehicle, float> workLeft = AccessTools.FieldRefAccess<JobDriver_DisassembleVehicle, float>("workLeft");
    static readonly AccessTools.FieldRef<JobDriver_DisassembleVehicle, float> totalWork = AccessTools.FieldRefAccess<JobDriver_DisassembleVehicle, float>("totalNeededWork");

    public static void FinishedRemovingPrefix(JobDriver_DisassembleVehicle __instance)
    {
        if (!On || __instance.job?.targetA.Thing is not VehiclePawn vehicle) return;
        Watch(vehicle, "the work is done; " + __instance.pawn.LabelShort + " starts removing it");
    }

    public static void FinishedRemovingFinalizer(JobDriver_DisassembleVehicle __instance, Exception? __exception)
    {
        if (__instance.job?.targetA.Thing is not VehiclePawn vehicle || !Watched(vehicle)) return;
        if (__exception != null) Fault(vehicle, "the job's finishing step", __exception);
        else Stage(vehicle, "the job's finishing step returned (the designation is cleared next)");
    }

    /// <summary>Every end of a disassembly job, however it ends: the condition, the work left and what the job's fail checks read.</summary>
    public static void CleanupPrefix(JobDriver __instance, JobCondition condition)
    {
        if (!On || __instance is not JobDriver_DisassembleVehicle driver) return;
        if (driver.job?.targetA.Thing is not VehiclePawn vehicle) return;
        float total = totalWork(driver), left = workLeft(driver);
        string done = total > 0 ? (1f - left / total).ToStringPercent() : "not started";
        Map? map = vehicle.MapHeld;
        bool designated = map != null && vehicle.Spawned && map.designationManager.DesignationOn(vehicle, DesignationDefOf.Deconstruct) != null;
        Logger.Info("Disassembly trace, " + Name(vehicle) + ": " + driver.pawn.LabelShort + "'s job ended " + condition + " at toil " + driver.CurToilIndex
            + ", work " + done + " done. Spawned " + vehicle.Spawned + ", destroyed " + vehicle.Destroyed + ", designated " + designated
            + ", forbidden " + vehicle.IsForbidden(driver.pawn) + ", moving " + (vehicle.vehiclePather?.Moving == true)
            + ", can be taken apart " + vehicle.DeconstructibleBy(driver.pawn.Faction) + ".");
    }
}

// The attribute patches. Each is a plain prefix, postfix or finalizer; none skips the original or swallows an exception.

[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.Destroy))]
public static class DisassemblyTraceDestroyPatch
{
    public static void Prefix(VehiclePawn __instance, DestroyMode mode) => DisassemblyTrace.DestroyPrefix(__instance, mode);
    public static void Finalizer(VehiclePawn __instance, Exception? __exception) => DisassemblyTrace.DestroyFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.DisembarkAll))]
public static class DisassemblyTraceDisembarkPatch
{
    public static void Postfix(VehiclePawn __instance) => DisassemblyTrace.DisembarkAllPostfix(__instance);
}

[HarmonyPatch(typeof(RGBMaterialPool), nameof(RGBMaterialPool.Release), new[] { typeof(IMaterialCacheTarget) })]
public static class DisassemblyTraceReleasePatch
{
    public static void Prefix(IMaterialCacheTarget target) => DisassemblyTrace.ReleasePrefix(target);
}

[HarmonyPatch(typeof(VehicleTurret), nameof(VehicleTurret.OnDestroy))]
public static class DisassemblyTraceTurretPatch
{
    public static void Finalizer(VehicleTurret __instance, Exception? __exception) => DisassemblyTrace.TurretOnDestroyFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Destroy))]
public static class DisassemblyTracePawnDestroyPatch
{
    public static void Prefix(Pawn __instance) => DisassemblyTrace.PawnDestroyPrefix(__instance);
    public static void Finalizer(Pawn __instance, Exception? __exception) => DisassemblyTrace.PawnDestroyFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.DeSpawn))]
public static class DisassemblyTraceDeSpawnPatch
{
    public static void Finalizer(VehiclePawn __instance, Exception? __exception) => DisassemblyTrace.DeSpawnFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(VehiclePawn), "GenerateGraphic")]
public static class DisassemblyTraceGraphicPatch
{
    public static void Finalizer(VehiclePawn __instance, Exception? __exception) => DisassemblyTrace.GenerateGraphicFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(JobDriver_DisassembleVehicle), "FinishedRemoving")]
public static class DisassemblyTraceFinishedPatch
{
    public static void Prefix(JobDriver_DisassembleVehicle __instance) => DisassemblyTrace.FinishedRemovingPrefix(__instance);
    public static void Finalizer(JobDriver_DisassembleVehicle __instance, Exception? __exception) => DisassemblyTrace.FinishedRemovingFinalizer(__instance, __exception);
}

[HarmonyPatch(typeof(JobDriver), nameof(JobDriver.Cleanup))]
public static class DisassemblyTraceCleanupPatch
{
    public static void Prefix(JobDriver __instance, JobCondition condition) => DisassemblyTrace.CleanupPrefix(__instance, condition);
}
