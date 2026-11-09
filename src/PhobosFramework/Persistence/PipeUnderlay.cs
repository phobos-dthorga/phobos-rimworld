using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// The pipes a thing has beneath it (owner, 9 Nov 2026): the piping mod's own pipe, one to a cell, wherever its footprint has none, so a pipe touching any side
/// connects and a run passes through, as a plumbed fixture's does. It never touches a pipe it did not lay. Saved as references, so what it laid is known after a
/// load; a thing that has none yet lays them at its next look, which is how an older save converts. If this mod is ever removed, what is left is ordinary pipe.
/// </summary>
public sealed class PipeUnderlay : IExposable
{
    const string SaveKey = "PhobosPipeUnderlayPieces";

    List<Thing> pieces = new();

    /// <summary>A pipe of this underlay lies on the cell.</summary>
    public bool Holds(IntVec3 cell)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] is { Spawned: true } piece && piece.Position == cell) return true;
        }
        return false;
    }

    /// <summary>Whether a pipe that is not this underlay's lies under the footprint or against a side of it.</summary>
    public bool TouchesRun(CellRect footprint, Map map, Func<IntVec3, bool> piped)
    {
        CellRect ring = footprint.ExpandedBy(1);
        foreach (IntVec3 cell in ring)
        {
            bool corner = (cell.x == ring.minX || cell.x == ring.maxX) && (cell.z == ring.minZ || cell.z == ring.maxZ);
            if (corner || !cell.InBounds(map) || Holds(cell)) continue;
            if (piped(cell)) return true;
        }
        return false;
    }

    /// <summary>Lays a pipe on every cell of the footprint that has none.</summary>
    public void Fill(CellRect footprint, Map map, Func<IntVec3, bool> piped, Func<Thing?> make)
    {
        foreach (IntVec3 cell in footprint)
        {
            if (!cell.InBounds(map) || piped(cell)) continue;
            Thing? piece = make();
            if (piece == null) return;
            if (piece.def.CanHaveFaction) piece.SetFaction(Faction.OfPlayer);
            pieces.Add(GenSpawn.Spawn(piece, cell, map));
        }
    }

    /// <summary>Takes up every pipe this underlay laid.</summary>
    public void Clear()
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] is { Destroyed: false } piece) piece.Destroy(DestroyMode.Vanish);
        }
        pieces.Clear();
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref pieces, SaveKey, LookMode.Reference);
        pieces ??= new List<Thing>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit) pieces.RemoveAll(piece => piece == null);
    }
}
