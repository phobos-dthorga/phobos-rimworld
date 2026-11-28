using System.Collections.Generic;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Aircraft as radar targets (Radio 0.74.0; docs/development/radar-and-air-defence.md, rows RD2, RD6 and RD9):
/// <list type="bullet">
/// <item>Between tiles: every faction's aircraft in flight on the world map, at the height it cruises over the ground under it, and its radar
/// size from its loadout (<see cref="AirTrackSpec.RadarSquareMetres"/>). Each answers by transponder: the factory radios the vehicle
/// standard gives aircraft (an agent default); the radar picture counts it a friend only when it is ours or an ally's.</item>
/// <item>Over a map: every vehicle in the air over it, on our own flight or another flight mod's (<see cref="OnMapFlight.Airborne"/>).</item>
/// </list>
/// Cost: asked only while a scope or the world map looks: one pass over the world's objects at most once a tick for every colony that asks;
/// over a map, one pass over its vehicles every 30 ticks while its scope is open on that scale.
/// </summary>
public sealed class AircraftRadarTargets : IRadarTargetSource, IMapRadarTargetSource
{
    public void Collect(List<RadarTarget> into)
    {
        if (Find.WorldObjects == null) return;
        foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
        {
            if (place is not AerialVehicleInFlight { Flying: true } flight || flight.Vehicle is not { } vehicle) continue;
            PlanetTile tile = WorldHelper.GetNearestTile(flight.DrawPos);
            if (!tile.Valid) continue;
            AirTrackSpec spec = AirCombat.Track(vehicle);
            double height = System.Math.Max(0.0, AirCombat.TrackCruiseMetres(vehicle, spec, out _) - TileAir.ElevationMetres(tile));
            into.Add(new RadarTarget(flight, tile, height, spec.RadarSquareMetres, vehicle.Faction, vehicle.LabelShortCap, RadarContactKind.Aircraft, true));
        }
    }

    public void Collect(Map map, List<MapRadarTarget> into)
    {
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is not VehiclePawn vehicle || !OnMapFlight.Airborne(vehicle)) continue;
            AirTrackSpec spec = AirCombat.Track(vehicle);
            into.Add(new MapRadarTarget(vehicle, spec.cruiseMetres * OnMapFlight.Climb(vehicle), spec.RadarSquareMetres, vehicle.Faction,
                RadarContactKind.Aircraft, true));
        }
    }
}
