using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What happened to a vehicle that its graphs mark (Framework 0.84.0).</summary>
public enum VehicleEventKind
{
    /// <summary>A reading entered a stage worth a look (boiling, flat, low).</summary>
    Stage,
    /// <summary>A system was serviced.</summary>
    Service,
    /// <summary>A part took a fault it did not have.</summary>
    Fault,
    /// <summary>A flat battery was jump-started.</summary>
    JumpStart,
}

/// <summary>One event: what, when, and which system; a stage names its quantity and stage, a fault its condition. Names, not text, so the
/// marks read in the player's language whenever they are shown.</summary>
public sealed class VehicleEvent : IExposable
{
    public VehicleEventKind kind;
    public int tick;
    /// <summary>The system's defName.</summary>
    public string system = "";
    /// <summary>A stage's quantity name, or a fault's condition defName; empty otherwise.</summary>
    public string detail = "";
    /// <summary>A stage's index; -1 otherwise.</summary>
    public int stage = -1;

    public bool Same(VehicleEvent other) => kind == other.kind && system == other.system && detail == other.detail && stage == other.stage;

    public void ExposeData()
    {
        Scribe_Values.Look(ref kind, "kind");
        Scribe_Values.Look(ref tick, "tick");
        Scribe_Values.Look(ref system, "system", "");
        Scribe_Values.Look(ref detail, "detail", "");
        Scribe_Values.Look(ref stage, "stage", -1);
    }
}

/// <summary>
/// The events a vehicle's graphs mark (Framework 0.84.0; docs/development/vehicle-presentation-design.md, G3): stages worth a look entered,
/// services, new faults and jump-starts, kept for the longest graph's span and no more than a few dozen. The same event within an hour is
/// one mark (EventMarkRule). Saved with the vehicle; recording is a list append on an event that already happens, never per tick.
/// </summary>
public sealed class VehicleEventLog : IExposable
{
    /// <summary>As long as the longest graph that shows marks (the wear trend's fifteen days).</summary>
    public const int MaxAgeTicks = 15 * GenDate.TicksPerDay;
    /// <summary>Authored: enough for a busy fortnight, small in a save.</summary>
    public const int MaxCount = 64;
    /// <summary>Authored: the same event again within an hour is one mark.</summary>
    public const int SameGapTicks = GenDate.TicksPerHour;

    List<VehicleEvent> events = new();

    public IReadOnlyList<VehicleEvent> Events => events;

    public void Record(VehicleEventKind kind, string system, string detail = "", int stage = -1)
    {
        int now = Find.TickManager.TicksGame;
        var added = new VehicleEvent { kind = kind, tick = now, system = system, detail = detail, stage = stage };
        int? lastSame = null;
        for (int i = events.Count - 1; i >= 0; i--)
        {
            if (events[i].Same(added)) { lastSame = events[i].tick; break; }
        }
        if (EventMarkRule.TooSoon(lastSame, now, SameGapTicks)) return;
        events.Add(added);
        var ticks = new List<int>(events.Count);
        foreach (VehicleEvent e in events) ticks.Add(e.tick);
        int drop = EventMarkRule.DropCount(ticks, now, MaxAgeTicks, MaxCount);
        if (drop > 0) events.RemoveRange(0, drop);
    }

    public void Clear() => events.Clear();

    public void ExposeData()
    {
        Scribe_Collections.Look(ref events, "PhobosVehicleSystems_Events", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) events ??= new List<VehicleEvent>();
    }
}
