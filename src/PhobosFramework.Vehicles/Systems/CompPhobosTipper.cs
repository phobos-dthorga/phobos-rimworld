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
public class CompPhobosTipper : CompPhobosSetUp, IVehicleEngineDemand
{
    const float SecondsPerRareTick = GenTicks.TickRareInterval / 60f;

    float allowance;
    int tipped;
    /// <summary>The loading grab is out (Framework 0.101.0): the truck is held as when tipping, and the rare tick lifts instead of tipping.</summary>
    bool grabbing;
    int lifted;
    float liftedKilograms;

    public CompProperties_PhobosTipper Props => (CompProperties_PhobosTipper)props;
    protected override string DeployedSaveKey => "PhobosTipperTipping";
    protected override GraphicDataOverlay? SetUpLook => null;
    protected override string SetUpRefusal => (grabbing ? "PhobosVehicleSystems_GrabHeld" : "PhobosVehicleSystems_TipHeld").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    public bool WantsEngine => deployed;
    public float EngineDemand => Props.engineDemand;

    ThingOwner Cargo => Vehicle.inventory.innerContainer;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref allowance, "PhobosTipperAllowance");
        Scribe_Values.Look(ref tipped, "PhobosTipperTipped");
        Scribe_Values.Look(ref grabbing, "PhobosTipperGrabbing");
        Scribe_Values.Look(ref lifted, "PhobosTipperLifted");
        Scribe_Values.Look(ref liftedKilograms, "PhobosTipperLiftedKg");
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !deployed) grabbing = false;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        deployed = false;
        grabbing = false;
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

    /// <summary>A cell this thing can land on: open ground with room for an item, or a stack of the same thing with room left.</summary>
    static bool Fits(IntVec3 cell, Map map, Thing thing)
    {
        if (!cell.Standable(map) || cell.GetEdifice(map) != null) return false;
        if (cell.GetItemCount(map) < cell.GetMaxItemsAllowedInCell(map)) return true;
        foreach (Thing other in cell.GetThingList(map))
        {
            if (other.def.category == ThingCategory.Item && other.CanStackWith(thing) && other.stackCount < other.def.stackLimit) return true;
        }
        return false;
    }

    IntVec3 CellFor(Thing thing) => Fan().Where(c => Fits(c, Vehicle.Map, thing)).FirstOrFallback(IntVec3.Invalid);

    /// <summary>Why it cannot tip now, or null.</summary>
    public string? CannotTip()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_TipNotOnMap".Translate().Resolve();
        if (grabbing) return "PhobosVehicleSystems_TipGrabOut".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Cargo.Count == 0) return "PhobosVehicleSystems_TipEmpty".Translate().Resolve();
        if (StillMoving) return "PhobosVehicleSystems_TipStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!Vehicle.CanMoveFinal) return "PhobosVehicleSystems_TipNoDriver".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_TipNoFuel".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_TipControls".Translate().Resolve();
        if (Systems?.HydraulicRefusal() is { } dry) return dry;
        return CellFor(Cargo[0]).IsValid ? null : FullLine();
    }

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    string FullLine() => "PhobosVehicleSystems_TipFull".Translate(Cargo.Count.Named("COUNT")).Resolve();

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
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        Tell("PhobosVehicleSystems_TipStarted".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void Stop() => Finish(null);

    void Finish(string? reason)
    {
        deployed = false;
        VehicleWorkSounds.Loop(Vehicle, "Rams", false);
        VehicleWorkSounds.Play(Vehicle, "Tailgate");
        string report = "PhobosVehicleSystems_TipDone".Translate(Vehicle.LabelShort.Named("VEHICLE"), tipped.Named("COUNT")).Resolve();
        Tell(reason == null ? report : report + " " + reason, reason == null ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!deployed || !Vehicle.Spawned) return;
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
        for (int i = 0; i < things; i++)
        {
            if (Cargo.Count == 0)
            {
                Finish(null);
                return;
            }
            if (TipOne()) continue;
            Finish(FullLine());
            return;
        }
        if (Cargo.Count == 0) Finish(null);
    }

    /// <summary>Puts down the first thing in the cargo. False when the ground has no room for it, and it stays aboard.</summary>
    bool TipOne()
    {
        Thing thing = Cargo[0];
        IntVec3 cell = CellFor(thing);
        if (!cell.IsValid) return false;
        Cargo.Remove(thing);
        if (!GenPlace.TryPlaceThing(thing, cell, Vehicle.Map, ThingPlaceMode.Direct) && !thing.Destroyed && thing.stackCount > 0) Cargo.TryAdd(thing);
        else tipped++;
        return true;
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
        if (!deployed) return null;
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

    /// <summary>What the grab may take: loose items on the ground, not forbidden, not stored, not too heavy for one lift; never pawns,
    /// corpses or packed buildings.</summary>
    bool Grabbable(Thing thing)
    {
        if (thing.def.category != ThingCategory.Item || !thing.def.EverHaulable || thing is Corpse || thing is MinifiedThing) return false;
        if (thing.IsForbidden(Faction.OfPlayer) || thing.IsInAnyStorage()) return false;
        return GrabRule.CountForLift(thing.stackCount, thing.GetStatValue(StatDefOf.Mass), LiftKilograms, double.MaxValue) > 0;
    }

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
                if (!Grabbable(thing)) continue;
                int distance = cell.DistanceToSquared(from);
                if (distance >= bestDistance) continue;
                best = thing;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>Why the grab cannot be worked now, or null.</summary>
    public string? CannotGrab()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_GrabNotOnMap".Translate().Resolve();
        if (deployed && !grabbing) return "PhobosVehicleSystems_GrabWhileTipping".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (StillMoving) return "PhobosVehicleSystems_GrabStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!Vehicle.CanMoveFinal) return "PhobosVehicleSystems_GrabNoDriver".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_GrabNoFuel".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_GrabControls".Translate().Resolve();
        if (Systems?.HydraulicRefusal() is { } dry) return dry;
        if (FreeKilograms <= 0f) return "PhobosVehicleSystems_GrabFull".Translate().Resolve();
        if (NextForGrab() == null) return "PhobosVehicleSystems_GrabNothing".Translate().Resolve();
        return null;
    }

    public void StartGrab()
    {
        if (CannotGrab() is { } refused)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        deployed = true;
        grabbing = true;
        allowance = 0f;
        lifted = 0;
        liftedKilograms = 0f;
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        Tell("PhobosVehicleSystems_GrabStarted".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void StopGrab() => FinishGrab(null);

    void FinishGrab(string? reason)
    {
        deployed = false;
        grabbing = false;
        VehicleWorkSounds.Loop(Vehicle, "Rams", false);
        string report = "PhobosVehicleSystems_GrabDone".Translate(Vehicle.LabelShort.Named("VEHICLE"), lifted.Named("COUNT"), liftedKilograms.ToString("N0").Named("MASS")).Resolve();
        Tell(reason == null ? report : report + " " + reason, reason == null ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
    }

    /// <summary>One rare tick of the grab: lifts paced by the rules and the hydraulic fluid, each one stack (or what one lift and the room
    /// allow) moved aboard through Vehicle Framework's own cargo call.</summary>
    void GrabTick()
    {
        VehicleWorkSounds.Loop(Vehicle, "Rams", true);
        if (Vehicle.CompFueledTravel is { EmptyTank: true } || Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor || !HasGrab)
        {
            FinishGrab("PhobosVehicleSystems_GrabStopped".Translate().Resolve());
            return;
        }
        if (Systems?.HydraulicRefusal() is { } dry)
        {
            FinishGrab(dry);
            return;
        }
        float perSecond = Rules.grabSecondsPerLift > 0f ? 1f / Rules.grabSecondsPerLift : 1f;
        int lifts = TipRule.WholeThings(ref allowance, perSecond * (Systems?.HydraulicSupply ?? 1f), SecondsPerRareTick);
        for (int i = 0; i < lifts; i++)
        {
            float free = FreeKilograms;
            if (free <= 0f)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve());
                return;
            }
            Thing? thing = NextForGrab();
            if (thing == null)
            {
                FinishGrab("PhobosVehicleSystems_GrabNothing".Translate().Resolve());
                return;
            }
            float unit = thing.GetStatValue(StatDefOf.Mass);
            int count = GrabRule.CountForLift(thing.stackCount, unit, LiftKilograms, free);
            if (count <= 0)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve());
                return;
            }
            int moved = Vehicle.AddOrTransfer(thing, count);
            if (moved <= 0)
            {
                FinishGrab("PhobosVehicleSystems_GrabFull".Translate().Resolve());
                return;
            }
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
