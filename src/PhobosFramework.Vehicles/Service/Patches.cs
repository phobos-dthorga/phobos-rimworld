using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Gives each vehicle design its loadout's parts and comp. A prefix, not a postfix: Vehicle Framework's ResolveReferences then
/// resolves our parts with its own (hitboxes, efficiency curves) and the base ThingDef resolves our comp. Vehicle Framework has
/// no hook between Def loading and this method, so a patch it is. Cross-references are resolved by now, so loadouts read fully.
/// </summary>
[HarmonyPatch(typeof(VehicleDef), nameof(VehicleDef.ResolveReferences))]
public static class VehicleDefResolvePatch
{
    public static void Prefix(VehicleDef __instance) => VehicleSystems.Inject(__instance);
}

/// <summary>
/// Keeps saved vehicles in step with their designs. Vehicle Framework saves a vehicle's parts as healths by position and rebinds
/// them to the design's list on load (VehicleStatHandler.ExposeData, read 8 Nov 2026): a design with more parts than the save leaves
/// the new ones uncreated, and one with fewer indexes past its list. The prefix drops saved parts beyond the design (a part this
/// service appended and no longer does, after a settings change); the postfix creates the design's new parts at full health, the
/// way the framework makes them for a new vehicle (the same rule as D'thorga Patches' Harrier part).
/// </summary>
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.ExposeData))]
public static class PartsOnLoad
{
    static readonly FieldInfo? VehicleField = AccessTools.Field(typeof(VehicleStatHandler), "vehicle");
    static readonly FieldInfo? ByKeys = AccessTools.Field(typeof(VehicleStatHandler), "componentsByKeys");
    static readonly FieldInfo? StatComponents = AccessTools.Field(typeof(VehicleStatHandler), "statComponents");
    static readonly MethodInfo? Recache = AccessTools.Method(typeof(VehicleStatHandler), "RecacheStatCategories");
    static readonly MethodInfo? MarkDirty = AccessTools.Method(typeof(VehicleStatHandler), "MarkAllDirty");

    public static void Prefix(VehicleStatHandler __instance)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit || __instance.components == null) return;
        if (VehicleField?.GetValue(__instance) is not VehiclePawn vehicle) return;
        int defined = vehicle.VehicleDef?.components?.Count ?? 0;
        int extra = __instance.components.Count - defined;
        if (extra <= 0) return;
        __instance.components.RemoveRange(defined, extra);
        Log.Message(VehicleSystemsMod.LogPrefix + " " + vehicle.LabelShort + " was saved with " + extra + " part" + (extra == 1 ? "" : "s")
                    + " its design no longer has; dropped.");
    }

    public static void Postfix(VehicleStatHandler __instance)
    {
        if (Scribe.mode != LoadSaveMode.PostLoadInit || __instance.components == null) return;
        if (VehicleField?.GetValue(__instance) is not VehiclePawn vehicle) return;
        List<VehicleComponentProperties>? defined = vehicle.VehicleDef?.components;
        if (defined == null || __instance.components.Count >= defined.Count) return;
        var byKeys = ByKeys?.GetValue(__instance) as Dictionary<string, VehicleComponent>;
        int added = 0;
        for (int i = __instance.components.Count; i < defined.Count; i++)
        {
            VehicleComponentProperties props = defined[i];
            try
            {
                if (Activator.CreateInstance(props.compClass, vehicle) is not VehicleComponent component)
                    throw new InvalidOperationException(props.compClass + " is not a vehicle component");
                component.Initialize(props);
                component.PostCreate();
                __instance.components.Add(component);
                if (byKeys != null) byKeys[props.key] = component;
                Recache?.Invoke(__instance, new object[] { component });
                added++;
            }
            catch (Exception error)
            {
                Log.Warning(VehicleSystemsMod.LogPrefix + " could not add the part " + props.key + " to " + vehicle.LabelShort + ": " + error.Message);
                break;
            }
        }
        if (added == 0) return;
        MarkDirty?.Invoke(__instance, null);
    }

    /// <summary>Cuts a running vehicle's parts back to <paramref name="count"/> and rebuilds the lookups. Returns how many went.</summary>
    public static int TrimTo(VehicleStatHandler handler, int count)
    {
        int extra = handler.components.Count - count;
        if (extra <= 0 || count < 0) return 0;
        handler.components.RemoveRange(count, extra);
        if (ByKeys?.GetValue(handler) is IDictionary byKeys)
        {
            byKeys.Clear();
            foreach (VehicleComponent component in handler.components) byKeys[component.props.key] = component;
        }
        if (StatComponents?.GetValue(handler) is IDictionary statComponents)
        {
            statComponents.Clear();
            foreach (VehicleComponent component in handler.components) Recache?.Invoke(handler, new object[] { component });
        }
        MarkDirty?.Invoke(handler, null);
        return extra;
    }
}

/// <summary>
/// The drivetrain's speed cap: a vehicle whose engine cannot deliver the power, or whose wheels cannot get the grip, pays less path
/// cost each tick and so moves slower, without touching its MoveSpeed stat or path planning (drivetrain design). Vehicle Framework has
/// no extension point here. It runs first so Vanilla Vehicles Expanded's acceleration postfix, which also scales this cost and adds up
/// what it pays to decide when to brake, counts the capped figure (read from its 1.7 source, 9 Nov 2026).
/// </summary>
[HarmonyPatch(typeof(VehiclePathFollower), "CostToPayThisTick")]
public static class SpeedCapPatch
{
    [HarmonyPriority(Priority.First)]
    public static void Postfix(ref float __result, VehiclePawn ___vehicle)
    {
        if (___vehicle == null) return;
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(___vehicle);
        if (comp == null) return;
        float cap = comp.SpeedCap;
        if (cap < 1f) __result *= cap;
    }
}

/// <summary>Lets the systems' stages scale any Vehicle Framework stat (a worn tyre slows the vehicle).</summary>
public class VehicleStatPart_PhobosSystems : VehicleStatPart
{
    public override float TransformValue(VehiclePawn vehicle, float value)
    {
        CompPhobosVehicleSystems? comp = vehicle?.GetComp<CompPhobosVehicleSystems>();
        return comp == null || !comp.Usable ? value : value * comp.StatFactor(statDef);
    }

    public override string ExplanationPart(VehiclePawn vehicle)
    {
        CompPhobosVehicleSystems? comp = vehicle?.GetComp<CompPhobosVehicleSystems>();
        if (comp == null || !comp.Usable || comp.StatFactor(statDef) == 1f) return "";
        return string.Join("\n", comp.StatFactorReasons(statDef));
    }
}

/// <summary>On every part bound to a role: tells the vehicle's systems the part was hit and how hard.</summary>
public class Reactor_PhobosVehicleSystems : Reactor
{
    public string role = "";

    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
    {
        vehicle.GetComp<CompPhobosVehicleSystems>()?.Notify_PartHit(role, dinfo.Amount);
    }
}
