using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A lump of deep ore put into a map's deep resource grid, by the game's own rules (its scanner's find: GenStep_ScatterDeepResourceLumps
/// and CompDeepScanner). Shared by the survey truck's find on a map and by a map opened on a tile a survey halt read (Framework 0.322.0's
/// helper, moved here in 0.324.0).
/// </summary>
public static class DeepLumps
{
    /// <summary>Puts a lump of this many cells of the ore on open ground away from the edge; false when no ground would take one.</summary>
    public static bool Scatter(Map map, ThingDef ore, int cells, out IntVec3 centre)
    {
        if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(10, x => CanScatterAt(x, map), map, out centre)) return false;
        foreach (IntVec3 cell in GridShapeMaker.IrregularLump(centre, map, Mathf.Max(1, cells)))
        {
            if (CanScatterAt(cell, map) && !cell.InNoBuildEdgeArea(map)) map.deepResourceGrid.SetAt(cell, ore, ore.deepCountPerCell);
        }
        return true;
    }

    /// <summary>The game's test for ground a lump can go in.</summary>
    public static bool CanScatterAt(IntVec3 cell, Map map)
    {
        TerrainDef? terrain = map.terrainGrid.BaseTerrainAt(cell);
        if (terrain != null && terrain.IsWater && terrain.passability == Traversability.Impassable) return false;
        return cell.GetAffordances(map).Contains(ThingDefOf.DeepDrill.terrainAffordanceNeeded) && map.deepResourceGrid.ThingDefAt(cell) == null;
    }
}

/// <summary>
/// Ground already read (Framework 0.324.0, EM32; docs/development/vehicle-road-work.md; the owner's answer of 26 Nov 2026 on the Decisions
/// page: "Yes, as EM32 later"). When a map is made on a tile a survey halt read, the read ore is put into its deep grid as one lump of the
/// read size, once, and the map's deep ore shows as if a scanner stood there. Its one saved fact is whether the lump is laid.
/// </summary>
public sealed class MapComponent_PhobosReadGround : MapComponent
{
    bool laid;

    public MapComponent_PhobosReadGround(Map map) : base(map) { }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (laid) return;
        laid = true;
        if (WorldComponent_PhobosRoadWork.Current?.ReadingAt(map.Tile) is not { ore: { } ore } reading) return;
        int cells = Mathf.CeilToInt(reading.units / (float)Mathf.Max(1, ore.deepCountPerCell));
        if (DeepLumps.Scatter(map, ore, cells, out IntVec3 centre) && Seen.Wants("vehicles.read-ground", null))
            Seen.Note("vehicles.read-ground", null, ore.label + ", " + cells + " cells");
    }

    public override void ExposeData() => Scribe_Values.Look(ref laid, "PhobosReadGroundLaid");
}
