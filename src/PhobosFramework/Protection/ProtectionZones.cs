using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Protection;

/// <summary>
/// One map's protective-gear lines: the painted areas (saved), the control points standing on the map (found again as they spawn, never
/// saved), the gear pawns put on at a line (saved, so they take it off on the way out after a reload) and pawns who were turned back for
/// want of gear (not saved: a short wait, so a pawn doesn't walk to the line again and again).
/// </summary>
public sealed class ProtectionZones : MapComponent
{
    /// <summary>One kind of line's painted cells. Its drawer is made on first use and never saved.</summary>
    sealed class Painted : IExposable, ICellBoolGiver
    {
        public ProtectionZoneDef def = null!;
        public BoolGrid grid = null!;
        CellBoolDrawer? drawer;

        public Color Color => def.color;
        public bool GetCellBool(int index) => grid[index];
        public Color GetCellExtraColor(int index) => Color.white;

        public CellBoolDrawer Drawer(Map map) => drawer ??= new CellBoolDrawer(this, map.Size.x, map.Size.z);

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Deep.Look(ref grid, "grid");
        }
    }

    List<Painted> painted = new();
    List<Apparel> lineGear = new();
    /// <summary>Gear a pawn is taking off on the way out, to be left free for haulers once it is down (Framework 0.77.0). Saved, so a reload
    /// mid-way still frees it.</summary>
    List<Apparel> takingOff = new();
    /// <summary>How often, in ticks, gear being taken off is looked at: the game's rare tick (about four seconds), and only while there is
    /// any; a map with none costs one field read a tick.</summary>
    const int TakingOffCheckTicks = GenTicks.TickRareInterval;
    readonly List<CompPhobosControlPoint> points = new();
    readonly Dictionary<int, int> turnedBackUntil = new();
    readonly Dictionary<int, (int count, int untilTick)> suitUps = new();
    bool anyPainted;

    public ProtectionZones(Map map) : base(map)
    {
    }

    static Map? lastMap;
    static ProtectionZones? last;

    /// <summary>The map's component. The game's allowed-area check calls this very often, and finding a component walks the map's
    /// list, so the last answer is kept: almost every call asks about the map in view.</summary>
    public static ProtectionZones? For(Map? map)
    {
        if (map == null) return null;
        if (map != lastMap)
        {
            last = map.GetComponent<ProtectionZones>();
            lastMap = map;
        }
        return last;
    }

    /// <summary>Whether this map has any line at all; every per-step check starts here, so a map without one costs a field read.</summary>
    public bool Active => anyPainted || points.Count > 0;

    public IReadOnlyList<CompPhobosControlPoint> Points => points;

    /// <summary>The kind of line a cell lies behind, or null: painted cells first, then rooms a control point faces into.</summary>
    public ProtectionZoneDef? ZoneAt(IntVec3 cell)
    {
        if (!Active || !cell.InBounds(map)) return null;
        if (anyPainted)
        {
            for (int i = 0; i < painted.Count; i++)
            {
                if (painted[i].grid[cell]) return painted[i].def;
            }
        }
        if (points.Count == 0) return null;
        Room? room = cell.GetRoom(map);
        if (room == null) return null;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].ControlledRoom == room) return points[i].Zone;
        }
        return null;
    }

    public bool IsPainted(ProtectionZoneDef def, IntVec3 cell) => Find(def)?.grid[cell] ?? false;

    public void SetPainted(ProtectionZoneDef def, IntVec3 cell, bool on)
    {
        Painted? layer = Find(def);
        if (layer == null)
        {
            if (!on) return;
            layer = new Painted { def = def, grid = new BoolGrid(map) };
            painted.Add(layer);
        }
        layer.grid[cell] = on;
        layer.Drawer(map).SetDirty();
        RecountPainted();
    }

    /// <summary>Shows one kind's painted area this frame, as the game's own area designators do for theirs.</summary>
    public void MarkForDraw(ProtectionZoneDef def)
    {
        Painted? layer = Find(def);
        if (layer != null) layer.Drawer(map).MarkForDraw();
    }

    public override void MapComponentUpdate()
    {
        for (int i = 0; i < painted.Count; i++) painted[i].Drawer(map).CellBoolDrawerUpdate();
    }

    public void Register(CompPhobosControlPoint point)
    {
        if (!points.Contains(point)) points.Add(point);
    }

    public void Unregister(CompPhobosControlPoint point) => points.Remove(point);

    public void AddLineGear(Apparel apparel)
    {
        if (!lineGear.Contains(apparel)) lineGear.Add(apparel);
    }

    public bool IsLineGear(Apparel apparel) => lineGear.Contains(apparel);

    public void RemoveLineGear(Apparel apparel) => lineGear.Remove(apparel);

    /// <summary>Gear being taken off on the way out of a zone. The game's own job drops what it takes off forbidden unless it hauls it
    /// itself, and hauling it could carry the pawn back across the line unprotected, so it drops it and this frees it once it is down.</summary>
    public void MarkTakingOff(Apparel apparel)
    {
        if (!takingOff.Contains(apparel)) takingOff.Add(apparel);
    }

    public override void MapComponentTick()
    {
        if (takingOff.Count == 0 || Verse.Find.TickManager.TicksGame % TakingOffCheckTicks != 0) return;
        for (int i = takingOff.Count - 1; i >= 0; i--)
        {
            Apparel apparel = takingOff[i];
            if (apparel == null || apparel.Destroyed)
            {
                takingOff.RemoveAt(i);
                continue;
            }
            if (apparel.Wearer != null) continue;    // still coming off
            if (apparel.Spawned) apparel.SetForbidden(false, warnOnFail: false);
            takingOff.RemoveAt(i);
        }
    }

    /// <summary>Forgets gear that is gone or that nobody is wearing or on the way to put on.</summary>
    public void PruneLineGear()
    {
        lineGear.RemoveAll(a => a == null || a.Destroyed || (a.Wearer == null && !a.Spawned));
    }

    public void TurnBack(Pawn pawn, int untilTick) => turnedBackUntil[pawn.thingIDNumber] = untilTick;

    public bool TurnedBack(Pawn pawn) =>
        turnedBackUntil.TryGetValue(pawn.thingIDNumber, out int until) && Verse.Find.TickManager.TicksGame < until;

    /// <summary>How many times a pawn has been sent to suit up lately; the tally lapses at its tick. Not saved.</summary>
    public int SuitUps(Pawn pawn) =>
        suitUps.TryGetValue(pawn.thingIDNumber, out (int count, int untilTick) tally) && Verse.Find.TickManager.TicksGame < tally.untilTick
            ? tally.count
            : 0;

    /// <summary>Counts one more sending to suit up; a lapsed tally starts again at one.</summary>
    public void NoteSuitUp(Pawn pawn, int lapsesAtTick) => suitUps[pawn.thingIDNumber] = (SuitUps(pawn) + 1, lapsesAtTick);

    public void ForgetSuitUps(Pawn pawn) => suitUps.Remove(pawn.thingIDNumber);

    /// <summary>When a pawn was last sent for a preventive dose: it is not sent again until then, so a dose that never arrives costs one
    /// try and no loop. Not saved.</summary>
    readonly System.Collections.Generic.Dictionary<int, int> doseTriedUntil = new();

    public bool DoseTriedLately(Pawn pawn) =>
        doseTriedUntil.TryGetValue(pawn.thingIDNumber, out int until) && Verse.Find.TickManager.TicksGame < until;

    public void NoteDoseTried(Pawn pawn, int untilTick) => doseTriedUntil[pawn.thingIDNumber] = untilTick;

    Painted? Find(ProtectionZoneDef def)
    {
        for (int i = 0; i < painted.Count; i++)
        {
            if (painted[i].def == def) return painted[i];
        }
        return null;
    }

    void RecountPainted()
    {
        anyPainted = false;
        for (int i = 0; i < painted.Count; i++)
        {
            if (painted[i].grid.TrueCount > 0) { anyPainted = true; return; }
        }
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref painted, "PhobosProtectionPainted", LookMode.Deep);
        Scribe_Collections.Look(ref lineGear, "PhobosProtectionLineGear", LookMode.Reference);
        Scribe_Collections.Look(ref takingOff, "PhobosProtectionTakingOff", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            painted ??= new List<Painted>();
            lineGear ??= new List<Apparel>();
            takingOff ??= new List<Apparel>();
            takingOff.RemoveAll(a => a == null);
            // A kind of line whose mod was removed loads with no Def: its cells are dropped rather than guessed at.
            painted.RemoveAll(p => p == null || p.def == null || p.grid == null);
            lineGear.RemoveAll(a => a == null);
            RecountPainted();
        }
    }
}
