using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What a supply truck does at a stop (the owner's card of 26 Nov 2026: "Chosen per stop").</summary>
public enum SupplyStopAction
{
    /// <summary>Only waits its time.</summary>
    Wait,
    /// <summary>Leaves everything it carries: into the depot on the tile (made if none), or on the ground beside it on a colony's map.</summary>
    Deliver,
    /// <summary>Takes everything the depot on the tile holds that it can carry. Not on a map: colonists load it there by hand.</summary>
    Fetch,
}

/// <summary>Whether a crewless run needs radio reach home the whole way, or only to set off and at its stops (a setting; the owner's card).</summary>
public enum SupplyRunRadio
{
    AllTheWay,
    StartAndStops,
}

/// <summary>What a crewless run does when it drives out of reach (a setting; the owner's card).</summary>
public enum SupplyRunLostLink
{
    Hold,
    CarryOn,
    TurnBack,
}

/// <summary>One stop of a supply run: a tile, the place on it (a depot or a colony of the player's), what it does there and how long it waits.</summary>
public sealed class SupplyStop : IExposable
{
    public PlanetTile tile = PlanetTile.Invalid;
    public WorldObject? place;
    public SupplyStopAction action;
    public int waitTicks = RoundRule.WaitTicks(RoundRule.DefaultWaitHours);
    /// <summary>At a colony: what colonists load into it while it waits (Framework 0.332.0), each a kind of thing and how many in all.</summary>
    public List<ThingDefCountClass> load = new();

    /// <summary>A colony map of the player's at this stop, entered at its edge.</summary>
    public Map? HomeMap => place is MapParent { HasMap: true, Faction.IsPlayer: true } parent ? parent.Map : null;

    /// <summary>How many of this kind its loading list asks for.</summary>
    public int LoadOf(ThingDef def) => load.FirstOrDefault(l => l.thingDef == def)?.count ?? 0;

    public string Label => place != null && !place.Destroyed ? place.LabelCap
        : "PhobosRound_TileLabel".Translate(Find.WorldGrid.LongLatOf(tile).x.ToStringLongitude().Named("LONG"),
            Find.WorldGrid.LongLatOf(tile).y.ToStringLatitude().Named("LAT")).Resolve();

    public void ExposeData()
    {
        Scribe_Values.Look(ref tile, "PhobosRunTile", PlanetTile.Invalid);
        Scribe_References.Look(ref place, "PhobosRunPlace");
        Scribe_Values.Look(ref action, "PhobosRunAction");
        Scribe_Values.Look(ref waitTicks, "PhobosRunWait", RoundRule.WaitTicks(RoundRule.DefaultWaitHours));
        Scribe_Collections.Look(ref load, "PhobosRunLoad", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            load ??= new List<ThingDefCountClass>();
            load.RemoveAll(l => l?.thingDef == null);
        }
    }
}

/// <summary>
/// Gives a vehicle a supply run: a round of stops on the world map it drives again and again, with a driver or by its route kit. D'thorga
/// Framework adds it at start-up to every land vehicle that can take the route kit; another mod's vehicle can name it in its comps.
/// </summary>
public class CompProperties_PhobosSupplyRun : CompProperties
{
    public CompProperties_PhobosSupplyRun() => compClass = typeof(CompPhobosSupplyRun);
}

/// <summary>
/// A truck's supply run (Framework 0.328.0; docs/development/vehicle-road-work.md; the owner's word of 26 Nov 2026, "Do the Fleet board,
/// relays, and crewless supply runs", and cards): a round of stops picked on the world map, depots, open tiles and the player's colonies,
/// driven with a driver aboard or, with the route kit working, with nobody, as an aircraft's round is flown. Between stops it is a convoy
/// of its own; at a colony it enters the map at its edge; at each stop it delivers, fetches or only waits, as set. A crewless run answers to
/// radio reach home as the settings say. Every rule is here; the buttons only ask.
/// </summary>
public sealed class CompPhobosSupplyRun : VehicleComp
{
    const int LookTicks = 250;
    /// <summary>A look while it drives to the map's edge, so it leaves within a second of arriving there.</summary>
    const int DrivingOutTicks = 60;
    /// <summary>The loading list's step buttons (the owner's way: preset buttons, round values).</summary>
    public static readonly int[] LoadSteps = { 10, 100 };

    List<SupplyStop> stops = new();
    bool running;
    int at = -1;
    int waitUntil = -1;
    bool crewless;
    bool heldForLink;
    string? waitingWhy;
    /// <summary>The edge cell it drives to before leaving a colony's map (Framework 0.332.0), or invalid.</summary>
    IntVec3 exitCell = IntVec3.Invalid;
    /// <summary>Whether colonists are loading it from its stop's list.</summary>
    bool loading;

    public IReadOnlyList<SupplyStop> Stops => stops;
    public bool Running => running;
    /// <summary>It drives with nobody aboard, by its route kit: Vehicle Framework's crew check reads this.</summary>
    public bool DrivesCrewless => crewless;

    // ---- Planning ----

    public string? AddStopRefusal(GlobalTargetInfo target)
    {
        if (!target.IsValid || !target.Tile.Valid) return "PhobosRound_RefuseNoPlace".Translate().Resolve();
        if (!RoundRule.MayAdd(stops.Count, stops.Count > 0 && stops[stops.Count - 1].tile == target.Tile))
            return stops.Count >= RoundRule.MostStops ? "PhobosRound_RefuseTooMany".Translate(RoundRule.MostStops.Named("MOST")).Resolve()
                : "PhobosRound_RefuseSameAsLast".Translate().Resolve();
        if (target.WorldObject is Settlement { Faction: { } faction } && faction != Faction.OfPlayer)
            return "PhobosSupplyRun_RefuseTown".Translate().Resolve();
        return null;
    }

    public void BeginPlan()
    {
        running = false;
        stops.Clear();
        at = -1;
        waitUntil = -1;
        waitingWhy = null;
        heldForLink = false;
        if (Vehicle.Spawned) AddStop(Vehicle.Map.Tile, Vehicle.Map.Parent);
        else if (Vehicle.GetVehicleCaravan() is { } convoy) AddStop(convoy.Tile, Depots.At(convoy.Tile));
    }

    public void AddStop(PlanetTile tile, WorldObject? place)
    {
        stops.Add(new SupplyStop { tile = tile, place = place is Caravan ? null : place });
        Messages.Message("PhobosRound_StopAdded".Translate(Vehicle.LabelShort.Named("VEHICLE"), stops.Count.Named("INDEX"), stops[stops.Count - 1].Label.Named("PLACE")),
            MessageTypeDefOf.SilentInput, false);
    }

    /// <summary>Why this stop cannot be set to this, or null: fetching is from a depot, never from a colony's map.</summary>
    public string? ActionRefusal(int index, SupplyStopAction action) =>
        action == SupplyStopAction.Fetch && stops[index].HomeMap != null ? "PhobosSupplyRun_RefuseFetchOnMap".Translate().Resolve() : null;

    public void SetAction(int index, SupplyStopAction action)
    {
        if (index < 0 || index >= stops.Count || ActionRefusal(index, action) != null) return;
        stops[index].action = action;
    }

    public void SetWait(int index, float hours)
    {
        if (index < 0 || index >= stops.Count) return;
        stops[index].waitTicks = RoundRule.WaitTicks(hours);
    }

    /// <summary>Why this stop can have no loading list, or null: colonists load only at a colony of the player's.</summary>
    public string? LoadRefusal(int index) =>
        index < 0 || index >= stops.Count || stops[index].HomeMap == null ? "PhobosSupplyRun_RefuseLoadOffMap".Translate().Resolve() : null;

    /// <summary>Sets how many of this kind colonists load at this stop; none takes it off the list.</summary>
    public void SetLoad(int index, ThingDef def, int count)
    {
        if (LoadRefusal(index) != null || def == null) return;
        List<ThingDefCountClass> load = stops[index].load;
        load.RemoveAll(l => l.thingDef == def);
        if (count > 0) load.Add(new ThingDefCountClass(def, count));
    }

    public string? StartRefusal()
    {
        if (!RouteControl.On) return "PhobosRound_RefuseSettingOff".Translate().Resolve();
        if (!RoundRule.Flyable(stops.Count, stops.Count > 0 && stops[0].tile == stops[stops.Count - 1].tile)) return "PhobosRound_RefuseTooFew".Translate().Resolve();
        return null;
    }

    public void Start()
    {
        if (StartRefusal() is { } why)
        {
            Messages.Message(why, Vehicle, MessageTypeDefOf.RejectInput, false);
            return;
        }
        running = true;
        at = -1;
        waitUntil = -1;
        waitingWhy = null;
        heldForLink = false;
        Messages.Message("PhobosSupplyRun_Started".Translate(Vehicle.LabelShort.Named("VEHICLE"), stops.Count.Named("COUNT")), Vehicle, MessageTypeDefOf.SilentInput, false);
    }

    public void Stop()
    {
        running = false;
        waitUntil = -1;
        waitingWhy = null;
        heldForLink = false;
        StopLoading();
        if (exitCell.IsValid && Vehicle.Spawned) VehicleAutoDrive.Halt(Vehicle, exitCell);
        exitCell = IntVec3.Invalid;
        if (Vehicle.Spawned || !RemoteControl.CrewDrives(Vehicle) && Vehicle.GetVehicleCaravan() is { } convoy && !convoy.vehiclePather.Moving) crewless = false;
        Messages.Message("PhobosSupplyRun_Stopped".Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.SilentInput, false);
    }

    // ---- Driving it ----

    public override void CompTick()
    {
        base.CompTick();
        if (!Vehicle.Spawned || !Gen.IsHashIntervalTick(Vehicle, exitCell.IsValid ? DrivingOutTicks : LookTicks)) return;
        if (!running)
        {
            crewless = false;
            return;
        }
        if (exitCell.IsValid) DriveOut(Vehicle.Map);
        else LookOnMap();
    }

    void Waits(string why)
    {
        if (why == waitingWhy) return;
        waitingWhy = why;
        Messages.Message("PhobosSupplyRun_Waits".Translate(Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")), Vehicle, MessageTypeDefOf.CautionInput, false);
    }

    int Next => stops.Count == 0 ? -1 : (at + 1) % stops.Count;

    /// <summary>Why it cannot set off now, or null: someone to drive (a driver, or the route kit working), fuel, and reach home for a crewless
    /// run on the world map (both settings ask it to set off in reach).</summary>
    string? LeaveRefusal(Caravan? convoy)
    {
        if (!RemoteControl.CrewDrives(Vehicle) && !RouteControl.KitWorks(Vehicle)) return "PhobosSupplyRun_WaitNoDriver".Translate().Resolve();
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosSupplyRun_WaitNoFuel".Translate().Resolve();
        if (convoy != null && !RemoteControl.CrewDrives(Vehicle) && ConvoyRadio.Available && !ConvoyRadio.InTouch(convoy))
            return "PhobosSupplyRun_WaitNoReach".Translate().Resolve();
        return null;
    }

    void LookOnMap()
    {
        Map map = Vehicle.Map;
        int here = stops.FindIndex(s => s.tile == map.Tile);
        if (here < 0)
        {
            Waits("PhobosRound_WaitNotAtStop".Translate().Resolve());
            return;
        }
        if (at != here)
        {
            at = here;
            waitUntil = -1;
        }
        int now = Find.TickManager.TicksGame;
        if (waitUntil < 0)
        {
            Arrive(stops[at], null);
            StartLoading(stops[at]);
            waitUntil = now + stops[at].waitTicks;
        }
        // The owner's card of 27 Nov 2026, "Loaded, or time up": with a loading list it leaves once the list is loaded, or when its time is up.
        // Registering again is a no-op while registered, and puts the haulers back to work after a load.
        if (loading) map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.LoadVehicle);
        bool loaded = loading && !AnyLeftToLoad;
        if (!loaded && !RoundRule.WaitOver(now, waitUntil)) return;
        if (LeaveRefusal(null) is { } why)
        {
            Waits(why);
            return;
        }
        if (loading)
        {
            Messages.Message((loaded ? "PhobosSupplyRun_Loaded" : "PhobosSupplyRun_LoadedPart").Translate(Vehicle.LabelShort.Named("VEHICLE"),
                stops[at].Label.Named("PLACE")), Vehicle, loaded ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput, false);
            if (Seen.Wants("vehicles.supply-run", Vehicle, "loaded")) Seen.Note("vehicles.supply-run", Vehicle, loaded ? "all" : "part", "loaded");
        }
        StopLoading();
        // Framework 0.332.0: it drives to the map's edge before leaving, as any convoy does (Vehicle Framework's own exit spot).
        crewless = !RemoteControl.CrewDrives(Vehicle);
        if (!CellFinderExtended.TryFindBestExitSpot(Vehicle, out IntVec3 edge))
        {
            crewless = false;
            Waits("PhobosSupplyRun_WaitCannotLeave".Translate().Resolve());
            return;
        }
        exitCell = edge;
        DriveOut(map);
    }

    /// <summary>
    /// A look while it drives to the edge: at the edge it leaves as a convoy of its own; otherwise it keeps (or takes again) its drive there,
    /// starting its engine as a driver would. A crewless run's crew check answers yes from here (RemoteControlOperatorsPatch).
    /// </summary>
    void DriveOut(Map map)
    {
        if (Vehicle.Position.InHorDistOf(exitCell, Vehicle.VehicleDef.Size.z))
        {
            VehicleAutoDrive.Halt(Vehicle, exitCell);
            exitCell = IntVec3.Invalid;
            if (Seen.Wants("vehicles.supply-run", Vehicle, "edge")) Seen.Note("vehicles.supply-run", Vehicle, map.Parent.LabelCap, "edge");
            int next = Next;
            VehicleCaravan? convoy = CaravanHelper.ExitMapAndCreateVehicleCaravan(new Pawn[] { Vehicle }, Faction.OfPlayer, map.Tile, stops[next].tile, stops[next].tile, sendMessage: false);
            if (convoy == null)
            {
                crewless = false;
                Waits("PhobosSupplyRun_WaitCannotLeave".Translate().Resolve());
                return;
            }
            Head(convoy, next);
            return;
        }
        if (VehicleAutoDrive.HeadingFor(Vehicle, exitCell) && VehicleAutoDrive.Moving(Vehicle)) return;
        if (VehicleAutoDrive.CannotDrive(Vehicle) is { } refused || !VehicleAutoDrive.StartEngine(Vehicle, out _) || !VehicleAutoDrive.DriveTo(Vehicle, exitCell))
        {
            exitCell = IntVec3.Invalid;
            crewless = false;
            Waits(VehicleAutoDrive.CannotDrive(Vehicle) ?? "PhobosSupplyRun_WaitCannotLeave".Translate().Resolve());
        }
    }

    // ---- Loading at a colony (Framework 0.332.0) ----

    bool AnyLeftToLoad => Vehicle.cargoToLoad != null && Vehicle.cargoToLoad.Any(t => t.CountToTransfer > 0);

    /// <summary>
    /// Asks colonists to load the stop's list, less what it carries already, from what lies on the map unforbidden: Vehicle Framework's own
    /// loading, as its cargo window sets it (VehiclePawn.cargoToLoad and the "load vehicle" lister), so haulers carry it in by the game's
    /// work priorities. What the colony lacks is loaded as far as it goes.
    /// </summary>
    void StartLoading(SupplyStop stop)
    {
        if (stop.HomeMap == null || stop.load.Count == 0 || !Vehicle.Spawned) return;
        Map map = Vehicle.Map;
        Vehicle.cargoToLoad ??= new List<TransferableOneWay>();
        Vehicle.cargoToLoad.Clear();
        foreach (ThingDefCountClass want in stop.load)
        {
            int need = want.count - Vehicle.inventory.innerContainer.TotalStackCountOfDef(want.thingDef);
            if (need <= 0) continue;
            List<Thing> stacks = map.listerThings.ThingsOfDef(want.thingDef)
                .Where(t => t.Spawned && !t.IsForbidden(Faction.OfPlayer) && !t.Position.Fogged(map)).ToList();
            if (stacks.Count == 0) continue;
            var transfer = new TransferableOneWay();
            transfer.things.AddRange(stacks);
            transfer.AdjustTo(Mathf.Min(need, transfer.MaxCount));
            if (transfer.CountToTransfer > 0) Vehicle.cargoToLoad.Add(transfer);
        }
        if (Vehicle.cargoToLoad.Count == 0) return;
        map.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(Vehicle, ReservationType.LoadVehicle);
        loading = true;
    }

    void StopLoading()
    {
        if (!loading) return;
        loading = false;
        Vehicle.cargoToLoad?.Clear();
        if (Vehicle.Spawned) Vehicle.Map.GetCachedMapComponent<VehicleReservationManager>().RemoveLister(Vehicle, ReservationType.LoadVehicle);
    }

    void Head(VehicleCaravan convoy, int next)
    {
        at = next;
        waitUntil = -1;
        waitingWhy = null;
        heldForLink = false;
        convoy.vehiclePather.StartPath(stops[next].tile, null, repathImmediately: true);
        if (Seen.Wants("vehicles.supply-run", Vehicle, crewless ? "crewless" : "driven")) Seen.Note("vehicles.supply-run", Vehicle, stops[next].Label, crewless ? "crewless" : "driven");
    }

    /// <summary>A look while it is a convoy on the world map (the world component's).</summary>
    internal void LookOnRoad(VehicleCaravan convoy)
    {
        if (!running)
        {
            if (!convoy.vehiclePather.Moving) crewless = false;
            return;
        }
        if (!RouteControl.On)
        {
            Waits("PhobosRound_RefuseSettingOff".Translate().Resolve());
            return;
        }
        if (convoy.VehiclesListForReading.Count > 1)
        {
            Waits("PhobosSupplyRun_WaitOthers".Translate().Resolve());
            return;
        }
        if (at < 0 || at >= stops.Count) at = Mathf.Max(0, stops.FindIndex(s => s.tile == convoy.Tile));
        SupplyStop target = stops[at];
        bool moving = convoy.vehiclePather.Moving;
        // Radio reach for a crewless run (the owner's two settings): out of reach while driving, it holds, carries on or turns back.
        if (crewless && ConvoyRadio.Available && moving && VehicleSystemsMod.Settings.supplyRunRadio == SupplyRunRadio.AllTheWay && !ConvoyRadio.InTouch(convoy))
        {
            switch (VehicleSystemsMod.Settings.supplyRunLostLink)
            {
                case SupplyRunLostLink.Hold:
                    convoy.vehiclePather.StopDead();
                    heldForLink = true;
                    LostLink(convoy, "PhobosSupplyRun_LostHold");
                    return;
                case SupplyRunLostLink.TurnBack:
                    int back = (at - 1 + stops.Count) % stops.Count;
                    LostLink(convoy, "PhobosSupplyRun_LostTurnBack");
                    Head(convoy, back);
                    return;
                default:
                    LostLink(convoy, "PhobosSupplyRun_LostCarryOn");
                    break;
            }
        }
        if (heldForLink)
        {
            if (!ConvoyRadio.InTouch(convoy)) return;
            heldForLink = false;
            Messages.Message("PhobosSupplyRun_LinkBack".Translate(Vehicle.LabelShort.Named("VEHICLE")), convoy, MessageTypeDefOf.PositiveEvent, false);
            Head(convoy, at);
            return;
        }
        if (moving) return;
        if (convoy.Tile != target.tile)
        {
            // Stopped short (a load, a fight, the fuel): set off again for where it was going.
            if (LeaveRefusal(convoy) is { } why) Waits(why);
            else Head(convoy, at);
            return;
        }
        if (target.HomeMap is { } map)
        {
            if (Seen.Wants("vehicles.supply-run", Vehicle, "entered")) Seen.Note("vehicles.supply-run", Vehicle, target.Label, "entered");
            CaravanEnterMapUtility.Enter(convoy, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (waitUntil < 0)
        {
            Arrive(target, convoy);
            waitUntil = now + target.waitTicks;
        }
        if (!RoundRule.WaitOver(now, waitUntil)) return;
        if (LeaveRefusal(convoy) is { } leave)
        {
            Waits(leave);
            return;
        }
        crewless = !RemoteControl.CrewDrives(Vehicle);
        Head(convoy, Next);
    }

    void LostLink(VehicleCaravan convoy, string key)
    {
        string why = key.Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (why == waitingWhy) return;
        waitingWhy = why;
        Messages.Message(why, convoy, MessageTypeDefOf.CautionInput, false);
        if (Seen.Wants("vehicles.supply-run", Vehicle, "lost-link")) Seen.Note("vehicles.supply-run", Vehicle, VehicleSystemsMod.Settings.supplyRunLostLink.ToString(), "lost-link");
    }

    /// <summary>What it does as it arrives at a stop: delivers or fetches, as set, and says what it did.</summary>
    void Arrive(SupplyStop stop, VehicleCaravan? convoy)
    {
        waitingWhy = null;
        int moved = stop.action switch
        {
            SupplyStopAction.Deliver => Deliver(stop),
            SupplyStopAction.Fetch => convoy != null ? Fetch(stop, convoy) : 0,
            _ => 0,
        };
        if (stop.action == SupplyStopAction.Wait) return;
        Messages.Message(("PhobosSupplyRun_" + stop.action).Translate(Vehicle.LabelShort.Named("VEHICLE"), moved.Named("COUNT"), stop.Label.Named("PLACE")),
            Vehicle.Spawned ? (LookTargets)Vehicle : convoy, MessageTypeDefOf.TaskCompletion, false);
        string variant = stop.action == SupplyStopAction.Deliver ? "deliver" : "fetch";
        if (moved > 0 && Seen.Wants("vehicles.supply-run", Vehicle, variant)) Seen.Note("vehicles.supply-run", Vehicle, moved + " things", variant);
    }

    int Deliver(SupplyStop stop)
    {
        int count = 0;
        List<Thing> load = Vehicle.inventory.innerContainer.ToList();
        if (Vehicle.Spawned)
        {
            foreach (Thing thing in load)
            {
                count += thing.stackCount;
                Vehicle.inventory.innerContainer.TryDrop(thing, Vehicle.Position, Vehicle.Map, ThingPlaceMode.Near, out _);
            }
            return count;
        }
        if (load.Count == 0) return 0;
        PhobosSlingWreck depot = Depots.At(stop.tile) ?? PhobosSlingWreck.MakeDepot(stop.tile);
        foreach (Thing thing in load)
        {
            count += thing.stackCount;
            depot.Keep(thing);
        }
        return count;
    }

    int Fetch(SupplyStop stop, VehicleCaravan convoy)
    {
        if (Depots.At(stop.tile) is not { } depot) return 0;
        int count = 0;
        foreach (Thing thing in depot.Items.ToList())
        {
            if (convoy.MassUsage + thing.GetStatValue(StatDefOf.Mass) * thing.stackCount > convoy.MassCapacity) continue;
            thing.holdingOwner?.Remove(thing);
            int stack = thing.stackCount;
            if (Vehicle.inventory.innerContainer.TryAdd(thing)) count += stack;
            else depot.Keep(thing);
        }
        convoy.RecacheVehicles();
        depot.RemoveIfEmpty();
        return count;
    }

    // ---- What the player sees ----

    /// <summary>The Fleet board's and the inspect pane's line while it runs, or null.</summary>
    public string? DoingLine
    {
        get
        {
            if (!running || stops.Count == 0) return null;
            if (waitingWhy != null) return "PhobosSupplyRun_DoingWaiting".Translate(waitingWhy.Named("REASON")).Resolve();
            SupplyStop stop = stops[Mathf.Clamp(at, 0, stops.Count - 1)];
            return (crewless ? "PhobosSupplyRun_DoingCrewless" : "PhobosSupplyRun_Doing").Translate(stop.Label.Named("PLACE")).Resolve();
        }
    }

    public override string? CompInspectStringExtra() => DoingLine ?? (stops.Count > 0 ? "PhobosSupplyRun_InspectPlanned".Translate(stops.Count.Named("COUNT")).Resolve() : null);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        foreach (Gizmo gizmo in Buttons()) yield return gizmo;
    }

    public override IEnumerable<Gizmo> CompCaravanGizmos()
    {
        foreach (Gizmo gizmo in base.CompCaravanGizmos()) yield return gizmo;
        foreach (Gizmo gizmo in Buttons()) yield return gizmo;
    }

    IEnumerable<Gizmo> Buttons()
    {
        if (Vehicle.Faction != Faction.OfPlayer || !RouteControl.On) yield break;
        yield return new Command_Action
        {
            defaultLabel = "PhobosSupplyRun_Plan".Translate(),
            defaultDesc = "PhobosSupplyRun_PlanDesc".Translate(RoundRule.MostStops.Named("MOST")),
            icon = RouteIcons.Waypoint,
            action = () => SupplyRunPlanner.Begin(this),
        };
        if (stops.Count == 0) yield break;
        yield return new Command_Action
        {
            defaultLabel = "PhobosSupplyRun_Stops".Translate(),
            defaultDesc = "PhobosSupplyRun_StopsDesc".Translate(),
            icon = RouteIcons.Loop,
            action = () => SupplyRunPlanner.StopsMenu(this),
        };
        var go = new Command_Action
        {
            defaultLabel = (running ? "PhobosSupplyRun_StopGizmo" : "PhobosSupplyRun_StartGizmo").Translate(),
            defaultDesc = (running ? "PhobosSupplyRun_StopGizmoDesc" : "PhobosSupplyRun_StartGizmoDesc").Translate(),
            icon = running ? RouteIcons.Stop : RouteIcons.Follow,
            action = running ? Stop : Start,
        };
        if (!running && StartRefusal() is { } refused) go.Disable(refused);
        yield return go;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref stops, "PhobosRunStops", LookMode.Deep);
        Scribe_Values.Look(ref running, "PhobosRunRunning");
        Scribe_Values.Look(ref at, "PhobosRunAt", -1);
        Scribe_Values.Look(ref waitUntil, "PhobosRunWaitUntil", -1);
        Scribe_Values.Look(ref crewless, "PhobosRunCrewless");
        Scribe_Values.Look(ref heldForLink, "PhobosRunHeldForLink");
        Scribe_Values.Look(ref exitCell, "PhobosRunExitCell", IntVec3.Invalid);
        Scribe_Values.Look(ref loading, "PhobosRunLoading");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) stops ??= new List<SupplyStop>();
    }
}

/// <summary>Picking a supply run's stops on the world map, and its stops' menu. Presentation only: the run's comp says what may be added.</summary>
public static class SupplyRunPlanner
{
    public static void Begin(CompPhobosSupplyRun run)
    {
        run.BeginPlan();
        PickNext(run);
    }

    static void PickNext(CompPhobosSupplyRun run)
    {
        CameraJumper.TryShowWorld();
        PlanetTile last = run.Stops.Count > 0 ? run.Stops[run.Stops.Count - 1].tile : PlanetTile.Invalid;
        Find.WorldTargeter.BeginTargeting(target => Picked(run, target), canTargetTiles: true, mouseAttachment: RouteIcons.Waypoint,
            closeWorldTabWhenFinished: false, onUpdate: null, extraLabelGetter: target => run.AddStopRefusal(target) ?? default(TaggedString),
            canSelectTarget: null, originForClosest: last, showCancelButton: true);
    }

    static bool Picked(CompPhobosSupplyRun run, GlobalTargetInfo target)
    {
        if (run.AddStopRefusal(target) is { } why)
        {
            Messages.Message(why, MessageTypeDefOf.RejectInput, false);
            return false;
        }
        WorldObject? place = target.WorldObject is MapParent { HasMap: true, Faction.IsPlayer: true } or PhobosSlingWreck { IsDepot: true } ? target.WorldObject : null;
        run.AddStop(target.Tile, place);
        return false;
    }

    /// <summary>The stops, each opening its own choices: what it does there, and how long it waits.</summary>
    public static void StopsMenu(CompPhobosSupplyRun run)
    {
        var options = new List<FloatMenuOption>();
        for (int i = 0; i < run.Stops.Count; i++)
        {
            int index = i;
            SupplyStop stop = run.Stops[i];
            string label = "PhobosSupplyRun_StopLine".Translate((i + 1).Named("INDEX"), stop.Label.Named("PLACE"),
                ("PhobosSupplyRun_Action" + stop.action).Translate().Named("ACTION"), stop.waitTicks.ToStringTicksToPeriod().Named("WAIT")).Resolve();
            options.Add(new FloatMenuOption(label, () => StopMenu(run, index)));
        }
        Find.WindowStack.Add(new FloatMenu(options));
    }

    static readonly float[] WaitHours = { 0f, 1f, 6f, 24f };

    static void StopMenu(CompPhobosSupplyRun run, int index)
    {
        var options = new List<FloatMenuOption>();
        foreach (SupplyStopAction action in System.Enum.GetValues(typeof(SupplyStopAction)))
        {
            string label = ("PhobosSupplyRun_Action" + action).Translate().CapitalizeFirst();
            options.Add(run.ActionRefusal(index, action) is { } why
                ? new FloatMenuOption(label + ": " + why, null)
                : new FloatMenuOption(label, () => run.SetAction(index, action)));
        }
        foreach (float hours in WaitHours)
            options.Add(new FloatMenuOption("PhobosSupplyRun_WaitFor".Translate(RoundRule.WaitTicks(hours).ToStringTicksToPeriod().Named("WAIT")), () => run.SetWait(index, hours)));
        string loadLabel = "PhobosSupplyRun_LoadList".Translate(run.Stops[index].load.Count.Named("COUNT")).Resolve();
        options.Add(run.LoadRefusal(index) is { } noLoad
            ? new FloatMenuOption(loadLabel + ": " + noLoad, null)
            : new FloatMenuOption(loadLabel, () => Find.WindowStack.Add(new Dialog_PhobosSupplyLoad(run, index))));
        Find.WindowStack.Add(new FloatMenu(options));
    }
}

/// <summary>
/// A colony stop's loading list (Framework 0.332.0): every kind of thing the colony has counted, and anything already on the list, each with
/// how many the colony has and how many to load, set by preset buttons. Presentation only: the run's comp keeps the list.
/// </summary>
public sealed class Dialog_PhobosSupplyLoad : Window
{
    const float Row = 30f;
    readonly CompPhobosSupplyRun run;
    readonly int index;
    Vector2 scroll;

    public Dialog_PhobosSupplyLoad(CompPhobosSupplyRun run, int index)
    {
        this.run = run;
        this.index = index;
        doCloseX = true;
        doCloseButton = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(620f, 640f);

    List<(ThingDef def, int have)> Rows()
    {
        SupplyStop stop = run.Stops[index];
        Map? map = stop.HomeMap;
        var counted = map?.resourceCounter.AllCountedAmounts.Where(p => p.Value > 0).ToDictionary(p => p.Key, p => p.Value) ?? new Dictionary<ThingDef, int>();
        foreach (ThingDefCountClass line in stop.load)
            if (!counted.ContainsKey(line.thingDef)) counted[line.thingDef] = map?.resourceCounter.GetCount(line.thingDef) ?? 0;
        return counted.Select(p => (p.Key, p.Value)).OrderByDescending(r => stop.LoadOf(r.Key) > 0).ThenBy(r => r.Key.label).ToList();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (index >= run.Stops.Count)
        {
            Close();
            return;
        }
        SupplyStop stop = run.Stops[index];
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "PhobosSupplyRun_LoadTitle".Translate(stop.Label.Named("PLACE")));
        Text.Font = GameFont.Small;
        Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, 44f), "PhobosSupplyRun_LoadExplain".Translate());
        List<(ThingDef def, int have)> rows = Rows();
        var outer = new Rect(inRect.x, inRect.y + 84f, inRect.width, inRect.height - 84f - CloseButSize.y - 10f);
        var view = new Rect(0f, 0f, outer.width - 16f, rows.Count * Row);
        Widgets.BeginScrollView(outer, ref scroll, view);
        for (int i = 0; i < rows.Count; i++)
        {
            (ThingDef def, int have) = rows[i];
            var rect = new Rect(0f, i * Row, view.width, Row);
            if (i % 2 == 1) Widgets.DrawLightHighlight(rect);
            Widgets.ThingIcon(new Rect(rect.x, rect.y + 3f, 24f, 24f), def);
            int want = stop.LoadOf(def);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x + 30f, rect.y, 200f, Row), def.LabelCap.Truncate(200f));
            Widgets.Label(new Rect(rect.x + 232f, rect.y, 90f, Row), "PhobosSupplyRun_LoadHave".Translate(have.Named("COUNT")));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + 322f, rect.y, 60f, Row), want.ToString());
            Text.Anchor = TextAnchor.UpperLeft;
            float x = rect.x + 386f;
            foreach (int step in CompPhobosSupplyRun.LoadSteps.Reverse())
            {
                if (Widgets.ButtonText(new Rect(x, rect.y + 2f, 44f, Row - 4f), "-" + step) && want > 0) run.SetLoad(index, def, Mathf.Max(0, want - step));
                x += 46f;
            }
            foreach (int step in CompPhobosSupplyRun.LoadSteps)
            {
                if (Widgets.ButtonText(new Rect(x, rect.y + 2f, 44f, Row - 4f), "+" + step)) run.SetLoad(index, def, want + step);
                x += 46f;
            }
        }
        Widgets.EndScrollView();
    }
}

/// <summary>Looks at the supply runs whose trucks are convoys on the world map, a few times a game hour (Framework 0.328.0).</summary>
public sealed class WorldComponent_PhobosSupplyRuns : WorldComponent
{
    public WorldComponent_PhobosSupplyRuns(World world) : base(world) { }

    public override void WorldComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 != 83) return;
        foreach (Caravan caravan in Find.WorldObjects.Caravans.ToList())
        {
            if (caravan is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) continue;
            foreach (VehiclePawn vehicle in convoy.VehiclesListForReading.ToList())
            {
                if (vehicle.GetComp<CompPhobosSupplyRun>() is { } run && (run.Running || run.DrivesCrewless)) run.LookOnRoad(convoy);
            }
        }
    }
}
