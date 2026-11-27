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
public sealed class CompPhobosSling : VehicleComp, IThingHolder, IKeepsTicking
{
    /// <summary>The ability a fitting grants for this to work.</summary>
    public const string HookAbility = "CargoHook";

    ThingOwner<Thing> held;
    Thing? hooked;
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

    public CompPhobosSling() => held = new ThingOwner<Thing>(this, oneStackOnly: true);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    /// <summary>Ticked only while a landing waits to be dealt with (CompTick), so a sling costs nothing each tick otherwise.</summary>
    public override bool TickByRequest => true;

    /// <summary>What hangs from the hook in flight, or waits to be set down: a vehicle, or since Framework 0.197.0 a destroyed vehicle's
    /// hull (docs/development/vehicle-sling-design.md, "Hulls on the hook").</summary>
    public Thing? Load => held.Count > 0 ? held[0] : null;
    /// <summary>The vehicle hanging from the hook, or null when nothing hangs there or a hull does.</summary>
    public VehiclePawn? Carried => Load as VehiclePawn;
    /// <summary>The hull hanging from the hook, or null.</summary>
    public VehicleBuilding? CarriedHull => Load as VehicleBuilding;
    /// <summary>What was chosen to lift, still on the ground.</summary>
    public Thing? HookedLoad => hooked;
    /// <summary>The vehicle chosen to lift, still on the ground; null when a hull was chosen.</summary>
    public VehiclePawn? Hooked => hooked as VehiclePawn;
    /// <summary>Whether the chosen load waits for a colonist to hook it up.</summary>
    public bool AwaitingCrew => hooked != null && awaitingCrew;
    /// <summary>Where the load stood when it was lifted, for the picture of the strops taking it up (SlingDrawing). Not saved: a game
    /// loaded during the climb draws the load already hanging.</summary>
    public Vector3? LiftedFrom => liftedFrom;

    // ---- The rules ----

    /// <summary>The hook's rated load: the loadout's, or more where an upgrade raises it (Framework 0.150.0: the Osprey's second hook).</summary>
    public float RatedKilograms => System.Math.Max(Systems?.Props.loadout?.sling?.ratedKilograms ?? 0f,
                                                   Vehicle.GetStatValue(AircraftUpgradeDefOf.Phobos_SlingKilograms));

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
        // Framework 0.182.0: every carried load, not only a tank's.
        kilograms += CarriedLoads.Kilograms(load);
        return kilograms;
    }

    /// <summary>Whether this is a destroyed vehicle's hull, which the sling can lift (Framework 0.197.0, EM21(b)).</summary>
    public static bool IsHull(Thing? thing) => thing is VehicleBuilding { VehicleDef: not null } hull && hull.GetComp<CompPhobosHull>() != null;

    /// <summary>
    /// Whether this is a thing the sling lifts besides vehicles and hulls (Framework 0.335.0; docs/development/vehicle-emergent-play.md, EM21(b);
    /// the owner's card of 27 Nov 2026): any item (a packed building, a stack, a corpse), or a cargo net with what lies in it. Never a pawn.
    /// </summary>
    public static bool IsLiftableThing(Thing? thing) => thing is Building_PhobosCargoNet || thing is { def.category: ThingCategory.Item } and not Pawn;

    /// <summary>The design whose size a load has: the vehicle's, or that of the vehicle a hull was.</summary>
    static VehicleDef? DesignOf(Thing load) => load is VehiclePawn vehicle ? vehicle.VehicleDef : (load as VehicleBuilding)?.VehicleDef;

    /// <summary>Any load's weight: a vehicle's as above; a hull's is what its vehicle weighed empty (authored: a wreck keeps its steel).</summary>
    public static double KilogramsOf(Thing load) => load switch
    {
        VehiclePawn vehicle => LoadKilograms(vehicle),
        VehicleBuilding { VehicleDef: { } design } => Powertrain.KerbFor(design, null, null),
        // Framework 0.335.0: a net weighs itself and what it holds; a stack weighs every piece (a packed building weighs as the building).
        Building_PhobosCargoNet net => net.Kilograms,
        _ => load.GetStatValue(StatDefOf.Mass) * load.stackCount,
    };

    bool HasSlingSet => Vehicle.inventory?.innerContainer.Any(t => t.def == VehicleSystemsThingDefOf.PhobosVehicleSystems_SlingSet) == true;

    /// <summary>Whether it still needs its tick (fault-read W11): a landing waits to be dealt with, or a lift or set-down from a hover.</summary>
    public bool WantsTicks => landed || NeedsOwnThinking;

    /// <summary>Whether a cargo hook is fitted: what a sling load and a water bucket both hang from.</summary>
    public bool HookFitted => Systems?.Fittings.Grants(HookAbility) == true;

    /// <summary>Why this helicopter cannot lift anything now, or null.</summary>
    public string? Refusal()
    {
        if (!HookFitted) return "PhobosSling_NoHook".Translate().Resolve();
        if (!HasSlingSet) return "PhobosSling_NoSlingSet".Translate().Resolve();
        if (Load is { } hanging) return "PhobosSling_AlreadyCarrying".Translate(hanging.LabelShort.Named("LOAD")).Resolve();
        // Framework 0.161.0 (0.165.0: any helicopter flying the map itself, the Hind too): in the air it lifts from a hover, so it must be able
        // to hold one.
        if (HoverPilot.For(Vehicle) is { } pilot && !pilot.CanHover) return "PhobosSling_CannotHoverToLift".Translate(Vehicle.LabelShort.Named("HELICOPTER")).Resolve();
        // Framework 0.185.0 (EM29, the owner's "warn, then allow"): weather that grounds a lift is a warning, and the lift strains the hook.
        return null;
    }

    /// <summary>
    /// What an unwise lift risks, or null (Framework 0.185.0; docs/development/vehicle-emergent-play.md, EM29: the owner chose "warn, then
    /// allow" on 20 Nov 2026). A load over what the hook lifts today, or weather that grounds a lift, is no longer refused: the lift goes
    /// ahead and strains the hook's mount by the overload and the weather (SlingRule.HookStrain); a mount strained to nothing gives way and
    /// drops the load.
    /// </summary>
    public string? Warning(Thing? load)
    {
        var lines = new List<string>();
        if (WeatherRefusal(Vehicle.Map) is { } weather) lines.Add("PhobosSling_WarnWeather".Translate(weather.Named("WEATHER")).Resolve());
        if (load != null)
        {
            double weight = KilogramsOf(load), capacity = CapacityKilograms(Vehicle.Map);
            if (!SlingRule.CanLift(weight, capacity))
            {
                lines.Add("PhobosSling_WarnOverload".Translate(load.LabelShort.Named("LOAD"), weight.ToString("N0").Named("WEIGHT"), capacity.ToString("N0").Named("CAPACITY")).Resolve());
            }
        }
        return lines.Count == 0 ? null : string.Join(" ", lines);
    }

    /// <summary>How hard this lift strains the hook's mount, as a share of its health (Framework 0.185.0, EM29).</summary>
    double StrainOf(Thing load)
    {
        bool grounded = WeatherRefusal(Vehicle.MapHeld) != null;
        return SlingRule.HookStrain(KilogramsOf(load), CapacityKilograms(Vehicle.MapHeld), grounded, SlingRule.WeatherStrain);
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

    /// <summary>What hangs from the hook now, kilograms (Framework 0.161.0: the engines lift it, so the hover and the cruise count it).</summary>
    public float CarriedKilograms => Load is { } load ? (float)KilogramsOf(load) : 0f;

    /// <summary>The share of its speed over a map it flies at with a load hanging (Framework 0.161.0): the slung flight's own figure, with
    /// the setting on.</summary>
    public float MapSpeedShare => Load != null && Settings.slingSlowFlight ? Rules.slingSpeedFactor : 1f;

    /// <summary>Whether a slung flight's cost applies to this helicopter now: a load ready on the hook or hanging from it, with the setting on.
    /// Vehicle Framework's flight speed and fuel use ask (SlingPatches), so the range drawn when choosing a destination shows it too.</summary>
    public bool SlungFlightNow => Settings.slingSlowFlight && (Load != null || (hooked != null && !awaitingCrew));

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
        // Framework 0.185.0 (EM29): a load over what the hook lifts today is a warning (Warning), not a refusal; the lift strains the hook.
        double weight = LoadKilograms(load);
        // Framework 0.161.0: lifted from a hover, it must still hover with the load on, or it could never set it down again.
        if (HoverPilot.For(Vehicle) != null && !FlightRules.WouldHoverWith(Vehicle, weight))
        {
            return "PhobosSling_TooHeavyToHover".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"), weight.ToString("N0").Named("WEIGHT")).Resolve();
        }
        return null;
    }

    /// <summary>Why this hull cannot be lifted by this helicopter now, or null (Framework 0.197.0, EM21(b)). Anyone's hull may be lifted: a
    /// wreck is nobody's until it is carried off.</summary>
    public string? RefusalForHull(VehicleBuilding hull)
    {
        NamedArgument name = hull.LabelShort.Named("LOAD");
        if (!IsHull(hull)) return "PhobosSling_NotLiftable".Translate(name).Resolve();
        if (!hull.Spawned || hull.Map != Vehicle.Map) return "PhobosSling_NotHere".Translate(name).Resolve();
        if (hull.Position.DistanceTo(Vehicle.Position) > Rules.slingReachCells) return "PhobosSling_TooFar".Translate(name, Rules.slingReachCells.Named("CELLS")).Resolve();
        if (hull.OccupiedRect().Any(cell => cell.Roofed(hull.Map))) return "PhobosSling_Roofed".Translate(name).Resolve();
        double weight = KilogramsOf(hull);
        if (HoverPilot.For(Vehicle) != null && !FlightRules.WouldHoverWith(Vehicle, weight))
        {
            return "PhobosSling_TooHeavyToHover".Translate(Vehicle.LabelShort.Named("HELICOPTER"), name, weight.ToString("N0").Named("WEIGHT")).Resolve();
        }
        return null;
    }

    /// <summary>Why this thing cannot be lifted now, or null (Framework 0.335.0, EM21(b)): an item or a cargo net, as a hull is judged.
    /// Anyone's may be lifted, as a hull may: carrying off another faction's things is the player's choice.</summary>
    public string? RefusalForThing(Thing thing)
    {
        NamedArgument name = thing.LabelShort.Named("LOAD");
        if (!IsLiftableThing(thing)) return "PhobosSling_NotLiftable".Translate(name).Resolve();
        if (!thing.Spawned || thing.Map != Vehicle.Map) return "PhobosSling_NotHere".Translate(name).Resolve();
        if (thing.Position.DistanceTo(Vehicle.Position) > Rules.slingReachCells) return "PhobosSling_TooFar".Translate(name, Rules.slingReachCells.Named("CELLS")).Resolve();
        if (thing.OccupiedRect().Any(cell => cell.Roofed(thing.Map))) return "PhobosSling_Roofed".Translate(name).Resolve();
        double weight = KilogramsOf(thing);
        if (HoverPilot.For(Vehicle) != null && !FlightRules.WouldHoverWith(Vehicle, weight))
            return "PhobosSling_TooHeavyToHover".Translate(Vehicle.LabelShort.Named("HELICOPTER"), name, weight.ToString("N0").Named("WEIGHT")).Resolve();
        return null;
    }

    /// <summary>Why this load cannot be lifted now, or null: a vehicle, a hull, or since Framework 0.335.0 an item or a cargo net.</summary>
    public string? RefusalForLoad(Thing load) => load switch
    {
        VehiclePawn vehicle => RefusalFor(vehicle),
        VehicleBuilding hull => RefusalForHull(hull),
        _ => RefusalForThing(load),
    };

    /// <summary>Why the helicopter cannot take off with its chosen load, or null (Vehicle Framework's launch check asks; SlingPatches).</summary>
    public string? LaunchRefusal()
    {
        if (hooked == null) return null;
        if (awaitingCrew) return "PhobosSling_LaunchWaitsForCrew".Translate(hooked.LabelShort.Named("LOAD")).Resolve();
        return RefusalForLoad(hooked);
    }

    // ---- What the player orders ----

    public void Choose(Thing load)
    {
        if ((Refusal() ?? RefusalForLoad(load)) is { } refusal)
        {
            Say(refusal, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        // Framework 0.185.0 (EM29): an unwise lift is allowed, and said plainly first.
        if (Warning(load) is { } warning) Say(warning, new LookTargets(Vehicle, load), MessageTypeDefOf.CautionInput, historical: false);
        hooked = load;
        approaches = 0;
        // A hull has nobody to make it ready, and the hook-up work is given on vehicles: the helicopter's own crew makes its strops fast.
        awaitingCrew = Settings.slingGroundCrew && load is VehiclePawn;
        // Framework 0.165.0: a helicopter flying the map on another flight (the Hind) is watched by this comp's own tick.
        if (NeedsOwnThinking) Vehicle.RequestTickStart(this);
        SlingWaits.Note(this, awaitingCrew);
        if (!awaitingCrew) Latch(load);
        Say(awaitingCrew
            ? "PhobosSling_ChosenCrew".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve()
            : "PhobosSling_ChosenReady".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(),
            new LookTargets(Vehicle, load), MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>
    /// Why a round may not hook this up now, or null (Framework 0.340.0): the sling's own refusals, and also any warning, such as a load over
    /// what the hook lifts today: with nobody watching, a round waits rather than strain the hook (the owner's card: wait and say why).
    /// </summary>
    public string? QuietRefusal(Thing load) => Refusal() ?? RefusalForLoad(load) ?? Warning(load);

    /// <summary>A round's hook-up (Framework 0.340.0): as <see cref="Choose"/>, without its messages; the round says what it does.</summary>
    public void HookQuietly(Thing load)
    {
        hooked = load;
        approaches = 0;
        awaitingCrew = Settings.slingGroundCrew && load is VehiclePawn;
        if (NeedsOwnThinking) Vehicle.RequestTickStart(this);
        SlingWaits.Note(this, awaitingCrew);
        if (!awaitingCrew) Latch(load);
    }

    /// <summary>The nearest filled cargo net within the sling's reach of this helicopter, or null (Framework 0.340.0: a round's lift).</summary>
    public static Thing? NearestNet(VehiclePawn helicopter)
    {
        if (!helicopter.Spawned) return null;
        float reach = Rules.slingReachCells;
        return helicopter.Map.listerThings.AllThings.OfType<Building_PhobosCargoNet>()
            .Where(net => net.Contents.Any() && net.Position.DistanceTo(helicopter.Position) <= reach)
            .OrderBy(net => net.Position.DistanceToSquared(helicopter.Position)).FirstOrDefault();
    }

    /// <summary>
    /// Leaves the load at this tile as a depot a caravan can collect from (Framework 0.340.0: a round's "leave the load here"), as a thing let
    /// go in flight is: a net's contents go in and the net comes apart; a vehicle or a hull stays on the hook. Says what it did.
    /// </summary>
    public void LeaveInDepot(PlanetTile tile)
    {
        if (Load is not { } thing || !IsLiftableThing(thing)) return;
        held.Remove(thing);
        liftedFrom = null;
        string what = thing.LabelShort;
        PhobosSlingWreck depot = Depots.At(tile) ?? PhobosSlingWreck.MakeDepot(tile);
        if (thing is Building_PhobosCargoNet net)
        {
            foreach (Thing inside in net.Netted.ToList())
            {
                net.Netted.Remove(inside);
                depot.Keep(inside);
            }
            net.Destroy();
        }
        else depot.Keep(thing);
        Systems?.Powertrain?.MarkMassDirty();
        if (Seen.Wants("aircraft.round-sling", Vehicle, "depot")) Seen.Note("aircraft.round-sling", Vehicle, what, "depot");
        Say("PhobosRound_SlingLeft".Translate(Vehicle.LabelShort.Named("HELICOPTER"), what.Named("LOAD")).Resolve(), new LookTargets(depot), MessageTypeDefOf.TaskCompletion);
    }

    public void Cancel()
    {
        if (hooked == null) return;
        Say("PhobosSling_Cancelled".Translate(hooked.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        PhobosResumableVehicleWork.ForgetOrder(hooked as VehiclePawn, VehicleSystemsJobDefOf.PhobosVehicleSystems_SlingHookUp);
        hooked = null;
        awaitingCrew = false;
        SlingWaits.Note(this, false);
    }

    /// <summary>A colonist has hooked the load up (the hook-up job).</summary>
    public void HookedUp(Pawn crew)
    {
        if (hooked == null) return;
        if (VehicleSystemsMod.SlingLog.DebugEnabled)
        {
            VehicleSystemsMod.SlingLog.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ") hooked up " + hooked.LabelShort + " (" + hooked.ThingID + "), "
                + KilogramsOf(hooked).ToString("0") + " kg of " + CapacityKilograms(Vehicle.MapHeld).ToString("0") + " kg");
        }
        awaitingCrew = false;
        SlingWaits.Note(this, false);
        Latch(hooked);
        if (NeedsOwnThinking) Vehicle.RequestTickStart(this);
        Say("PhobosSling_HookedUp".Translate(crew.LabelShort.Named("CREW"), hooked.LabelShort.Named("LOAD"), Vehicle.LabelShort.Named("HELICOPTER")).Resolve(),
            new LookTargets(Vehicle, hooked), MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>The hook's latch closing on the load (S128, sling slice 3), heard where the load stands. Not while the self-test runs.</summary>
    static void Latch(Thing load)
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
        Thing? load = hooked;
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
        (load as VehiclePawn)?.vehiclePather?.StopDead();
        // Framework 0.335.0: a cargo net takes up what lies in it.
        (load as Building_PhobosCargoNet)?.Gather();
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
            KilogramsOf(load).ToString("N0").Named("WEIGHT")).Resolve(), MessageTypeDefOf.PositiveEvent);
        if (load is VehicleBuilding) Seen.Note("vehicles.sling-hull", Vehicle, variant: "lifted");
        StrainHook(load, cell, map, facing);
    }

    /// <summary>The height a load falls from when the hook gives way as it is lifted (authored: a few metres off the ground).</summary>
    const float GaveWayMetres = 3f;

    /// <summary>
    /// An unwise lift strains the hook's mount (Framework 0.185.0, EM29): it loses the strain's share of its health. A mount strained to
    /// nothing gives way: the load drops back where it stood from a few metres, and the hook is gone with its mount until it is repaired.
    /// </summary>
    void StrainHook(Thing load, IntVec3 cell, Map map, Rot4 facing)
    {
        if (Settings.showOnly || Systems?.Fittings.PartGranting(HookAbility) is not { } mount) return;
        double strain = StrainOf(load);
        if (strain <= 0) return;
        float amount = (float)(strain * mount.MaxHealth);
        mount.TakeDamage(Vehicle, new DamageInfo(DamageDefOf.Blunt, amount, 999f), ignoreArmor: true);
        Seen.Note("vehicles.sling-strain", Vehicle, load.LabelShort, mount.Health > 0f ? "held" : "gave-way");
        if (mount.Health > 0f)
        {
            Say("PhobosSling_Strained".Translate(Vehicle.LabelShort.Named("HELICOPTER"), ((float)strain).ToStringPercent().Named("SHARE")).Resolve(),
                Vehicle, MessageTypeDefOf.CautionInput);
            return;
        }
        if (!held.Remove(load)) return;
        Systems?.Powertrain?.MarkMassDirty();
        liftedFrom = null;
        GenSpawn.Spawn(load, cell, map, facing);
        string harm = SlingDrops.FallOf(load, GaveWayMetres);
        Say("PhobosSling_GaveWay".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"), harm.Named("HARM")).Resolve(), load,
            MessageTypeDefOf.NegativeEvent);
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
        if (Load == null)
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
        // Framework 0.165.0: a lift or set-down from a hover by a helicopter on another flight (the Hind), every half second.
        if (NeedsOwnThinking)
        {
            if ((Find.TickManager.TicksGame + Vehicle.thingIDNumber) % 30 == 0 && HoverPilot.For(Vehicle) is { } pilot) Think(pilot);
            return;
        }
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
        if (Load == null) return;
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
    bool Place(Thing load, IntVec3 cell, Map map, Rot4 facing)
    {
        held.Remove(load);
        string? failure = null;
        try
        {
            GenSpawn.Spawn(load, cell, map, facing);
            // The game can also refuse without throwing (Framework 0.140.1, as the hitch learnt in fault-read H10): the load is down only
            // if it really stands on the map. Without this a refused load was neither on the hook nor on the ground.
            if (!load.Spawned) failure = "the game did not put it on the map";
        }
        catch (System.Exception e)
        {
            failure = e.ToString();
        }
        if (failure != null)
        {
            VehicleSystemsMod.Logger.Error($"Setting {load.LabelShort} down from {Vehicle.LabelShort} at {cell} failed: {failure}");
            if (!load.Spawned && !load.Destroyed && !held.Contains(load)) held.TryAdd(load, canMergeWithExistingStacks: false);
            Say("PhobosSling_SetDownFailed".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return false;
        }
        liftedFrom = null;
        if (load is VehicleBuilding) Seen.Note("vehicles.sling-hull", load, variant: "set-down");
        if (load is Building_PhobosCargoNet net) net.Spill();
        if (IsLiftableThing(load) && Seen.Wants("vehicles.sling-thing", Vehicle, load is Building_PhobosCargoNet ? "net" : load is MinifiedThing ? "packed" : "stack"))
            Seen.Note("vehicles.sling-thing", Vehicle, load.LabelShort, load is Building_PhobosCargoNet ? "net" : load is MinifiedThing ? "packed" : "stack");
        return true;
    }

    bool ShouldAsk() => Vehicle.Faction == Faction.OfPlayer && Vehicle.Map != null && Vehicle.Map == Find.CurrentMap && !VehicleNotices.Quiet
                        && Current.ProgramState == ProgramState.Playing
                        // Framework 0.340.0: an aircraft flying a round sets its load down by itself; nobody is asked, and the game never pauses.
                        && Vehicle.GetComp<CompPhobosRound>() is not { Running: true };

    /// <summary>Vehicle Framework's crash event: the helicopter comes down with its load. With release and drops on, the load falls when the
    /// helicopter next reaches a map; off, it is set down as after any landing.</summary>
    internal void NoteCrash()
    {
        if (Load != null && Settings.slingRelease) crashing = true;
    }

    /// <summary>Pauses and lets the player place the load within reach of the helicopter, its outline drawn under the mouse. Declined, the
    /// load is set down on the nearest clear ground, as before slice 2.</summary>
    void AskWhereToSetDown()
    {
        Thing? load = Load;
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
                GenDraw.DrawFieldEdges(SlingPlacement.RectOf(load, target.Cell, facing).Cells.ToList(),
                    SpotRefusal(target.Cell, facing) == null ? Color.white : Color.red);
            },
            target => target.Cell.IsValid && SpotRefusal(target.Cell, facing) == null,
            actionWhenFinished: () =>
            {
                // Declined (right-click or Escape): the load comes down where it would have before.
                if (Load != null && Vehicle.Spawned) SetDown();
            });
    }

    /// <summary>Why the load cannot be set down with its corner cell here, or null.</summary>
    string? SpotRefusal(IntVec3 cell, Rot4 facing)
    {
        Thing? load = Load;
        Map map = Vehicle.Map;
        if (load == null || map == null) return "PhobosSling_NothingToSetDown".Translate().Resolve();
        if (cell.DistanceTo(Vehicle.Position) > Rules.slingReachCells) return "PhobosSling_SpotTooFar".Translate(Rules.slingReachCells.Named("CELLS")).Resolve();
        return SlingPlacement.Fits(load, facing, map, cell, Vehicle.VehicleRect()) ? null : "PhobosSling_SpotBlocked".Translate().Resolve();
    }

    /// <summary>Sets the load down where the player chose, if it still fits there; otherwise on the nearest clear ground.</summary>
    internal void SetDownAt(IntVec3 cell, Rot4 facing)
    {
        Thing? load = Load;
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
        Thing? load = Load;
        Map map = Vehicle.Map;
        if (load == null || map == null) return;
        if (!SlingPlacement.TryFindSpot(load, Vehicle, map, Rules.slingReachCells, out IntVec3 cell))
        {
            // Nowhere for it to fall clear: it stays on the hook, and the player can set it down later (the gizmo below).
            Say("PhobosSling_NoSpot".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return;
        }
        if (!Place(load, cell, map, Vehicle.Rotation)) return;
        string harm = SlingDrops.FallOf(load, Rules.slingCrashDropMetres);
        Say("PhobosSling_FellInCrash".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"), harm.Named("HARM")).Resolve(), load, MessageTypeDefOf.NegativeEvent);
    }

    /// <summary>Why the crew cannot let the load go in flight on the world map now, or null.</summary>
    public string? ReleaseRefusal()
    {
        if (!Settings.slingRelease) return "PhobosSling_ReleaseOff".Translate().Resolve();
        if (Load == null) return "PhobosSling_NothingToRelease".Translate().Resolve();
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
        if (Load is VehicleBuilding hull)
        {
            // A hull let go from cruising height breaks up where it falls: nothing is left for a caravan to find (Framework 0.197.0).
            held.Remove(hull);
            liftedFrom = null;
            string name = hull.LabelShort;
            hull.Destroy();
            Say("PhobosSling_ReleasedHull".Translate(Vehicle.LabelShort.Named("HELICOPTER"), name.Named("LOAD")).Resolve(), MessageTypeDefOf.NegativeEvent);
            return;
        }
        if (Load is { } thing && IsLiftableThing(thing))
        {
            // Framework 0.335.0: a thing let go falls to the ground below and lies there as a depot a caravan can collect from; a net's
            // contents fall with it, and the net comes apart.
            PlanetTile spot = PhobosSlingWreck.FreeTileNear(below);
            if (!spot.Valid)
            {
                Say("PhobosSling_ReleaseNoTile".Translate(thing.LabelShort.Named("LOAD")).Resolve(), MessageTypeDefOf.RejectInput, historical: false);
                return;
            }
            held.Remove(thing);
            liftedFrom = null;
            string what = thing.LabelShort;
            PhobosSlingWreck depot = Depots.At(spot) ?? PhobosSlingWreck.MakeDepot(spot);
            if (thing is Building_PhobosCargoNet net)
            {
                foreach (Thing inside in net.Netted.ToList())
                {
                    net.Netted.Remove(inside);
                    depot.Keep(inside);
                }
                net.Destroy();
            }
            else depot.Keep(thing);
            Say("PhobosSling_ReleasedThing".Translate(Vehicle.LabelShort.Named("HELICOPTER"), what.Named("LOAD")).Resolve(), new LookTargets(depot),
                MessageTypeDefOf.NegativeEvent);
            return;
        }
        if (Carried is not { } load) return;
        PlanetTile tile = PhobosSlingWreck.FreeTileNear(below);
        VehicleSystemsMod.SlingLog.Debug(Vehicle.LabelShort + " (" + Vehicle.ThingID + ") released " + load.LabelShort + " (" + load.ThingID + ") in flight over tile " + below + ", wreck at " + tile);
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
        Thing? load = Load;
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
        if (Load is not { } load || !Vehicle.Spawned || !OnMapFlight.FliesItself(Vehicle) || !OnMapFlight.Airborne(Vehicle)) return;
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
        Thing? load = Load;
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

    // ---- From a hover, on our own flight (Framework 0.161.0; the sling record's hover, set aside on 12 Nov 2026 and possible now) ----

    /// <summary>Cells beyond the two vehicles' half-lengths that it hovers off to one side of a load (authored: clear of the load's
    /// footprint, the strops' length).</summary>
    const float ApartMargin = 1f;
    /// <summary>Slack around that distance within which it is over the load (authored).</summary>
    const float NearSlack = 2.5f;
    /// <summary>Tries at getting near before it gives up and says so (authored).</summary>
    const int MostApproaches = 3;

    // Saved: where to set the load down from the air, and how often it has tried to get near.
    IntVec3 dropCell = IntVec3.Invalid;
    Rot4 dropRot = Rot4.North;
    int approaches;

    /// <summary>
    /// Whether this comp's own tick must watch a lift or set-down from a hover (Framework 0.165.0): a helicopter flying the map on another
    /// flight (the Hind, on VTOL Vehicle's), whose own flight does not call <see cref="Think"/> as ours does.
    /// </summary>
    bool NeedsOwnThinking => Vehicle.Spawned && !CompPhobosFlight.FliesOnOurs(Vehicle) && OnMapFlight.Airborne(Vehicle)
                             && ((hooked != null && !awaitingCrew) || (Load != null && dropCell.IsValid));

    /// <summary>The load is going to be set down from the air at this spot, or nowhere (Invalid).</summary>
    public IntVec3 SetDownSpot => dropCell;

    static float Apart(VehiclePawn helicopter, IntVec2 load) =>
        Mathf.Max(helicopter.VehicleDef.Size.x, helicopter.VehicleDef.Size.z) / 2f + Mathf.Max(load.x, load.z) / 2f + ApartMargin;

    /// <summary>A load's size on the ground: a vehicle's or a hull's design, or since Framework 0.340.0 any thing's own (a packed building,
    /// a stack, a cargo net), so all are lifted and set down from a hover alike.</summary>
    static IntVec2 SizeOf(Thing load) => DesignOf(load)?.Size ?? load.def.Size;

    /// <summary>Where a load of this size stands centred when set down at this cell, as the game or Vehicle Framework places it.</summary>
    static Vector3 CentreAt(Thing load, IntVec3 cell, Rot4 rot) => DesignOf(load) is { } design
        ? Ext_Vehicles.TrueCenter(cell, new Rot8(rot), design.Size, 0f)
        : GenThing.TrueCenter(cell, rot, load.def.Size, 0f);

    /// <summary>
    /// Every half second while it is in the air on our own flight: a load hooked up and ready, it goes to hover beside it and lifts it; a
    /// load hanging with a spot chosen, it goes to hover beside the spot and lowers it there. It works only from a steady hover.
    /// </summary>
    public void FlightThink(CompPhobosFlight flight) => Think(new OurFlightPilot(flight));

    /// <summary>The same for any helicopter flying the map itself (Framework 0.165.0: our flight calls it; the Hind's is watched by
    /// <see cref="CompTick"/>).</summary>
    public void Think(HoverPilot pilot)
    {
        // Framework 0.340.0: any load, not only a vehicle or a hull (a cargo net is lifted from the air too).
        if (hooked is { Spawned: true } load && !awaitingCrew)
        {
            Approach(pilot, SizeOf(load), load is VehiclePawn vehicle ? vehicle.TrueCenter() : load.TrueCenter(), lift: true);
        }
        else if (Load is { } carried && dropCell.IsValid)
        {
            Approach(pilot, SizeOf(carried), CentreAt(carried, dropCell, dropRot), lift: false);
        }
    }

    void Approach(HoverPilot pilot, IntVec2 load, Vector3 target, bool lift)
    {
        if (!pilot.Hovering) return;
        float apart = Apart(Vehicle, load);
        Vector3 here = pilot.Position;
        Vector3 off = new Vector3(here.x - target.x, 0f, here.z - target.z);
        if (off.magnitude <= apart + NearSlack)
        {
            approaches = 0;
            if (lift) LiftFromHover(pilot);
            else SetDownFromHover(pilot);
            return;
        }
        if (++approaches > MostApproaches)
        {
            Say("PhobosSling_CouldNotReach".Translate(Vehicle.LabelShort.Named("HELICOPTER")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            approaches = 0;
            if (lift) Cancel();
            else dropCell = IntVec3.Invalid;
            return;
        }
        Vector3 dir = off.sqrMagnitude < 0.01f ? Vector3.forward : off.normalized;
        IntVec3 over = (target + dir * apart).ToIntVec3().ClampInsideMap(Vehicle.Map);
        pilot.GoTo(over);
    }

    /// <summary>Beside its load in a steady hover: the hook takes it, and the engines feel its weight at once.</summary>
    void LiftFromHover(HoverPilot pilot)
    {
        TakeLoad();
        if (Load == null) return;
        Systems?.Powertrain?.MarkMassDirty();
        if (IsLiftableThing(Load) && Seen.Wants("vehicles.sling-hover-thing", Vehicle)) Seen.Note("vehicles.sling-hover-thing", Vehicle, Load!.LabelShort);
        if (pilot is VtolPilot)
        {
            // Its own flight lands it where the player says: the load waits on the hook and is set down once it is down, as after an arrival.
            landed = true;
            Vehicle.RequestTickStart(this);
            Seen.Note("vehicles.sling-hover-vtol", Vehicle, variant: "lifted");
        }
        else Seen.Note("vehicles.sling-hover", Vehicle, variant: "lifted");
    }

    /// <summary>Beside the chosen spot in a steady hover: the load goes down there if it still fits, else it stays on the hook and says why.</summary>
    void SetDownFromHover(HoverPilot pilot)
    {
        Thing? load = Load;
        Map map = Vehicle.Map;
        IntVec3 cell = dropCell;
        dropCell = IntVec3.Invalid;
        if (load == null || map == null) return;
        if (!SlingPlacement.Fits(load, dropRot, map, cell, Vehicle.VehicleRect()))
        {
            Say("PhobosSling_SpotBlockedNow".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        if (!Place(load, cell, map, dropRot)) return;
        Systems?.Powertrain?.MarkMassDirty();
        if (pilot is VtolPilot) Seen.Note("vehicles.sling-hover-vtol", Vehicle, variant: "set-down");
        else Seen.Note("vehicles.sling-hover", Vehicle, variant: "set-down");
        Say("PhobosSling_SetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD")).Resolve(), load, MessageTypeDefOf.TaskCompletion);
    }

    /// <summary>Why the load cannot be set down from the air at this spot, or null: anywhere on the map it fits, not under this helicopter.</summary>
    string? AirSpotRefusal(IntVec3 cell, Rot4 facing)
    {
        Thing? load = Load;
        Map map = Vehicle.Map;
        if (load == null || map == null) return "PhobosSling_NothingToSetDown".Translate().Resolve();
        return SlingPlacement.Fits(load, facing, map, cell, Vehicle.VehicleRect()) ? null : "PhobosSling_SpotBlocked".Translate().Resolve();
    }

    /// <summary>Lets the load go where it hangs (Framework 0.161.0): it falls from the helicopter's height on the nearest clear ground below and
    /// lands hard (SlingDrops). <paramref name="crash"/> when the helicopter is coming down, else the crew's own release.</summary>
    internal void DropOnMap(float metres, bool crash)
    {
        Thing? load = Load;
        Map map = Vehicle.Map;
        dropCell = IntVec3.Invalid;
        if (load == null || map == null) return;
        if (!SlingPlacement.TryFindSpot(load, Vehicle, map, Rules.slingReachCells, out IntVec3 cell))
        {
            Say("PhobosSling_NoSpot".Translate(load.LabelShort.Named("LOAD")).Resolve(), Vehicle, MessageTypeDefOf.NegativeEvent);
            return;
        }
        if (!Place(load, cell, map, Vehicle.Rotation)) return;
        Systems?.Powertrain?.MarkMassDirty();
        string harm = SlingDrops.FallOf(load, metres);
        if (crash) Seen.Note("vehicles.sling-hover", Vehicle, variant: "fell");
        else Seen.Note("vehicles.sling-hover", Vehicle, variant: "released");
        Say((crash ? "PhobosSling_FellInCrash" : "PhobosSling_ReleasedOnMap").Translate(Vehicle.LabelShort.Named("HELICOPTER"), load.LabelShort.Named("LOAD"),
            harm.Named("HARM")).Resolve(), load, MessageTypeDefOf.NegativeEvent);
    }

    /// <summary>Our flight is coming down with a load hanging: with release and drops on, it falls from where it hangs; off, it stays on the
    /// hook and is set down beside the helicopter once it is on the ground.</summary>
    public void OnFlightFalling(float metres)
    {
        if (Load != null && Settings.slingRelease) DropOnMap(metres, crash: true);
    }

    /// <summary>Our flight has put the helicopter on the ground with a load still on the hook: it is set down, as after any landing.</summary>
    public void OnFlightLanded()
    {
        if (Load == null) return;
        dropCell = IntVec3.Invalid;
        if (ShouldAsk()) AskWhereToSetDown();
        else SetDown();
    }

    /// <summary>The buttons for a load hanging under a helicopter in the air on our own flight: where to set it down, and the release.</summary>
    IEnumerable<Gizmo> AirGizmos(HoverPilot pilot, Thing carried)
    {
        Rot4 facing = pilot.Facing;
        var setDown = new Command_Action
        {
            defaultLabel = "PhobosSling_SetDownHereGizmo".Translate(),
            defaultDesc = "PhobosSling_SetDownHereGizmoDesc".Translate(carried.LabelShort.Named("LOAD")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/SetDown"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters { canTargetLocations = true, canTargetPawns = false, canTargetBuildings = false },
                target =>
                {
                    if (AirSpotRefusal(target.Cell, facing) is { } why)
                    {
                        Say(why, MessageTypeDefOf.RejectInput, historical: false);
                        return;
                    }
                    dropCell = target.Cell;
                    dropRot = facing;
                    approaches = 0;
                    if (NeedsOwnThinking) Vehicle.RequestTickStart(this);
                    Say("PhobosSling_WillSetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER"), carried.LabelShort.Named("LOAD")).Resolve(), Vehicle,
                        MessageTypeDefOf.SilentInput, historical: false);
                },
                target =>
                {
                    if (!target.Cell.IsValid) return;
                    GenDraw.DrawFieldEdges(SlingPlacement.RectOf(carried, target.Cell, facing).Cells.ToList(), AirSpotRefusal(target.Cell, facing) == null ? Color.white : Color.red);
                },
                target => target.Cell.IsValid && AirSpotRefusal(target.Cell, facing) == null),
        };
        if (!pilot.CanHover) setDown.Disable("PhobosSling_CannotHoverToSetDown".Translate(Vehicle.LabelShort.Named("HELICOPTER")).Resolve());
        yield return setDown;
        var release = new Command_Action
        {
            defaultLabel = "PhobosSling_ReleaseGizmo".Translate(),
            defaultDesc = (carried is VehicleBuilding ? "PhobosSling_ReleaseHullOnMapDesc" : "PhobosSling_ReleaseOnMapDesc").Translate(carried.LabelShort.Named("LOAD")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/Release"),
            action = () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                (carried is VehicleBuilding ? "PhobosSling_ReleaseHullOnMapConfirm" : "PhobosSling_ReleaseOnMapConfirm").Translate(
                    carried.LabelShort.Named("LOAD"), pilot.HeightMetres.ToString("0").Named("METRES")),
                () => DropOnMap(HoverPilot.For(Vehicle)?.HeightMetres ?? pilot.HeightMetres, crash: false), destructive: true)),
        };
        if (ReleaseRefusal() is { } refused) release.Disable(refused);
        yield return release;
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
        Scribe_Values.Look(ref dropCell, "PhobosSling_DropCell", IntVec3.Invalid);
        Scribe_Values.Look(ref dropRot, "PhobosSling_DropRot", Rot4.North);
        Scribe_Values.Look(ref approaches, "PhobosSling_Approaches");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            held ??= new ThingOwner<Thing>(this, oneStackOnly: true);
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
        if (Load is { } hanging && HoverPilot.For(Vehicle) is { } flying)
        {
            // Framework 0.161.0 (0.165.0: the Hind too): in the air flying the map itself, it sets its load down from a hover, or lets it go.
            foreach (Gizmo gizmo in AirGizmos(flying, hanging)) yield return gizmo;
            yield break;
        }
        if (Load is { } carried)
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
            defaultDesc = "PhobosSling_LiftGizmoDesc".Translate(CapacityKilograms(Vehicle.Map).ToString("N0").Named("CAPACITY"), Rules.slingReachCells.Named("CELLS"))
                + (Warning(null) is { } warning ? "\n\n" + warning : ""),
            icon = SystemIcons.For("Phobos/VehicleSystems/Sling/Lift"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
            {
                canTargetPawns = true,
                // Framework 0.197.0: a destroyed vehicle's hull is a building, and can be lifted too.
                canTargetBuildings = true,
                // Framework 0.335.0 (EM21(b)): a packed building, a stack of anything, or a cargo net with what lies in it.
                canTargetItems = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = target => target.Thing is VehiclePawn || IsHull(target.Thing) || IsLiftableThing(target.Thing),
            }, target =>
            {
                if (target.Thing is { } load) Choose(load);
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
        if (Load is { } carried)
        {
            string carrying = "PhobosSling_InspectCarrying".Translate(carried.LabelShort.Named("LOAD")).Resolve();
            return dropCell.IsValid ? carrying + "\n" + "PhobosSling_InspectSettingDown".Translate().Resolve() : carrying;
        }
        if (hooked == null) return null;
        string? grounded = WeatherRefusal(Vehicle.Map);
        double weight = KilogramsOf(hooked), capacity = CapacityKilograms(Vehicle.Map);
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
            helicopter.Spawned ? helicopter.VehicleRect() : CellRect.Empty, out found);

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

    /// <summary>The same for any slung load (Framework 0.197.0): a vehicle as above, or a destroyed vehicle's hull.</summary>
    public static bool TryFindSpot(Thing load, VehiclePawn helicopter, Map map, float radius, out IntVec3 found)
    {
        if (load is VehiclePawn vehicle) return TryFindSpot(vehicle, helicopter, map, radius, out found);
        Rot4 rotation = helicopter.Spawned ? helicopter.Rotation : Rot4.North;
        IntVec3 centre = helicopter.Spawned ? helicopter.Position : load.Position;
        CellRect avoid = helicopter.Spawned ? helicopter.VehicleRect() : CellRect.Empty;
        int count = GenRadial.NumCellsInRadius(System.Math.Min(radius, GenRadial.MaxRadialPatternRadius - 1f));
        for (int i = 0; i < count; i++)
        {
            IntVec3 cell = centre + GenRadial.RadialPattern[i];
            if (!Fits(load, rotation, map, cell, avoid)) continue;
            found = cell;
            return true;
        }
        found = IntVec3.Invalid;
        return false;
    }

    /// <summary>The cells a load covers when set down here: a vehicle's by Vehicle Framework's footprint; a hull's, being a building, by
    /// the game's own, which differs for an even side facing south or west.</summary>
    public static CellRect RectOf(Thing load, IntVec3 cell, Rot4 rotation) =>
        load is VehiclePawn vehicle ? vehicle.VehicleDef.VehicleRect(cell, rotation) : VehicleHulls.Footprint(load, cell, rotation);

    /// <summary>Whether this load fits here, by the cells it will really cover.</summary>
    public static bool Fits(Thing load, Rot4 rotation, Map map, IntVec3 cell, CellRect avoid) =>
        load is VehiclePawn vehicle ? Fits(vehicle.VehicleDef, rotation, map, cell, avoid) : Clear(RectOf(load, cell, rotation), map, avoid);

    /// <summary>Whether a vehicle of this design fits with its corner here: open, unroofed, standable ground with nobody and nothing solid in
    /// the way, clear of <paramref name="avoid"/>.</summary>
    public static bool Fits(VehicleDef design, Rot4 rotation, Map map, IntVec3 cell, CellRect avoid) =>
        // Vehicle Framework's own footprint (Framework 0.140.1, as the hitch learnt in fault-read H1): it reads south as north and west as
        // east, so the game's rect was a cell out for a vehicle of even length, and one set down at the north or east edge could be
        // destroyed as it spawned.
        Clear(design.VehicleRect(cell, rotation), map, avoid);

    static bool Clear(CellRect rect, Map map, CellRect avoid)
    {
        if (!rect.InBounds(map) || rect.Overlaps(avoid)) return false;
        foreach (IntVec3 c in rect)
        {
            if (!c.Standable(map) || c.Roofed(map) || c.GetThingList(map).Any(t => t is Pawn || t.def.passability == Traversability.Impassable)) return false;
        }
        return true;
    }
}
