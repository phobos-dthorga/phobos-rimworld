using Phobos.Framework.Interface;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The loadout role a cargo hook is fitted to.</summary>
public static class SlingRoles
{
    public const string HookMount = "HookMount";
}

public sealed class CompProperties_PhobosSling : CompProperties
{
    public CompProperties_PhobosSling() => compClass = typeof(CompPhobosSling);
}

/// <summary>
/// A helicopter's sling (Framework 0.92.0, slice 1 of sling loads; docs/development/vehicle-sling-design.md). With a cargo hook fitted and
/// a sling set aboard, the player picks a vehicle parked nearby; a colonist hooks it up (or the helicopter does, with the ground crew
/// setting off); when the helicopter launches through Vehicle Framework's own flight, the load leaves the map with it, held in this comp;
/// when it lands anywhere, the load is set down beside it. Added to a vehicle whose loadout names a hook rating.
/// </summary>
public sealed class CompPhobosSling : VehicleComp, IThingHolder
{
    /// <summary>The ability a fitting grants for this to work.</summary>
    public const string HookAbility = "CargoHook";

    ThingOwner<VehiclePawn> held;
    VehiclePawn? hooked;
    bool awaitingCrew;
    bool registered;
    Vector3? liftedFrom;
    /// <summary>The flight the picture's ground spot was found for, and that spot (SlingDrawing's, kept here so two slung flights never share one).</summary>
    internal int pictureFlight = -1;
    internal Vector3 pictureGround;

    public CompPhobosSling() => held = new ThingOwner<VehiclePawn>(this, oneStackOnly: true);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    /// <summary>The vehicle hanging from the hook in flight, or waiting to be set down.</summary>
    public VehiclePawn? Carried => held.Count > 0 ? held[0] : null;
    /// <summary>The vehicle chosen to lift, still on the ground.</summary>
    public VehiclePawn? Hooked => hooked;
    /// <summary>Whether the chosen load waits for a colonist to hook it up.</summary>
    public bool AwaitingCrew => hooked != null && awaitingCrew;
    /// <summary>Where the load stood when it was lifted, for the picture of the strops taking it up (SlingDrawing). Not saved: a game
    /// loaded during the climb draws the load already hanging.</summary>
    public Vector3? LiftedFrom => liftedFrom;

    // ---- The rules ----

    public float RatedKilograms => Systems?.Props.loadout?.sling?.ratedKilograms ?? 0f;

    /// <summary>What the hook lifts now: the rated load, less on a hot day (SlingRule, with the player's setting).</summary>
    public double CapacityKilograms(Map? map)
    {
        PhobosVehicleRulesDef rules = Rules;
        float celsius = map?.mapTemperature.OutdoorTemp ?? 15f;
        return SlingRule.Capacity(RatedKilograms, celsius, Settings.slingHeatDerating, rules.slingDerateFromCelsius, rules.slingLossPerDegree, rules.slingMaxLoss);
    }

    /// <summary>A load's weight: the vehicle empty (its kerb mass, as its repairs and drive use), its cargo, the people aboard and a tank's load.</summary>
    public static double LoadKilograms(VehiclePawn load)
    {
        CompPhobosVehicleSystems? comp = load.GetComp<CompPhobosVehicleSystems>();
        double kilograms = comp != null ? comp.Repairs.Kilograms : Powertrain.KerbFor(load.VehicleDef, null, null);
        kilograms += MassUtility.InventoryMass(load);
        foreach (Pawn pawn in load.AllPawnsAboard) kilograms += pawn.GetStatValue(StatDefOf.Mass);
        kilograms += load.GetComp<CompPhobosTank>()?.LoadKilograms ?? 0f;
        return kilograms;
    }

    bool HasSlingSet => Vehicle.inventory?.innerContainer.Any(t => t.def == VehicleSystemsThingDefOf.PhobosVehicleSystems_SlingSet) == true;

    /// <summary>Why this helicopter cannot lift anything now, or null.</summary>
    public string? Refusal()
    {
        if (Systems?.Fittings.Grants(HookAbility) != true) return "PhobosSling_NoHook".Translate().Resolve();
        if (!HasSlingSet) return "PhobosSling_NoSlingSet".Translate().Resolve();
        if (Carried != null) return "PhobosSling_AlreadyCarrying".Translate(Carried.LabelShort.Named("LOAD")).Resolve();
        return null;
    }

    /// <summary>Why this vehicle cannot be lifted by this helicopter now, or null.</summary>
    public string? RefusalFor(VehiclePawn load)
    {
        if (load == Vehicle) return "PhobosSling_NotItself".Translate().Resolve();
        if (load.Faction != Faction.OfPlayer) return "PhobosSling_NotOurs".Translate(load.LabelShort.Named("LOAD")).Resolve();
        if (load.GetComp<CompVehicleLauncher>() != null) return "PhobosSling_NoAircraft".Translate(load.LabelShort.Named("LOAD")).Resolve();
        if (!load.Spawned || load.Map != Vehicle.Map) return "PhobosSling_NotHere".Translate(load.LabelShort.Named("LOAD")).Resolve();
        if (load.Position.DistanceTo(Vehicle.Position) > Rules.slingReachCells)
        {
            return "PhobosSling_TooFar".Translate(load.LabelShort.Named("LOAD"), Rules.slingReachCells.Named("CELLS")).Resolve();
        }
        if (load.OccupiedRect().Any(cell => cell.Roofed(load.Map))) return "PhobosSling_Roofed".Translate(load.LabelShort.Named("LOAD")).Resolve();
        if (load.vehiclePather != null && load.vehiclePather.Moving) return "PhobosSling_Moving".Translate(load.LabelShort.Named("LOAD")).Resolve();
        double weight = LoadKilograms(load), capacity = CapacityKilograms(Vehicle.Map);
        if (!SlingRule.CanLift(weight, capacity))
        {
            return "PhobosSling_TooHeavy".Translate(load.LabelShort.Named("LOAD"), weight.ToString("N0").Named("WEIGHT"), capacity.ToString("N0").Named("CAPACITY")).Resolve();
        }
        return null;
    }

    /// <summary>Why the helicopter cannot take off with its chosen load, or null (Vehicle Framework's launch check asks; SlingPatches).</summary>
    public string? LaunchRefusal()
    {
        if (hooked == null) return null;
        if (awaitingCrew) return "PhobosSling_LaunchWaitsForCrew".Translate(hooked.LabelShort.Named("LOAD")).Resolve();
        return RefusalFor(hooked);
    }

    // ---- What the player orders ----

    public void Choose(VehiclePawn load)
    {
        if ((Refusal() ?? RefusalFor(load)) is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        hooked = load;
        awaitingCrew = Settings.slingGroundCrew;
        SlingWaits.Note(this, awaitingCrew);
        Say(awaitingCrew
            ? "PhobosSling_ChosenCrew".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve()
            : "PhobosSling_ChosenReady".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(),
            new LookTargets(Vehicle, load), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    public void Cancel()
    {
        if (hooked == null) return;
        Say("PhobosSling_Cancelled".Translate(hooked.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        hooked = null;
        awaitingCrew = false;
        SlingWaits.Note(this, false);
    }

    /// <summary>A colonist has hooked the load up (the hook-up job).</summary>
    public void HookedUp(Pawn crew)
    {
        if (hooked == null) return;
        awaitingCrew = false;
        SlingWaits.Note(this, false);
        Say("PhobosSling_HookedUp".Translate(crew.LabelShort.Named("CREW"), hooked.LabelShort.Named("LOAD"), Vehicle.LabelShort.Named("HELICOPTER")).Resolve(),
            new LookTargets(Vehicle, hooked), MessageTypeDefOf.TaskCompletion);
    }

    // ---- Flight ----

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.AerialVehicleLaunch, OnLaunch);
    }

    /// <summary>Messages go through here so the self-test can run a lift without telling the player about it.</summary>
    static void Say(string text, MessageTypeDef type, bool historical = true)
    {
        if (!VehicleNotices.Quiet) Messages.Message(text, type, historical);
    }

    static void Say(string text, LookTargets where, MessageTypeDef type, bool historical = true)
    {
        if (!VehicleNotices.Quiet) Messages.Message(text, where, type, historical);
    }

    /// <summary>The helicopter has lifted off through Vehicle Framework's own launch: the load goes with it.</summary>
    void OnLaunch() => TakeLoad();

    /// <summary>Takes the chosen load off the map and onto the hook, or leaves it behind if it was never hooked up. The launch event calls
    /// this; so does the self-test, without a launch.</summary>
    internal void TakeLoad()
    {
        VehiclePawn? load = hooked;
        hooked = null;
        SlingWaits.Note(this, false);
        if (load == null) return;
        if (awaitingCrew || !load.Spawned)
        {
            awaitingCrew = false;
            Say("PhobosSling_LeftBehind".Translate(load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.NegativeEvent);
            return;
        }
        Map map = load.Map;
        IntVec3 cell = load.Position;
        Rot4 facing = load.Rotation;
        Vector3 stood = ((Thing)load).DrawPos;
        load.vehiclePather?.StopDead();
        load.DeSpawn();
        if (!held.TryAdd(load, canMergeWithExistingStacks: false))
        {
            // A vehicle is never lost to a hook that would not take it: it goes back where it stood, and the player is told.
            GenSpawn.Spawn(load, cell, map, facing);
            Say("PhobosSling_CouldNotLift".Translate(load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.NegativeEvent);
            return;
        }
        liftedFrom = stood;
        Say("PhobosSling_Lifted".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"),
            LoadKilograms(load).ToString("N0").Named("WEIGHT")).Resolve(), MessageTypeDefOf.PositiveEvent);
    }

    /// <summary>The helicopter has landed (or come out of a caravan) on a map: the load is set down beside it.</summary>
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (!respawningAfterLoad && Carried != null) SetDown();
    }

    internal void SetDown()
    {
        VehiclePawn? load = Carried;
        Map map = Vehicle.Map;
        if (load == null || map == null) return;
        if (!SlingPlacement.TryFindSpot(load, Vehicle, map, Rules.slingReachCells, out IntVec3 cell))
        {
            Say("PhobosSling_NoSpot".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return;
        }
        held.Remove(load);
        liftedFrom = null;
        GenSpawn.Spawn(load, cell, map, Vehicle.Rotation);
        Say("PhobosSling_SetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>The helicopter is destroyed: on a map, what hung from it falls where it was; elsewhere it is lost with it.</summary>
    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        VehiclePawn? load = Carried;
        if (load == null) return;
        held.Remove(load);
        if (previousMap != null && SlingPlacement.TryFindSpot(load, Vehicle, previousMap, Rules.slingReachCells, out IntVec3 cell))
        {
            GenSpawn.Spawn(load, cell, previousMap, Rot4.North);
        }
        else if (!load.Destroyed)
        {
            load.Destroy();
        }
    }

    // ---- Holding ----

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref held, "PhobosSling_Held", this);
        Scribe_References.Look(ref hooked, "PhobosSling_Hooked");
        Scribe_Values.Look(ref awaitingCrew, "PhobosSling_AwaitingCrew");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            held ??= new ThingOwner<VehiclePawn>(this, oneStackOnly: true);
            if (AwaitingCrew) SlingWaits.Note(this, true);
        }
    }

    // ---- What the player sees ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !Vehicle.Spawned) yield break;
        if (hooked != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "PhobosSling_CancelGizmo".Translate(),
                defaultDesc = "PhobosSling_CancelGizmoDesc".Translate(hooked.LabelShort.Named("LOAD")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Sling/SetDown"),
                action = Cancel,
            };
            yield break;
        }
        if (Carried is { } carried)
        {
            // Landed with the load still on the hook (no clear ground at the time): the player sets it down when there is room.
            yield return new Command_Action
            {
                defaultLabel = "PhobosSling_SetDownGizmo".Translate(),
                defaultDesc = "PhobosSling_SetDownGizmoDesc".Translate(carried.LabelShort.Named("LOAD")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Sling/SetDown"),
                action = SetDown,
            };
            yield break;
        }
        var lift = new Command_Action
        {
            defaultLabel = "PhobosSling_LiftGizmo".Translate(),
            defaultDesc = "PhobosSling_LiftGizmoDesc".Translate(CapacityKilograms(Vehicle.Map).ToString("N0").Named("CAPACITY"), Rules.slingReachCells.Named("CELLS")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/Lift"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                validator = target => target.Thing is VehiclePawn,
            }, target =>
            {
                if (target.Thing is VehiclePawn load) Choose(load);
            }),
        };
        if (Refusal() is { } refusal) lift.Disable(refusal);
        yield return lift;
    }

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try { return InspectLine(); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A helicopter's sling line";

    string? InspectLine()
    {
        if (Carried is { } carried) return "PhobosSling_InspectCarrying".Translate(carried.LabelShort.Named("LOAD")).Resolve();
        if (hooked == null) return null;
        double weight = LoadKilograms(hooked), capacity = CapacityKilograms(Vehicle.Map);
        string line = "PhobosSling_InspectHooked".Translate(hooked.LabelShort.Named("LOAD"), weight.ToString("N0").Named("WEIGHT"),
            capacity.ToString("N0").Named("CAPACITY"), ((float)SlingRule.Share(weight, capacity)).ToStringPercent().Named("SHARE")).Resolve();
        if (awaitingCrew) line += "\n" + "PhobosSling_InspectAwaitingCrew".Translate().Resolve();
        return line;
    }
}

[DefOf]
public static class VehicleSystemsThingDefOf
{
    public static ThingDef PhobosVehicleSystems_SlingSet = null!;

    static VehicleSystemsThingDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleSystemsThingDefOf));
}

/// <summary>Where a slung load comes down: the nearest open ground beside the helicopter that the whole vehicle fits on, outdoors.</summary>
public static class SlingPlacement
{
    public static bool TryFindSpot(VehiclePawn load, VehiclePawn helicopter, Map map, float radius, out IntVec3 found) =>
        TryFindClear(load.VehicleDef, helicopter.Spawned ? helicopter.Rotation : Rot4.North, map, helicopter.Spawned ? helicopter.Position : load.Position, radius,
            helicopter.Spawned ? helicopter.OccupiedRect() : CellRect.Empty, out found);

    /// <summary>The nearest cell to <paramref name="centre"/> where a vehicle of this design fits on open, unroofed, standable ground with
    /// nobody and nothing solid in the way, clear of <paramref name="avoid"/>.</summary>
    public static bool TryFindClear(VehicleDef design, Rot4 rotation, Map map, IntVec3 centre, float radius, CellRect avoid, out IntVec3 found)
    {
        int count = GenRadial.NumCellsInRadius(radius);
        for (int i = 0; i < count; i++)
        {
            IntVec3 cell = centre + GenRadial.RadialPattern[i];
            CellRect rect = GenAdj.OccupiedRect(cell, rotation, design.Size);
            if (!rect.InBounds(map) || rect.Overlaps(avoid)) continue;
            bool clear = true;
            foreach (IntVec3 c in rect)
            {
                if (!c.Standable(map) || c.Roofed(map) || c.GetThingList(map).Any(t => t is Pawn || t.def.passability == Traversability.Impassable))
                {
                    clear = false;
                    break;
                }
            }
            if (!clear) continue;
            found = cell;
            return true;
        }
        found = IntVec3.Invalid;
        return false;
    }
}
