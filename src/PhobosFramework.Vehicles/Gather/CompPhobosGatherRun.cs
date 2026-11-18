using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A vehicle's gathering run, configured in its Def (Framework 0.143.0; docs/development/vehicle-gathering-runs.md).</summary>
public class CompProperties_PhobosGatherRun : VehicleCompProperties
{
    /// <summary>What it gathers until the player chooses, written as a stockpile's filter is. Null for everything it may choose.</summary>
    public ThingFilter? defaultFilter;
    /// <summary>What the player may choose from. Null for everything.</summary>
    public ThingFilter? choosableFilter;
    /// <summary>How many things, nearest first, one look tests for a place to stand. Authored.</summary>
    public int thingsPerLook = 16;
    /// <summary>How long a drive may stand still before it gives that thing up, in ticks. Authored.</summary>
    public int stuckTicks = 2000;
    /// <summary>How many drives in a row that reach nothing end the run. Authored.</summary>
    public int failedDrivesToGiveUp = 5;
    /// <summary>Whether enemies threatening the map end the run. Agent default; the owner's choice of 19 Nov 2026.</summary>
    public bool stopForEnemies = true;
    /// <summary>The run's button picture: the game's haul designator until one of ours is drawn.</summary>
    public string runIconPath = "UI/Designators/Haul";
    /// <summary>The filter's button picture: the game's stone chunks category until one of ours is drawn.</summary>
    public string filterIconPath = "UI/Icons/ThingCategories/StoneChunks";

    public CompProperties_PhobosGatherRun() => compClass = typeof(CompPhobosGatherRun);

    public override void ResolveReferences(ThingDef parentDef)
    {
        base.ResolveReferences(parentDef);
        if (choosableFilter == null)
        {
            choosableFilter = new ThingFilter();
            choosableFilter.SetAllowAll(null);
        }
        else
        {
            choosableFilter.ResolveReferences();
        }
        defaultFilter?.ResolveReferences();
    }
}

/// <summary>
/// A vehicle that drives about the map by itself, stopping beside loose things the player chose and picking them up with its worker (the
/// Talus's loading grab), until its cargo is full or nothing it can reach is left (Framework 0.143.0; docs/development/vehicle-gathering-runs.md).
/// With a place to unload at and a worker that unloads over its tail (the Talus's tip), it backs up there when full, puts down what it
/// gathered and goes back out (Framework 0.144.0). It decides what is wanted and where to stand; the workers lift and tip; VehicleAutoDrive
/// drives. Nothing ticks without a run.
/// </summary>
public class CompPhobosGatherRun : VehicleComp
{
    ThingFilter? filter;
    Area? area;
    Building_Storage? unloadBuilding;
    Zone_Stockpile? unloadZone;
    GatherPhase phase;
    Thing? target;
    IntVec3 standCell = IntVec3.Invalid;
    Rot4 unloadFacing = Rot4.Invalid;
    bool lastTrip;
    int lastMovedTick;
    IntVec3 lastPosition = IntVec3.Invalid;
    int failedDrives;
    int loads;
    float kilograms;
    int places;
    int trips;
    int unloaded;
    bool engineStarted;

    // Derived, never saved: after a load a run may try a thing or a place it had given up on once more.
    readonly HashSet<int> outOfReach = new();
    readonly HashSet<IntVec3> badSpots = new();
    IVehicleGatherWorker? worker;
    IVehicleUnloadWorker? unloader;
    Predicate<Thing>? wanted;
    Predicate<Thing>? tips;
    string? filterWords;

    public CompProperties_PhobosGatherRun Props => (CompProperties_PhobosGatherRun)props;

    public bool Running => phase != GatherPhase.Idle;

    IVehicleGatherWorker? Worker => worker ??= Vehicle.AllComps.OfType<IVehicleGatherWorker>().FirstOrDefault();

    IVehicleUnloadWorker? Unloader => unloader ??= Vehicle.AllComps.OfType<IVehicleUnloadWorker>().FirstOrDefault();

    Predicate<Thing> Wanted => wanted ??= Wants;

    /// <summary>What a run's unloading puts down: what it gathers, so anything else aboard, such as spare parts, stays.</summary>
    Predicate<Thing> Tips => tips ??= thing => Filter.Allows(thing);

    ThingFilter ChoosableFilter => Props.choosableFilter!;

    /// <summary>The player's filter, made from the Def's default the first time it is needed for a run or the window.</summary>
    public ThingFilter Filter
    {
        get
        {
            if (filter == null)
            {
                filter = new ThingFilter(FilterChanged);
                filter.CopyAllowancesFrom(Props.defaultFilter ?? ChoosableFilter);
            }
            return filter;
        }
    }

    /// <summary>The filter as it stands, without making one: the Def's default until the player has chosen.</summary>
    ThingFilter CurrentFilter => filter ?? Props.defaultFilter ?? ChoosableFilter;

    void FilterChanged() => filterWords = null;

    /// <summary>The filter in a few words, kept until it changes.</summary>
    string FilterWords => filterWords ??= ThingFilterWords.Summary(CurrentFilter, ChoosableFilter);

    /// <summary>The area it keeps inside, or null for anywhere. An area deleted or on another map is dropped.</summary>
    public Area? ChosenArea
    {
        get
        {
            if (area == null) return null;
            Map? map = Vehicle.MapHeld;
            if (map == null || area.Map != map || !map.areaManager.AllAreas.Contains(area)) area = null;
            return area;
        }
    }

    public void SetArea(Area? chosen) => area = chosen;

    string AreaWords => ChosenArea?.Label ?? "PhobosFramework_FilterAnywhere".Translate().Resolve();

    // ---- Where it unloads (Framework 0.144.0) ----

    /// <summary>Whether it can unload by itself at all: a worker that unloads over its tail.</summary>
    bool CanUnload => Unloader is { CanUnloadAtAll: true };

    /// <summary>
    /// The cells of the place it unloads at, or null for none: one of the player's storage buildings or stockpile zones on its map. A place
    /// gone, emptied of cells, on another map or no longer the player's is dropped.
    /// </summary>
    List<IntVec3>? UnloadCells()
    {
        Map? map = Vehicle.MapHeld;
        if (unloadBuilding != null && (!unloadBuilding.Spawned || unloadBuilding.Map != map || unloadBuilding.Faction != Faction.OfPlayer)) unloadBuilding = null;
        if (unloadZone != null && (map == null || unloadZone.Map != map || !map.zoneManager.AllZones.Contains(unloadZone) || unloadZone.Cells.Count == 0)) unloadZone = null;
        if (unloadBuilding != null) return unloadBuilding.OccupiedRect().Cells.ToList();
        return unloadZone?.Cells;
    }

    bool HasUnloadPlace => CanUnload && UnloadCells() != null;

    string UnloadWords => unloadBuilding?.LabelShort ?? unloadZone?.label ?? "PhobosGather_ThePlace".Translate().Resolve();

    /// <summary>The one way to face when backing up to the place, for a building that says which side it opens on (the bulk bunker); else any.</summary>
    Rot4? UnloadFacingRule()
    {
        if (unloadBuilding?.def.GetModExtension<PhobosBackUpSideExtension>() is not { } side) return null;
        int? facing = GatherRule.BackUpFacing(side.side, unloadBuilding.Rotation.AsInt);
        return facing is { } rot ? new Rot4(rot) : null;
    }

    public void SetUnloadPlace(Building_Storage? building, Zone_Stockpile? zone)
    {
        unloadBuilding = building;
        unloadZone = building == null ? zone : null;
    }

    /// <summary>Whether a storage would take at least one kind the run gathers: only those are offered as places to unload.</summary>
    bool TakesSomethingGathered(StorageSettings? settings)
    {
        if (settings == null) return false;
        foreach (ThingDef def in CurrentFilter.AllowedThingDefs)
        {
            if (settings.AllowedToAccept(def)) return true;
        }
        return false;
    }

    /// <summary>Nowhere, then the player's storage buildings and stockpile zones that take something it gathers, nearest first.</summary>
    List<FloatMenuOption> UnloadOptions()
    {
        var options = new List<FloatMenuOption>
        {
            new("PhobosGather_UnloadNowhere".Translate().CapitalizeFirst(), () => SetUnloadPlace(null, null)),
        };
        Map? map = Vehicle.MapHeld;
        if (map == null) return options;
        IntVec3 from = Vehicle.PositionHeld;
        var places = new List<(string Label, int Distance, Building_Storage? Building, Zone_Stockpile? Zone)>();
        foreach (Building_Storage store in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
        {
            if (!TakesSomethingGathered(store.GetStoreSettings())) continue;
            places.Add((store.LabelCap, (int)store.Position.DistanceTo(from), store, null));
        }
        foreach (Zone_Stockpile zone in map.zoneManager.AllZones.OfType<Zone_Stockpile>())
        {
            if (zone.Cells.Count == 0 || !TakesSomethingGathered(zone.GetStoreSettings())) continue;
            places.Add((zone.label, (int)zone.Cells[0].DistanceTo(from), null, zone));
        }
        foreach ((string label, int distance, Building_Storage? building, Zone_Stockpile? zone) in places.OrderBy(p => p.Distance))
        {
            var option = new FloatMenuOption("PhobosGather_PlaceOption".Translate(label.Named("PLACE"), distance.Named("CELLS")).Resolve(),
                () => SetUnloadPlace(building, zone));
            if (building != null) option.mouseoverGuiAction = _ => TargetHighlighter.Highlight(building);
            options.Add(option);
        }
        return options;
    }

    string UnloadChoiceWords => UnloadCells() == null
        ? "PhobosGather_UnloadNowhere".Translate().Resolve()
        : UnloadWords;

    /// <summary>
    /// What the run wants: a spawned item on its map that the filter allows, inside the area if one is set, not under fog, not forbidden, in no
    /// storage, claimed by nobody of the player's side, not burning, and that the worker could pick up at all.
    /// </summary>
    bool Wants(Thing thing) => Wants(thing, ChosenArea);

    bool Wants(Thing thing, Area? within)
    {
        Map? map = Vehicle.Map;
        if (map == null || !thing.Spawned || thing.Map != map || thing.def.category != ThingCategory.Item) return false;
        if (!Filter.Allows(thing)) return false;
        if (within != null && !within[thing.Position]) return false;
        if (thing.Position.Fogged(map) || thing.IsForbidden(Faction.OfPlayer) || thing.IsInAnyStorage() || thing.IsBurning()) return false;
        if (map.reservationManager.IsReservedByAnyoneOf(thing, Faction.OfPlayer)) return false;
        return Worker?.CouldGather(thing) == true;
    }

    /// <summary>Whether anything aboard is of what it gathers: what an unloading would put down.</summary>
    bool AnythingToUnload()
    {
        ThingOwner cargo = Vehicle.inventory.innerContainer;
        for (int i = 0; i < cargo.Count; i++)
        {
            if (Tips(cargo[i])) return true;
        }
        return false;
    }

    // ---- Starting and stopping ----

    /// <summary>Why a run cannot start now, or null.</summary>
    public string? CannotStart()
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        IVehicleGatherWorker? w = Worker;
        if (w == null || !w.CanGatherAtAll) return "PhobosGather_NoWorker".Translate(name).Resolve();
        if (CurrentFilter.AllowedDefCount == 0) return "PhobosGather_NothingChosen".Translate().Resolve();
        if (Props.stopForEnemies && Vehicle.Spawned && GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map)) return "PhobosGather_EnemiesNear".Translate().Resolve();
        if (VehicleAutoDrive.CannotDrive(Vehicle) is { } cannot) return cannot;
        if (w.GatherRoomKilograms <= 0f && !HasUnloadPlace) return "PhobosGather_AlreadyFull".Translate(name).Resolve();
        return null;
    }

    public void Start()
    {
        if (Running) return;
        if (CannotStart() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        // Vehicle Framework says why if it will not start.
        if (!VehicleAutoDrive.StartEngine(Vehicle, out bool started)) return;
        engineStarted = started;
        VehicleAutoDrive.ClearOrders(Vehicle);
        phase = GatherPhase.Seeking;
        loads = 0;
        kilograms = 0f;
        places = 0;
        trips = 0;
        unloaded = 0;
        failedDrives = 0;
        lastTrip = false;
        target = null;
        standCell = IntVec3.Invalid;
        unloadFacing = Rot4.Invalid;
        outOfReach.Clear();
        badSpots.Clear();
        Messages.Message("PhobosGather_Started".Translate(Vehicle.LabelShort.Named("VEHICLE"), FilterWords.Named("WHAT")).Resolve(), Vehicle,
            MessageTypeDefOf.SilentInput, historical: false);
        Seen.Note("vehicles.gather-run.start", Vehicle);
        Seek();
    }

    public void Stop() => End(GatherRunEnd.Stopped);

    /// <summary>Ends the run: stops the drive or the worker, switches off the engine it started where that is safe, and reports once.</summary>
    void End(GatherRunEnd why, string? reason = null)
    {
        if (phase == GatherPhase.Idle) return;
        GatherPhase was = phase;
        phase = GatherPhase.Idle;
        if (was == GatherPhase.Working && Worker is { } w)
        {
            if (w.Gathering) w.StopGathering();
            CollectWork(w);
        }
        if (was == GatherPhase.Unloading && Unloader is { } u)
        {
            if (u.Unloading) u.StopUnloading();
            CollectUnload(u);
        }
        if (was is GatherPhase.Driving or GatherPhase.Returning && standCell.IsValid && Vehicle.Spawned) VehicleAutoDrive.Halt(Vehicle, standCell);
        if (engineStarted && Vehicle.Spawned && GatherRule.SwitchesOffAtEnd(why)) VehicleAutoDrive.StopEngine(Vehicle);
        engineStarted = false;
        target = null;
        standCell = IntVec3.Invalid;
        unloadFacing = Rot4.Invalid;
        failedDrives = 0;
        lastTrip = false;
        outOfReach.Clear();
        badSpots.Clear();
        Report(why, reason);
        if (Seen.Wants("vehicles.gather-run.end", Vehicle, why.ToString()))
        {
            Seen.Note("vehicles.gather-run.end", Vehicle, variant: why.ToString(), values: new List<KeyValuePair<string, double>>
            {
                new("loads", loads),
                new("kg", kilograms),
                new("places", places),
                new("trips", trips),
            });
        }
    }

    void Report(GatherRunEnd why, string? reason)
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        string head = loads > 0
            ? "PhobosGather_Done".Translate(name, loads.Named("COUNT"), kilograms.ToString("N0").Named("MASS"), places.Named("PLACES")).Resolve()
            : "PhobosGather_DoneNothing".Translate(name).Resolve();
        if (trips > 0)
        {
            head += " " + "PhobosGather_DoneUnloaded".Translate(unloaded.Named("COUNT"), UnloadWords.Named("PLACE"), trips.Named("TRIPS")).Resolve();
        }
        string because = reason ?? WhyLine(why, name);
        MessageTypeDef type = GatherRule.EndedWell(why) || why == GatherRunEnd.Stopped ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput;
        Messages.Message(because.NullOrEmpty() ? head : head + " " + because, Vehicle, type);
    }

    static string WhyLine(GatherRunEnd why, NamedArgument name) => why switch
    {
        GatherRunEnd.Full => "PhobosGather_EndFull".Translate().Resolve(),
        GatherRunEnd.NothingLeft => "PhobosGather_EndNothingLeft".Translate().Resolve(),
        GatherRunEnd.NoWay => "PhobosGather_EndNoWay".Translate().Resolve(),
        GatherRunEnd.Orders => "PhobosGather_EndOrders".Translate(name).Resolve(),
        GatherRunEnd.EngineOff => "PhobosGather_EndEngineOff".Translate().Resolve(),
        GatherRunEnd.Threat => "PhobosGather_EndThreat".Translate().Resolve(),
        GatherRunEnd.Stopped => "PhobosGather_EndStopped".Translate().Resolve(),
        GatherRunEnd.Fault => "PhobosGather_EndFault".Translate().Resolve(),
        GatherRunEnd.UnloadFull => "PhobosGather_EndUnloadFull".Translate().Resolve(),
        GatherRunEnd.NoWayToUnload => "PhobosGather_EndNoWayToUnload".Translate().Resolve(),
        GatherRunEnd.UnloadPlaceGone => "PhobosGather_EndUnloadPlaceGone".Translate().Resolve(),
        _ => "",
    };

    /// <summary>Adds what the worker picked up at the place it is leaving.</summary>
    void CollectWork(IVehicleGatherWorker w)
    {
        loads += w.GatheredLoads;
        kilograms += w.GatheredKilograms;
    }

    /// <summary>Adds what an unloading put down, as one trip if it put anything down.</summary>
    void CollectUnload(IVehicleUnloadWorker u)
    {
        if (u.UnloadedCount <= 0) return;
        trips++;
        unloaded += u.UnloadedCount;
    }

    // ---- The run ----

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (phase == GatherPhase.Idle || !Vehicle.Spawned) return;
        if (Vehicle.Faction != Faction.OfPlayer)
        {
            End(GatherRunEnd.Orders);
            return;
        }
        if (Props.stopForEnemies && GenHostility.AnyHostileActiveThreatToPlayer(Vehicle.Map))
        {
            End(GatherRunEnd.Threat);
            return;
        }
        switch (phase)
        {
            case GatherPhase.Seeking:
                Seek();
                break;
            case GatherPhase.Driving:
                DriveTick();
                break;
            case GatherPhase.Working:
                WorkTick();
                break;
            case GatherPhase.Returning:
                ReturnTick();
                break;
            case GatherPhase.Unloading:
                UnloadTick();
                break;
        }
    }

    /// <summary>
    /// Chooses what to fetch next: every thing of the kinds the filter allows that the run wants and has not given up on, nearest first. One
    /// already in reach is worked at once; otherwise the first with a place to stand is driven to. Up to <see cref="CompProperties_PhobosGatherRun.thingsPerLook"/>
    /// a look; the rest wait for the next rare tick. Full, or with nothing left, it takes what it has to its unloading place if it has one.
    /// </summary>
    void Seek()
    {
        IVehicleGatherWorker? w = Worker;
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        if (w == null || !w.CanGatherAtAll)
        {
            End(GatherRunEnd.Fault, "PhobosGather_NoWorker".Translate(name).Resolve());
            return;
        }
        if (!Vehicle.Drafted)
        {
            End(GatherRunEnd.EngineOff);
            return;
        }
        // Between drives no order of ours is out, so any drive it has is one the player gave.
        if (VehicleAutoDrive.HeadingElsewhere(Vehicle, IntVec3.Invalid))
        {
            End(GatherRunEnd.Orders);
            return;
        }
        if (VehicleAutoDrive.CannotDrive(Vehicle) is { } cannot)
        {
            End(GatherRunEnd.Fault, cannot);
            return;
        }
        if (w.GatherRoomKilograms <= 0f)
        {
            if (HasUnloadPlace) BeginReturn(false);
            else End(GatherRunEnd.Full);
            return;
        }
        Map map = Vehicle.Map;
        IntVec3 from = Vehicle.Position;
        Area? within = ChosenArea;
        var candidates = new List<Thing>();
        foreach (ThingDef def in Filter.AllowedThingDefs)
        {
            List<Thing> things = map.listerThings.ThingsOfDef(def);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!outOfReach.Contains(thing.thingIDNumber) && Wants(thing, within)) candidates.Add(thing);
            }
        }
        if (candidates.Count == 0)
        {
            if (HasUnloadPlace && AnythingToUnload()) BeginReturn(true);
            else End(GatherRunEnd.NothingLeft);
            return;
        }
        candidates.Sort((a, b) => a.Position.DistanceToSquared(from).CompareTo(b.Position.DistanceToSquared(from)));
        int ring = GatherRule.StandOffRing(Vehicle.def.Size.x, Vehicle.def.Size.z);
        int looked = 0;
        foreach (Thing thing in candidates)
        {
            if (looked++ >= Props.thingsPerLook) return;
            if (w.InGatherReach(thing.Position))
            {
                target = thing;
                BeginWork();
                return;
            }
            if (VehicleAutoDrive.TryFindStandCell(Vehicle, thing.Position, ring, out IntVec3 cell) && cell != Vehicle.Position)
            {
                Drive(thing, cell);
                return;
            }
            outOfReach.Add(thing.thingIDNumber);
        }
    }

    void Drive(Thing thing, IntVec3 cell)
    {
        if (!VehicleAutoDrive.DriveTo(Vehicle, cell))
        {
            target = thing;
            FailedDrive();
            return;
        }
        target = thing;
        standCell = cell;
        phase = GatherPhase.Driving;
        lastMovedTick = Find.TickManager.TicksGame;
        lastPosition = Vehicle.Position;
    }

    /// <summary>
    /// One look while driving: on its way it waits; stopped with the thing in reach, it works; stopped short, standing still too long, or the
    /// thing gone, it looks again. Another drive order, or the engine switched off, ends the run.
    /// </summary>
    void DriveTick()
    {
        if (!Vehicle.Drafted)
        {
            End(GatherRunEnd.EngineOff);
            return;
        }
        if (VehicleAutoDrive.HeadingElsewhere(Vehicle, standCell))
        {
            End(GatherRunEnd.Orders);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (Vehicle.Position != lastPosition)
        {
            lastPosition = Vehicle.Position;
            lastMovedTick = now;
        }
        bool gone = target == null || !Wants(target);
        if (VehicleAutoDrive.HeadingFor(Vehicle, standCell) || VehicleAutoDrive.Moving(Vehicle))
        {
            if (gone)
            {
                // Someone else took it, or it was forbidden: look again from here.
                VehicleAutoDrive.Halt(Vehicle, standCell);
                LookAgain();
                return;
            }
            if (GatherRule.Stuck(now - lastMovedTick, Props.stuckTicks))
            {
                VehicleAutoDrive.Halt(Vehicle, standCell);
                FailedDrive();
            }
            return;
        }
        if (!gone && Worker!.InGatherReach(target!.Position))
        {
            failedDrives = 0;
            BeginWork();
            return;
        }
        if (gone) LookAgain();
        else FailedDrive();
    }

    void LookAgain()
    {
        target = null;
        standCell = IntVec3.Invalid;
        unloadFacing = Rot4.Invalid;
        phase = GatherPhase.Seeking;
    }

    /// <summary>A drive that reached nothing: the thing is given up for this run, and enough of them in a row end it.</summary>
    void FailedDrive()
    {
        if (target != null) outOfReach.Add(target.thingIDNumber);
        LookAgain();
        failedDrives++;
        if (GatherRule.GiveUp(failedDrives, Props.failedDrivesToGiveUp)) End(GatherRunEnd.NoWay);
    }

    void BeginWork()
    {
        GatherWorkEnd started = Worker!.StartGathering(Wanted, out string? refusal);
        if (started == GatherWorkEnd.None)
        {
            phase = GatherPhase.Working;
            places++;
            if (Seen.Wants("vehicles.gather-run.lift", Vehicle)) Seen.Note("vehicles.gather-run.lift", Vehicle, target?.def.label);
            return;
        }
        if (target != null) outOfReach.Add(target.thingIDNumber);
        LookAgain();
        if (GatherRule.AfterWork(started) is { } end) End(end, refusal);
    }

    /// <summary>One look while working: it waits for the worker, then looks for the next thing or ends with the worker's reason.</summary>
    void WorkTick()
    {
        IVehicleGatherWorker w = Worker!;
        if (w.Gathering) return;
        CollectWork(w);
        // Still lying there once the worker has finished in reach: it could not take it, so it is not chosen again.
        if (target is { Spawned: true }) outOfReach.Add(target.thingIDNumber);
        LookAgain();
        if (GatherRule.AfterWork(w.LastGatherEnd) is { } end)
        {
            // Full, with somewhere to take it: back to the place, and out again (Framework 0.144.0).
            if (end == GatherRunEnd.Full && HasUnloadPlace) BeginReturn(false);
            else End(end, w.LastGatherReason);
            return;
        }
        Seek();
    }

    // ---- Taking the load to its place (Framework 0.144.0) ----

    /// <summary>
    /// Sets off to back up to the place it unloads at, tail first. <paramref name="last"/> marks the trip that ends the run once unloaded,
    /// when nothing is left to gather.
    /// </summary>
    void BeginReturn(bool last)
    {
        lastTrip = last;
        target = null;
        List<IntVec3>? cells = UnloadCells();
        if (cells == null)
        {
            End(GatherRunEnd.UnloadPlaceGone);
            return;
        }
        if (!VehicleAutoDrive.TryFindBackUpSpot(Vehicle, cells, UnloadFacingRule(), badSpots, out IntVec3 stand, out Rot4 facing))
        {
            End(GatherRunEnd.NoWayToUnload);
            return;
        }
        standCell = stand;
        unloadFacing = facing;
        if (stand == Vehicle.Position)
        {
            VehicleAutoDrive.Face(Vehicle, facing);
            BeginUnload();
            return;
        }
        if (!VehicleAutoDrive.DriveTo(Vehicle, stand, facing))
        {
            End(GatherRunEnd.NoWayToUnload);
            return;
        }
        phase = GatherPhase.Returning;
        lastMovedTick = Find.TickManager.TicksGame;
        lastPosition = Vehicle.Position;
    }

    /// <summary>
    /// One look on the way back: on its way it waits; at the place it turns tail to it and unloads; stuck, or stopped short, it tries another
    /// spot, and enough failures in a row end the run. The place gone, another order or the engine switched off end it too.
    /// </summary>
    void ReturnTick()
    {
        if (!Vehicle.Drafted)
        {
            End(GatherRunEnd.EngineOff);
            return;
        }
        if (VehicleAutoDrive.HeadingElsewhere(Vehicle, standCell))
        {
            End(GatherRunEnd.Orders);
            return;
        }
        if (UnloadCells() == null)
        {
            End(GatherRunEnd.UnloadPlaceGone);
            return;
        }
        int now = Find.TickManager.TicksGame;
        if (Vehicle.Position != lastPosition)
        {
            lastPosition = Vehicle.Position;
            lastMovedTick = now;
        }
        if (VehicleAutoDrive.HeadingFor(Vehicle, standCell) || VehicleAutoDrive.Moving(Vehicle))
        {
            if (!GatherRule.Stuck(now - lastMovedTick, Props.stuckTicks)) return;
            VehicleAutoDrive.Halt(Vehicle, standCell);
            FailedReturn();
            return;
        }
        if (Vehicle.Position == standCell)
        {
            VehicleAutoDrive.Face(Vehicle, unloadFacing);
            failedDrives = 0;
            BeginUnload();
            return;
        }
        FailedReturn();
    }

    void FailedReturn()
    {
        badSpots.Add(standCell);
        failedDrives++;
        if (GatherRule.GiveUp(failedDrives, Props.failedDrivesToGiveUp)) End(GatherRunEnd.NoWayToUnload);
        else BeginReturn(lastTrip);
    }

    void BeginUnload()
    {
        IVehicleUnloadWorker? u = Unloader;
        if (u == null)
        {
            End(GatherRunEnd.UnloadPlaceGone);
            return;
        }
        UnloadEnd started = u.StartUnloading(Tips, out string? refusal);
        if (started == UnloadEnd.None)
        {
            phase = GatherPhase.Unloading;
            if (Seen.Wants("vehicles.gather-run.unload", Vehicle, unloadBuilding != null ? "building" : "zone"))
            {
                Seen.Note("vehicles.gather-run.unload", Vehicle, UnloadWords, unloadBuilding != null ? "building" : "zone");
            }
            return;
        }
        AfterUnload(started, refusal);
    }

    /// <summary>One look while unloading: it waits for the worker, then goes back out for more or ends with the worker's reason.</summary>
    void UnloadTick()
    {
        IVehicleUnloadWorker? u = Unloader;
        if (u == null)
        {
            End(GatherRunEnd.UnloadPlaceGone);
            return;
        }
        if (u.Unloading) return;
        CollectUnload(u);
        AfterUnload(u.LastUnloadEnd, u.LastUnloadReason);
    }

    void AfterUnload(UnloadEnd end, string? reason)
    {
        bool last = lastTrip;
        lastTrip = false;
        LookAgain();
        badSpots.Clear();
        bool roomLeft = Worker is { GatherRoomKilograms: > 0f };
        if (GatherRule.AfterUnload(end, last, roomLeft) is { } runEnd) End(runEnd, runEnd is GatherRunEnd.UnloadFull or GatherRunEnd.Fault ? reason : null);
    }

    // ---- The map, the save and the screen ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // The tests are not saved: a run working or unloading when the game was saved hands its test back.
        if (phase == GatherPhase.Working) Worker?.ResumeGathering(Wanted);
        if (phase == GatherPhase.Unloading) Unloader?.ResumeUnloading(Tips);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map (a caravan, a launch) ends a run where it is: the player sent it.
        phase = GatherPhase.Idle;
        engineStarted = false;
        target = null;
        standCell = IntVec3.Invalid;
        unloadFacing = Rot4.Invalid;
        lastTrip = false;
        outOfReach.Clear();
        badSpots.Clear();
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        bool saving = Scribe.mode == LoadSaveMode.Saving;
        if (saving)
        {
            if (target is { Destroyed: true }) target = null;
            _ = ChosenArea;
            _ = UnloadCells();
        }
        // A vehicle that has never run, or has no area, place or target, writes nothing for them.
        if (!saving || filter != null) Scribe_Deep.Look(ref filter, "PhobosGatherFilter", (Action)FilterChanged);
        if (!saving || area != null) Scribe_References.Look(ref area, "PhobosGatherArea");
        if (!saving || unloadBuilding != null) Scribe_References.Look(ref unloadBuilding, "PhobosGatherUnloadBuilding");
        if (!saving || unloadZone != null) Scribe_References.Look(ref unloadZone, "PhobosGatherUnloadZone");
        Scribe_Values.Look(ref phase, "PhobosGatherPhase", GatherPhase.Idle);
        if (!saving || target != null) Scribe_References.Look(ref target, "PhobosGatherTarget");
        Scribe_Values.Look(ref standCell, "PhobosGatherStandCell", IntVec3.Invalid);
        Scribe_Values.Look(ref unloadFacing, "PhobosGatherUnloadFacing", Rot4.Invalid);
        Scribe_Values.Look(ref lastTrip, "PhobosGatherLastTrip");
        Scribe_Values.Look(ref lastMovedTick, "PhobosGatherLastMoved");
        Scribe_Values.Look(ref lastPosition, "PhobosGatherLastPosition", IntVec3.Invalid);
        Scribe_Values.Look(ref failedDrives, "PhobosGatherFailedDrives");
        Scribe_Values.Look(ref loads, "PhobosGatherLoads");
        Scribe_Values.Look(ref kilograms, "PhobosGatherKilograms");
        Scribe_Values.Look(ref places, "PhobosGatherPlaces");
        Scribe_Values.Look(ref trips, "PhobosGatherTrips");
        Scribe_Values.Look(ref unloaded, "PhobosGatherUnloaded");
        Scribe_Values.Look(ref engineStarted, "PhobosGatherEngineStarted");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && phase == GatherPhase.Driving && target == null) LookAgain();
    }

    public override string? CompInspectStringExtra()
    {
        switch (phase)
        {
            case GatherPhase.Seeking:
                return "PhobosGather_LineSeeking".Translate().Resolve();
            case GatherPhase.Driving when target != null:
                return "PhobosGather_LineDriving".Translate(target.LabelShort.Named("THING"),
                    ((int)target.Position.DistanceTo(Vehicle.Position)).Named("CELLS")).Resolve();
            case GatherPhase.Working:
                IVehicleGatherWorker? w = Worker;
                int count = loads + (w?.GatheredLoads ?? 0);
                float mass = kilograms + (w?.GatheredKilograms ?? 0f);
                return "PhobosGather_LineWorking".Translate(count.Named("COUNT"), mass.ToString("N0").Named("MASS")).Resolve();
            case GatherPhase.Returning:
                return "PhobosGather_LineReturning".Translate(UnloadWords.Named("PLACE")).Resolve();
            case GatherPhase.Unloading:
                return "PhobosGather_LineUnloading".Translate(UnloadWords.Named("PLACE")).Resolve();
            default:
                return null;
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        IVehicleGatherWorker? w = Worker;
        if (w == null || (!w.CanGatherAtAll && !Running)) yield break;
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        bool on = Running;
        string whenFull = HasUnloadPlace
            ? "PhobosGather_WhenFullUnload".Translate(UnloadWords.Named("PLACE")).Resolve()
            : "PhobosGather_WhenFullStop".Translate().Resolve();
        var run = new Command_Action
        {
            defaultLabel = on ? "PhobosGather_GizmoStop".Translate().Resolve() : "PhobosGather_Gizmo".Translate().Resolve(),
            defaultDesc = "PhobosGather_GizmoDesc".Translate(name, FilterWords.Named("WHAT"), w.GatherReachCells.Named("CELLS"), whenFull.Named("WHENFULL")).Resolve(),
            icon = SystemIcons.For(Props.runIconPath),
            action = () =>
            {
                if (on) Stop();
                else Start();
            },
        };
        if (!on && CannotStart() is { } refused) run.Disable(refused);
        yield return run;
        yield return new Command_Action
        {
            defaultLabel = "PhobosGather_FilterGizmo".Translate().Resolve(),
            defaultDesc = "PhobosGather_FilterGizmoDesc".Translate(name, FilterWords.Named("WHAT"), AreaWords.Named("AREA"),
                UnloadChoiceWords.Named("UNLOAD")).Resolve(),
            icon = SystemIcons.For(Props.filterIconPath),
            action = OpenFilter,
        };
    }

    void OpenFilter()
    {
        Map? map = Vehicle.MapHeld;
        var dropdowns = new List<FilterDropdown>();
        if (map != null)
        {
            dropdowns.Add(FilterDropdown.Area(map, () => ChosenArea, SetArea));
            if (CanUnload)
            {
                dropdowns.Add(new FilterDropdown(() => "PhobosGather_UnloadAt".Translate(UnloadChoiceWords.Named("PLACE")).Resolve(), UnloadOptions,
                    "PhobosGather_UnloadAtDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve()));
            }
        }
        Find.WindowStack.Add(new Dialog_PhobosThingFilter("PhobosGather_FilterTitle".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Filter, ChoosableFilter, map, dropdowns));
    }
}
