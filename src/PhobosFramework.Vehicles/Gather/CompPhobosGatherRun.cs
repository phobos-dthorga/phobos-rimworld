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
    /// <summary>Whether enemies threatening the map end the run. Agent default.</summary>
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
/// It decides what is wanted and where to stand; the worker lifts; VehicleAutoDrive drives. Nothing ticks without a run.
/// </summary>
public class CompPhobosGatherRun : VehicleComp
{
    ThingFilter? filter;
    Area? area;
    GatherPhase phase;
    Thing? target;
    IntVec3 standCell = IntVec3.Invalid;
    int lastMovedTick;
    IntVec3 lastPosition = IntVec3.Invalid;
    int failedDrives;
    int loads;
    float kilograms;
    int places;
    bool engineStarted;

    // Derived, never saved: after a load a run may try a thing it had given up on once more.
    readonly HashSet<int> outOfReach = new();
    IVehicleGatherWorker? worker;
    Predicate<Thing>? wanted;
    string? filterWords;

    public CompProperties_PhobosGatherRun Props => (CompProperties_PhobosGatherRun)props;

    public bool Running => phase != GatherPhase.Idle;

    IVehicleGatherWorker? Worker => worker ??= Vehicle.AllComps.OfType<IVehicleGatherWorker>().FirstOrDefault();

    Predicate<Thing> Wanted => wanted ??= Wants;

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
        if (w.GatherRoomKilograms <= 0f) return "PhobosGather_AlreadyFull".Translate(name).Resolve();
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
        failedDrives = 0;
        target = null;
        standCell = IntVec3.Invalid;
        outOfReach.Clear();
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
        if (was == GatherPhase.Driving && standCell.IsValid && Vehicle.Spawned) VehicleAutoDrive.Halt(Vehicle, standCell);
        if (engineStarted && Vehicle.Spawned && GatherRule.SwitchesOffAtEnd(why)) VehicleAutoDrive.StopEngine(Vehicle);
        engineStarted = false;
        target = null;
        standCell = IntVec3.Invalid;
        failedDrives = 0;
        outOfReach.Clear();
        Report(why, reason);
        if (Seen.Wants("vehicles.gather-run.end", Vehicle, why.ToString()))
        {
            Seen.Note("vehicles.gather-run.end", Vehicle, variant: why.ToString(), values: new List<KeyValuePair<string, double>>
            {
                new("loads", loads),
                new("kg", kilograms),
                new("places", places),
            });
        }
    }

    void Report(GatherRunEnd why, string? reason)
    {
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        string head = loads > 0
            ? "PhobosGather_Done".Translate(name, loads.Named("COUNT"), kilograms.ToString("N0").Named("MASS"), places.Named("PLACES")).Resolve()
            : "PhobosGather_DoneNothing".Translate(name).Resolve();
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
        _ => "",
    };

    /// <summary>Adds what the worker picked up at the place it is leaving.</summary>
    void CollectWork(IVehicleGatherWorker w)
    {
        loads += w.GatheredLoads;
        kilograms += w.GatheredKilograms;
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
        }
    }

    /// <summary>
    /// Chooses what to fetch next: every thing of the kinds the filter allows that the run wants and has not given up on, nearest first. One
    /// already in reach is worked at once; otherwise the first with a place to stand is driven to. Up to <see cref="CompProperties_PhobosGatherRun.thingsPerLook"/>
    /// a look; the rest wait for the next rare tick.
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
        if (w.GatherRoomKilograms <= 0f)
        {
            End(GatherRunEnd.Full);
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
            End(GatherRunEnd.NothingLeft);
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
            End(end, w.LastGatherReason);
            return;
        }
        Seek();
    }

    // ---- The map, the save and the screen ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        // The wanted test is not saved: a run working when the game was saved hands it back.
        if (phase == GatherPhase.Working) Worker?.ResumeGathering(Wanted);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map (a caravan, a launch) ends a run where it is: the player sent it.
        phase = GatherPhase.Idle;
        engineStarted = false;
        target = null;
        standCell = IntVec3.Invalid;
        outOfReach.Clear();
        base.PostDeSpawn(map, mode);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            if (target is { Destroyed: true }) target = null;
            _ = ChosenArea;
        }
        // A vehicle that has never run, or has no area or target, writes nothing for them.
        bool saving = Scribe.mode == LoadSaveMode.Saving;
        if (!saving || filter != null) Scribe_Deep.Look(ref filter, "PhobosGatherFilter", (Action)FilterChanged);
        if (!saving || area != null) Scribe_References.Look(ref area, "PhobosGatherArea");
        Scribe_Values.Look(ref phase, "PhobosGatherPhase", GatherPhase.Idle);
        if (!saving || target != null) Scribe_References.Look(ref target, "PhobosGatherTarget");
        Scribe_Values.Look(ref standCell, "PhobosGatherStandCell", IntVec3.Invalid);
        Scribe_Values.Look(ref lastMovedTick, "PhobosGatherLastMoved");
        Scribe_Values.Look(ref lastPosition, "PhobosGatherLastPosition", IntVec3.Invalid);
        Scribe_Values.Look(ref failedDrives, "PhobosGatherFailedDrives");
        Scribe_Values.Look(ref loads, "PhobosGatherLoads");
        Scribe_Values.Look(ref kilograms, "PhobosGatherKilograms");
        Scribe_Values.Look(ref places, "PhobosGatherPlaces");
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
        var run = new Command_Action
        {
            defaultLabel = on ? "PhobosGather_GizmoStop".Translate().Resolve() : "PhobosGather_Gizmo".Translate().Resolve(),
            defaultDesc = "PhobosGather_GizmoDesc".Translate(name, FilterWords.Named("WHAT"), w.GatherReachCells.Named("CELLS")).Resolve(),
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
            defaultDesc = "PhobosGather_FilterGizmoDesc".Translate(name, FilterWords.Named("WHAT"), AreaWords.Named("AREA")).Resolve(),
            icon = SystemIcons.For(Props.filterIconPath),
            action = OpenFilter,
        };
    }

    void OpenFilter()
    {
        Find.WindowStack.Add(new Dialog_PhobosThingFilter("PhobosGather_FilterTitle".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Filter, ChoosableFilter, Vehicle.MapHeld, () => ChosenArea, SetArea));
    }
}
