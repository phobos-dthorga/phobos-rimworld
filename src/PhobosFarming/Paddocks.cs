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
        return byZone.TryGetValue(zone.ID, out PaddockState? state) ? state : null;
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
