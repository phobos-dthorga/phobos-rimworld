using Phobos.Framework.Interface;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

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
    /// <summary>The helicopter is coming down in a crash with its load (Vehicle Framework's crash event; SlingPatches): when it is next on a
    /// map, the load falls rather than being set down. Saved, since the crash lands a few seconds after the event.</summary>
    bool crashing;
    /// <summary>The helicopter has just been put on a map with its load still on the hook; the load is dealt with at the helicopter's next
    /// tick, never inside its own spawning (see PostSpawnSetup). Saved, in case the game is saved between the two.</summary>
    bool landed;
    /// <summary>The flight the picture's ground spot was found for, and that spot (SlingDrawing's, kept here so two slung flights never share one).</summary>
    internal int pictureFlight = -1;
    internal Vector3 pictureGround;

    public CompPhobosSling() => held = new ThingOwner<VehiclePawn>(this, oneStackOnly: true);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    /// <summary>Ticked only while a landing waits to be dealt with (CompTick), so a sling costs nothing each tick otherwise.</summary>
    public override bool TickByRequest => true;

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

    /// <summary>Whether a cargo hook is fitted: what a sling load and a water bucket both hang from.</summary>
    public bool HookFitted => Systems?.Fittings.Grants(HookAbility) == true;

    /// <summary>Why this helicopter cannot lift anything now, or null.</summary>
    public string? Refusal()
    {
        if (!HookFitted) return "PhobosSling_NoHook".Translate().Resolve();
        if (!HasSlingSet) return "PhobosSling_NoSlingSet".Translate().Resolve();
        if (Carried != null) return "PhobosSling_AlreadyCarrying".Translate(Carried.LabelShort.Named("LOAD")).Resolve();
        return WeatherRefusal(Vehicle.Map);
    }

    /// <summary>Why the weather grounds a lift on this map now, or null (sling slice 2, with the player's setting): a storm with lightning, or
    /// wind above the rules' limit.</summary>
    public static string? WeatherRefusal(Map? map)
    {
        if (map == null || !Settings.slingWeather) return null;
        float wind = map.windManager.WindSpeed;
        bool storm = IsStorm(map.weatherManager.curWeather);
        if (!SlingRule.Grounded(wind, storm, Rules.slingMaxWind)) return null;
        return storm
            ? "PhobosSling_Storm".Translate().Resolve()
            : "PhobosSling_TooWindy".Translate(wind.ToStringPercent().Named("WIND"), Rules.slingMaxWind.ToStringPercent().Named("LIMIT")).Resolve();
    }

    /// <summary>A storm, read from what the weather does (it brings lightning), never from its name.</summary>
    static bool IsStorm(WeatherDef? weather)
    {
        if (weather?.eventMakers == null) return false;
        foreach (WeatherEventMaker maker in weather.eventMakers)
        {
            System.Type? kind = maker.eventClass;
            if (kind != null && (typeof(WeatherEvent_LightningStrike).IsAssignableFrom(kind) || typeof(WeatherEvent_LightningFlash).IsAssignableFrom(kind))) return true;
        }
        return false;
    }

    /// <summary>Whether a slung flight's cost applies to this helicopter now: a load ready on the hook or hanging from it, with the setting on.
    /// Vehicle Framework's flight speed and fuel use ask (SlingPatches), so the range drawn when choosing a destination shows it too.</summary>
    public bool SlungFlightNow => Settings.slingSlowFlight && (Carried != null || (hooked != null && !awaitingCrew));

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
        // A vehicle getting ready to leave in a caravan belongs to that group's orders; lifted off the map, the group would go on ordering
        // a vehicle that is not there.
        if (load.GetLord() != null) return "PhobosSling_UnderOrders".Translate(load.LabelShort.Named("LOAD")).Resolve();
        // A vehicle towing or towed (the hitch) would leave its partner on the map, or take it along unseen: unhitch first.
        if (load.GetComp<CompPhobosHitch>() is { Trailer: not null } || Hitches.Towing(load) != null) return "PhobosSling_Hitched".Translate(load.LabelShort.Named("LOAD")).Resolve();
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
        return WeatherRefusal(Vehicle.Map) ?? RefusalFor(hooked);
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
        if (!awaitingCrew) Latch(load);
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
        Latch(hooked);
        Say("PhobosSling_HookedUp".Translate(crew.LabelShort.Named("CREW"), hooked.LabelShort.Named("LOAD"), Vehicle.LabelShort.Named("HELICOPTER")).Resolve(),
            new LookTargets(Vehicle, hooked), MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>The hook's latch closing on the load (S128, sling slice 3), heard where the load stands. Not while the self-test runs.</summary>
    static void Latch(VehiclePawn load)
    {
        if (!VehicleNotices.Quiet && load.Spawned) VehicleSystemsThingDefOf.PhobosVehicleSystems_HookLatch.PlayOneShot(new TargetInfo(load.Position, load.Map));
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
        // A helicopter that flies on the map by itself (the Hind) may set off for another tile from wherever it is, even in the air far from
        // its load; Vehicle Framework's own aircraft always leave from where they stand, beside it.
        if (OnMapFlight.FliesItself(Vehicle) && Vehicle.Spawned
            && (load.Map != Vehicle.Map || load.Position.DistanceTo(Vehicle.Position) > Rules.slingReachCells))
        {
            Say("PhobosSling_LeftBehindFar".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), load,
                MessageTypeDefOf.NegativeEvent);
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

    /// <summary>
    /// The helicopter has landed (or come out of a caravan) on a map with its load. Nothing is spawned here: the game calls this from inside
    /// the helicopter's own spawning, before it has given the helicopter its movement parts (Pawn.SpawnSetup adds them after its comps have
    /// run), and a pawn spawned beside a pawn in that state makes the game's standing-room check throw. That left both vehicles half on the
    /// map and broke the map's drawing (the owner's landings of 12 Nov 2026, Framework 0.100.0). So this only notes the landing, and the
    /// helicopter's next tick deals with the load.
    /// </summary>
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (Carried == null)
        {
            landed = false;
            return;
        }
        if (!respawningAfterLoad) landed = true;
        if (landed) Vehicle.RequestTickStart(this);
    }

    /// <summary>Runs only while a landing waits to be dealt with (Vehicle Framework ticks this comp on request). A helicopter that flies on
    /// the map by itself (the Hind) arrives in the air at the map's edge and flies in to land: the load waits on the hook until it is down.</summary>
    public override void CompTick()
    {
        base.CompTick();
        if (landed && !Vehicle.Spawned) return;
        if (landed && OnMapFlight.Airborne(Vehicle)) return;
        Vehicle.RequestTickStop(this);
        if (!landed) return;
        landed = false;
        try
        {
            AfterLanding();
        }
        catch (System.Exception e)
        {
            // The load stays on the hook or where it was put; the player still has "Set the load down".
            VehicleSystemsMod.Logger.Error($"A slung load could not be dealt with after {Vehicle.LabelShort} landed: {e}");
        }
    }

    /// <summary>After a crash the load falls beside the helicopter; on the map the player is looking at, the game pauses and asks where to
    /// set the load down (sling slice 2); anywhere else, or if the player declines, it is set down on the nearest clear ground.</summary>
    void AfterLanding()
    {
        if (Carried == null) return;
        if (crashing)
        {
            crashing = false;
            DropOnCrash();
        }
        else if (ShouldAsk()) AskWhereToSetDown();
        else SetDown();
    }

    /// <summary>
    /// Takes the load off the hook and puts it on the map. If the game refuses partway, a load that never reached the map goes back on the
    /// hook, so it is never left in neither place; the player is told either way.
    /// </summary>
    bool Place(VehiclePawn load, IntVec3 cell, Map map, Rot4 facing)
    {
        held.Remove(load);
        try
        {
            GenSpawn.Spawn(load, cell, map, facing);
        }
        catch (System.Exception e)
        {
            VehicleSystemsMod.Logger.Error($"Setting {load.LabelShort} down from {Vehicle.LabelShort} at {cell} failed: {e}");
            if (!load.Spawned && !load.Destroyed && !held.Contains(load)) held.TryAdd(load, canMergeWithExistingStacks: false);
            Say("PhobosSling_SetDownFailed".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return false;
        }
        liftedFrom = null;
        return true;
    }

    bool ShouldAsk() => Vehicle.Faction == Faction.OfPlayer && Vehicle.Map != null && Vehicle.Map == Find.CurrentMap && !VehicleNotices.Quiet
                        && Current.ProgramState == ProgramState.Playing;

    /// <summary>Vehicle Framework's crash event: the helicopter comes down with its load. With release and drops on, the load falls when the
    /// helicopter next reaches a map; off, it is set down as after any landing.</summary>
    internal void NoteCrash()
    {
        if (Carried != null && Settings.slingRelease) crashing = true;
    }

    /// <summary>Pauses and lets the player place the load within reach of the helicopter, its outline drawn under the mouse. Declined, the
    /// load is set down on the nearest clear ground, as before slice 2.</summary>
    void AskWhereToSetDown()
    {
        VehiclePawn? load = Carried;
        Map map = Vehicle.Map;
        if (load == null || map == null) return;
        Find.TickManager.Pause();
        Say("PhobosSling_ChooseSpot".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NeutralEvent, historical: false);
        Rot4 facing = Vehicle.Rotation;
        Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false },
            target => SetDownAt(target.Cell, facing),
            target =>
            {
                if (!target.Cell.IsValid) return;
                GenDraw.DrawFieldEdges(GenAdj.OccupiedRect(target.Cell, facing, load.VehicleDef.Size).Cells.ToList(),
                    SpotRefusal(target.Cell, facing) == null ? Color.white : Color.red);
            },
            target => target.Cell.IsValid && SpotRefusal(target.Cell, facing) == null,
            actionWhenFinished: () =>
            {
                // Declined (right-click or Escape): the load comes down where it would have before.
                if (Carried != null && Vehicle.Spawned) SetDown();
            });
    }

    /// <summary>Why the load cannot be set down with its corner cell here, or null.</summary>
    string? SpotRefusal(IntVec3 cell, Rot4 facing)
    {
        VehiclePawn? load = Carried;
        Map map = Vehicle.Map;
        if (load == null || map == null) return "PhobosSling_NothingToSetDown".Translate().Resolve();
        if (cell.DistanceTo(Vehicle.Position) > Rules.slingReachCells) return "PhobosSling_SpotTooFar".Translate(Rules.slingReachCells.Named("CELLS")).Resolve();
        return SlingPlacement.Fits(load.VehicleDef, facing, map, cell, Vehicle.OccupiedRect()) ? null : "PhobosSling_SpotBlocked".Translate().Resolve();
    }

    /// <summary>Sets the load down where the player chose, if it still fits there; otherwise on the nearest clear ground.</summary>
    internal void SetDownAt(IntVec3 cell, Rot4 facing)
    {
        VehiclePawn? load = Carried;
        Map map = Vehicle.Map;
        if (load == null || map == null) return;
        if (SpotRefusal(cell, facing) is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            SetDown();
            return;
        }
        if (!Place(load, cell, map, facing)) return;
        Say("PhobosSling_SetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>The helicopter came down in a crash: the load falls on the nearest clear ground beside it and lands hard.</summary>
    void DropOnCrash()
    {
        VehiclePawn? load = Carried;
        Map map = Vehicle.Map;
        if (load == null || map == null) return;
        if (!SlingPlacement.TryFindSpot(load, Vehicle, map, Rules.slingReachCells, out IntVec3 cell))
        {
            // Nowhere for it to fall clear: it stays on the hook, and the player can set it down later (the gizmo below).
            Say("PhobosSling_NoSpot".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return;
        }
        if (!Place(load, cell, map, Vehicle.Rotation)) return;
        string harm = SlingDrops.Fall(load, Rules.slingCrashDropMetres);
        Say("PhobosSling_FellInCrash".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"), harm.Named("HARM")).Resolve(), load, MessageTypeDefOf.NegativeEvent);
    }

    /// <summary>Why the crew cannot let the load go in flight on the world map now, or null.</summary>
    public string? ReleaseRefusal()
    {
        if (!Settings.slingRelease) return "PhobosSling_ReleaseOff".Translate().Resolve();
        if (Carried == null) return "PhobosSling_NothingToRelease".Translate().Resolve();
        return null;
    }

    /// <summary>
    /// Emergency release over the world map (sling slice 2; owner, 12 Nov 2026: the load becomes a wreck a caravan can recover). The load falls
    /// from cruise onto the nearest tile with nothing else on it, and lies there as a wreck, damaged when a caravan first reaches it.
    /// </summary>
    public void ReleaseInFlight(PlanetTile below)
    {
        if (ReleaseRefusal() is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        VehiclePawn load = Carried!;
        PlanetTile tile = PhobosSlingWreck.FreeTileNear(below);
        if (!tile.Valid)
        {
            Say("PhobosSling_ReleaseNoTile".Translate(load.LabelShort.Named("LOAD")).Resolve(), MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        held.Remove(load);
        liftedFrom = null;
        PhobosSlingWreck wreck = PhobosSlingWreck.Make(tile, load, Rules.slingReleaseMetres);
        Say("PhobosSling_Released".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(),
            new LookTargets(wreck), MessageTypeDefOf.NegativeEvent);
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
        if (!Place(load, cell, map, Vehicle.Rotation)) return;
        Say("PhobosSling_SetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>The load hanging under a helicopter that flies on the map by itself, while it is in the air (Vehicle Framework draws its own
    /// aircraft in a stand-in instead: SlingDrawing.Draw). Nothing for a helicopter with nothing on the hook.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (Carried is not { } load || !Vehicle.Spawned || !OnMapFlight.FliesItself(Vehicle) || !OnMapFlight.Airborne(Vehicle)) return;
        SlingDrawing.DrawUnderFlying(Vehicle, load, liftedFrom);
    }

    /// <summary>A helicopter that flies on the map by itself leaves the map with its load at the edge: where the load stood on that map no
    /// longer means anything for the picture.</summary>
    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (OnMapFlight.FliesItself(Vehicle)) liftedFrom = null;
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
        Scribe_Values.Look(ref crashing, "PhobosSling_Crashing");
        Scribe_Values.Look(ref landed, "PhobosSling_Landed");
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
            var setDown = new Command_Action
            {
                defaultLabel = "PhobosSling_SetDownGizmo".Translate(),
                defaultDesc = "PhobosSling_SetDownGizmoDesc".Translate(carried.LabelShort.Named("LOAD")),
                icon = SystemIcons.For("Phobos/VehicleSystems/Sling/SetDown"),
                action = SetDown,
            };
            if (OnMapFlight.Airborne(Vehicle)) setDown.Disable("PhobosSling_StillFlying".Translate(Vehicle.LabelShort.Named("HELICOPTER")).Resolve());
            yield return setDown;
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
        // The water bucket (Framework 0.101.0) hangs from the same hook: offered once a hook is fitted, and says why when it cannot go.
        if (HookFitted) yield return WaterDrops.Gizmo(this);
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
        string? grounded = WeatherRefusal(Vehicle.Map);
        double weight = LoadKilograms(hooked), capacity = CapacityKilograms(Vehicle.Map);
        string line = "PhobosSling_InspectHooked".Translate(hooked.LabelShort.Named("LOAD"), weight.ToString("N0").Named("WEIGHT"),
            capacity.ToString("N0").Named("CAPACITY"), ((float)SlingRule.Share(weight, capacity)).ToStringPercent().Named("SHARE")).Resolve();
        if (awaitingCrew) line += "\n" + "PhobosSling_InspectAwaitingCrew".Translate().Resolve();
        if (grounded != null) line += "\n" + grounded;
        return line;
    }
}

[DefOf]
public static class VehicleSystemsThingDefOf
{
    public static ThingDef PhobosVehicleSystems_SlingSet = null!;
    public static SoundDef PhobosVehicleSystems_HookLatch = null!;

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
        int count = GenRadial.NumCellsInRadius(System.Math.Min(radius, GenRadial.MaxRadialPatternRadius - 1f));
        for (int i = 0; i < count; i++)
        {
            IntVec3 cell = centre + GenRadial.RadialPattern[i];
            if (!Fits(design, rotation, map, cell, avoid)) continue;
            found = cell;
            return true;
        }
        found = IntVec3.Invalid;
        return false;
    }

    /// <summary>Whether a vehicle of this design fits with its corner here: open, unroofed, standable ground with nobody and nothing solid in
    /// the way, clear of <paramref name="avoid"/>.</summary>
    public static bool Fits(VehicleDef design, Rot4 rotation, Map map, IntVec3 cell, CellRect avoid)
    {
        CellRect rect = GenAdj.OccupiedRect(cell, rotation, design.Size);
        if (!rect.InBounds(map) || rect.Overlaps(avoid)) return false;
        foreach (IntVec3 c in rect)
        {
            if (!c.Standable(map) || c.Roofed(map) || c.GetThingList(map).Any(t => t is Pawn || t.def.passability == Traversability.Impassable)) return false;
        }
        return true;
    }
}
