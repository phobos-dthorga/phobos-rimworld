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

    public CompProperties_PhobosTipper Props => (CompProperties_PhobosTipper)props;
    protected override string DeployedSaveKey => "PhobosTipperTipping";
    protected override GraphicDataOverlay? SetUpLook => null;
    protected override string SetUpRefusal => "PhobosVehicleSystems_TipHeld".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    public bool WantsEngine => deployed;
    public float EngineDemand => Props.engineDemand;

    ThingOwner Cargo => Vehicle.inventory.innerContainer;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref allowance, "PhobosTipperAllowance");
        Scribe_Values.Look(ref tipped, "PhobosTipperTipped");
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        deployed = false;
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
        if (Cargo.Count == 0) return "PhobosVehicleSystems_TipEmpty".Translate().Resolve();
        if (Vehicle.Drafted) return "PhobosVehicleSystems_TipStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (!Vehicle.CanMoveFinal) return "PhobosVehicleSystems_TipNoDriver".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) return "PhobosVehicleSystems_TipNoFuel".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor) return "PhobosVehicleSystems_TipControls".Translate().Resolve();
        return CellFor(Cargo[0]).IsValid ? null : FullLine();
    }

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
        Tell("PhobosVehicleSystems_TipStarted".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void Stop() => Finish(null);

    void Finish(string? reason)
    {
        deployed = false;
        string report = "PhobosVehicleSystems_TipDone".Translate(Vehicle.LabelShort.Named("VEHICLE"), tipped.Named("COUNT")).Resolve();
        Tell(reason == null ? report : report + " " + reason, reason == null ? MessageTypeDefOf.TaskCompletion : MessageTypeDefOf.CautionInput);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!deployed || !Vehicle.Spawned) return;
        if (Vehicle.CompFueledTravel is { EmptyTank: true } || Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < Props.controlFloor)
        {
            Finish("PhobosVehicleSystems_TipStopped".Translate().Resolve());
            return;
        }
        int things = TipRule.WholeThings(ref allowance, Vehicle.GetStatValue(TipperDefOf.Phobos_TipRate), SecondsPerRareTick);
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

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (Vehicle.Spawned) GenDraw.DrawFieldEdges(Fan().ToList());
    }

    public override string? CompInspectStringExtra() => deployed ? "PhobosVehicleSystems_TipLine".Translate(Cargo.Count.Named("COUNT")).Resolve() : null;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        bool on = deployed;
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
}
