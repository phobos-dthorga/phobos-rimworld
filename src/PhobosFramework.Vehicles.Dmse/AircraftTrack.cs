using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Fortified;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.AirDefence;

/// <summary>
/// One aircraft in flight on the world map as The Dead Man's Switch - Expedition's air defence sees it (Framework 0.119.0; AD3). It wraps Vehicle
/// Framework's flight and holds nothing of its own: where it is, where it is going, how fast and how high are read from the flight each time,
/// and what it looks like comes from its loadout (AirCombat.Track). Hit chances and lock times go through ModifyDefense (countermeasures, A16);
/// a hit goes to D'thorga Framework's air-combat service. Never saved: DMS keeps its own state for a track by the flight's ID.
/// </summary>
public sealed class AircraftTrack : IAirTrack
{
    readonly AerialVehicleInFlight flight;
    /// <summary>Where the flight was first seen: the start of the leg it was on, as Vehicle Framework keeps no take-off point. Never saved.</summary>
    readonly Vector3 firstSeen;

    public AircraftTrack(AerialVehicleInFlight flight)
    {
        this.flight = flight;
        // Framework 0.120.5 (fault-read A3): the climb is measured from here. A flight first seen right after launch is measured from its
        // take-off; after a load mid-route, from the start of the leg it is on (a later leg then reads as climbing near its start: an
        // approximation, labelled in the record).
        firstSeen = flight.position;
    }

    public AerialVehicleInFlight Flight => flight;

    VehiclePawn? Vehicle => flight.Vehicle;

    public WorldObject Self => flight;

    public Faction Attacker => flight.Faction;

    public PlanetTile Origin => flight.Tile;

    public PlanetTile Destination => flight.flightPath is { Empty: false } path ? path.Last.Tile : flight.Tile;

    /// <summary>Its pace: Vehicle Framework flies a reconnaissance circle at a fixed speed, whatever the aircraft (fault-read A12).</summary>
    float FlightSpeed => flight.recon ? AerialVehicleInFlight.ReconFlightSpeed : Vehicle?.CompVehicleLauncher?.FlightSpeed ?? 0f;

    /// <summary>Where it really is: Vehicle Framework's <c>position</c> is the start of the current leg, and the aircraft slides from there
    /// towards the next node (Framework 0.120.5, fault-read A1: DMS saw every aircraft at the start of its leg).</summary>
    Vector3 Here => flight.DrawPos;

    float TilesLeft => flight.flightPath is { Empty: false } path ? path.DistanceLeft : 0f;

    public AirTrackSignature Signature
    {
        get
        {
            AirTrackSpec spec = AirCombat.Track(Vehicle);
            return new AirTrackSignature
            {
                distanceLevel = spec.size,
                stealthLevel = 0,
                speed = spec.speed,
                altitude = AltitudeAt(0),
            };
        }
    }

    public float AngularSpeed => (float)AirCombatRule.AngularSpeed(FlightSpeed, Find.WorldGrid.ApproxDistanceInTiles(1f));

    public int TicksToArrival => AirCombatRule.TicksToArrival(TilesLeft, FlightSpeed);

    /// <summary>
    /// Changes whenever the player sends the aircraft somewhere else, so an interceptor already on its way to the old route misses. Framework
    /// 0.120.5 (fault-read A4): from the destination and the order itself, not the nodes left, since Vehicle Framework takes a node off at
    /// every waypoint passed and circles by pushing and taking them, which voided every interceptor in the air without a roll.
    /// </summary>
    public int RouteRevision
    {
        get
        {
            if (flight.flightPath is not { Empty: false } path) return 0;
            int order = path.ArrivalAction == null ? 0 : RuntimeHelpers.GetHashCode(path.ArrivalAction);
            return path.Last.Tile.tileId * 31 + order;
        }
    }

    public bool ModifiesDefense => true;

    /// <summary>Where the aircraft will be, following its route node by node at its flight speed.</summary>
    public Vector3 PositionAt(int ticksAhead)
    {
        Vector3 here = Here;
        if (ticksAhead <= 0 || flight.flightPath is not { Empty: false } path) return here;
        double tilesAhead = AirCombatRule.TilesPerTickPerSpeed * FlightSpeed * ticksAhead;
        foreach (FlightNode node in path.Path)
        {
            Vector3 next = Find.WorldGrid.GetTileCenter(node.Tile);
            float leg = Find.WorldGrid.ApproxDistanceInTiles(GenMath.SphericalDistance(here.normalized, next.normalized));
            if (leg <= 0f) continue;
            if (tilesAhead <= leg) return Vector3.Slerp(here, next, (float)(tilesAhead / leg));
            tilesAhead -= leg;
            here = next;
        }
        return here;
    }

    /// <summary>How high it flies: its cruise, low without oxygen (A14), climbing away from take-off and coming down to land.</summary>
    public float AltitudeAt(int ticksAhead)
    {
        AirTrackSpec spec = AirCombat.Track(Vehicle);
        float left = TilesLeft;
        double ahead = AirCombatRule.TilesPerTickPerSpeed * FlightSpeed * Mathf.Max(0, ticksAhead);
        // Measured from where the flight was first seen, in the same units as the distance left (fault-read A3): Vehicle Framework's
        // TotalDistance leaves out the leg from the aircraft to its first node, so it was nothing on a direct flight, which then read
        // as a tenth of its cruise the whole way.
        double flown = Ext_Math.SphericalDistance(firstSeen, Here) + ahead;
        double stillLeft = Mathf.Max(0f, (float)(left - ahead));
        double cruise = AirCombat.TrackCruiseMetres(Vehicle, spec, out bool low);
        return (float)AirCombatRule.Altitude(cruise, spec.lowCruiseMetres, low, flown, stillLeft,
            Framework.Defs.PhobosRulesDef.Current<PhobosAirCombatRulesDef>().climbTiles);
    }

    /// <summary>Countermeasures (A16): a shot is less likely to hit, and a lock takes longer, while the dispensers have cartridges to fire.</summary>
    public void ModifyDefense(AirDefenseQuery query, ref float value)
    {
        if (Vehicle is not { } vehicle || query == null) return;
        switch (query.Stat)
        {
            case AirDefenseStat.HitChance:
                value *= AirCombat.HitChanceFactor(vehicle);
                break;
            case AirDefenseStat.LockTicks:
                value *= AirCombat.LockTimeFactor(vehicle);
                break;
        }
    }

    public void Notify_Intercepted(Faction by) => AirCombat.Intercepted(flight, by);
}

/// <summary>
/// Hands DMS Expedition every aircraft in flight on the world map, any faction's (Framework 0.119.0; AD3). Named in our AirDefenseExtensionDef,
/// so DMS asks it whenever it gathers tracks. One wrapper per flight, kept only while the flight lives; the list is built at most once a tick.
/// When a new flight appears, DMS is asked to look for tracks near its radars at its next scan.
/// </summary>
public class PhobosAircraftTrackProvider : AirTrackProvider
{
    static readonly ConditionalWeakTable<AerialVehicleInFlight, AircraftTrack> tracks = new();
    static readonly List<IAirTrack> cache = new();
    static readonly HashSet<int> known = new();
    static int cacheTick = -1;
    static Game? game;

    public override void CollectTracks(List<IAirTrack> into)
    {
        if (!VehicleSystemsMod.Settings.airDefenceSeesAircraft) return;
        // A loaded game starts afresh (Framework 0.120.5, fault-read A7): world object IDs repeat between games, so a flight launched again
        // after a load could look known and DMS woke late; and the last game's flights were handed out until the first tick.
        if (game != Current.Game)
        {
            game = Current.Game;
            known.Clear();
            cache.Clear();
            cacheTick = -1;
        }
        int now = Find.TickManager.TicksGame;
        if (now != cacheTick)
        {
            cacheTick = now;
            cache.Clear();
            bool fresh = false;
            List<WorldObject> objects = Find.WorldObjects.AllWorldObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] is not AerialVehicleInFlight flight || flight.Destroyed || flight.Vehicle == null || !flight.Flying) continue;
                cache.Add(tracks.GetValue(flight, f => new AircraftTrack(f)));
                if (known.Add(flight.ID)) fresh = true;
            }
            if (fresh) WorldComponent_AirDefenseNetwork.Current?.NotifyTrackChanged();
        }
        into.AddRange(cache);
    }
}

/// <summary>
/// The warning receiver (Framework 0.119.0; AD2): DMS Expedition raises an event as a radar first sees a track, as a fire-control radar starts
/// to lock on, and as an interceptor is launched. For our tracks, D'thorga Framework's air-combat service tells the crew, if its receiver works,
/// and fires chaff and flares at a launch.
/// </summary>
public class PhobosAircraftWarningListener : AirCombatListener
{
    public override void Notify(AirCombatEvent e)
    {
        if (e == null || e.Layer != AirCombatLayer.World || e.Track is not AircraftTrack track) return;
        AirCombat.Warning? warning = e.Kind switch
        {
            AirCombatEventKind.TrackDetected => AirCombat.Warning.Tracked,
            AirCombatEventKind.LockStarted => AirCombat.Warning.LockedOn,
            AirCombatEventKind.InterceptorLaunched => AirCombat.Warning.Launched,
            _ => null,
        };
        if (warning == null) return;
        string site = e.Site?.LabelCap ?? e.Defender?.Name ?? "PhobosAirCombat_Unknown".Translate().Resolve();
        AirCombat.Heard(track.Flight, warning.Value, site);
    }
}
