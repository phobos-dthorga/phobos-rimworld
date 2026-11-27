using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A searchlight (Framework 0.366.0; the vehicle standard's batch 4, the owner's card of 28 Nov 2026: "Also a helicopter searchlight"). The
/// searchlight fitting brings a load of its own with a <see cref="BeamSpec"/>; while that load runs (switched on, at night, with power) a pool of
/// the game's own light stands a few cells ahead of the aircraft's nose and follows it as it flies over the map, hovers or taxis, as a real
/// searchlight under a helicopter's nose lights the ground it looks at. What the light does is the game's: work in its pool goes at
/// daylight's pace, and under Combat Extended a shot at someone standing in it is aimed by its light (Combat Extended reads the light at the
/// target, CE_Utility.GetLightingShift).
///
/// Cost: as the headlights' beam (<see cref="Headlights"/>), whose rule and cadence it shares: nothing while it is out; while lit, a look every
/// <see cref="Headlights.LookInterval"/> ticks and a move only when the aircraft has reached another cell. Whether it burns is decided on the
/// rare tick, at the vehicle's events and when its switch is pressed. Derived state: nothing is saved, and a loaded game lights it again.
/// The player's own aircraft only: another faction's has no switch (the light tier keeps no electrics).
/// </summary>
public sealed class Searchlight
{
    /// <summary>The fitting's load, by name (Fittings.xml, PhobosFitting_Searchlight).</summary>
    public const string LoadName = "searchlight";

    readonly CompPhobosVehicleSystems comp;
    readonly VehicleLight light = new VehicleLight("searchlight");
    int lastLook = -Headlights.LookInterval;

    public Searchlight(CompPhobosVehicleSystems comp) => this.comp = comp;

    /// <summary>Whether the searchlight's pool is on the map now.</summary>
    public bool Lit => light.Lit;

    /// <summary>The fitted searchlight's load, while one is fitted and casts a beam; null otherwise.</summary>
    ElectricalLoadSpec? Load => comp.LoadNamed(LoadName) is { beam: not null } load ? load : null;

    bool ShouldBurn()
    {
        VehiclePawn vehicle = comp.Vehicle;
        return comp.Tier == DetailTier.Full && VehicleSystemsMod.Settings.headlightBeams && vehicle is { Spawned: true }
            && Load is { } load && comp.LoadActive(load.name);
    }

    /// <summary>Lights, moves or puts out the pool as the switch, the hour and the power say. On the rare tick, at events and at the switch.</summary>
    public void Update()
    {
        if (!ShouldBurn())
        {
            PutOut();
            return;
        }
        bool wasLit = light.Lit;
        Place();
        if (!wasLit && light.Lit && Seen.Wants("vehicles.searchlight-lit", comp.Vehicle))
        {
            Seen.Note("vehicles.searchlight-lit", comp.Vehicle, light.Cell.ToString(), OnMapFlight.Airborne(comp.Vehicle) ? "airborne" : "on the ground");
        }
    }

    /// <summary>Every tick: nothing while out; while lit, a look every few ticks, and a move only when the aircraft reached another cell.</summary>
    public void Tick()
    {
        if (!light.Lit) return;
        int now = Find.TickManager.TicksGame;
        if (!HeadlightRule.Due(lastLook, now, Headlights.LookInterval)) return;
        lastLook = now;
        Place();
    }

    void Place()
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (Load?.beam is not { } beam || vehicle.Map is not { } map)
        {
            PutOut();
            return;
        }
        double heading = CompPhobosHitch.VehicleHeading(vehicle);
        IntVec3 at = vehicle.Position;
        (int x, int z) = HeadlightRule.BeamCell(at.x + 0.5, at.z + 0.5, heading, vehicle.VehicleDef.Size.z / 2.0, beam.ahead);
        var cell = new IntVec3(x, 0, z).ClampInsideMap(map);
        // Dimmed with the battery's voltage while the generator cannot carry every load, as the headlights are.
        light.Place(vehicle, map, cell, beam.radius, beam.colour * (beam.brightness * comp.LampShare));
    }

    /// <summary>Takes the pool off the map: switched off, out of power, by day, or the aircraft left the map.</summary>
    public void PutOut() => light.PutOut();
}
