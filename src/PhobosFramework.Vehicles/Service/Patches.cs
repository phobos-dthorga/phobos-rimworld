using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Vehicles;
using System.Text;
using Phobos.Framework.Environment;
using Phobos.Framework.Vehicles.Rules;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Gives each vehicle design its loadout's parts and comp. A prefix, not a postfix: Vehicle Framework's ResolveReferences then
/// resolves our parts with its own (hitboxes, efficiency curves) and the base ThingDef resolves our comp. Vehicle Framework has
/// no hook between Def loading and this method, so a patch it is. Cross-references are resolved by now, so loadouts read fully.
/// </summary>
/// <summary>
/// Counts a set-up survey truck as an active deep scanner, so placing a deep drill shows the ore it found. The game asks only about colony buildings, and has no other way in. A postfix; only ever turns false to true.
/// </summary>
[HarmonyPatch(typeof(DeepResourceGrid), nameof(DeepResourceGrid.AnyActiveDeepScannersOnMap))]
public static class SurveyTruckCountsAsScanner
{
    public static void Postfix(ref bool __result, Map ___map)
    {
        if (__result) return;
        foreach (Pawn pawn in ___map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is not VehiclePawn vehicle || vehicle.GetComp<CompPhobosSurveyTruck>() is not { Deployed: true }) continue;
            __result = true;
            return;
        }
    }
}

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
        // The first moment both the parts and the saved grades exist; a part's grade changes its maximum health, which is not saved.
        vehicle.GetComp<CompPhobosVehicleSystems>()?.Fittings.ReapplyGrades();
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
/// what it pays to decide when to brake, counts the capped figure (read from its 1.7 source, 8 Nov 2026).
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

/// <summary>
/// A destroyed part waits for supplies (vehicle health, slice 3): this list is what Vehicle Framework's repair job, its caravans and the
/// garage heal from, and it has no extension point. A postfix that takes the waiting parts out of a list made fresh for each call.
/// </summary>
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.ComponentsPrioritized), MethodType.Getter)]
public static class DestroyedPartsLeftForSupplies
{
    public static void Postfix(VehiclePawn ___vehicle, List<VehicleComponent> __result)
    {
        VehicleRepairs? repairs = ___vehicle?.GetComp<CompPhobosVehicleSystems>()?.Repairs;
        if (repairs != null) __result.RemoveAll(repairs.PartBlocked);
    }
}

/// <summary>
/// The watchdog on Vehicle Framework's own free repair (owner, 9 Nov 2026: a long repair and a stuck one look alike): each step reports the
/// vehicle's total health, and a mechanic whose steps stop raising it is stood down and told so. No extension point on the job; a postfix.
/// </summary>
[HarmonyPatch(typeof(JobDriver_RepairVehicle), "WorkComplete")]
public static class FreeRepairWatchdog
{
    public static void Postfix(JobDriver_RepairVehicle __instance, Pawn actor)
    {
        if (__instance.job?.targetA.Thing is not VehiclePawn vehicle) return;
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (comp == null || !comp.Notify_FreeRepairStep(actor) || actor.CurJob != __instance.job) return;
        actor.jobs.EndCurrentJob(Verse.AI.JobCondition.Incompletable);
    }
}

/// <summary>While the watchdog has repairs waiting, Vehicle Framework's work giver leaves the vehicle alone.</summary>
[HarmonyPatch(typeof(WorkGiver_RepairVehicle), nameof(WorkGiver_RepairVehicle.CanBeWorkedOn))]
public static class FreeRepairWaits
{
    public static void Postfix(VehiclePawn vehicle, ref bool __result)
    {
        if (__result && vehicle.GetComp<CompPhobosVehicleSystems>()?.RepairsPaused == true) __result = false;
    }
}

/// <summary>The same for the question "does it need repairs?", or mechanics would walk to a vehicle with nothing they could do.</summary>
[HarmonyPatch(typeof(VehicleStatHandler), nameof(VehicleStatHandler.NeedsRepairs), MethodType.Getter)]
public static class DestroyedPartsNeedNoFreeRepair
{
    public static void Postfix(VehicleStatHandler __instance, VehiclePawn ___vehicle, ref bool __result)
    {
        if (!__result) return;
        VehicleRepairs? repairs = ___vehicle?.GetComp<CompPhobosVehicleSystems>()?.Repairs;
        if (repairs == null) return;
        __result = __instance.components.Any(part => part.HealthPercent < 1f && !repairs.PartBlocked(part));
    }
}

/// <summary>
/// On every part bound to a role: tells the vehicle's systems the part was hit. What counts is the health the part lost, not the
/// shot's damage: Combat Extended gives reactors the full damage before armour, sometimes twice, even when the armour stopped the
/// round (read 8 Nov 2026), and Vehicle Framework may fire before or after the health is written, so the comp compares the part's
/// health with what it last saw, here and on its next update. The kind of damage is passed on, for part-hit rules that name kinds
/// (a radiator holed by shot, not scratched by an animal or Vehicle Framework's hand brakes).
/// </summary>
public class Reactor_PhobosVehicleSystems : Reactor
{
    public string role = "";

    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration)
    {
        vehicle.GetComp<CompPhobosVehicleSystems>()?.Notify_PartSeen(role, component, dinfo.Def);
    }
}

/// <summary>
/// The lift-off check (owner's hover-limit setting, 8 Nov 2026): an aircraft that lifts off through Vehicle Framework's own launch
/// (its stock helicopters and the drop pods) and whose engines cannot lift it now, too heavy, too hot or with engines out, is refused
/// with the reason when the player chose to refuse. Every launch path and the launch button ask this method, which already gives
/// the button its reason (read from Vehicle Framework 1.6, 8 Nov 2026); a postfix adds ours only when it would otherwise allow.
/// </summary>
[HarmonyPatch(typeof(CompVehicleLauncher), nameof(CompVehicleLauncher.CanLaunchWithCargoCapacity))]
public static class LiftOffPatch
{
    public static void Postfix(CompVehicleLauncher __instance, ref bool __result, ref string disableReason)
    {
        if (!__result || __instance.Vehicle is not VehiclePawn vehicle || !HoverLimits.RefusesLaunch(vehicle)) return;
        __result = false;
        disableReason = HoverLimits.LaunchRefusalText(vehicle);
    }
}

/// <summary>
/// A passenger's temperature comes from the cab (docs/development/vehicle-world-map-design.md): the game asks a held thing's
/// holders for a fixed temperature before it looks at the cell or the world tile (so a transport pod stays at 14 °C), and a
/// passenger's holder is the vehicle's role handler. The Framework's registry answers for holder types that registered a reader
/// (CabClimate registers VehicleRoleHandler); everything else falls through to the game. A postfix, so the game's own answers stand.
/// </summary>
[HarmonyPatch(typeof(ThingOwnerUtility), nameof(ThingOwnerUtility.TryGetFixedTemperature))]
public static class HeldTemperaturePatch
{
    public static void Postfix(IThingHolder holder, Thing forThing, ref float temperature, ref bool __result)
    {
        if (__result || holder == null) return;
        if (HeldTemperature.TryGet(holder, out float held))
        {
            temperature = held;
            __result = true;
        }
    }
}

/// <summary>
/// The drivetrain's speed cap on the world map: a caravan moves as its slowest vehicle, so a laden truck on mountains takes longer
/// per tile, and the caravan's own explanation says which vehicle holds it back. Vehicle Framework's utility has no extension point
/// for this; a postfix on the figure it returns.
/// </summary>
[HarmonyPatch(typeof(VehicleCaravanTicksPerMoveUtility), nameof(VehicleCaravanTicksPerMoveUtility.GetTicksPerMove),
    new[] { typeof(List<Pawn>), typeof(float), typeof(float), typeof(StringBuilder) })]
public static class CaravanSpeedCapPatch
{
    public static void Postfix(List<Pawn> pawns, StringBuilder explanation, ref int __result)
    {
        if (pawns == null || pawns.Count == 0) return;
        double worst = 1;
        CompPhobosVehicleSystems? slowest = null;
        foreach (Pawn pawn in pawns)
        {
            if (pawn is not VehiclePawn vehicle) continue;
            CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (comp == null || !comp.Usable) continue;
            float cap = comp.SpeedCap;
            if (cap < worst)
            {
                worst = cap;
                slowest = comp;
            }
        }
        if (slowest == null || worst >= 1) return;
        double combined = CaravanRules.CombineCaps(new[] { worst });
        __result = CaravanRules.TicksPerMove(__result, combined);
        explanation?.AppendLine().Append("PhobosVehicleSystems_CaravanCapExplanation".Translate(slowest.Vehicle.LabelShort.Named("VEHICLE"),
            ((float)combined).ToStringPercent().Named("CAP")).Resolve());
    }
}

/// <summary>
/// A vehicle that will not move (a flat battery, worn brakes, a cold boiler) stops its caravan too: Vehicle Framework asks each
/// vehicle's CanMoveFinal on the world map and halts the caravan when one says no (its VehicleCantMove). On a map the float menu
/// asks the comp's CanMove instead, so this applies only off-map; the reason is in the caravan's inspect text.
/// </summary>
[HarmonyPatch(typeof(VehiclePawn), nameof(VehiclePawn.CanMoveFinal), MethodType.Getter)]
public static class CaravanRefusalPatch
{
    public static void Postfix(VehiclePawn __instance, ref bool __result)
    {
        if (!__result || __instance.Spawned) return;
        CompPhobosVehicleSystems? comp = __instance.GetComp<CompPhobosVehicleSystems>();
        if (comp != null && comp.Usable && comp.MoveRefusal != null) __result = false;
    }
}

/// <summary>
/// How fast an electric vehicle charges from the grid follows its battery's state: a cold battery takes charge slowly (charging a
/// frozen lithium battery fast damages it), a hot one too. Vehicle Framework draws ChargeRate from the grid's stored energy and adds
/// the same to the vehicle each time it charges, so scaling the getter keeps both sides equal. No extension point: the rate is a
/// protected property of CompFueledTravel. Postfix only.
/// </summary>
[HarmonyPatch(typeof(CompFueledTravel), "ChargeRate", MethodType.Getter)]
public static class ChargeRatePatch
{
    public static void Postfix(CompFueledTravel __instance, ref float __result)
    {
        CompPhobosVehicleSystems? comp = __instance.Vehicle?.GetComp<CompPhobosVehicleSystems>();
        if (comp != null && comp.Usable) __result *= comp.ChargeFactor;
    }
}
