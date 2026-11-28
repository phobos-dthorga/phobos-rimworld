using System.Collections.Generic;
using Fortified;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.Radio.Fortified;

/// <summary>
/// Fortified Features Framework on our radar (Radio 0.74.0; the owner's card of 28 Nov 2026, "Fortified/DMS tracks now"; read from its build
/// of 28 Nov 2026):
/// <list type="bullet">
/// <item>Its tracks on the world map (missiles, drop-pod tracks, interceptors: every world object that is an <c>IAirTrack</c>) are radar
/// targets: where it is now (<c>PositionAt(0)</c>), how high (<c>AltitudeAt(0)</c>, metres), and its radar size from its signature, on the
/// same reading of its scale as our aircraft (<c>5 m² × (distanceLevel / 1.5)⁴</c>, inferred). One aimed at a player's map is inbound. Our own
/// aircraft, which we hand it as provided tracks, are left out: our vehicles' source already gives them.</item>
/// <item>Its radars on a colony's map (<c>AirSensors.On</c>) are listed on our console, working or not.</item>
/// </list>
/// Reading only; joining its network is radar slice 4. Cost: one pass over the world's objects at most once a tick while a scope or the
/// warnings' look asks; its sensors only while a scope is open.
/// </summary>
[StaticConstructorOnStartup]
public static class FortifiedRadar
{
    static FortifiedRadar()
    {
        RadarTargets.Register(new Tracks());
        RadarTargets.Register(new Sensors());
    }

    private sealed class Tracks : IRadarTargetSource
    {
        public void Collect(List<RadarTarget> into)
        {
            foreach (WorldObject place in Find.WorldObjects.AllWorldObjects)
            {
                if (place.Destroyed || place is not IAirTrack track) continue;
                Vector3 at = track.PositionAt(0);
                PlanetTile tile = RadarTargets.TileUnder(place, at, track.Origin.Valid ? track.Origin : place.Tile);
                if (!tile.Valid) continue;
                float level = Mathf.Max(0.1f, track.Signature.distanceLevel);
                double squareMetres = 5.0 * Mathf.Pow(level / 1.5f, 4f);
                bool inbound = track.Attacker != Faction.OfPlayer && track.Destination.Valid
                    && Find.WorldObjects.MapParentAt(track.Destination) is { HasMap: true } target && target.Map.IsPlayerHome;
                RadarContactKind kind = place.GetType().Name.Contains("DropPod") ? RadarContactKind.Falling : RadarContactKind.Missile;
                into.Add(new RadarTarget(place, tile, Mathf.Max(0f, track.AltitudeAt(0)), squareMetres, track.Attacker, place.LabelCap, kind, false, inbound));
            }
        }
    }

    private sealed class Sensors : IOtherRadarSource
    {
        public void Collect(Map map, List<OtherRadar> into)
        {
            foreach (IAirSensor sensor in AirSensors.On(map))
            {
                if (sensor.Source is not { Spawned: true } thing || thing.Faction != Faction.OfPlayer) continue;
                into.Add(new OtherRadar(thing, sensor.Active, "PhobosRadar_FortifiedSensor".Translate().Resolve()));
            }
        }
    }
}
