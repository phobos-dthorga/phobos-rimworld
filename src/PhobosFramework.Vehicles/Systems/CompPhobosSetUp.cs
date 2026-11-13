using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Sets an overlay's position by name. Vehicle Framework's build names Unity's types from another Unity version, so the compiler refuses the member directly, though at run time it is the one Vector3. Only runs while a part is moving, so the boxing costs nothing that matters.
/// </summary>
internal static class OverlayPosition
{
    static FieldInfo? field;
    static PropertyInfo? property;
    static bool resolved;

    internal static void Set(object transform, Vector3 value)
    {
        if (!resolved)
        {
            resolved = true;
            field = AccessTools.Field(transform.GetType(), "position");
            property = field == null ? AccessTools.Property(transform.GetType(), "position") : null;
            if (field == null && property == null) VehicleSystemsMod.Logger.Warn("An overlay's position could not be found, so a moving part will turn about its own middle instead of its hinge.");
        }
        if (field != null) field.SetValue(transform, value);
        else property?.SetValue(transform, value);
    }
}

/// <summary>A part of a set-up vehicle that swings flat about a hinge when it sets up: its own picture, drawn at rest on the body's canvas. Named in XML.</summary>
public class SetUpMovingPart
{
    public string key = "";
    [MustTranslate] public string label = "";
    public GraphicDataOverlay? look;
    /// <summary>The hinge, in cells from the centre of the picture, with the vehicle facing north.</summary>
    public float pivotX;
    public float pivotZ;
    /// <summary>How far the part reaches from its hinge, in cells: what must be clear for it to swing.</summary>
    public float reach = 3f;
    public float angle = 90f;
    public float seconds = 2f;
    /// <summary>Pictures drawn on the part, on its own canvas, while their node is fitted, swinging with it (12 Nov 2026: the survey truck's
    /// geophone spikes and resistivity reel ride on its boom). Each with a node; several can show at once.</summary>
    public List<NodeLook> extras = new();
}

/// <summary>
/// A vehicle that sets up to work: while set up it can be neither driven nor drafted (utility vehicles, principle 3). Each vehicle says what setting up means and keeps its
/// own saved key; everything that holds it still lives here.
/// </summary>
public abstract class CompPhobosSetUp : VehicleComp
{
    const string LookKey = "PhobosSetUpLook";
    const string PartKey = "PhobosSetUpPart";
    const float TicksPerSecond = 60f;
    /// <summary>Vehicle Framework burns half its daily rate while a vehicle idles (read from its source).</summary>
    const float IdlingFuelShare = 0.5f;
    /// <summary>Trees are not walls: a part may swing over them, but only if neither side is free of them.</summary>
    const int HardBlocked = -1;

    protected bool deployed;
    int swingSide = 1;

    // The moving parts' state. Derived, never saved: a load shows them where the set-up state says.
    readonly List<GraphicOverlay> partOverlays = new();
    // The part each overlay belongs to, in step with partOverlays: a part's extras swing with it.
    readonly List<SetUpMovingPart> overlayParts = new();
    bool registeredForUpgrades;
    int motionStartTick = -1;
    bool motionOpening;
    HashSet<IntVec3>? sweptCells;

    public bool Deployed => deployed;

    /// <summary>The saved key, stable once a save can hold it.</summary>
    protected abstract string DeployedSaveKey { get; }

    /// <summary>What the player is told when they try to move it.</summary>
    protected abstract string SetUpRefusal { get; }

    protected virtual bool HeldBySetUp => deployed || MotionBusy;

    /// <summary>The parts that swing out when it sets up; none by default.</summary>
    protected virtual List<SetUpMovingPart>? MovingParts => null;

    float MotionTicks => (MovingParts?.Max(p => p.seconds) ?? 0f) * TicksPerSecond;

    bool MotionBusy => motionStartTick >= 0 && Find.TickManager.TicksGame - motionStartTick < MotionTicks;

    /// <summary>The cells a part would pass through to swing to this side, in the map's frame.</summary>
    IEnumerable<IntVec3> SweepOf(SetUpMovingPart part, int side)
    {
        Vector3 centre = Vehicle.Position.ToVector3Shifted();
        float facing = Vehicle.FullRotation.AsAngle;
        foreach ((float x, float z) in HingeRule.Swept(part.pivotX, part.pivotZ, part.reach, part.angle * side))
        {
            (float x, float z) world = HingeRule.Rotate(x, z, facing);
            yield return new Vector3(centre.x + world.x, 0f, centre.z + world.z).ToIntVec3();
        }
    }

    /// <summary>How bad a side is for the parts: a count of trees in the way, or <see cref="HardBlocked"/> if a wall, a building or another vehicle stands in it, so the part would draw through them.</summary>
    int SideScore(List<SetUpMovingPart> parts, int side)
    {
        Map map = Vehicle.Map;
        CellRect own = Vehicle.OccupiedRect();
        List<CellRect> others = map.mapPawns.AllPawnsSpawned.OfType<VehiclePawn>().Where(v => v != Vehicle).Select(v => v.OccupiedRect()).ToList();
        var trees = new HashSet<IntVec3>();
        foreach (SetUpMovingPart part in parts)
        {
            foreach (IntVec3 cell in SweepOf(part, side))
            {
                if (own.Contains(cell)) continue;
                if (!cell.InBounds(map) || cell.Impassable(map) || cell.GetEdifice(map) != null || others.Any(rect => rect.Contains(cell))) return HardBlocked;
                if (cell.GetPlant(map) is { def.plant.IsTree: true }) trees.Add(cell);
            }
        }
        return trees.Count;
    }

    /// <summary>The clearer side for the parts to swing to; 0 if neither has room.</summary>
    int ClearerSide(List<SetUpMovingPart> parts)
    {
        int left = SideScore(parts, 1), right = SideScore(parts, -1);
        if (left == HardBlocked && right == HardBlocked) return 0;
        if (left == HardBlocked) return -1;
        if (right == HardBlocked) return 1;
        return right < left ? -1 : 1;
    }

    /// <summary>Why the parts have no room to swing out, or null. Read before a set-up so a part never draws through a wall.</summary>
    protected string? MovingPartsRefusal()
    {
        List<SetUpMovingPart>? parts = MovingParts;
        if (parts == null || parts.Count == 0 || !Vehicle.Spawned || ClearerSide(parts) != 0) return null;
        return "PhobosVehicleSystems_PartNoRoom".Translate(parts[0].label.Named("PART")).Resolve();
    }

    /// <summary>Sets the parts swinging out, or back in.</summary>
    protected void StartMotion(bool opening)
    {
        List<SetUpMovingPart>? parts = MovingParts;
        if (parts == null || parts.Count == 0) return;
        if (opening)
        {
            int side = ClearerSide(parts);
            swingSide = side == 0 ? swingSide : side;
            sweptCells = new HashSet<IntVec3>(parts.SelectMany(p => SweepOf(p, swingSide)));
        }
        else
        {
            sweptCells = null;
        }
        motionOpening = opening;
        motionStartTick = Find.TickManager.TicksGame;
    }

    void AddMovingParts()
    {
        List<SetUpMovingPart>? parts = MovingParts;
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (parts == null || renderer == null || !Vehicle.Spawned) return;
        RemoveMovingParts();
        foreach (SetUpMovingPart part in parts)
        {
            if (part.look == null) continue;
            AddPartOverlay(renderer, part, part.look);
            foreach (NodeLook extra in part.extras)
            {
                if (extra.look != null && extra.node != null && VehicleUpgrades.Unlocked(Vehicle, extra.node)) AddPartOverlay(renderer, part, extra.look);
            }
        }
        if (deployed) sweptCells = new HashSet<IntVec3>(parts.SelectMany(p => SweepOf(p, swingSide)));
        Pose(deployed ? 1f : 0f);
    }

    void AddPartOverlay(GraphicOverlayRenderer renderer, SetUpMovingPart part, GraphicDataOverlay look)
    {
        GraphicOverlay overlay = GraphicOverlay.Create(look, Vehicle);
        renderer.AddOverlay(PartKey, overlay);
        partOverlays.Add(overlay);
        overlayParts.Add(part);
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registeredForUpgrades || MovingParts?.Any(p => p.extras.Count > 0) != true) return;
        registeredForUpgrades = true;
        // An extra comes and goes with its upgrade: the parts are drawn again, in their current pose.
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeCompleted, AddMovingParts);
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, AddMovingParts);
    }

    void RemoveMovingParts()
    {
        partOverlays.Clear();
        overlayParts.Clear();
        Vehicle.DrawTracker?.overlayRenderer?.RemoveOverlays(PartKey);
    }

    /// <summary>Puts the parts at this share of their swing. The game turns a picture about its own centre, so each is also moved to keep its hinge in place.</summary>
    void Pose(float share)
    {
        if (MovingParts == null || partOverlays.Count != overlayParts.Count) return;
        float facing = Vehicle.FullRotation.AsAngle;
        for (int index = 0; index < partOverlays.Count; index++)
        {
            SetUpMovingPart part = overlayParts[index];
            float degrees = part.angle * swingSide * HingeRule.Eased(share);
            (float x, float z) pivot = HingeRule.Rotate(part.pivotX, part.pivotZ, facing);
            (float x, float z) offset = HingeRule.PivotOffset(pivot.x, pivot.z, degrees);
            GraphicOverlay overlay = partOverlays[index];
            overlay.Transform.rotation = degrees;
            OverlayPosition.Set(overlay.Transform, new Vector3(offset.x, 0f, offset.z));
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (MovingParts != null) LongEventHandler.ExecuteWhenFinished(AddMovingParts);
        if (handOrder != HandSetUpOrder.None) HandSetUps.Note(this, true);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (MovingParts != null) RemoveMovingParts();
        // Leaving the map calls off a colonist's waiting work there.
        handOrder = HandSetUpOrder.None;
        HandSetUps.Note(this, false);
        VehicleWorkSounds.EndAll(Vehicle);
        motionStartTick = -1;
        sweptCells = null;
        base.PostDeSpawn(map, mode);
    }

    public override void PostDraw()
    {
        base.PostDraw();
        if (motionStartTick < 0) return;
        float share = Mathf.Clamp01((Find.TickManager.TicksGame - motionStartTick) / Mathf.Max(1f, MotionTicks));
        Pose(motionOpening ? share : 1f - share);
        if (share >= 1f) motionStartTick = -1;
    }

    /// <summary>What it looks like set up, drawn over the body; null for nothing.</summary>
    protected abstract GraphicDataOverlay? SetUpLook { get; }

    protected void ShowSetUp(bool on)
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null || SetUpLook == null) return;
        renderer.RemoveOverlays(LookKey);
        if (on && Vehicle.Spawned) renderer.AddOverlay(LookKey, GraphicOverlay.Create(SetUpLook, Vehicle));
    }

    /// <summary>Whether a small fitting of ours could go down on this cell, within this many cells of the vehicle.</summary>
    public bool CellClearFor(IntVec3 cell, int reach)
    {
        Map? map = Vehicle.MapHeld;
        if (map == null || !cell.InBounds(map) || !cell.Standable(map) || cell.GetTransmitter(map) != null) return false;
        if (sweptCells != null && sweptCells.Contains(cell)) return false;
        CellRect footprint = Vehicle.OccupiedRect();
        if (footprint.Contains(cell) || !footprint.ExpandedBy(System.Math.Max(1, reach)).Contains(cell)) return false;
        return !map.mapPawns.AllPawnsSpawned.Any(p => p is VehiclePawn other && other != Vehicle && other.OccupiedRect().Contains(cell));
    }

    /// <summary>The cell just behind the middle of the vehicle.</summary>
    public IntVec3 RearCell
    {
        get
        {
            CellRect footprint = Vehicle.OccupiedRect();
            IntVec3 rear = -Vehicle.Rotation.FacingCell;
            IntVec3 cell = Vehicle.Position;
            while (footprint.Contains(cell)) cell += rear;
            return cell;
        }
    }

    /// <summary>The best cell beside the vehicle for a small fitting: touching a conduit if one is there, or else invalid if none is clear.</summary>
    public IntVec3 BestAdjacentCell()
    {
        Map map = Vehicle.Map;
        var cells = Vehicle.OccupiedRect().ExpandedBy(1).ClipInsideMap(map).EdgeCells.Where(c => CellClearFor(c, 1)).ToList();
        if (cells.Count == 0) return IntVec3.Invalid;
        return cells.FirstOrDefault(c => GenAdj.CardinalDirections.Any(d => (c + d).InBounds(map) && (c + d).GetTransmitter(map) != null), cells[0]);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref deployed, DeployedSaveKey);
        Scribe_Values.Look(ref swingSide, DeployedSaveKey + "Side", 1);
        // Framework 0.106.1. A save made before it has no stall, so a set left working with nobody aboard keeps on until it runs dry.
        Scribe_Values.Look(ref stalled, DeployedSaveKey + "Stalled");
        // Framework 0.111.0: a colonist's set-up or pack-up still to do at a trailer.
        Scribe_Values.Look(ref handOrder, DeployedSaveKey + "HandOrder", HandSetUpOrder.None);
    }

    // ---- Crew, engine and the stall (Framework 0.106.1; owner, 13 Nov 2026) ----

    /// <summary>The work stopped because its engine did: it ran dry or was switched off. It stays stopped, even refuelled, until someone
    /// aboard starts the engine again. Saved; cleared when the vehicle packs up.</summary>
    protected bool stalled;
    /// <summary>Whether the engine was switched on at the last look, so switching it off and on again can be seen. Derived, never saved:
    /// after a load the first look only records it.</summary>
    bool? lastIgnition;

    public bool Stalled => deployed && stalled;

    /// <summary>
    /// Why it cannot set up for want of a crew or a running engine, or null (owner, 13 Nov 2026: setting up needs someone aboard and the
    /// engine running). "Running" is the engine switched on, which Vehicle Framework allows only with enough crew aboard to operate it
    /// (VehiclePawn.CanDraft); once it runs, the crew may get out and the work goes on by itself. Every set-up vehicle asks this first.
    /// </summary>
    protected string? CrewAndEngineRefusal()
    {
        // On a tow bar or a hitch it is not standing anywhere to work (Framework 0.111.0; vehicle-trailers.md, B0.2): the recovery tow could
        // otherwise drag a tanker with its hoses out.
        if (Hitches.Towing(Vehicle) is { } towedBy)
        {
            return "PhobosTrailer_SetUpWhileTowed".Translate(Vehicle.LabelShort.Named("VEHICLE"), towedBy.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        // A trailer has no crew and no engine: a colonist sets it up by hand instead (B0.1).
        if (SetUpByHand) return null;
        if (Vehicle.Drafted) return null;
        if (Vehicle.AllPawnsAboard.Count == 0) return "PhobosVehicleSystems_SetUpNoCrew".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        return "PhobosVehicleSystems_SetUpEngineOff".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
    }

    /// <summary>The line for an inspect pane, a refusal or a stopped job while stalled.</summary>
    protected string StalledLine => "PhobosVehicleSystems_SetUpStalledLine".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    /// <summary>Whether the work turns the engine now, so switching it off stops the work. A vehicle whose work runs on mains power does not.</summary>
    protected virtual bool NeedsEngineNow => this is not IVehicleEngineDemand demand || demand.WantsEngine;

    /// <summary>A vehicle that says the stall in its own report (the tipper's) sets this, so the player is told once.</summary>
    protected virtual bool ReportsOwnStall => false;

    public override void CompTickRare()
    {
        base.CompTickRare();
        WatchEngine();
    }

    /// <summary>On the rare tick: stalls the work when its engine stops under it, and starts it again when someone aboard restarts the engine.</summary>
    void WatchEngine()
    {
        bool ignition = Vehicle.Drafted;
        bool? before = lastIgnition;
        lastIgnition = ignition;
        if (!deployed)
        {
            stalled = false;
            return;
        }
        if (stalled)
        {
            if (ignition && before == false)
            {
                stalled = false;
                if (!ReportsOwnStall) Messages.Message("PhobosVehicleSystems_SetUpRestarted".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.PositiveEvent, historical: false);
            }
            return;
        }
        if (Vehicle.CompFueledTravel is { EmptyTank: true }) Stall("PhobosVehicleSystems_SetUpStalledDry".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve());
        else if (before == true && !ignition && NeedsEngineNow) Stall("PhobosVehicleSystems_SetUpStalledOff".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve());
    }

    void Stall(string message)
    {
        stalled = true;
        // The engine is off: restarting it is the one way back, and Vehicle Framework allows that only with someone aboard.
        if (Vehicle.Drafted) Vehicle.ignition.Drafted = false;
        lastIgnition = false;
        if (!ReportsOwnStall) Messages.Message(message, Vehicle, MessageTypeDefOf.NegativeEvent);
    }

    // ---- Set up and packed by hand: trailers (Framework 0.111.0; docs/development/vehicle-trailers.md, B0.1) ----

    /// <summary>What a colonist has been asked to do at this vehicle by hand. Saved; cleared when the work is done or called off.</summary>
    HandSetUpOrder handOrder;

    /// <summary>Whether a colonist sets it up and packs it up by hand: a trailer, which has no crew or engine of its own.</summary>
    public bool SetUpByHand => Vehicle.GetComp<CompPhobosTrailer>() != null;

    public HandSetUpOrder HandOrder => handOrder;

    /// <summary>The work the waiting order takes, at a colonist's construction speed: set by the trailer's Def.</summary>
    public float HandWork
    {
        get
        {
            CompProperties_PhobosTrailer? trailer = Vehicle.GetComp<CompPhobosTrailer>()?.Props;
            if (trailer == null) return 1f;
            return handOrder == HandSetUpOrder.PackUp ? trailer.packUpWork : trailer.setUpWork;
        }
    }

    /// <summary>
    /// For a vehicle set up by hand, asks a colonist to set it up or pack it up, and returns true: the change itself waits for the work. False for a vehicle
    /// whose crew does it at once, which the caller then does. The caller has already checked that the change may be made.
    /// </summary>
    protected bool OrderByHand(bool on)
    {
        if (!SetUpByHand) return false;
        handOrder = on ? HandSetUpOrder.SetUp : HandSetUpOrder.PackUp;
        HandSetUps.Note(this, true);
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        Messages.Message(on ? "PhobosTrailer_SetUpOrdered".Translate(name).Resolve() : "PhobosTrailer_PackUpOrdered".Translate(name).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
        return true;
    }

    /// <summary>Calls off a waiting order: nothing changes at the vehicle.</summary>
    public void CancelHandOrder()
    {
        if (handOrder == HandSetUpOrder.None) return;
        bool wasSetUp = handOrder == HandSetUpOrder.SetUp;
        handOrder = HandSetUpOrder.None;
        HandSetUps.Note(this, false);
        NamedArgument name = Vehicle.LabelShort.Named("VEHICLE");
        Messages.Message(wasSetUp ? "PhobosTrailer_SetUpCancelled".Translate(name).Resolve() : "PhobosTrailer_PackUpCancelled".Translate(name).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Why a colonist cannot do the waiting order now, or null: read before the job starts and again when the work is done.</summary>
    public string? HandOrderRefusal()
    {
        if (handOrder == HandSetUpOrder.None) return null;
        if (Hitches.Towing(Vehicle) is { } towedBy)
        {
            return "PhobosTrailer_SetUpWhileTowed".Translate(Vehicle.LabelShort.Named("VEHICLE"), towedBy.Vehicle.LabelShort.Named("TRACTOR")).Resolve();
        }
        return handOrder == HandSetUpOrder.SetUp ? HandSetUpRefusal() : null;
    }

    /// <summary>Why it cannot be set up now, for a vehicle set up by hand; the subclass's own refusal.</summary>
    protected virtual string? HandSetUpRefusal() => null;

    /// <summary>A colonist has finished the work: the change is made now, if it still may be.</summary>
    public void HandWorkDone(Pawn worker)
    {
        HandSetUpOrder done = handOrder;
        handOrder = HandSetUpOrder.None;
        HandSetUps.Note(this, false);
        if (done == HandSetUpOrder.None) return;
        if (done == HandSetUpOrder.SetUp && HandSetUpRefusal() is { } refusal)
        {
            Messages.Message(refusal, Vehicle, MessageTypeDefOf.RejectInput);
            return;
        }
        FinishByHand(done == HandSetUpOrder.SetUp, worker);
    }

    /// <summary>Makes the change a colonist's work has earned: set up when <paramref name="on"/>, packed up otherwise.</summary>
    protected virtual void FinishByHand(bool on, Pawn worker)
    {
    }

    /// <summary>The inspect line while a colonist is awaited, or null.</summary>
    protected string? HandOrderLine => handOrder switch
    {
        HandSetUpOrder.SetUp => "PhobosTrailer_InspectSetUpWaiting".Translate().Resolve(),
        HandSetUpOrder.PackUp => "PhobosTrailer_InspectPackUpWaiting".Translate().Resolve(),
        _ => null,
    };

    /// <summary>The button that calls off a waiting order, with the game's own cancel picture.</summary>
    public Command_Action CancelHandOrderGizmo() => new()
    {
        defaultLabel = handOrder == HandSetUpOrder.SetUp ? "PhobosTrailer_GizmoCancelSetUp".Translate().Resolve() : "PhobosTrailer_GizmoCancelPackUp".Translate().Resolve(),
        defaultDesc = "PhobosTrailer_GizmoCancelDesc".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
        icon = SystemIcons.For("UI/Designators/Cancel"),
        action = CancelHandOrder,
    };

    // Only moving is refused. Drafting is also how a vehicle's engine starts (Vehicle Framework has one switch for both), and a vehicle set up to work may
    // want its engine on: real rigs run their hydraulics from it (owner, 9 Nov 2026).
    public override AcceptanceReport CanMove(FloatMenuContext context) => HeldBySetUp ? SetUpRefusal : base.CanMove(context);

    /// <summary>What it tells a driver who tries to take it away now, or null if it may go. For orders that do not come through the right-click menu.</summary>
    public string? HoldRefusal => HeldBySetUp ? SetUpRefusal : null;

    /// <summary>It is still rolling, so it cannot set up.</summary>
    protected bool StillMoving => Vehicle.vehiclePather is { Moving: true };

    /// <summary>The fuel to take for this much work over these days: with a driver holding the engine on, the idling Vehicle Framework already charges counts towards it.</summary>
    protected float WorkFuel(float amount, float days)
    {
        CompFueledTravel? fuel = Vehicle.CompFueledTravel;
        return fuel == null ? amount : OverdriveRule.WorkFuel(amount, fuel.FuelEfficiency * IdlingFuelShare * days, Vehicle.Drafted);
    }
}

/// <summary>What a colonist has been asked to do at a vehicle set up by hand. Saved by name: append only.</summary>
public enum HandSetUpOrder
{
    None,
    SetUp,
    PackUp,
}

/// <summary>A comp that needs the vehicle's engine turning while it works, though nobody is driving.</summary>
public interface IVehicleEngineDemand
{
    bool WantsEngine { get; }

    /// <summary>How hard it works the engine, from 0 to 1.</summary>
    float EngineDemand { get; }
}
