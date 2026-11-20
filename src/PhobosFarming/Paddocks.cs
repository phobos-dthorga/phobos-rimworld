using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Farming;

/// <summary>A growing zone's paddock settings. Saved by the map's paddock component.</summary>
public class PaddockState : IExposable
{
    public int zoneId = -1;
    public bool machineWorked;
    public bool handsMayHelp;
    /// <summary>The zone's own "allow sowing" as it was when it was marked, given back when the mark is taken off.</summary>
    public bool sowWasAllowed = true;
    /// <summary>"ploughed" or "cultivated" once a run has worked most of it (farming batch two), for the soil rules to come; empty for neither.</summary>
    public string lastTilled = "";
    /// <summary>When it was last tilled, in ticks; -1 for never.</summary>
    public int lastTilledTick = -1;

    public bool MachineOnly => machineWorked && !handsMayHelp;

    public void ExposeData()
    {
        Scribe_Values.Look(ref zoneId, "zoneId", -1);
        Scribe_Values.Look(ref machineWorked, "machineWorked");
        Scribe_Values.Look(ref handsMayHelp, "handsMayHelp");
        Scribe_Values.Look(ref sowWasAllowed, "sowWasAllowed", true);
        Scribe_Values.Look(ref lastTilled, "lastTilled", "");
        Scribe_Values.Look(ref lastTilledTick, "lastTilledTick", -1);
    }
}

/// <summary>
/// The paddocks on a map (docs/development/farming-batch-one.md, "Paddocks"): growing zones marked for machines, keyed by the zone's ID,
/// which the game keeps across saves. Nothing about the zone itself changes, so blight, raiders and every other mod still see a growing
/// zone. A mark for a zone that is gone is dropped when next read.
/// </summary>
public class MapComponent_PhobosPaddocks : MapComponent
{
    List<PaddockState> states = new();
    readonly Dictionary<int, PaddockState> byZone = new();

    public MapComponent_PhobosPaddocks(Map map) : base(map)
    {
    }

    public PaddockState? For(Zone_Growing zone)
    {
        if (byZone.Count != states.Count) Reindex();
        if (byZone.TryGetValue(zone.ID, out PaddockState? state)) return state;
        // A zone that came here from another map (a gravship carries its growing zones, with their IDs: the fault-read of 21 Nov 2026, F10)
        // brings its mark with it. Asked once per zone and map.
        if (!lookedFor.Add(zone.ID)) return null;
        state = Adopt(zone.ID);
        if (state == null) return null;
        states.Add(state);
        byZone[zone.ID] = state;
        return state;
    }

    readonly HashSet<int> lookedFor = new();

    /// <summary>Takes a zone's mark from the map it left, or from the marks of maps that are gone.</summary>
    PaddockState? Adopt(int zoneId)
    {
        foreach (Map other in Find.Maps)
        {
            if (other == map || other.GetComponent<MapComponent_PhobosPaddocks>() is not { } theirs) continue;
            PaddockState? found = theirs.states.Find(s => s.zoneId == zoneId);
            if (found == null || other.zoneManager.AllZones.Exists(z => z.ID == zoneId)) continue;
            theirs.states.Remove(found);
            theirs.byZone.Remove(zoneId);
            return found;
        }
        return Current.Game?.GetComponent<GameComponent_PhobosPaddocksAdrift>()?.Take(zoneId);
    }

    /// <summary>The map is gone: its marks wait a while for their zones to turn up on another map.</summary>
    public override void MapRemoved()
    {
        base.MapRemoved();
        if (states.Count > 0) Current.Game?.GetComponent<GameComponent_PhobosPaddocksAdrift>()?.Keep(states);
    }

    public PaddockState Ensure(Zone_Growing zone)
    {
        PaddockState? state = For(zone);
        if (state != null) return state;
        state = new PaddockState { zoneId = zone.ID, sowWasAllowed = zone.allowSow };
        states.Add(state);
        byZone[zone.ID] = state;
        return state;
    }

    public void Remove(Zone_Growing zone)
    {
        if (For(zone) is not { } state) return;
        states.Remove(state);
        byZone.Remove(zone.ID);
    }

    void Reindex()
    {
        byZone.Clear();
        foreach (PaddockState state in states) byZone[state.zoneId] = state;
    }

    /// <summary>Drops marks for zones that no longer exist, once a long while: zones are few and this costs nothing between.</summary>
    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % GenDate.TicksPerDay != 0 || states.Count == 0) return;
        var alive = new HashSet<int>();
        foreach (Zone zone in map.zoneManager.AllZones)
        {
            if (zone is Zone_Growing) alive.Add(zone.ID);
        }
        if (states.RemoveAll(s => !alive.Contains(s.zoneId)) > 0) Reindex();
        lookedFor.Clear();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref states, "PhobosPaddocks", LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            states ??= new List<PaddockState>();
            Reindex();
        }
    }
}

/// <summary>
/// The marks of paddocks whose map is gone (a gravship has left its old map behind), kept until their zones are set down on another map or
/// a quadrum has passed. Saved, so a flight that spans a save keeps its paddocks.
/// </summary>
public class GameComponent_PhobosPaddocksAdrift : GameComponent
{
    /// <summary>How long a mark waits for its zone. Authored [A]: longer than any flight.</summary>
    const int KeepTicks = GenDate.TicksPerQuadrum;

    List<PaddockState> adrift = new();
    int keptTick = -1;

    public GameComponent_PhobosPaddocksAdrift(Game game)
    {
    }

    public void Keep(List<PaddockState> states)
    {
        adrift.AddRange(states);
        keptTick = Find.TickManager.TicksGame;
    }

    public PaddockState? Take(int zoneId)
    {
        if (adrift.Count == 0) return null;
        if (Find.TickManager.TicksGame - keptTick > KeepTicks)
        {
            adrift.Clear();
            return null;
        }
        PaddockState? found = adrift.Find(s => s.zoneId == zoneId);
        if (found != null) adrift.Remove(found);
        return found;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode != LoadSaveMode.Saving || adrift.Count > 0)
        {
            Scribe_Collections.Look(ref adrift, "PhobosPaddocksAdrift", LookMode.Deep);
            Scribe_Values.Look(ref keptTick, "PhobosPaddocksAdriftTick", -1);
        }
        if (Scribe.mode == LoadSaveMode.PostLoadInit) adrift ??= new List<PaddockState>();
    }
}

/// <summary>The paddock service: marking a growing zone for machines and letting hands help (UI calls these; they decide).</summary>
public static class Paddocks
{
    // The last map asked about and its paddocks, so a colonist's harvest scan, which asks for every growing zone, does not search the map's
    // components each time (the fault-read of 20 Nov 2026). A different map, or one the game has dropped, is looked up afresh.
    static Map? cachedMap;
    static MapComponent_PhobosPaddocks? cachedPaddocks;

    static MapComponent_PhobosPaddocks? Of(Map? map)
    {
        if (map == null) return null;
        if (map != cachedMap || cachedPaddocks == null)
        {
            cachedMap = map;
            cachedPaddocks = map.GetComponent<MapComponent_PhobosPaddocks>();
        }
        return cachedPaddocks;
    }

    public static PaddockState? Get(Zone_Growing zone) => Of(zone.Map)?.For(zone);

    /// <summary>Whether this zone is a paddock (marked for machines).</summary>
    public static bool IsPaddock(Zone? zone) => zone is Zone_Growing growing && Get(growing) is { machineWorked: true };

    /// <summary>Whether hands keep off this zone: marked for machines and hands may not help.</summary>
    public static bool MachineOnly(Zone_Growing zone) => Get(zone) is { MachineOnly: true };

    /// <summary>Marks a growing zone for machines, or takes the mark off, and says so.</summary>
    public static void SetMachineWorked(Zone_Growing zone, bool on)
    {
        MapComponent_PhobosPaddocks? paddocks = Of(zone.Map);
        if (paddocks == null) return;
        if (on)
        {
            PaddockState state = paddocks.Ensure(zone);
            state.machineWorked = true;
            state.handsMayHelp = FarmingMod.Settings.handsMayHelpOnNew;
            Apply(zone, state);
            Messages.Message("PhobosFarming_PaddockMarked".Translate(zone.label.Named("ZONE")).Resolve(), MessageTypeDefOf.SilentInput, historical: false);
            if (Seen.Wants("farming.paddock.marked"))
            {
                Seen.Note("farming.paddock.marked", null, zone.label, values: new List<KeyValuePair<string, double>> { new("cells", zone.Cells.Count) });
            }
            return;
        }
        if (paddocks.For(zone) is { } old) zone.allowSow = old.sowWasAllowed;
        paddocks.Remove(zone);
        Messages.Message("PhobosFarming_PaddockUnmarked".Translate(zone.label.Named("ZONE")).Resolve(), MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Lets colonists sow and harvest a paddock by hand too, or keeps them off it, and says so.</summary>
    public static void SetHandsMayHelp(Zone_Growing zone, bool on)
    {
        if (Get(zone) is not { machineWorked: true } state) return;
        state.handsMayHelp = on;
        Apply(zone, state);
        Messages.Message((on ? "PhobosFarming_HandsMayHelpOn" : "PhobosFarming_HandsMayHelpOff").Translate(zone.label.Named("ZONE")).Resolve(),
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Hand sowing follows the mark: off while machines only, on while hands may help (the game's own toggle is back then).</summary>
    static void Apply(Zone_Growing zone, PaddockState state) => zone.allowSow = !state.MachineOnly;

    // ---- The buttons on a growing zone (presentation; each delegates to the calls above) ----

    static Texture2D? machineIcon;
    static Texture2D MachineIcon => machineIcon ??= ContentFinder<Texture2D>.Get("Phobos/Farming/UI/Paddock", reportFailure: false)
        ?? ContentFinder<Texture2D>.Get("UI/Designators/ZoneCreate_Growing", reportFailure: false) ?? BaseContent.WhiteTex;

    public static IEnumerable<Gizmo> Gizmos(Zone_Growing zone)
    {
        PaddockState? state = Get(zone);
        bool marked = state is { machineWorked: true };
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosFarming_GizmoMachineWorked".Translate().Resolve(),
            defaultDesc = "PhobosFarming_GizmoMachineWorkedDesc".Translate().Resolve(),
            icon = MachineIcon,
            isActive = () => IsPaddock(zone),
            toggleAction = () => SetMachineWorked(zone, !IsPaddock(zone)),
        };
        if (!marked) yield break;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosFarming_GizmoHandsMayHelp".Translate().Resolve(),
            defaultDesc = "PhobosFarming_GizmoHandsMayHelpDesc".Translate().Resolve(),
            icon = TexCommand.ForbidOff,
            isActive = () => Get(zone) is { handsMayHelp: true },
            toggleAction = () => SetHandsMayHelp(zone, !(Get(zone)?.handsMayHelp ?? false)),
        };
    }
}
