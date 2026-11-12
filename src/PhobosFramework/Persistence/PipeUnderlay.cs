using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// A network's pieces a thing has beneath it (owner, 9 Nov 2026; conduits too from Framework 0.107.0): the network's own ordinary piece, one to a cell, wherever its
/// footprint has none, so a pipe or conduit touching any side connects and a run passes through, as a plumbed fixture's does. What it lays is the visible piece the
/// player would build (owner, 14 Nov 2026: "that's the convention of the vanilla game and other mods"); pieces laid hidden by an older version are swapped for
/// visible ones by <see cref="Renew"/>, on the same cells. It never touches a piece it did not lay. Saved as references with their cells, so what it laid is known
/// after a load; a thing that has none yet lays them at its next look, which is how an older save converts. If this mod is ever removed, what is left is ordinary
/// pipe or conduit.
/// <para>A piece that goes (the player deconstructs it, or it is destroyed) is not laid again on that cell while the thing stays where it is: the player's choice
/// stands, and nothing is laid for free over and over. It is laid again when the thing is placed afresh (a vehicle parked again, a set set up again).</para>
/// </summary>
public sealed class PipeUnderlay : IExposable
{
    const string SaveKey = "PhobosPipeUnderlayPieces";
    const string CellsKey = "PhobosPipeUnderlayCells";
    const string DeclinedKey = "PhobosPipeUnderlayDeclined";

    List<Thing> pieces = new();
    /// <summary>Where each piece was laid, beside <see cref="pieces"/>: a piece that has gone can still say where it was.</summary>
    List<IntVec3> cells = new();
    /// <summary>Cells whose piece has gone; not laid again until <see cref="Clear"/>.</summary>
    List<IntVec3> declined = new();

    /// <summary>Whether it has laid anything that still stands.</summary>
    public bool Any
    {
        get
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                if (pieces[i] is { Spawned: true }) return true;
            }
            return false;
        }
    }

    /// <summary>A piece of this underlay lies on the cell.</summary>
    public bool Holds(IntVec3 cell)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i] is { Spawned: true } piece && piece.Position == cell) return true;
        }
        return false;
    }

    /// <summary>Whether a piece that is not this underlay's lies under the footprint or against a side of it.</summary>
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

    /// <summary>Lays a piece on every cell of the footprint that has none, except where one of ours went.</summary>
    public void Fill(CellRect footprint, Map map, Func<IntVec3, bool> piped, Func<Thing?> make)
    {
        NoteGone();
        foreach (IntVec3 cell in footprint)
        {
            if (!cell.InBounds(map) || declined.Contains(cell) || piped(cell)) continue;
            Thing? piece = make();
            if (piece == null) return;
            Add(GenSpawn.Spawn(Own(piece), cell, map), cell);
        }
    }

    /// <summary>
    /// Swaps each piece of ours that is not of <paramref name="wanted"/> for a new one on the same cell (Framework 0.107.0: the hidden pipe older versions laid becomes
    /// the visible one). Safe to call at every look: once all are of the wanted kind it does nothing.
    /// </summary>
    public void Renew(ThingDef? wanted, Func<Thing?> make)
    {
        if (wanted == null) return;
        NoteGone();
        for (int i = 0; i < pieces.Count; i++)
        {
            Thing old = pieces[i];
            if (!old.Spawned || old.def == wanted) continue;
            Thing? piece = make();
            if (piece == null) return;
            Map map = old.Map;
            IntVec3 cell = old.Position;
            old.Destroy(DestroyMode.Vanish);
            pieces[i] = GenSpawn.Spawn(Own(piece), cell, map);
            cells[i] = cell;
        }
    }

    /// <summary>Takes up every piece this underlay laid, and forgets which cells the player had cleared: the thing is being moved or packed up.</summary>
    public void Clear()
    {
        List<Thing> laid = pieces;
        pieces = new List<Thing>();
        cells.Clear();
        declined.Clear();
        for (int i = 0; i < laid.Count; i++)
        {
            if (laid[i] is { Destroyed: false } piece) piece.Destroy(DestroyMode.Vanish);
        }
    }

    static Thing Own(Thing piece)
    {
        if (piece.def.CanHaveFaction) piece.SetFaction(Faction.OfPlayer);
        return piece;
    }

    void Add(Thing piece, IntVec3 cell)
    {
        pieces.Add(piece);
        cells.Add(cell);
    }

    /// <summary>Pieces that have gone without us taking them up: their cells are not laid again.</summary>
    void NoteGone()
    {
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i] is { Destroyed: false }) continue;
            if (i < cells.Count && cells[i].IsValid && !declined.Contains(cells[i])) declined.Add(cells[i]);
            pieces.RemoveAt(i);
            if (i < cells.Count) cells.RemoveAt(i);
        }
    }

    public void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) NoteGone();
        Scribe_Collections.Look(ref pieces, SaveKey, LookMode.Reference);
        Scribe_Collections.Look(ref cells, CellsKey, LookMode.Value);
        Scribe_Collections.Look(ref declined, DeclinedKey, LookMode.Value);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        pieces ??= new List<Thing>();
        cells ??= new List<IntVec3>();
        declined ??= new List<IntVec3>();
        if (cells.Count != pieces.Count)
        {
            // A save from before Framework 0.107.0 kept no cells: read them from the pieces that still stand.
            pieces.RemoveAll(piece => piece == null || !piece.Spawned);
            cells = pieces.ConvertAll(piece => piece.Position);
        }
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i] != null) continue;
            pieces.RemoveAt(i);
            cells.RemoveAt(i);
        }
    }
}
