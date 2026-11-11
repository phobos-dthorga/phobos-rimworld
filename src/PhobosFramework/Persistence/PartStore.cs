using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>
/// Saved data and timers for parts of our mods that load only with another mod (owner, 11 Nov 2026; docs/development/code-audit-2026-11-11.md).
/// The game writes every game and world component into the save by its class name, and a class that is gone at the next load (its
/// assembly no longer loads because the other mod was removed) costs a red "Could not find class" error on every load of that save.
/// This component lives in the core, which always loads, so a save never names a class that can vanish. A part keeps its data in a
/// record here under its own key, and registers its timer with <see cref="Every"/> at start-up instead of owning a component.
/// A record left by a part whose mod was removed is kept untouched, so the part finds it again if the mod comes back.
/// </summary>
public sealed class GameComponent_PhobosPartStore : GameComponent
{
    Dictionary<string, PartRecord> records = new();
    List<string>? keysWorking;
    List<PartRecord>? valuesWorking;

    static readonly List<Timer> Timers = new();

    public GameComponent_PhobosPartStore(Game game) { }

    /// <summary>The store of the game in play, or null before a game exists.</summary>
    public static GameComponent_PhobosPartStore? Store => Current.Game?.GetComponent<GameComponent_PhobosPartStore>();

    /// <summary>The record for a key, made on first use. Null before a game exists.</summary>
    public static PartRecord? Record(string key) => Store?.RecordFor(key);

    public PartRecord RecordFor(string key)
    {
        if (!records.TryGetValue(key, out PartRecord? record))
        {
            record = new PartRecord();
            records[key] = record;
        }
        return record;
    }

    /// <summary>Runs an action every so many ticks while a game is played. Call once at start-up; never saved.</summary>
    public static void Every(int ticks, string owner, Action action) => Timers.Add(new Timer(Math.Max(1, ticks), owner, action));

    public override void GameComponentTick()
    {
        if (Timers.Count == 0) return;
        int now = Find.TickManager.TicksGame;
        for (int i = 0; i < Timers.Count; i++)
        {
            Timer timer = Timers[i];
            if (now % timer.Ticks != 0) continue;
            try
            {
                timer.Action();
            }
            catch (Exception error)
            {
                // One failing part must not stop the others, nor fill the log: it is told once and then left out.
                FrameworkMod.Logger.Error(timer.Owner + "'s timer failed and is switched off for this session: " + error);
                Timers.RemoveAt(i);
                i--;
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref records, "PhobosPartStore_Records", LookMode.Value, LookMode.Deep, ref keysWorking, ref valuesWorking);
        records ??= new Dictionary<string, PartRecord>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            var empty = new List<string>();
            foreach (KeyValuePair<string, PartRecord> pair in records)
            {
                if (pair.Value == null) empty.Add(pair.Key);
            }
            foreach (string key in empty) records.Remove(key);
        }
    }

    sealed class Timer
    {
        public readonly int Ticks;
        public readonly string Owner;
        public readonly Action Action;

        public Timer(int ticks, string owner, Action action)
        {
            Ticks = ticks;
            Owner = owner;
            Action = action;
        }
    }
}

/// <summary>One part's saved data: named lists of whole numbers and of text. A reference to a world object is kept as its ID, so the
/// record never names a class either.</summary>
public sealed class PartRecord : IExposable
{
    List<NamedInts> ints = new();
    List<NamedStrings> strings = new();

    /// <summary>The list under a name, made empty on first use. The list itself is returned: change it in place.</summary>
    public List<int> Ints(string name)
    {
        foreach (NamedInts entry in ints)
        {
            if (entry.name == name) return entry.values;
        }
        var made = new NamedInts { name = name };
        ints.Add(made);
        return made.values;
    }

    /// <summary>The list under a name, made empty on first use. The list itself is returned: change it in place.</summary>
    public List<string> Strings(string name)
    {
        foreach (NamedStrings entry in strings)
        {
            if (entry.name == name) return entry.values;
        }
        var made = new NamedStrings { name = name };
        strings.Add(made);
        return made.values;
    }

    public void ExposeData()
    {
        Scribe_Collections.Look(ref ints, "ints", LookMode.Deep);
        Scribe_Collections.Look(ref strings, "strings", LookMode.Deep);
        ints ??= new List<NamedInts>();
        strings ??= new List<NamedStrings>();
        if (Scribe.mode == LoadSaveMode.Saving) return;
        ints.RemoveAll(entry => entry == null || entry.name.NullOrEmpty());
        strings.RemoveAll(entry => entry == null || entry.name.NullOrEmpty());
    }

    sealed class NamedInts : IExposable
    {
        public string name = "";
        public List<int> values = new();

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "");
            Scribe_Collections.Look(ref values, "values", LookMode.Value);
            values ??= new List<int>();
        }
    }

    sealed class NamedStrings : IExposable
    {
        public string name = "";
        public List<string> values = new();

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name", "");
            Scribe_Collections.Look(ref values, "values", LookMode.Value);
            values ??= new List<string>();
        }
    }
}

/// <summary>
/// Takes a component of ours out of the game once loading has finished, so the next save no longer names its class. For a component
/// that used to hold a part's data: it is kept in its assembly so a save that still holds it loads cleanly and can hand its data to
/// the <see cref="GameComponent_PhobosPartStore"/>, then it retires. The game makes one again at every load and every new game, and
/// it retires again each time, before the player can save.
/// </summary>
public static class RetiredComponents
{
    public static void Retire(GameComponent component) =>
        LongEventHandler.ExecuteWhenFinished(() => Current.Game?.components.Remove(component));

    public static void Retire(WorldComponent component) =>
        LongEventHandler.ExecuteWhenFinished(() => Find.World?.components.Remove(component));
}
