using System;
using System.Collections.Generic;
using Verse;

namespace Phobos.Framework.Health;

/// <summary>
/// A per-map list rebuilt at most once per interval, when something asks for it, so work givers and gizmos never scan the map
/// themselves. Derived state: nothing is saved, maps that are gone are forgotten on the next build.
/// </summary>
public sealed class MapPatientCache<T>
{
    sealed class Entry
    {
        public int builtTick = -1;
        public readonly List<T> items = new();
    }

    readonly Dictionary<Map, Entry> entries = new();
    readonly List<Map> stale = new();
    readonly Action<Map, List<T>> rebuild;

    /// <param name="rebuild">Fills the list for a map; it receives the list already cleared.</param>
    public MapPatientCache(Action<Map, List<T>> rebuild)
    {
        this.rebuild = rebuild;
    }

    /// <summary>The map's list, rebuilt when older than <paramref name="intervalTicks"/>.</summary>
    public IReadOnlyList<T> Get(Map map, int intervalTicks)
    {
        if (!entries.TryGetValue(map, out Entry? entry))
        {
            Prune();
            entry = new Entry();
            entries[map] = entry;
        }
        int now = Find.TickManager.TicksGame;
        if (entry.builtTick >= 0 && now - entry.builtTick < intervalTicks) return entry.items;
        entry.builtTick = now;
        entry.items.Clear();
        rebuild(map, entry.items);
        return entry.items;
    }

    /// <summary>The list as last built, without rebuilding; empty for a map never asked about.</summary>
    public IReadOnlyList<T> Peek(Map map) => entries.TryGetValue(map, out Entry? entry) ? entry.items : (IReadOnlyList<T>)System.Array.Empty<T>();

    /// <summary>Forces the next <see cref="Get"/> to rebuild, for a dev action or a setting change.</summary>
    public void Invalidate(Map? map = null)
    {
        if (map == null)
        {
            foreach (Entry entry in entries.Values) entry.builtTick = -1;
        }
        else if (entries.TryGetValue(map, out Entry? entry))
        {
            entry.builtTick = -1;
        }
    }

    void Prune()
    {
        stale.Clear();
        foreach (Map map in entries.Keys)
        {
            if (Find.Maps == null || !Find.Maps.Contains(map)) stale.Add(map);
        }
        for (int i = 0; i < stale.Count; i++) entries.Remove(stale[i]);
    }
}
