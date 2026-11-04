using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Verse;

namespace Phobos.Dosing.Services;

/// <summary>
/// The patients on a map who need a dose, rebuilt at most once per authored interval when a doctor asks, so the work giver
/// never scans. Nothing is saved: the cache is a static per-map list with a tick stamp.
/// </summary>
public static class PatientCache
{
    sealed class Entry
    {
        public int builtTick = -1;
        public readonly List<Pawn> patients = new();
    }

    static readonly Dictionary<Map, Entry> entries = new();
    static readonly List<Map> stale = new();

    public static IReadOnlyList<Pawn> Patients(Map map)
    {
        PhobosDosingRulesDef rules = PhobosDosingRulesDef.Current;
        if (!entries.TryGetValue(map, out Entry? entry))
        {
            Prune();
            entry = new Entry();
            entries[map] = entry;
        }
        int now = Find.TickManager.TicksGame;
        if (entry.builtTick >= 0 && now - entry.builtTick < rules.recomputeIntervalTicks) return entry.patients;
        entry.builtTick = now;
        entry.patients.Clear();
        DosingSettings settings = DosingMod.Settings;
        if (!settings.enabled) return entry.patients;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (DoseService.IsPatient(pawns[i], settings, rules)) entry.patients.Add(pawns[i]);
        }
        return entry.patients;
    }

    static void Prune()
    {
        stale.Clear();
        foreach (Map map in entries.Keys)
        {
            if (Find.Maps == null || !Find.Maps.Contains(map)) stale.Add(map);
        }
        for (int i = 0; i < stale.Count; i++) entries.Remove(stale[i]);
    }
}
