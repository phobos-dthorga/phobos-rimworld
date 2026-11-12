using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Headlights that light the ground (Framework 0.104.0; owner, 12 Nov 2026: "We have a gizmo for headlights so why not make it properly
/// functional?"). A load of the vehicle's loadout that names a <see cref="BeamSpec"/> casts a pool of the game's own light a few cells
/// ahead of the vehicle's nose (<see cref="HeadlightRule"/>) whenever the load runs: today, the headlights at night with their switch on,
/// whether or not the engine runs (owner, 12 Nov 2026: with the engine off they run the battery down; the switch is off by default). Every
/// vehicle our loadouts reach has them, ours and other authors'.
///
/// Cost (the owner's colony runs at 36 to 50 ticks a second): each move of a light makes the game work out the light round it again, so
/// the beam is moved only when the vehicle has reached another cell or turned, and looked at no more than every
/// <see cref="LookInterval"/> ticks while it is lit; a dark vehicle costs one check a tick. Whether it should burn is decided on the
/// rare tick and at the vehicle's ignition and move events. Derived state: nothing is saved, and a loaded game lights it again.
///
/// Other factions' vehicles run on the light tier, with no electrics or switch: theirs burn at night only while they move (HeadlightRule.LiteBurns),
/// so a raid driving in at night lights itself up, and the vehicles parked at a base stay dark and cost nothing.
/// </summary>
public sealed class Headlights
{
    /// <summary>Ticks between looks at a lit, moving beam (authored: about three times a second at normal speed).</summary>
    public const int LookInterval = 20;

    readonly CompPhobosVehicleSystems comp;
    readonly VehicleLight light = new VehicleLight();
    readonly ElectricalLoadSpec? load;
    int lastLook = -LookInterval;
    double litHeading = double.NaN;

    public Headlights(CompPhobosVehicleSystems comp)
    {
        this.comp = comp;
        PhobosVehicleLoadoutDef? loadout = comp.Props.loadout;
        if (loadout == null) return;
        foreach (ElectricalLoadSpec spec in loadout.loads)
        {
            if (spec.beam == null) continue;
            load = spec;
            break;
        }
    }

    /// <summary>Whether this vehicle has a load that casts a beam at all.</summary>
    public bool Fitted => load != null;

    /// <summary>Whether the beam is on the map now.</summary>
    public bool Lit => light.Lit;

    /// <summary>Whether the headlights are switched on and burning by the systems' rules (night), whether or not the setting draws their beam
    /// on the ground: what the driver sees by (G9).</summary>
    public bool On => load != null && comp.Tier == DetailTier.Full && comp.LoadActive(load.name);

    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    bool ShouldBurn()
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (load == null || !Settings.headlightBeams || vehicle == null || !vehicle.Spawned) return false;
        return comp.Tier switch
        {
            DetailTier.Full => comp.LoadActive(load.name),
            DetailTier.Lite => HeadlightRule.LiteBurns(When_Night.IsNight(vehicle), vehicle.vehiclePather is { Moving: true }),
            _ => false,
        };
    }

    /// <summary>Lights, moves or puts out the beam as the switch, the hour, the engine and the vehicle's place say. Called on the rare tick,
    /// at ignition and move events, and when the switch is pressed.</summary>
    public void Update()
    {
        if (load == null) return;
        if (!ShouldBurn())
        {
            PutOut();
            return;
        }
        Place();
    }

    /// <summary>Every tick: nothing for a dark vehicle; for a lit one, a look every <see cref="LookInterval"/> ticks, and a move only if the
    /// vehicle reached another cell or turned.</summary>
    public void Tick()
    {
        if (!light.Lit) return;
        int now = Find.TickManager.TicksGame;
        if (!HeadlightRule.Due(lastLook, now, LookInterval)) return;
        lastLook = now;
        // A raider's lights go out when it stops; ours follow the rare tick and the events.
        if (comp.Tier == DetailTier.Lite && !ShouldBurn())
        {
            PutOut();
            return;
        }
        Place();
    }

    void Place()
    {
        VehiclePawn vehicle = comp.Vehicle;
        BeamSpec beam = load!.beam!;
        // The eight ways Vehicle Framework draws a vehicle, as the hitch reads them; the middle of its cell as its centre (a simplification:
        // an even-sized vehicle's true middle is half a cell off, which a pool of light six cells across does not show).
        double heading = CompPhobosHitch.VehicleHeading(vehicle);
        IntVec3 at = vehicle.Position;
        (int x, int z) = HeadlightRule.BeamCell(at.x + 0.5, at.z + 0.5, heading, vehicle.VehicleDef.Size.z / 2.0, beam.ahead);
        var cell = new IntVec3(x, 0, z);
        if (light.Lit && cell == light.Cell && heading == litHeading) return;
        litHeading = heading;
        light.Place(vehicle.Map, cell, beam.radius, beam.colour * beam.brightness);
    }

    /// <summary>Takes the beam off the map: the switch, the engine or the hour put it out, or the vehicle left the map.</summary>
    public void PutOut() => light.PutOut();
}
