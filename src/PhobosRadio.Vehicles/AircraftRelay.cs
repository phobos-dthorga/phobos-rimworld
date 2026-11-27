using System.Collections.Generic;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// The flying relay (Radio 0.50.0; docs/development/electronic-warfare.md, EW7; the owner's card of 27 Nov 2026, "Both a 'hold station'
/// order and in transit"): a player's aircraft in flight on the world map with the repeater kit passes calls on from its height. Height is
/// what makes it worth having: from 3,000 m its radio horizon is over 200 km, where a mast's is a few tens.
/// <list type="bullet">
/// <item>Its radio: the dual-band radio when fitted, so convoys' VHF and UHF calls pass through it; else its aviation radio, so aviation-band
/// calls do.</item>
/// <item>Its height: the height it cruises at (the leg's, or its track's figure), less the ground under it, as the radio's heights are over the
/// tile's plain; <c>Airborne</c>, so the side view floats it.</item>
/// <item>Its place: holding station (D'thorga Framework's <see cref="HoldStation"/>), the tile it holds over, fixed, so the network picture
/// draws it as any relay; in transit, the tile under it, marked <c>Moving</c>, so the picture names it and leaves it out while calls still
/// pass through.</item>
/// <item>Its owner: the vehicle, which outlives each flight's world object, so the channel ledger keeps its places.</item>
/// </list>
/// Cost: asked whenever a chain is searched (a convoy's reach, a relay plan); one pass over the world's objects, as the convoy source makes.
/// Only the player's aircraft count: another faction's in flight is never updated. On a refuel stop it is down and relays nothing here.
/// </summary>
public sealed class AircraftRelaySource : IRelaySource
{
    public IEnumerable<RelayNode> Nodes()
    {
        if (Find.WorldObjects == null) yield break;
        foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
        {
            if (place is not AerialVehicleInFlight { Flying: true } flight || flight.Vehicle is not { } vehicle || vehicle.Faction != Faction.OfPlayer) continue;
            if (!VehicleRadios.Repeats(vehicle)) continue;
            CompRadio? radio = CrossBand.Fitted(vehicle) ? CrossBand.Voice
                : VehicleRadios.AviationFitted(vehicle) ? vehicle.GetComp<CompRadio>() : null;
            if (radio == null) continue;
            bool holding = HoldStation.Holding(flight);
            PlanetTile tile = holding && HoldStation.Of(flight) is { } hold ? hold.Centre : WorldHelper.GetNearestTile(flight.DrawPos);
            if (!tile.Valid) continue;
            double heightMetres = System.Math.Max(0.0, AirCombat.TrackCruiseMetres(vehicle, AirCombat.Track(vehicle), out _) - TileAir.ElevationMetres(tile));
            var variant = holding ? "holding" : "transit";
            if (Seen.Wants("radio.airborne-relay", vehicle, variant))
                Seen.Note("radio.airborne-relay", vehicle, heightMetres.ToString("0") + " m over the ground", variant);
            yield return new RelayNode(RadioEndpoint.BuiltIn(vehicle, IntVec3.Zero, radio), tile, vehicle.LabelShort, vehicle,
                Repeater: true, SiteMetres: heightMetres, Airborne: true, Moving: !holding);
        }
    }
}
