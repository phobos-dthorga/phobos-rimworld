using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Gases;

/// <summary>
/// The framework's own additive room gases, one mixture per room, copied from how the game keeps room temperature
/// (Verse.Room, GenTemperature.EqualizeTemperaturesThroughBuilding, TemperatureVacuumCache; read 4 Nov 2026):
/// a pass every 60 ticks over the rooms that hold gas, and only those; exchange through doors (open or closed, at the game's own
/// door rates) and switched-on vents; loss through gaps in the roof and through doors to the outdoors; fading at each gas's
/// half-life; nothing held outdoors, in doorways or in vacuum. Rooms are remembered by the cells they covered, so a wall built
/// through a smoky room leaves both halves smoky, a merged room dilutes, and a save holds cells, not room numbers.
/// </summary>
public sealed class MapComponent_PhobosRoomGases : MapComponent
{
    sealed class RoomGasState
    {
        public int roomId;
        public int cellCount;
        public float[] concentration = System.Array.Empty<float>();
        public readonly List<int> cells = new();
    }

    /// <summary>One saved cell: the gases it held when the game was saved.</summary>
    sealed class SavedCell : IExposable
    {
        public int cellIndex;
        public List<string> gases = new();
        public List<float> amounts = new();

        public void ExposeData()
        {
            Scribe_Values.Look(ref cellIndex, "cell");
            Scribe_Collections.Look(ref gases, "gases", LookMode.Value);
            Scribe_Collections.Look(ref amounts, "amounts", LookMode.Value);
            gases ??= new List<string>();
            amounts ??= new List<float>();
        }
    }

    readonly Dictionary<int, RoomGasState> states = new();
    int[]? cellToRoom;
    List<SavedCell>? loaded;
    readonly Dictionary<int, Room> roomsById = new();
    readonly HashSet<int> openingsDone = new();
    readonly List<RoomGasState> stale = new();
    readonly List<RoomGasState> pass = new();
    readonly List<int> toRemove = new();
    readonly Dictionary<Room, float[]> rebuilt = new();

    public MapComponent_PhobosRoomGases(Map map) : base(map) { }

    public static MapComponent_PhobosRoomGases? For(Map? map) => map?.GetComponent<MapComponent_PhobosRoomGases>();

    static int GasCount => DefDatabase<PhobosGasDef>.AllDefsListForReading.Count;

    /// <summary>Rooms holding any gas right now.</summary>
    public int RoomsWithGas => states.Count;

    public float Concentration(IntVec3 cell, PhobosGasDef gas)
    {
        if (states.Count == 0 || !cell.InBounds(map)) return 0f;
        Room? room = cell.GetRoom(map);
        if (room == null || !states.TryGetValue(room.ID, out RoomGasState? state)) return 0f;
        return gas.index < state.concentration.Length ? state.concentration[gas.index] : 0f;
    }

    /// <summary>Adds <paramref name="cells"/> cells of gas to the room at a cell. Outdoors, in a doorway or in vacuum it is lost.</summary>
    public void AddGas(IntVec3 cell, PhobosGasDef gas, float cells)
    {
        if (cells <= 0f || !cell.InBounds(map)) return;
        Room? room = cell.GetRoom(map);
        if (!Holds(room)) return;
        RoomGasState state = StateFor(room!);
        int g = gas.index;
        if (g >= state.concentration.Length) return;
        state.concentration[g] = GasMixRule.Clamp(state.concentration[g] + cells / state.cellCount, gas.maxConcentration);
    }

    public override void MapComponentTick()
    {
        if (states.Count == 0 || Find.TickManager.TicksGame % GasMixRule.PassTicks != 0) return;
        PhobosGasRulesDef rules = PhobosGasRulesDef.Current;
        List<PhobosGasDef> gases = DefDatabase<PhobosGasDef>.AllDefsListForReading;
        IndexRooms();
        RebuildStaleRooms();
        openingsDone.Clear();
        toRemove.Clear();
        // Exchange can give a neighbouring room its first state, so walk a copy of the list.
        pass.Clear();
        pass.AddRange(states.Values);
        for (int s = 0; s < pass.Count; s++)
        {
            RoomGasState state = pass[s];
            if (!roomsById.TryGetValue(state.roomId, out Room? room) || !Holds(room) || room.Vacuum >= rules.vacuumClears)
            {
                toRemove.Add(state.roomId);
                continue;
            }
            Exchange(state, room, rules);
            bool any = false;
            for (int g = 0; g < gases.Count && g < state.concentration.Length; g++)
            {
                float value = state.concentration[g];
                if (value <= 0f) continue;
                value *= GasMixRule.DecayFactor(gases[g].decayHalfLifeTicks);
                if (room.OpenRoofCount > 0)
                {
                    value *= GasMixRule.OpenRoofFactor(room.OpenRoofCount, room.CellCount, gases[g].openRoofHalfLifeTicks);
                }
                value = GasMixRule.Clamp(value, gases[g].maxConcentration);
                if (value < rules.minConcentration) value = 0f;
                state.concentration[g] = value;
                if (value > 0f) any = true;
            }
            if (!any) toRemove.Add(state.roomId);
        }
        foreach (int id in toRemove) Remove(id);
    }

    /// <summary>Doors and vents: the game's own openings, at the game's own rates, moving gas toward the shared mean.</summary>
    void Exchange(RoomGasState state, Room room, PhobosGasRulesDef rules)
    {
        List<Region> regions = room.Regions;
        for (int r = 0; r < regions.Count; r++)
        {
            foreach (Region neighbour in regions[r].Neighbors)
            {
                if (neighbour.type != RegionType.Portal || neighbour.door == null) continue;
                Building_Door door = neighbour.door;
                if (!openingsDone.Add(door.thingIDNumber)) continue;
                float coefficient = door.Open ? rules.DoorOpenCoefficient : rules.DoorClosedCoefficient;
                foreach (Region beyond in neighbour.Neighbors)
                {
                    if (beyond.type != RegionType.Normal) continue;
                    Room? other = beyond.Room;
                    if (other == null || other == room) continue;
                    ExchangeWith(state, room, other, coefficient, rules);
                }
            }
        }
        List<Thing> things = room.ContainedAndAdjacentThings;
        for (int t = 0; t < things.Count; t++)
        {
            if (things[t] is not Building_Vent vent || !vent.Spawned || !FlickUtility.WantsToBeOn(vent)) continue;
            if (!openingsDone.Add(vent.thingIDNumber)) continue;
            Room? a = (vent.Position + vent.Rotation.FacingCell).GetRoom(map);
            Room? b = (vent.Position - vent.Rotation.FacingCell).GetRoom(map);
            if (a != room && b != room) continue;    // beside this room, not venting it
            Room? other = a == room ? b : b == room ? a : null;
            if (other == null && (a == null || b == null)) other = a ?? b;
            if (other == room) continue;
            ExchangeWith(state, room, other, rules.VentCoefficient, rules);
        }
    }

    void ExchangeWith(RoomGasState state, Room room, Room? other, float coefficient, PhobosGasRulesDef rules)
    {
        if (coefficient <= 0f) return;
        if (!Holds(other))
        {
            // An opening onto the outdoors (or a doorway, or vacuum) is a sink.
            float lost = GasMixRule.VentFraction(room.CellCount, coefficient);
            for (int g = 0; g < state.concentration.Length; g++) state.concentration[g] *= 1f - lost;
            return;
        }
        RoomGasState? otherState = states.TryGetValue(other!.ID, out RoomGasState? found) ? found : null;
        for (int g = 0; g < state.concentration.Length; g++)
        {
            float mine = state.concentration[g];
            float theirs = otherState != null && g < otherState.concentration.Length ? otherState.concentration[g] : 0f;
            if (mine <= 0f && theirs <= 0f) continue;
            float moved = GasMixRule.Exchange(mine, room.CellCount, theirs, other.CellCount, coefficient);
            if (moved == 0f) continue;
            if (otherState == null)
            {
                if (moved >= 0f) continue; // nothing to pull from an empty room
                otherState = StateFor(other);
            }
            state.concentration[g] = mine + moved / room.CellCount;
            if (g < otherState.concentration.Length) otherState.concentration[g] = theirs - moved / other.CellCount;
        }
        if (otherState != null && otherState.concentration.Length > 0)
        {
            bool any = false;
            for (int g = 0; g < otherState.concentration.Length; g++)
            {
                if (otherState.concentration[g] >= rules.minConcentration) any = true;
                else otherState.concentration[g] = 0f;
            }
            if (!any) toRemove.Add(other.ID);
        }
    }

    static bool Holds(Room? room) =>
        room != null && !room.IsDoorway && !room.UsesOutdoorTemperature && !room.PsychologicallyOutdoors && room.CellCount > 0;

    RoomGasState StateFor(Room room)
    {
        // A room that has changed size keeps its ID: its state is left as it is for the next pass to rebuild, or the gas would never be shared out.
        if (states.TryGetValue(room.ID, out RoomGasState? state)) return state;
        state = new RoomGasState { roomId = room.ID, concentration = new float[GasCount] };
        states[room.ID] = state;
        Cover(state, room);
        return state;
    }

    /// <summary>Points the room's cells at its state; the cell list is what survives a rebuild.</summary>
    void Cover(RoomGasState state, Room room)
    {
        cellToRoom ??= NewCellIndex();
        foreach (int index in state.cells) if (cellToRoom[index] == state.roomId) cellToRoom[index] = -1;
        state.cells.Clear();
        foreach (IntVec3 cell in room.Cells)
        {
            int index = map.cellIndices.CellToIndex(cell);
            cellToRoom[index] = state.roomId;
            state.cells.Add(index);
        }
        state.cellCount = room.CellCount;
    }

    int[] NewCellIndex()
    {
        var index = new int[map.cellIndices.NumGridCells];
        for (int i = 0; i < index.Length; i++) index[i] = -1;
        return index;
    }

    void IndexRooms()
    {
        roomsById.Clear();
        IReadOnlyList<Room> rooms = map.regionGrid.AllRooms;
        for (int i = 0; i < rooms.Count; i++) roomsById[rooms[i].ID] = rooms[i];
    }

    /// <summary>
    /// A room that no longer exists, or changed shape, is spread over the rooms its cells now belong to, amount-weighted
    /// (the game does the same for temperature through its cell cache).
    /// </summary>
    void RebuildStaleRooms()
    {
        stale.Clear();
        foreach (RoomGasState state in states.Values)
        {
            if (!roomsById.TryGetValue(state.roomId, out Room? room) || room.CellCount != state.cellCount) stale.Add(state);
        }
        if (stale.Count == 0) return;
        rebuilt.Clear();
        foreach (RoomGasState old in stale)
        {
            states.Remove(old.roomId);
            foreach (int index in old.cells)
            {
                if (cellToRoom != null && cellToRoom[index] == old.roomId) cellToRoom[index] = -1;
                Room? now = map.cellIndices.IndexToCell(index).GetRoom(map);
                if (!Holds(now)) continue;
                if (!rebuilt.TryGetValue(now!, out float[]? amount))
                {
                    amount = new float[GasCount];
                    rebuilt[now] = amount;
                }
                for (int g = 0; g < amount.Length && g < old.concentration.Length; g++) amount[g] += old.concentration[g];
            }
        }
        foreach (KeyValuePair<Room, float[]> pair in rebuilt)
        {
            RoomGasState state = StateFor(pair.Key);
            for (int g = 0; g < state.concentration.Length; g++)
            {
                state.concentration[g] += GasMixRule.Rebuilt(pair.Value[g], pair.Key.CellCount);
            }
        }
    }

    void Remove(int roomId)
    {
        if (!states.TryGetValue(roomId, out RoomGasState? state)) return;
        if (cellToRoom != null)
        {
            foreach (int index in state.cells) if (cellToRoom[index] == roomId) cellToRoom[index] = -1;
        }
        states.Remove(roomId);
    }

    /// <summary>A line for the dev overlay and the debug action: every framework gas at a cell's room.</summary>
    public string Describe(IntVec3 cell)
    {
        Room? room = cell.InBounds(map) ? cell.GetRoom(map) : null;
        if (room == null || !states.TryGetValue(room.ID, out RoomGasState? state)) return "";
        var parts = new List<string>();
        List<PhobosGasDef> gases = DefDatabase<PhobosGasDef>.AllDefsListForReading;
        for (int g = 0; g < gases.Count && g < state.concentration.Length; g++)
        {
            if (state.concentration[g] > 0f) parts.Add(gases[g].label + " " + state.concentration[g].ToString("0.00") + " (own)");
        }
        return string.Join(", ", parts);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
        {
            List<SavedCell> saved = Snapshot();
            Scribe_Collections.Look(ref saved, "PhobosFramework_gasCells", LookMode.Deep);
        }
        else if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Collections.Look(ref loaded, "PhobosFramework_gasCells", LookMode.Deep);
        }
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        if (loaded == null) return;
        Restore(loaded);
        loaded = null;
    }

    /// <summary>Cells, not room numbers: rooms are renumbered on every load.</summary>
    List<SavedCell> Snapshot()
    {
        var saved = new List<SavedCell>();
        List<PhobosGasDef> gases = DefDatabase<PhobosGasDef>.AllDefsListForReading;
        foreach (RoomGasState state in states.Values)
        {
            foreach (int index in state.cells)
            {
                var cell = new SavedCell { cellIndex = index };
                for (int g = 0; g < gases.Count && g < state.concentration.Length; g++)
                {
                    if (state.concentration[g] <= 0f) continue;
                    cell.gases.Add(gases[g].defName);
                    cell.amounts.Add(state.concentration[g]);
                }
                if (cell.gases.Count > 0) saved.Add(cell);
            }
        }
        return saved;
    }

    void Restore(List<SavedCell> cells)
    {
        rebuilt.Clear();
        int gasCount = GasCount;
        foreach (SavedCell cell in cells)
        {
            if (cell.cellIndex < 0 || cell.cellIndex >= map.cellIndices.NumGridCells) continue;
            Room? room = map.cellIndices.IndexToCell(cell.cellIndex).GetRoom(map);
            if (!Holds(room)) continue;
            if (!rebuilt.TryGetValue(room!, out float[]? amount))
            {
                amount = new float[gasCount];
                rebuilt[room] = amount;
            }
            for (int i = 0; i < cell.gases.Count && i < cell.amounts.Count; i++)
            {
                PhobosGasDef? gas = DefDatabase<PhobosGasDef>.GetNamedSilentFail(cell.gases[i]);
                if (gas != null && gas.index < amount.Length) amount[gas.index] += cell.amounts[i];
            }
        }
        foreach (KeyValuePair<Room, float[]> pair in rebuilt)
        {
            RoomGasState state = StateFor(pair.Key);
            for (int g = 0; g < state.concentration.Length; g++)
            {
                state.concentration[g] = GasMixRule.Rebuilt(pair.Value[g], pair.Key.CellCount);
            }
        }
    }
}
