using System.Linq;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle that sets up to work: while set up it can be neither driven nor drafted (utility vehicles, principle 3). Each vehicle says what setting up means and keeps its
/// own saved key; everything that holds it still lives here.
/// </summary>
public abstract class CompPhobosSetUp : VehicleComp
{
    const string LookKey = "PhobosSetUpLook";

    protected bool deployed;

    public bool Deployed => deployed;

    /// <summary>The saved key, stable once a save can hold it.</summary>
    protected abstract string DeployedSaveKey { get; }

    /// <summary>What the player is told when they try to move it.</summary>
    protected abstract string SetUpRefusal { get; }

    protected virtual bool HeldBySetUp => deployed;

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
