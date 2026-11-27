using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An aircraft of ours on the world map with every engine stopped comes down (Framework 0.360.0; the vehicle standard, batch 3; the owner's
/// card of 27 Nov 2026: a twin flies on one engine, a single-engine aircraft must land). Before, it struggled on across the leg. Now it
/// glides down onto the tile it is over and lands there as a caravan; where it cannot land (water, impassable mountains, space) it
/// crashes, through Vehicle Framework's own crash, as when it runs out of fuel or is shot down. Only with the player's
/// <c>aircraftEngineFaults</c> setting on and not figures only. A twin that still has one engine is left to the cruising height's
/// own judgement (fly on, turn back or struggle). Looked at every rare tick, before the cruising height and the hold-station order.
/// </summary>
public static class DeadStick
{
    /// <summary>The aircraft already sent down, by thing ID: told once. Derived, never saved: a load looks again and sends it down again.</summary>
    static readonly HashSet<int> sentDown = new();

    /// <summary>Sends the aircraft down if every engine is out; true while it is coming down, so nothing else re-routes it.</summary>
    public static bool Look(AerialVehicleInFlight flight, VehiclePawn vehicle, CompPhobosVehicleSystems comp)
    {
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        if (!settings.aircraftEngineFaults || settings.showOnly || vehicle.Faction != Faction.OfPlayer || comp.Powertrain is not { } train) return false;
        (int working, int engines) = train.Engines();
        if (engines <= 0 || working > 0)
        {
            sentDown.Remove(vehicle.thingIDNumber);
            return false;
        }
        if (!sentDown.Add(vehicle.thingIDNumber)) return true;
        PlanetTile below = flight.Tile;
        NamedArgument who = vehicle.LabelShort.Named("VEHICLE");
        string? refusal = CargoRefuel.NoLandingReason(below);
        if (refusal == null)
        {
            flight.OrderFlyToTiles(new List<FlightNode> { new(below) }, new ArrivalAction_LandToCaravan(vehicle));
            Messages.Message("PhobosVehicleSystems_MsgDeadStickLanding".Translate(who), new GlobalTargetInfo(flight), MessageTypeDefOf.ThreatBig);
            Seen.Note("aircraft.dead-stick", vehicle, null, "landing");
            return true;
        }
        Seen.Note("aircraft.dead-stick", vehicle, refusal, "crash");
        flight.InitiateCrashEvent(null, "PhobosVehicleSystems_DeadStickCrash".Translate(refusal.Named("WHY")).Resolve());
        return true;
    }
}
