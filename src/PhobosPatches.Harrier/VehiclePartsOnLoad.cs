using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Vehicles;
using Verse;

namespace Phobos.VehiclePatches;

/// <summary>
/// Gives a vehicle saved with fewer parts the parts its Def has gained. Vehicle Framework saves a vehicle's parts as a list of
/// healths with no keys and, on load, matches them to the Def's parts by position; parts beyond the saved count are never created,
/// so a Harrier from before D'thorga Patches 0.11.0 would keep its four parts for ever, and an upgrade naming a new part would throw
/// (VehicleStatHandler.ExposeData and VehicleUpgrade.Unlock, read 7 Nov 2026). This appends the missing parts at full health, the
/// way the framework makes them for a new vehicle, in the same load step. Parts are only ever appended to a Def, never reordered or
/// removed, which is the rule this relies on.
/// </summary>
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.ExposeData))]
public static class VehiclePartsOnLoadPatch
{
    static readonly FieldInfo? VehicleField = AccessTools.Field(typeof(VehicleStatHandler), "vehicle");
    static readonly FieldInfo? ByKeys = AccessTools.Field(typeof(VehicleStatHandler), "componentsByKeys");
    static readonly MethodInfo? Recache = AccessTools.Method(typeof(VehicleStatHandler), "RecacheStatCategories");
    static readonly MethodInfo? MarkDirty = AccessTools.Method(typeof(VehicleStatHandler), "MarkAllDirty");

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
                Log.Warning(VehiclePatchesMod.LogPrefix + " could not add the part " + props.key + " to " + vehicle.LabelShort + ": " + error.Message);
                break;
            }
        }
        if (added == 0) return;
        MarkDirty?.Invoke(__instance, null);
        Log.Message(VehiclePatchesMod.LogPrefix + " " + vehicle.LabelShort + " gained " + added + " part" + (added == 1 ? "" : "s") + " its design now has, at full health.");
    }
}
