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
    /// <summary>Trees are not walls: a part may swing over them, but only if neither side is free of them.</summary>
    const int HardBlocked = -1;

    protected bool deployed;
    int swingSide = 1;

    // The moving parts' state. Derived, never saved: a load shows them where the set-up state says.
    readonly List<GraphicOverlay> partOverlays = new();
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
            GraphicOverlay overlay = GraphicOverlay.Create(part.look, Vehicle);
            renderer.AddOverlay(PartKey, overlay);
            partOverlays.Add(overlay);
        }
        if (deployed) sweptCells = new HashSet<IntVec3>(parts.SelectMany(p => SweepOf(p, swingSide)));
        Pose(deployed ? 1f : 0f);
    }

    void RemoveMovingParts()
    {
        partOverlays.Clear();
        Vehicle.DrawTracker?.overlayRenderer?.RemoveOverlays(PartKey);
    }

    /// <summary>Puts the parts at this share of their swing. The game turns a picture about its own centre, so each is also moved to keep its hinge in place.</summary>
    void Pose(float share)
    {
        List<SetUpMovingPart>? parts = MovingParts;
        if (parts == null || partOverlays.Count != parts.Count(p => p.look != null)) return;
        float facing = Vehicle.FullRotation.AsAngle;
        int index = 0;
        foreach (SetUpMovingPart part in parts)
        {
            if (part.look == null) continue;
            float degrees = part.angle * swingSide * HingeRule.Eased(share);
            (float x, float z) pivot = HingeRule.Rotate(part.pivotX, part.pivotZ, facing);
            (float x, float z) offset = HingeRule.PivotOffset(pivot.x, pivot.z, degrees);
            GraphicOverlay overlay = partOverlays[index++];
            overlay.Transform.rotation = degrees;
            OverlayPosition.Set(overlay.Transform, new Vector3(offset.x, 0f, offset.z));
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        if (MovingParts != null) LongEventHandler.ExecuteWhenFinished(AddMovingParts);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        if (MovingParts != null) RemoveMovingParts();
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
    }

    public override AcceptanceReport CanMove(FloatMenuContext context) => HeldBySetUp ? SetUpRefusal : base.CanMove(context);

    public override AcceptanceReport CanDraft() => HeldBySetUp ? SetUpRefusal : base.CanDraft();
}

/// <summary>A comp that needs the vehicle's engine turning while it works, though nobody is driving.</summary>
public interface IVehicleEngineDemand
{
    bool WantsEngine { get; }

    /// <summary>How hard it works the engine, from 0 to 1.</summary>
    float EngineDemand { get; }
}
