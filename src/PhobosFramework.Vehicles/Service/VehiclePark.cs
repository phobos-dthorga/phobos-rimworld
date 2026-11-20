using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle park (Framework 0.201.0; docs/development/vehicle-bay-trips.md, batch 2; owner, 20 Nov 2026: "our own vehicle park which we
/// designate as a zone"): ground the player marks where a vehicle with no depot bay is put once its work at a dyno bay is done. It holds
/// nothing of its own: where a vehicle may stand in it is asked when one needs a place (<see cref="BayTrips.TryFindHome"/>).
/// </summary>
public class Zone_PhobosVehiclePark : Zone
{
    /// <summary>One colour for every park, at the game's own zone opacity (authored: the blue of a parking sign).</summary>
    static readonly Color ParkColour = new(0.2f, 0.43f, 0.75f, 0.09f);

    static Designator_ZoneAdd_PhobosVehiclePark? expand;

    public Zone_PhobosVehiclePark()
    {
    }

    public Zone_PhobosVehiclePark(ZoneManager zoneManager)
        : base("PhobosVehiclePark_Label".Translate(), zoneManager)
    {
    }

    public override bool IsMultiselectable => true;

    protected override Color NextZoneColor => ParkColour;

    /// <summary>The same tool that marks a park grows the one selected: the game adds dragged cells to a selected zone of its kind.</summary>
    public override IEnumerable<Gizmo> GetZoneAddGizmos()
    {
        yield return expand ??= new Designator_ZoneAdd_PhobosVehiclePark { defaultLabel = "PhobosVehiclePark_Expand".Translate() };
    }

    public override string GetInspectString()
    {
        int parked = 0;
        foreach (Pawn pawn in Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is VehiclePawn vehicle && Map.zoneManager.ZoneAt(vehicle.Position) == this) parked++;
        }
        return "PhobosVehiclePark_Inspect".Translate(parked.Named("COUNT")).Resolve();
    }
}

/// <summary>Marks a vehicle park on the ground, from the motor pool tab.</summary>
public class Designator_ZoneAdd_PhobosVehiclePark : Designator_ZoneAdd
{
    protected override string NewZoneLabel => "PhobosVehiclePark_Label".Translate();

    public Designator_ZoneAdd_PhobosVehiclePark()
    {
        zoneTypeToPlace = typeof(Zone_PhobosVehiclePark);
        defaultLabel = "PhobosVehiclePark_Label".Translate();
        defaultDesc = "PhobosVehiclePark_Desc".Translate();
        icon = SystemIcons.For("Phobos/VehicleSystems/BayTrips/VehiclePark");
    }

    protected override Zone MakeNewZone() => new Zone_PhobosVehiclePark(Find.CurrentMap.zoneManager);
}

/// <summary>Finds a vehicle its place in a park.</summary>
public static class VehicleParks
{
    public static IEnumerable<Zone_PhobosVehiclePark> On(Map map) => map.zoneManager.AllZones.OfType<Zone_PhobosVehiclePark>();

    /// <summary>
    /// A place in a park for this vehicle, the nearest park first: its whole body on the park's cells, a cell clear of every other vehicle
    /// and of the places other trips keep (so nothing is boxed in door to door), on ground it can drive, with a way there. Places are
    /// tried from the park's south-west corner, facing north then east, so a park fills in a row. The caller gives its own tests:
    /// <paramref name="clear"/> of other vehicles and kept places, <paramref name="fits"/> of a body on its cells, <paramref name="reaches"/> of a way.
    /// </summary>
    public static bool TryFindPlace(VehiclePawn vehicle, System.Func<CellRect, bool> clear, System.Func<CellRect, bool> fits,
        System.Func<IntVec3, bool> reaches, int maxWayTests, out IntVec3 cell, out Rot4 facing)
    {
        int wayTests = 0;
        return TryFindPlaceIn(vehicle, On(vehicle.Map), clear, fits, reaches, maxWayTests, ref wayTests, null, out cell, out facing);
    }

    /// <summary>
    /// The same search over any zones (Framework 0.210.0: the service yard's too), counting way tests in <paramref name="wayTests"/> so a
    /// caller's other searches share the limit. With <paramref name="onlyFacing"/>, a vehicle carried as it stands (towed, pushed) takes only
    /// a place it fits facing as it faces, and is asked for no way of its own: what carries it finds the way.
    /// </summary>
    public static bool TryFindPlaceIn(VehiclePawn vehicle, IEnumerable<Zone> zones, System.Func<CellRect, bool> clear, System.Func<CellRect, bool> fits,
        System.Func<IntVec3, bool> reaches, int maxWayTests, ref int wayTests, Rot4? onlyFacing, out IntVec3 cell, out Rot4 facing)
    {
        cell = IntVec3.Invalid;
        facing = Rot4.Invalid;
        Map map = vehicle.Map;
        Rot4[] facings = onlyFacing is { } only ? new[] { only } : new[] { Rot4.North, Rot4.East };
        foreach (Zone zone in zones.OrderBy(p => p.Cells.Count == 0 ? int.MaxValue : p.Cells[0].DistanceToSquared(vehicle.Position)))
        {
            foreach (IntVec3 candidate in zone.Cells.OrderBy(c => c.z).ThenBy(c => c.x))
            {
                if (!candidate.Standable(vehicle, map)) continue;
                foreach (Rot4 rot in facings)
                {
                    CellRect body = vehicle.VehicleRect(candidate, rot);
                    if (!Inside(body, zone, map) || !fits(body) || !clear(body.ExpandedBy(1).ClipInsideMap(map))) continue;
                    if (onlyFacing == null && candidate != vehicle.Position)
                    {
                        if (wayTests++ >= maxWayTests) return false;
                        if (!reaches(candidate)) break;
                    }
                    cell = candidate;
                    facing = rot;
                    return true;
                }
            }
        }
        return false;
    }

    static bool Inside(CellRect body, Zone park, Map map)
    {
        foreach (IntVec3 c in body)
        {
            if (!c.InBounds(map) || map.zoneManager.ZoneAt(c) != park) return false;
        }
        return true;
    }
}
