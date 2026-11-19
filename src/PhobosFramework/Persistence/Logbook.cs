using System.Collections.Generic;
using Phobos.Framework.Rules;
using Phobos.Framework.WorldMap;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Framework.Persistence;

/// <summary>How a logbook entry's figure reads.</summary>
public enum LogValueKind
{
    None,
    Count,
    Kilometres,
    Hours,
    Percent,
}

/// <summary>
/// A kind of logbook entry (Framework 0.181.0; docs/development/vehicle-emergent-play.md, EM23). Its words live in XML, so translators can
/// override them and other mods can add kinds. <c>text</c> takes the named placeholders {WHO}, {SUBJECT}, {PLACE}, {VALUE} and {DETAIL};
/// an entry fills only those it has.
/// </summary>
public class PhobosLogEntryDef : Def
{
    public string text = "";
    public LogValueKind value = LogValueKind.None;
    /// <summary>Something went wrong: the entry is shown in a warning colour.</summary>
    public bool bad;
}

/// <summary>One entry: its kind, when and where, and the names and figure it was written with. Names are kept as written at the time, as
/// the game's own letters keep their text: a colonist who has since died or been renamed is remembered as they were.</summary>
public sealed class PhobosLogEntry : IExposable
{
    public PhobosLogEntryDef? def;
    public int tick;
    public PlanetTile tile = PlanetTile.Invalid;
    public string who = "";
    public string subject = "";
    public string detail = "";
    public float value;

    /// <summary>The entry in words, from its kind's text.</summary>
    public string Text()
    {
        if (def == null) return "";
        return def.text.Formatted(who.Named("WHO"), subject.Named("SUBJECT"), WorldPlaces.Describe(tile).Named("PLACE"), ValueText().Named("VALUE"),
            detail.Named("DETAIL")).Resolve();
    }

    string ValueText() => def?.value switch
    {
        LogValueKind.Count => value.ToString("0"),
        LogValueKind.Kilometres => "PhobosPlace_Kilometres".Translate(value.ToString("0").Named("KM")).Resolve(),
        LogValueKind.Hours => "PhobosLogbook_Hours".Translate(value.ToString("0.#").Named("HOURS")).Resolve(),
        LogValueKind.Percent => value.ToStringPercent(),
        _ => "",
    };

    public void ExposeData()
    {
        Scribe_Defs.Look(ref def, "def");
        Scribe_Values.Look(ref tick, "tick");
        Scribe_Values.Look(ref tile, "tile", PlanetTile.Invalid);
        Scribe_Values.Look(ref who, "who", "");
        Scribe_Values.Look(ref subject, "subject", "");
        Scribe_Values.Look(ref detail, "detail", "");
        Scribe_Values.Look(ref value, "value");
    }
}

/// <summary>
/// A lasting record of what happened to a thing (Framework 0.181.0, EM23): unlike the graphs' marks, which keep 15 days, it keeps the
/// thing's whole life up to <see cref="LogbookRule.DefaultCap"/> entries, oldest going first. Any comp can hold one (<see cref="ILogbookHolder"/>),
/// and the logbook tab shows it. Saved with its holder; adding is a list append on an event that already happens, never per tick.
/// </summary>
public sealed class Logbook
{
    List<PhobosLogEntry> entries = new();

    public IReadOnlyList<PhobosLogEntry> Entries => entries;

    /// <summary>Rises at every change, so a tab can tell its rows went stale.</summary>
    public int Version { get; private set; }

    public PhobosLogEntry? Add(PhobosLogEntryDef? def, PlanetTile tile, string who = "", string subject = "", string detail = "", float value = 0f)
    {
        if (def == null || Current.Game == null) return null;
        var entry = new PhobosLogEntry { def = def, tick = Find.TickManager.TicksGame, tile = tile, who = who ?? "", subject = subject ?? "", detail = detail ?? "", value = value };
        entries.Add(entry);
        int drop = LogbookRule.DropCount(entries.Count, LogbookRule.DefaultCap);
        if (drop > 0) entries.RemoveRange(0, drop);
        Version++;
        return entry;
    }

    public void Clear()
    {
        entries.Clear();
        Version++;
    }

    public void ExposeData(string key)
    {
        Scribe_Collections.Look(ref entries, key, LookMode.Deep);
        if (Scribe.mode != LoadSaveMode.PostLoadInit) return;
        entries ??= new List<PhobosLogEntry>();
        // An entry whose kind no longer exists (a mod removed) is dropped quietly.
        entries.RemoveAll(e => e?.def == null);
        Version++;
    }
}

/// <summary>A comp that keeps a logbook, for the logbook tab to find.</summary>
public interface ILogbookHolder
{
    /// <summary>The logbook, or null where this thing keeps none (another faction's vehicle).</summary>
    Logbook? Logbook { get; }
}
