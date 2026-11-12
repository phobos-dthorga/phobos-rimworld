using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// A network's pieces a thing has beneath it (owner, 9 Nov 2026; conduits too from Framework 0.107.0): the network's own ordinary piece, one to a cell, wherever its
/// footprint has none, so a pipe or conduit touching any side connects and a run passes through, as a plumbed fixture's does. What it lays is the visible piece the
/// player would build (owner, 13 Nov 2026: "that's the convention of the vanilla game and other mods"); pieces laid hidden by an older version are swapped for
/// visible ones by <see cref="Renew"/>, on the same cells. It never touches a piece it did not lay. Saved as references with their cells, so what it laid is known
/// after a load; a thing that has none yet lays them at its next look, which is how an older save converts. If this mod is ever removed, what is left is ordinary
/// pipe or conduit.
/// <para>A piece that goes (the player deconstructs it, or it is destroyed) is not laid again on that cell while the thing stays where it is: the player's choice
/// stands, and nothing is laid for free over and over. It is laid again when the thing is placed afresh (a vehicle parked again, a set set up again).</para>
/// <para>Framework 0.108.0 mends a loading fault of 0.107.0: a save from before 0.107.0 kept no cells, and the cells were rebuilt during loading by asking which
/// pieces were on the map, before the game had put any thing on the map, so every piece was dropped and left orphaned under the thing that laid it. Cells are
/// now rebuilt at the first look, and an underlay saved before 0.108.0 adopts, once, the old hidden pieces standing on its thing's own footprint
/// (<see cref="AdoptOnce"/>), so the visible pipe replaces them.</para>
/// </summary>
public sealed class PipeUnderlay : IExposable
{
    /// <summary>
    /// Every piece any underlay has laid and still holds, so that taking one apart refunds nothing (Framework 0.108.1): since 0.108.0 a vehicle lays its pieces
    /// again at every stop, and a piece that gave back materials when deconstructed would make them from nothing, over and over (Dubs Bad Hygiene's pipe gives
    /// back half its steel). Keyed by the thing itself, weakly, so a thing that has gone, or a game that has closed, leaves nothing behind. Never saved: it is
    /// rebuilt from each underlay's own list as it loads.
    /// </summary>
    static readonly ConditionalWeakTable<Thing, object> Laid = new();
    static readonly object Mark = new();

    /// <summary>Whether this thing was laid by one of our underlays, which refunds nothing when it is taken apart.</summary>
    public static bool LaidByUs(Thing thing) => thing != null && Laid.TryGetValue(thing, out _);

    static void Register(Thing thing)
    {
        if (thing != null && !Laid.TryGetValue(thing, out _)) Laid.Add(thing, Mark);
    }

    static void Unregister(Thing thing)
    {
        if (thing != null) Laid.Remove(thing);
    }

    const string SaveKey = "PhobosPipeUnderlayPieces";
    const string CellsKey = "PhobosPipeUnderlayCells";
    const string DeclinedKey = "PhobosPipeUnderlayDeclined";
    const string AdoptedKey = "PhobosPipeUnderlayAdopted";

    List<Thing> pieces = new();
    /// <summary>Where each piece was laid, beside <see cref="pieces"/>: a piece that has gone can still say where it was.</summary>
    List<IntVec3> cells = new();
    /// <summary>Cells whose piece has gone; not laid again until <see cref="Clear"/>.</summary>
    List<IntVec3> declined = new();
    /// <summary>Whether this underlay has had its one look for orphaned old pieces. True for a new one, which has none to look for; false for one loaded from a
    /// save that does not have the key (saved before Framework 0.108.0).</summary>
    bool adopted = true;
    /// <summary>Cells to rebuild from the pieces at the first look, once the map's things stand: a save from before Framework 0.107.0 kept none. Never saved.</summary>
    bool cellsPending;

    /// <summary>Whether it has laid anything that still stands.</summary>
    /// <summary>Counts every piece laid, swapped or taken up, never saved: a network whose own drawing lags behind its pieces (Dubs Bad Hygiene's
    /// overlay, Framework 0.108.3) compares it before and after a look to know whether to bring its drawing up to date.</summary>
    public int Changes { get; private set; }

    public bool Any
    {
        get
        {
            Settle();
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

    /// <summary>
    /// Lays a piece on every cell of the footprint that has none, except where one of ours went, and, with <paramref name="canLay"/>, where the piece could not
    /// stand (a conduit on water).
    /// </summary>
    public void Fill(CellRect footprint, Map map, Func<IntVec3, bool> piped, Func<Thing?> make, Func<IntVec3, bool>? canLay = null)
    {
        NoteGone();
        foreach (IntVec3 cell in footprint)
        {
            if (!cell.InBounds(map) || declined.Contains(cell) || piped(cell)) continue;
            if (canLay != null && !canLay(cell)) continue;
            Thing? piece = make();
            if (piece == null) return;
            Add(GenSpawn.Spawn(Own(piece), cell, map), cell);
            Changes++;
        }
    }

    /// <summary>
    /// Once, for an underlay saved before Framework 0.108.0: takes back as its own every piece of the old hidden kind (<paramref name="oldKind"/>) standing on the
    /// thing's own footprint where it holds nothing, so <see cref="Renew"/> makes them visible and they go with the thing as they should. It assumes a hidden piece
    /// exactly under one of our things was laid by our earlier versions; a player who built hidden pipe there keeps it, as visible pipe on the same cells.
    /// Returns how many were taken back.
    /// </summary>
    public int AdoptOnce(CellRect footprint, Map map, Func<Thing, bool> oldKind)
    {
        if (adopted) return 0;
        Settle();
        adopted = true;
        int taken = 0;
        foreach (IntVec3 cell in footprint)
        {
            if (!cell.InBounds(map)) continue;
            List<Thing> things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if (!UnderlayRule.Adopts(adoptedBefore: false, oldHiddenKind: oldKind(thing), cellAlreadyHeld: Holds(cell)) || pieces.Contains(thing)) continue;
                Add(thing, cell);
                taken++;
                break;
            }
        }
        return taken;
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
            Unregister(old);
            old.Destroy(DestroyMode.Vanish);
            pieces[i] = GenSpawn.Spawn(Own(piece), cell, map);
            Register(pieces[i]);
            cells[i] = cell;
            Changes++;
        }
    }

    /// <summary>Takes up every piece this underlay laid, and forgets which cells the player had cleared: the thing is being moved or packed up.</summary>
    public void Clear()
    {
        List<Thing> laid = pieces;
        pieces = new List<Thing>();
        cells.Clear();
        declined.Clear();
        cellsPending = false;
        for (int i = 0; i < laid.Count; i++)
        {
            if (laid[i] is not { } piece) continue;
            Unregister(piece);
            if (piece.Destroyed) continue;
            piece.Destroy(DestroyMode.Vanish);
            Changes++;
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
        Register(piece);
    }

    /// <summary>Rebuilds the cells from the pieces, once the map's things stand (a save from before Framework 0.107.0 kept none).</summary>
    void Settle()
    {
        if (!cellsPending) return;
        cellsPending = false;
        pieces.RemoveAll(piece => piece is not { Spawned: true });
        cells = pieces.ConvertAll(piece => piece.Position);
    }

    /// <summary>Pieces that have gone without us taking them up: their cells are not laid again.</summary>
    void NoteGone()
    {
        Settle();
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i] is { Destroyed: false }) continue;
            if (i < cells.Count && cells[i].IsValid && !declined.Contains(cells[i])) declined.Add(cells[i]);
            if (pieces[i] is { } gone) Unregister(gone);
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
        // Missing from a save made before Framework 0.108.0: such an underlay looks once for the pieces 0.107.0's loading fault orphaned.
        Scribe_Values.Look(ref adopted, AdoptedKey, false);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        pieces ??= new List<Thing>();
        cells ??= new List<IntVec3>();
        declined ??= new List<IntVec3>();
        foreach (Thing piece in pieces) Register(piece);
        if (cells.Count != pieces.Count)
        {
            // A save from before Framework 0.107.0 kept no cells. Nothing is on the map yet while loading (the fault 0.107.0 had: it asked, and dropped every
            // piece), so the cells are rebuilt at the first look.
            pieces.RemoveAll(piece => piece == null);
            cells.Clear();
            cellsPending = true;
            return;
        }
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            if (pieces[i] != null) continue;
            pieces.RemoveAt(i);
            cells.RemoveAt(i);
        }
    }
}
