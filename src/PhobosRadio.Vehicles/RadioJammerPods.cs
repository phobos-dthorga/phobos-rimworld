using System.Collections.Generic;
using Phobos.Framework.WorldMap;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// The radio jamming pod for any aircraft (Radio 0.75.0; docs/development/electronic-warfare.md, EW10 and EW3): a fitting on the pylon the
/// self-protection pod uses, which sends out noise over the VHF and UHF bands while the aircraft is in the air and its switch is on (a
/// toggle load that starts off, RadioJammerPod.xml: our own jamming drowns our own radios too, the owner's card of 27 Nov 2026). Over a map
/// it is one more jammer in the noise snapshot, its place rounded to the rules' grid so a pod crossing the map changes the picture only at
/// each grid line; in flight on the world map it jams every link end on the tile beneath it, from its cruising height. Any faction's
/// aircraft: another faction's is never short of power (Feeds), though no faction fields the pod yet.
/// </summary>
public static class RadioJammerPods
{
    /// <summary>What the pod fitting grants.</summary>
    public const string Grant = "RadioJammerPod";

    /// <summary>Whether this aircraft's pod is fitted, switched on and powered (its feed met). In the air is asked separately.</summary>
    public static bool Works(VehiclePawn vehicle) => Feeds.Works(vehicle, Grant);

    /// <summary>The pods jamming over this map now: aircraft in the air on it, flying themselves (our flight, the Hind's, the Harrier's).</summary>
    public static IEnumerable<RadioJammer> OnMap(Map map)
    {
        var rules = RadioRulesDef.Current;
        IReadOnlyList<BandDef>? bands = RadioJamming.BandsIn(rules.jamPodMHz);
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            if (pawns[i] is not VehiclePawn vehicle || !Works(vehicle) || !OnMapFlight.Airborne(vehicle)) continue;
            IntVec3 cell = Gridded(vehicle.Position, rules.jamPodGridCells).ClampInsideMap(map);
            if (Seen.Wants("radio.jam-pod", vehicle, "over a map"))
                Seen.Note("radio.jam-pod", vehicle, rules.jamPodRadiatedWatts.ToString("0") + " W sent out", "over a map");
            yield return new RadioJammer(cell, rules.jamPodRadiatedWatts, bands, vehicle);
        }
    }

    /// <summary>The pods in flight on the world map now, each over the tile it holds or passes over, at its cruising height above it.</summary>
    public static IEnumerable<WorldRadioJammer> Overhead()
    {
        if (Find.WorldObjects == null) yield break;
        var rules = RadioRulesDef.Current;
        IReadOnlyList<BandDef>? bands = RadioJamming.BandsIn(rules.jamPodMHz);
        foreach (var place in Find.WorldObjects.AllWorldObjects)
        {
            if (place is not AerialVehicleInFlight { Flying: true } flight || flight.Vehicle is not { } vehicle || !Works(vehicle)) continue;
            bool holding = HoldStation.Holding(flight);
            PlanetTile tile = holding && HoldStation.Of(flight) is { } hold ? hold.Centre : WorldHelper.GetNearestTile(flight.DrawPos);
            if (!tile.Valid) continue;
            double heightMetres = System.Math.Max(0.0, AirCombat.TrackCruiseMetres(vehicle, AirCombat.Track(vehicle), out _) - TileAir.ElevationMetres(tile));
            var variant = holding ? "holding" : "transit";
            if (Seen.Wants("radio.jam-pod", vehicle, variant))
                Seen.Note("radio.jam-pod", vehicle, heightMetres.ToString("0") + " m over the ground", variant);
            yield return new WorldRadioJammer(tile, rules.jamPodRadiatedWatts, bands, heightMetres, vehicle);
        }
    }

    /// <summary>A cell rounded to the middle of its square of the grid.</summary>
    static IntVec3 Gridded(IntVec3 cell, int grid)
    {
        if (grid <= 1) return cell;
        return new IntVec3(cell.x / grid * grid + grid / 2, 0, cell.z / grid * grid + grid / 2);
    }
}
