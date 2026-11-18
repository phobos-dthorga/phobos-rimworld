using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class TipperDefOf
{
    public static VehicleStatDef Phobos_TipRate = null!;
    public static VehicleStatDef Phobos_GrabLift = null!;

    static TipperDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(TipperDefOf));
}

public class CompProperties_PhobosTipper : VehicleCompProperties
{
    /// <summary>The half-disc the load lands on, in cells. Authored: about 100 cells, a base load of chunks.</summary>
    public float fanRadius = 8f;
    public float controlFloor = 0.5f;
    public float engineDemand = 0.5f;

    public CompProperties_PhobosTipper() => compClass = typeof(CompPhobosTipper);
}

/// <summary>
/// A truck that puts its whole load on the ground behind it (docs/development/vehicle-tipper-hauler.md). Nothing in the game or Vehicle Framework tips, so this does, a few things each rare tick, which is cheap and reads as a steady slide.
/// While tipping it holds the truck still, through the set-up base. It picks cells itself, because the game's own placement logs an error when nothing fits, and a full yard is an ordinary thing for a tipper to meet.
/// </summary>
public class CompPhobosTipper : CompPhobosSetUp, IVehicleEngineDemand, IVehicleGatherWorker
{
    const float SecondsPerRareTick = GenTicks.TickRareInterval / 60f;

    float allowance;
    int tipped;
    /// <summary>How many of the things tipped went into storage (Framework 0.125.0, UH1), for the report.</summary>
    int stored;
    /// <summary>The loading grab is out (Framework 0.101.0): the truck is held as when tipping, and the rare tick lifts instead of tipping.</summary>
    bool grabbing;
    int lifted;
    float liftedKilograms;
    /// <summary>The grab is out for a gathering run (Framework 0.143.0): it lifts only what the run wants, and the run reports, not the grab.</summary>
    bool grabForRun;
    /// <summary>What the run wants. Not saved: the run hands it back after a load, and the grab lifts nothing until it has it.</summary>
    System.Predicate<Thing>? grabWanted;
    GatherWorkEnd lastGrabEnd;
    string? lastGrabReason;

    public CompProperties_PhobosTipper Props => (CompProperties_PhobosTipper)props;
    protected override string DeployedSaveKey => "PhobosTipperTipping";
    protected override GraphicDataOverlay? SetUpLook => null;
    protected override string SetUpRefusal => (grabbing ? "PhobosVehicleSystems_GrabHeld" : "PhobosVehicleSystems_TipHeld").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    public bool WantsEngine => deployed;
    public float EngineDemand => Props.engineDemand;
    /// <summary>Tipping and the grab cost heat and power, not fuel (Framework 0.131.0; the engine-loads record): a truck left with its
    /// grab out would otherwise drain its tank standing.</summary>
    public bool FuelFromModel => false;

    ThingOwner Cargo => Vehicle.inventory.innerContainer;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref allowance, "PhobosTipperAllowance");
        Scribe_Values.Look(ref tipped, "PhobosTipperTipped");
        Scribe_Values.Look(ref stored, "PhobosTipperStored");
        Scribe_Values.Look(ref grabbing, "PhobosTipperGrabbing");
        Scribe_Values.Look(ref lifted, "PhobosTipperLifted");
        Scribe_Values.Look(ref liftedKilograms, "PhobosTipperLiftedKg");
        // Framework 0.143.0: the grab working for a gathering run, and how it last stopped, which the run reads.
        Scribe_Values.Look(ref grabForRun, "PhobosTipperGrabForRun");
        Scribe_Values.Look(ref lastGrabEnd, "PhobosTipperGrabEnd", GatherWorkEnd.None);
        Scribe_Values.Look(ref lastGrabReason, "PhobosTipperGrabReason");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !deployed)
        {
            grabbing = false;
            grabForRun = false;
        }
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        deployed = false;
        grabbing = false;
        grabForRun = false;
        grabWanted = null;
        base.PostDeSpawn(map, mode);
    }

    /// <summary>The cells the load may land on, nearest first.</summary>
    IEnumerable<IntVec3> Fan()
    {
        Map map = Vehicle.Map;
        IntVec3 tail = RearCell;
        IntVec3 rear = -Vehicle.Rotation.FacingCell;
        CellRect footprint = Vehicle.OccupiedRect();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(tail, Props.fanRadius, true))
        {
            IntVec3 offset = cell - tail;
            if (cell.InBounds(map) && !footprint.Contains(cell) && TipRule.InFan(offset.x, offset.z, rear.x, rear.z, Props.fanRadius)) yield return cell;
        }
    }

    /// <summary>Open ground this thing can land on: room for an item, or a stack of the same thing with room left.</summary>
    static bool FitsGround(IntVec3 cell, Map map, Thing thing)
    {
        if (!cell.Standable(map) || cell.GetEdifice(map) != null) return false;
        if (cell.GetItemCount(map) < cell.GetMaxItemsAllowedInCell(map)) return true;
        foreach (Thing other in cell.GetThingList(map))
        {
            if (other.def.category == ThingCategory.Item && other.CanStackWith(thing) && other.stackCount < other.def.stackLimit) return true;
        }
        return false;
    }

    /// <summary>
    /// Where one thing goes now, and the cell (Framework 0.125.0; docs/development/universal-hoppers.md, UH1). A storage building of the truck's
    /// own side in the outlined area that would take it gets it, nearest cell with room first, by the game's own storage rule; once full, the
    /// thing stays aboard (owner, 17 Nov 2026). Anything no storage there would take goes on open ground, as before. TipRule.Destination decides.
    /// </summary>
    TipDestination Place(Thing thing, List<IntVec3> fan, out IntVec3 cell, out Building_Storage? into)
    {
        Map map = Vehicle.Map;
        bool storageWouldTake = false;
        IntVec3 storageCell = IntVec3.Invalid;
        IntVec3 groundCell = IntVec3.Invalid;
        into = null;
        foreach (IntVec3 c in fan)
        {
            if (c.GetEdifice(map) is Building_Storage store)
            {
                // Another side's storage is a building like any other: nothing lands on it.
                if (store.Faction != Vehicle.Faction || !store.Accepts(thing)) continue;
                storageWouldTake = true;
                if (c.IsValidStorageFor(map, thing))
                {
                    storageCell = c;
                    into = store;
                    break;
                }
            }
            else if (!groundCell.IsValid && FitsGround(c, map, thing)) groundCell = c;
        }
        TipDestination where = TipRule.Destination(storageWouldTake, storageCell.IsValid, groundCell.IsValid);
        cell = where switch
        {
            TipDestination.Storage => storageCell,
            TipDestination.Ground => groundCell,
            _ => IntVec3.Invalid,
        };
        if (where != TipDestination.Storage) into = null;
        return where;
    }

    /// <summary>Whether anything aboard can come off now; if not, whether full storage is what holds it.</summary>
    bool AnythingCanGo(out bool storageFull)
    {
        storageFull = false;
        List<IntVec3> fan = Fan().ToList();
        for (int i = 0; i < Cargo.Count; i++)
        {
            if (Place(Cargo[i], fan, out _, out _) != TipDestination.StaysAboard) return true;
            if (StorageWouldTake(Cargo[i], fan)) storageFull = true;
        }
        return false;
    }

    bool StorageWouldTake(Thing thing, List<IntVec3> fan)
    {
        Map map = Vehicle.Map;
        foreach (IntVec3 c in fan)
        {
            if (c.GetEdifice(map) is Building_Storage store && store.Faction == Vehicle.Faction && store.Accepts(thing)) return true;
        }
        return false;
    }

    /// <summary>Why it cannot tip now, or null.</summary>
    public string? CannotTip()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_TipNotOnMap".Translate().Resolve();
        if (grabbing) return "PhobosVehicleSystems_TipGrabOut".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Cargo.Count == 0) return "PhobosVehicleSystems_TipEmpty".Translate().Resolve();
        if (StillMoving) return "PhobosVehicleSystems_TipStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (CrewAndEngineRefusal() is { } crew) return crew;
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_TipNoFuel".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_TipControls".Translate().Resolve();
        if (Systems?.HydraulicRefusal() is { } dry) return dry;
        return AnythingCanGo(out bool storageFull) ? null : FullLine(storageFull);
    }

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    string FullLine(bool storageFull) => (storageFull ? "PhobosVehicleSystems_TipStorageFull" : "PhobosVehicleSystems_TipFull").Translate(Cargo.Count.Named("COUNT")).Resolve();

    public void Start()
    {
        string? refused = CannotTip();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        deployed = true;
        allowance = 0f;
        tipped = 0;
        stored = 0;
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        Tell("PhobosVehicleSystems_TipStarted".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void Stop() => Finish(null);

    void Finish(string? reason)
    {
        deployed = false;
        VehicleWorkSounds.Loop(Vehicle, "Rams", false);
        VehicleWorkSounds.Play(Vehicle, "Tailgate");
        string report = stored > 0
            ? "PhobosVehicleSystems_TipDoneStored".Translate(Vehicle.LabelShort.Named("VEHICLE"), tipped.Named("COUNT"), stored.Named("STORED")).Resolve()
            : "PhobosVehicleSystems_TipDone".Translate(Vehicle.LabelShort.Named("VEHICLE"), tipped.Named("COUNT")).Resolve();
        Tell(reason == null ? report : report + " " + reason, reason == null ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
    }

    /// <summary>The tip's or grab's own report says the stall, so the player is told once (Framework 0.106.1).</summary>
    protected override bool ReportsOwnStall => true;

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!deployed || !Vehicle.Spawned) return;
        // The engine stopped under the rams (ran dry or switched off): the body or grab stops where it is, and someone aboard must start
        // the engine to work it again (Framework 0.106.1).
        if (Stalled)
        {
            if (grabbing) FinishGrab(StalledLine, GatherWorkEnd.Fault);
            else Finish(StalledLine);
            return;
        }
        if (grabbing)
        {
            GrabTick();
            return;
        }
        // Set again each look, so a tip under way when a save loads is heard again.
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        if (Vehicle.CompFueledTravel is { EmptyTank: true } || Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor)
        {
            Finish("PhobosVehicleSystems_TipStopped".Translate().Resolve());
            return;
        }
        // The body is raised by hydraulic rams: the fluid paces it, and too little stops it (T1, Framework 0.73.0).
        if (Systems?.HydraulicRefusal() is { } dry)
        {
            Finish(dry);
            return;
        }
        int things = TipRule.WholeThings(ref allowance, Vehicle.GetStatValue(TipperDefOf.Phobos_TipRate) * (Systems?.HydraulicSupply ?? 1f), SecondsPerRareTick);
        if (things > 0) VehicleWorkSounds.Play(Vehicle, "Slide");
        List<IntVec3> fan = things > 0 ? Fan().ToList() : new List<IntVec3>();
        for (int i = 0; i < things; i++)
        {
            if (Cargo.Count == 0)
            {
                Finish(null);
                return;
            }
            if (TipOne(fan, out _)) continue;
            AnythingCanGo(out bool storageFull);
            Finish(FullLine(storageFull));
            return;
        }
        if (Cargo.Count == 0) Finish(null);
    }

    /// <summary>
    /// Puts down the first thing in the cargo that has somewhere to go, into storage or onto the ground (Place decides). False when nothing aboard
    /// has, and the rest stays aboard. A thing held back by full storage is passed over, so the rest of a mixed load still comes off.
    /// </summary>
    bool TipOne(List<IntVec3> fan, out Building_Storage? into)
    {
        into = null;
        for (int i = 0; i < Cargo.Count; i++)
        {
            Thing thing = Cargo[i];
            TipDestination where = Place(thing, fan, out IntVec3 cell, out into);
            if (where == TipDestination.StaysAboard) continue;
            Cargo.Remove(thing);
            if (!GenPlace.TryPlaceThing(thing, cell, Vehicle.Map, ThingPlaceMode.Direct) && !thing.Destroyed && thing.stackCount > 0) Cargo.TryAdd(thing);
            else
            {
                tipped++;
                if (into != null)
                {
                    stored++;
                    Seen.Note("vehicles.tip-into-storage", Vehicle, into.LabelShort);
                }
            }
            return true;
        }
        return false;
    }

    void Tell(string text, MessageTypeDef type) => Messages.Message(text, Vehicle, type, historical: false);

    /// <summary>The game's work bar while it tips (owner, 12 Nov 2026): the share of the load put down so far.</summary>
    public override void PostDraw()
    {
        base.PostDraw();
        if (!deployed || grabbing) return;
        int left = Cargo.Count;
        if (tipped + left > 0) Phobos.Framework.Interface.MapProgressBar.DrawOver(Vehicle, (float)tipped / (tipped + left));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (!Vehicle.Spawned) return;
        if (HasGrab) GenDraw.DrawFieldEdges(GrabCells().ToList(), UnityEngine.Color.cyan);
        if (!grabbing) GenDraw.DrawFieldEdges(Fan().ToList());
    }

    public override string? CompInspectStringExtra()
    {
        // A gathering run says what it is doing in its own line.
        if (!deployed || grabForRun) return null;
        return grabbing
            ? "PhobosVehicleSystems_GrabLine".Translate(lifted.Named("COUNT")).Resolve()
            : "PhobosVehicleSystems_TipLine".Translate(Cargo.Count.Named("COUNT")).Resolve();
    }

    // ---- The loading grab (Framework 0.101.0; docs/development/vehicle-upgrades-round-two.md) ----

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    float LiftKilograms => Vehicle.GetStatValue(TipperDefOf.Phobos_GrabLift);
    public bool HasGrab => LiftKilograms > 0f;
    public bool Grabbing => deployed && grabbing;

    float FreeKilograms => Vehicle.GetStatValue(VehicleStatDefOf.CargoCapacity) - MassUtility.InventoryMass(Vehicle);

    /// <summary>The cells within the grab's reach of the truck's edge, not under it.</summary>
    IEnumerable<IntVec3> GrabCells()
    {
        Map map = Vehicle.Map;
        CellRect footprint = Vehicle.OccupiedRect();
        foreach (IntVec3 cell in footprint.ExpandedBy((int)Rules.grabReachCells).ClipInsideMap(map))
        {
            if (!footprint.Contains(cell)) yield return cell;
        }
    }

    /// <summary>What the grab may take: items not forbidden and not too heavy for one lift, lying loose or in storage of the truck's side whose
    /// "Vehicles may load from here" switch is on (Framework 0.125.0, UH12; VehicleLoadOut decides); never pawns, corpses or packed buildings.</summary>
    bool Grabbable(Thing thing)
    {
        if (!LiftableAtAll(thing)) return false;
        return !thing.IsForbidden(Faction.OfPlayer) && VehicleLoadOut.MayLift(thing, Vehicle.Faction);
    }

    /// <summary>The grab's own limits, wherever the thing lies: an item, never a corpse or a packed building, with one unit no heavier than a lift.</summary>
    bool LiftableAtAll(Thing thing)
    {
        if (thing.def.category != ThingCategory.Item || !thing.def.EverHaulable || thing is Corpse || thing is MinifiedThing) return false;
        return GrabRule.CountForLift(thing.stackCount, thing.GetStatValue(StatDefOf.Mass), LiftKilograms, double.MaxValue) > 0;
    }

    /// <summary>What the grab takes now: for a gathering run, what the run wants (it asks the grab's own limits too); otherwise its own rule.</summary>
    bool TakesNow(Thing thing) => grabForRun ? grabWanted != null && grabWanted(thing) : Grabbable(thing);

    /// <summary>The nearest thing in reach the grab can take now, or null.</summary>
    Thing? NextForGrab()
    {
        Map map = Vehicle.Map;
        IntVec3 from = Vehicle.Position;
        Thing? best = null;
        int bestDistance = int.MaxValue;
        foreach (IntVec3 cell in GrabCells())
        {
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!TakesNow(thing)) continue;
                int distance = cell.DistanceToSquared(from);
                if (distance >= bestDistance) continue;
                best = thing;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>Why the grab cannot be worked now, or null.</summary>
    public string? CannotGrab() => CannotGrab(out _);

    /// <summary>Why the grab cannot be worked now, or null; <paramref name="kind"/> says whether it is full, has nothing in reach, or a fault.</summary>
    string? CannotGrab(out GatherWorkEnd kind)
    {
        kind = GatherWorkEnd.Fault;
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_GrabNotOnMap".Translate().Resolve();
        if (deployed && !grabbing) return "PhobosVehicleSystems_GrabWhileTipping".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (StillMoving) return "PhobosVehicleSystems_GrabStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (CrewAndEngineRefusal() is { } crew) return crew;
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_GrabNoFuel".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_GrabControls".Translate().Resolve();
        if (Systems?.HydraulicRefusal() is { } dry) return dry;
        kind = GatherWorkEnd.Full;
        if (FreeKilograms <= 0f) return "PhobosVehicleSystems_GrabFull".Translate().Resolve();
        kind = GatherWorkEnd.NothingInReach;
        if (NextForGrab() == null) return "PhobosVehicleSystems_GrabNothing".Translate().Resolve();
        kind = GatherWorkEnd.None;
        return null;
    }

    public void StartGrab()
    {
        grabForRun = false;
        grabWanted = null;
        if (CannotGrab() is { } refused)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        SwingGrabOut();
        Tell("PhobosVehicleSystems_GrabStarted".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    void SwingGrabOut()
    {
        deployed = true;
        grabbing = true;
        allowance = 0f;
        lifted = 0;
        liftedKilograms = 0f;
        lastGrabEnd = GatherWorkEnd.None;
        lastGrabReason = null;
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
    }

    public void StopGrab() => FinishGrab(null, GatherWorkEnd.Stopped);

    /// <summary>Stows the grab. Worked by hand, it reports what it lifted; for a gathering run it only notes why it stopped, and the run reports.</summary>
    void FinishGrab(string? reason, GatherWorkEnd kind)
    {
        deployed = false;
        grabbing = false;
        lastGrabEnd = kind;
        lastGrabReason = reason;
        VehicleWorkSounds.Loop(Vehicle, "Rams", false);
        if (grabForRun)
        {
            grabForRun = false;
            grabWanted = null;
            return;
        }
        string report = "PhobosVehicleSystems_GrabDone".Translate(Vehicle.LabelShort.Named("VEHICLE"), lifted.Named("COUNT"), liftedKilograms.ToString("N0").Named("MASS")).Resolve();
        Tell(reason == null ? report : report + " " + reason, reason == null ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
    }

    // ---- The grab as a gathering run's worker (Framework 0.143.0; docs/development/vehicle-gathering-runs.md) ----

    public bool CanGatherAtAll => HasGrab;

    public int GatherReachCells => (int)Rules.grabReachCells;

    public bool InGatherReach(IntVec3 cell)
    {
        CellRect footprint = Vehicle.OccupiedRect();
        return !footprint.Contains(cell) && footprint.ExpandedBy(GatherReachCells).Contains(cell);
    }

    public bool CouldGather(Thing thing) => LiftableAtAll(thing);

    public float GatherRoomKilograms => FreeKilograms;

    public GatherWorkEnd StartGathering(System.Predicate<Thing> wanted, out string? refusal)
    {
        // Set first, so the refusal looks for what the run wants.
        grabForRun = true;
        grabWanted = wanted;
        refusal = CannotGrab(out GatherWorkEnd kind);
        if (refusal != null)
        {
            grabForRun = false;
            grabWanted = null;
            return kind;
        }
        SwingGrabOut();
        return GatherWorkEnd.None;
    }

    public void ResumeGathering(System.Predicate<Thing> wanted)
    {
        if (grabForRun) grabWanted = wanted;
    }

    public bool Gathering => Grabbing && grabForRun;

    public void StopGathering()
    {
        if (Gathering) FinishGrab(null, GatherWorkEnd.Stopped);
    }

    public GatherWorkEnd LastGatherEnd => lastGrabEnd;

    public string? LastGatherReason => lastGrabReason;

    public int GatheredLoads => lifted;

    public float GatheredKilograms => liftedKilograms;

    /// <summary>One rare tick of the grab: lifts paced by the rules and the hydraulic fluid, each one stack (or what one lift and the room
    /// allow) moved aboard through Vehicle Framework's own cargo call.</summary>
    void GrabTick()
    {
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        if (Vehicle.CompFueledTravel is { EmptyTank: true } || Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor || !HasGrab)
        {
            FinishGrab("PhobosVehicleSystems_GrabStopped".Translate().Resolve(), GatherWorkEnd.Fault);
            return;
        }
        if (Systems?.HydraulicRefusal() is { } dry)
        {
            FinishGrab(dry, GatherWorkEnd.Fault);
            return;
        }
        // Just after a load, the run has not yet handed back what it wants: lift nothing until it has.
        if (grabForRun && grabWanted == null) return;
        float perSecond = Rules.grabSecondsPerLift > 0f ? 1f / Rules.grabSecondsPerLift : 1f;
        int lifts = TipRule.WholeThings(ref allowance, perSecond * (Systems?.HydraulicSupply ?? 1f), SecondsPerRareTick);
        for (int i = 0; i < lifts; i++)
        {
            float free = FreeKilograms;
            if (free <= 0f)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve(), GatherWorkEnd.Full);
                return;
            }
            Thing? thing = NextForGrab();
            if (thing == null)
            {
                FinishGrab("PhobosVehicleSystems_GrabNothing".Translate().Resolve(), GatherWorkEnd.NothingInReach);
                return;
            }
            float unit = thing.GetStatValue(StatDefOf.Mass);
            int count = GrabRule.CountForLift(thing.stackCount, unit, LiftKilograms, free);
            if (count <= 0)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve(), GatherWorkEnd.Full);
                return;
            }
            Building_Storage? from = VehicleLoadOut.StoredIn(thing);
            int moved = Vehicle.AddOrTransfer(thing, count);
            if (moved <= 0)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve(), GatherWorkEnd.Full);
                return;
            }
            if (from != null) Seen.Note("vehicles.grab-from-storage", Vehicle, from.LabelShort);
            lifted++;
            liftedKilograms += moved * unit;
            VehicleWorkSounds.Play(Vehicle, "Slide");
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        if (HasGrab || grabbing) yield return GrabButton();
        bool on = deployed && !grabbing;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_TipGizmoStop".Translate().Resolve() : "PhobosVehicleSystems_TipGizmo".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_TipGizmoDesc".Translate().Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Stations/Tip"),
            action = () =>
            {
                if (on) Stop();
                else Start();
            },
        };
        string? refused = on ? null : CannotTip();
        if (refused != null) command.Disable(refused);
        yield return command;
    }

    Gizmo GrabButton()
    {
        bool on = Grabbing;
        var command = new Command_Action
        {
            defaultLabel = on ? "PhobosVehicleSystems_GrabGizmoStop".Translate().Resolve() : "PhobosVehicleSystems_GrabGizmo".Translate().Resolve(),
            defaultDesc = "PhobosVehicleSystems_GrabGizmoDesc".Translate(((int)Rules.grabReachCells).Named("CELLS"), LiftKilograms.ToString("N0").Named("LIFT")).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Stations/Grab"),
            action = () =>
            {
                if (on) StopGrab();
                else StartGrab();
            },
        };
        string? refused = on ? null : CannotGrab();
        if (refused != null) command.Disable(refused);
        return command;
    }
}
