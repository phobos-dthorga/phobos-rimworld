using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Heat;

/// <summary>
/// The hot zones around fires: for every cell near a fire, how many degrees hotter than the air the game gives it (the weather in
/// the open, the room's temperature indoors) it is. In a room only the share the game's even spread misses is added, so a small
/// room counts no heat twice. Rebuilt from the map's fires on the game's own fire cadence (every 150 ticks by default), read by
/// <see cref="FireHeatPatch"/> on every cell-temperature lookup, so the read is one dictionary lookup and the empty case one count.
/// Derived state: nothing is saved. Cells inside walls have no air and are skipped.
/// </summary>
public sealed class MapComponent_PhobosFireHeat : MapComponent
{
    static readonly Dictionary<Map, MapComponent_PhobosFireHeat> byMap = new();
    /// <summary>Cell index to degrees above the outdoor air, uncapped; the cap is applied on read.</summary>
    readonly Dictionary<int, float> rise = new();
    /// <summary>Each room's share during one rebuild; cleared after, so no room is held between rebuilds.</summary>
    static readonly Dictionary<Room, float> shareByRoom = new();

    public MapComponent_PhobosFireHeat(Map map) : base(map)
    {
        byMap[map] = this;
    }

    public override void MapRemoved()
    {
        byMap.Remove(map);
    }

    /// <summary>Hot zones in the open: the setting is on and no other mod models the air cell by cell.</summary>
    public static bool EnabledOutdoors => BurnsAndSmokeMod.Settings.fireHeatOutdoors && !BurnsAndSmokeMod.PerCellTemperatureModActive;
    /// <summary>Hot zones in rooms: the setting is on and no other mod models the air cell by cell.</summary>
    public static bool EnabledIndoors => BurnsAndSmokeMod.Settings.fireHeatIndoors && !BurnsAndSmokeMod.PerCellTemperatureModActive;
    /// <summary>Whether hot zones are computed at all.</summary>
    public static bool Enabled => EnabledOutdoors || EnabledIndoors;

    public static MapComponent_PhobosFireHeat? For(Map map) => byMap.TryGetValue(map, out MapComponent_PhobosFireHeat? comp) ? comp : null;

    /// <summary>How many cells are hotter than the air right now.</summary>
    public int HotCellCount => rise.Count;

    /// <summary>The degrees above the game's air at a cell, capped; false when the cell is not in a hot zone.</summary>
    public static bool TryGetRise(Map? map, IntVec3 cell, out float degrees)
    {
        degrees = 0f;
        if (map == null || !byMap.TryGetValue(map, out MapComponent_PhobosFireHeat? comp) || comp.rise.Count == 0) return false;
        if (!comp.rise.TryGetValue(map.cellIndices.CellToIndex(cell), out float total)) return false;
        degrees = FireHeatRule.Cap(total, PhobosBurnRulesDef.Current.fireHeatMaxRise);
        return true;
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % PhobosBurnRulesDef.Current.fireHeatIntervalTicks != 0) return;
        Recompute();
    }

    /// <summary>Rebuilds the zones from the fires on the map. Public for the dev action.</summary>
    public void Recompute()
    {
        rise.Clear();
        if (!Enabled) return;
        List<Thing> fires = map.listerThings.ThingsOfDef(ThingDefOf.Fire);
        if (fires.Count == 0) return;
        PhobosBurnRulesDef rules = PhobosBurnRulesDef.Current;
        int cellsInRadius = GenRadial.NumCellsInRadius(rules.fireHeatRadiusCells);
        CellIndices indices = map.cellIndices;
        bool outdoors = EnabledOutdoors;
        bool indoors = EnabledIndoors;
        for (int i = 0; i < fires.Count; i++)
        {
            if (fires[i] is not Fire fire || !fire.Spawned) continue;
            IntVec3 origin = fire.Position;
            for (int j = 0; j < cellsInRadius; j++)
            {
                IntVec3 cell = origin + GenRadial.RadialPattern[j];
                if (!cell.InBounds(map)) continue;
                float share = ShareAt(cell, rules, outdoors, indoors);
                if (share <= 0f) continue;
                float degrees = share * FireHeatRule.Rise(fire.fireSize, rules.fullFireSize, (cell - origin).LengthHorizontal, rules.fireHeatPeakRise, rules.fireHeatRadiusCells);
                if (degrees <= 0f) continue;
                int index = indices.CellToIndex(cell);
                rise[index] = rise.TryGetValue(index, out float total) ? total + degrees : degrees;
            }
        }
        shareByRoom.Clear();
    }

    /// <summary>
    /// How much of a fire's local heat a cell gets: all of it in the open, the share the game's even spread misses in a room, nothing
    /// inside a wall (no room) or where that kind of zone is switched off.
    /// </summary>
    float ShareAt(IntVec3 cell, PhobosBurnRulesDef rules, bool outdoors, bool indoors)
    {
        Room? room = cell.GetRoom(map);
        if (room == null) return 0f;
        if (shareByRoom.TryGetValue(room, out float share)) return share;
        if (room.UsesOutdoorTemperature) share = outdoors ? 1f : 0f;
        else share = indoors ? FireHeatRule.IndoorShare(room.CellCount, rules.fireHeatRoomCellsWarmedEvenly) : 0f;
        shareByRoom[room] = share;
        return share;
    }
}
