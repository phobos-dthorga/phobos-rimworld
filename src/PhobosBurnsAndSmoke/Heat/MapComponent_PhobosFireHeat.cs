using System.Collections.Generic;
using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Rules;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Heat;

/// <summary>
/// The hot zones around fires in the open: for every cell near a fire whose air is the outdoor air, how many degrees hotter than
/// that air it is. Rebuilt from the map's fires on the game's own fire cadence (every 150 ticks by default), read by
/// <see cref="FireHeatPatch"/> on every cell-temperature lookup, so the read is one dictionary lookup and the empty case one count.
/// Derived state: nothing is saved. Rooms keep the game's own heating; cells inside walls have no air and are skipped.
/// </summary>
public sealed class MapComponent_PhobosFireHeat : MapComponent
{
    static readonly Dictionary<Map, MapComponent_PhobosFireHeat> byMap = new();
    /// <summary>Cell index to degrees above the outdoor air, uncapped; the cap is applied on read.</summary>
    readonly Dictionary<int, float> rise = new();

    public MapComponent_PhobosFireHeat(Map map) : base(map)
    {
        byMap[map] = this;
    }

    public override void MapRemoved()
    {
        byMap.Remove(map);
    }

    /// <summary>Whether hot zones are computed at all: the setting is on and no other mod models the air cell by cell.</summary>
    public static bool Enabled => BurnsAndSmokeMod.Settings.fireHeatOutdoors && !BurnsAndSmokeMod.PerCellTemperatureModActive;

    public static MapComponent_PhobosFireHeat? For(Map map) => byMap.TryGetValue(map, out MapComponent_PhobosFireHeat? comp) ? comp : null;

    /// <summary>How many cells are hotter than the air right now.</summary>
    public int HotCellCount => rise.Count;

    /// <summary>The degrees above the outdoor air at a cell, capped; false when the cell is not near a fire in the open.</summary>
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
        for (int i = 0; i < fires.Count; i++)
        {
            if (fires[i] is not Fire fire || !fire.Spawned) continue;
            IntVec3 origin = fire.Position;
            for (int j = 0; j < cellsInRadius; j++)
            {
                IntVec3 cell = origin + GenRadial.RadialPattern[j];
                if (!cell.InBounds(map) || !UsesOutdoorAir(cell)) continue;
                float degrees = FireHeatRule.Rise(fire.fireSize, rules.fullFireSize, (cell - origin).LengthHorizontal, rules.fireHeatPeakRise, rules.fireHeatRadiusCells);
                if (degrees <= 0f) continue;
                int index = indices.CellToIndex(cell);
                rise[index] = rise.TryGetValue(index, out float total) ? total + degrees : degrees;
            }
        }
    }

    /// <summary>A cell whose air is the outdoor air: in a room the game treats as outdoors. A cell inside a wall has no room.</summary>
    bool UsesOutdoorAir(IntVec3 cell)
    {
        Room? room = cell.GetRoom(map);
        return room != null && room.UsesOutdoorTemperature;
    }
}
