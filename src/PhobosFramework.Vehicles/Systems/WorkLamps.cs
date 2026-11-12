using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle's work lamps (the work lights family, Framework 0.103.0; docs/development/vehicle-upgrade-families.md). With the lamps fitted
/// and their switch on, a vehicle standing still at night lights the ground round it through the game's own light: anyone working there
/// does so at the pace light gives (the game's own work speed in the dark), and the drill rig counts as lit. The lamps draw on the
/// vehicle's electrics as any load does, so with the engine off they run the battery down.
///
/// The light is a <see cref="VehicleLight"/> (Framework 0.104.0: one place for every light a vehicle casts), so nothing new is saved and
/// removing the mod leaves no trace. Derived state, worked out again at every rare tick.
/// </summary>
public sealed class WorkLamps
{
    /// <summary>The ability the lamps' fitting grants, and the name of their switch in the loadouts.</summary>
    public const string Ability = "WorkLights";
    public const string LoadName = "workLamps";

    /// <summary>How far the lamps light, in cells (authored: a pair of mast or roof-bar floodlights, about a building's pool of light).</summary>
    const float Radius = 8f;
    /// <summary>A cool white, as LED floodlights give, at the game's own lamp brightness (its glowers' default scale; authored).</summary>
    static readonly ColorInt Colour = new ColorInt(235, 240, 255, 0) * 1.45f;

    readonly CompPhobosVehicleSystems comp;
    readonly VehicleLight light = new VehicleLight();

    public WorkLamps(CompPhobosVehicleSystems comp) => this.comp = comp;

    /// <summary>Whether the lamps burn now, lighting the ground.</summary>
    public bool Lit => light.Lit;

    /// <summary>Whether the vehicle has work lights of any kind: our fitted lamps, or a design's own work light upgrade (a stat above 0).</summary>
    public static bool Fitted(VehiclePawn vehicle) =>
        vehicle.GetStatValue(RigDefOf.Phobos_WorkLights) > 0f || vehicle.GetComp<CompPhobosVehicleSystems>()?.Fittings.Grants(Ability) == true;

    /// <summary>Lights or puts out the lamps as their switch, the hour and the vehicle's place say. Called from the vehicle's rare tick.</summary>
    public void Update()
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned || !comp.LoadActive(LoadName))
        {
            PutOut();
            return;
        }
        light.Place(vehicle.Map, vehicle.Position, Radius, Colour);
    }

    /// <summary>Takes the light off the map it was on: the lamps went off, the vehicle moved, or it left the map.</summary>
    public void PutOut() => light.PutOut();
}
