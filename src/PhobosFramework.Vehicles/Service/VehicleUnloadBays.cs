using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The "Vehicles may unload here" switch (Framework 0.175.0; docs/development/vehicle-gathering-runs.md, "Marked bays and Deliver"): on
/// every storage building, through <see cref="CompPhobosVehicleLoadOut"/>, and on every stockpile zone, through
/// <see cref="MapComponent_PhobosUnloadZones"/>. A truck unloading by itself (a gathering run set to unload at any marked bay, or Deliver)
/// makes for the nearest marked place of the player's that takes its load and has room. This decides what is marked and changes it.
/// </summary>
public static class VehicleUnloadBays
{
    /// <summary>The tip's own button picture, reused: the switch says where a truck may tip.</summary>
    public const string IconPath = "Phobos/VehicleSystems/Stations/Tip";

    /// <summary>
    /// Whether a storage type starts marked: storage built for vehicles to tip into, which says which side they back up to (the bulk bunker).
    /// Owner's choice of 19 Nov 2026: the bunker on, everything else off.
    /// </summary>
    public static bool StartsOn(ThingDef def) => def.HasModExtension<PhobosBackUpSideExtension>();

    public static bool IsMarked(Building_Storage building) => building.GetComp<CompPhobosVehicleLoadOut>()?.VehiclesMayUnload ?? false;

    public static bool IsMarked(Zone_Stockpile zone) => zone.Map?.GetComponent<MapComponent_PhobosUnloadZones>()?.Marked(zone) ?? false;

    /// <summary>Turns a building's switch on or off and says so.</summary>
    public static void Set(CompPhobosVehicleLoadOut comp, bool on)
    {
        comp.VehiclesMayUnload = on;
        Said(on, comp.parent.LabelShort, comp.parent);
    }

    /// <summary>Turns a stockpile zone's switch on or off and says so.</summary>
    public static void Set(Zone_Stockpile zone, bool on)
    {
        if (zone.Map?.GetComponent<MapComponent_PhobosUnloadZones>() is not { } marks) return;
        marks.Set(zone, on);
        Said(on, zone.label, null);
        if (on && Seen.Wants("vehicles.unload-zone-switch")) Seen.Note("vehicles.unload-zone-switch", null, zone.label);
    }

    static void Said(bool on, string label, Thing? subject)
    {
        string text = (on ? "PhobosVehicleSystems_MayUnloadOn" : "PhobosVehicleSystems_MayUnloadOff").Translate(label.Named("STORAGE")).Resolve();
        if (subject != null) Messages.Message(text, subject, MessageTypeDefOf.SilentInput, historical: false);
        else Messages.Message(text, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Every place of the player's on this map marked for vehicles to unload at: storage buildings, then stockpile zones.</summary>
    public static List<UnloadPlace> MarkedPlaces(Map map)
    {
        var places = new List<UnloadPlace>();
        foreach (Building_Storage store in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
        {
            if (store.Faction == Faction.OfPlayer && IsMarked(store)) places.Add(new UnloadPlace(store, null));
        }
        if (map.GetComponent<MapComponent_PhobosUnloadZones>() is { } marks)
        {
            foreach (Zone_Stockpile zone in marks.Live()) places.Add(new UnloadPlace(null, zone));
        }
        return places;
    }

    /// <summary>Whether anything on this map is marked, without listing them.</summary>
    public static bool AnyMarked(Map map)
    {
        if (map.GetComponent<MapComponent_PhobosUnloadZones>() is { } marks && marks.Live().Any()) return true;
        foreach (Building_Storage store in map.listerBuildings.AllBuildingsColonistOfClass<Building_Storage>())
        {
            if (store.Faction == Faction.OfPlayer && IsMarked(store)) return true;
        }
        return false;
    }

    static HashSet<ThingDef>? spareParts;

    /// <summary>
    /// Whether this is a vehicle part, ours or a substitute from another mod, from the part tables (PhobosVehiclePartSupplyDef): a delivery
    /// leaves these aboard, as the owner chose (19 Nov 2026). Read once, after the Defs have loaded.
    /// </summary>
    public static bool IsSparePart(ThingDef def)
    {
        if (spareParts == null)
        {
            spareParts = new HashSet<ThingDef>();
            foreach (PhobosVehiclePartSupplyDef supply in DefDatabase<PhobosVehiclePartSupplyDef>.AllDefsListForReading)
            {
                if (supply.item != null) spareParts.Add(supply.item);
                foreach (Phobos.Framework.Bills.RoleSubstitute substitute in supply.substitutes)
                {
                    if (substitute.thing != null) spareParts.Add(substitute.thing);
                }
            }
        }
        return spareParts.Contains(def);
    }
}

/// <summary>A place a vehicle may unload at: one storage building or one stockpile zone.</summary>
public readonly struct UnloadPlace
{
    public readonly Building_Storage? Building;
    public readonly Zone_Stockpile? Zone;

    public UnloadPlace(Building_Storage? building, Zone_Stockpile? zone)
    {
        Building = building;
        Zone = zone;
    }

    public string Label => Building?.LabelShort ?? Zone?.label ?? "";

    /// <summary>Its cells, to back up to and to tip into.</summary>
    public List<IntVec3> Cells => Building != null ? Building.OccupiedRect().Cells.ToList() : Zone!.Cells;

    /// <summary>Whether its storage settings take this thing.</summary>
    public bool Accepts(Thing thing) => Building != null ? Building.Accepts(thing) : Zone!.GetStoreSettings().AllowedToAccept(thing);

    /// <summary>Whether one of its cells has room for this thing now, by the game's own storage rule.</summary>
    public bool HasRoomFor(Thing thing, Map map)
    {
        foreach (IntVec3 cell in Cells)
        {
            if (cell.IsValidStorageFor(map, thing)) return true;
        }
        return false;
    }

    /// <summary>The one way to face when backing up to it, for a building that says which side it opens on (the bulk bunker); else any.</summary>
    public Rot4? BackUpFacing()
    {
        if (Building?.def.GetModExtension<PhobosBackUpSideExtension>() is not { } side) return null;
        int? facing = Phobos.Framework.Rules.GatherRule.BackUpFacing(side.side, Building.Rotation.AsInt);
        return facing is { } rot ? new Rot4(rot) : null;
    }

    /// <summary>How far it is from a cell, squared: a building from its centre, a zone from its nearest cell.</summary>
    public int DistanceSquaredFrom(IntVec3 from)
    {
        if (Building != null) return Building.Position.DistanceToSquared(from);
        int best = int.MaxValue;
        foreach (IntVec3 cell in Zone!.Cells)
        {
            int d = cell.DistanceToSquared(from);
            if (d < best) best = d;
        }
        return best;
    }

    /// <summary>Whether it is still there, on this map, the player's, and still marked.</summary>
    public bool StillMarkedOn(Map? map)
    {
        if (map == null) return false;
        if (Building != null) return Building.Spawned && Building.Map == map && Building.Faction == Faction.OfPlayer && VehicleUnloadBays.IsMarked(Building);
        return Zone != null && Zone.Map == map && map.zoneManager.AllZones.Contains(Zone) && Zone.Cells.Count > 0 && VehicleUnloadBays.IsMarked(Zone);
    }

    public bool Same(UnloadPlace other) => ReferenceEquals(Building, other.Building) && ReferenceEquals(Zone, other.Zone);
}

/// <summary>
/// Which stockpile zones on a map are marked "Vehicles may unload here" (Framework 0.175.0). Zones carry no comps, so the marks are kept
/// here, saved by reference; a zone deleted since is dropped. Off for every zone to start.
/// </summary>
public sealed class MapComponent_PhobosUnloadZones : MapComponent
{
    List<Zone_Stockpile> zones = new();

    public MapComponent_PhobosUnloadZones(Map map) : base(map) { }

    public bool Marked(Zone_Stockpile zone) => zones.Contains(zone);

    public void Set(Zone_Stockpile zone, bool on)
    {
        if (on && !zones.Contains(zone)) zones.Add(zone);
        else if (!on) zones.Remove(zone);
    }

    /// <summary>The marked zones still on the map with cells, dropping any deleted since.</summary>
    public IEnumerable<Zone_Stockpile> Live()
    {
        zones.RemoveAll(zone => zone == null || !map.zoneManager.AllZones.Contains(zone));
        return zones.Where(zone => zone.Cells.Count > 0);
    }

    public override void ExposeData()
    {
        // A deleted zone is never saved: its reference would not load.
        if (Scribe.mode == LoadSaveMode.Saving) zones.RemoveAll(zone => zone == null || !map.zoneManager.AllZones.Contains(zone));
        Scribe_Collections.Look(ref zones, "PhobosVehicleUnloadZones", LookMode.Reference);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            zones ??= new List<Zone_Stockpile>();
            zones.RemoveAll(zone => zone == null);
        }
    }
}

/// <summary>
/// Adds "Vehicles may unload here" to a stockpile zone's buttons (owner's choice, 19 Nov 2026: zones as well as storage buildings). Zones
/// carry no comps and the game has no other way to add a button to one, so a postfix it is; it passes the game's buttons through unchanged
/// and adds one after them. TD Enhancement Pack - Continued also adds buttons here with a postfix (scripts/query-patches.py, 19 Nov 2026);
/// both only add.
/// </summary>
[HarmonyPatch(typeof(Zone_Stockpile), nameof(Zone_Stockpile.GetGizmos))]
public static class UnloadSwitchOnStockpileZones
{
    public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Zone_Stockpile __instance)
    {
        foreach (Gizmo gizmo in __result) yield return gizmo;
        Zone_Stockpile zone = __instance;
        yield return new Command_Toggle
        {
            defaultLabel = "PhobosVehicleSystems_MayUnload".Translate(),
            defaultDesc = "PhobosVehicleSystems_MayUnloadZoneDesc".Translate(),
            icon = SystemIcons.For(VehicleUnloadBays.IconPath),
            isActive = () => VehicleUnloadBays.IsMarked(zone),
            toggleAction = () => VehicleUnloadBays.Set(zone, !VehicleUnloadBays.IsMarked(zone)),
        };
    }
}
