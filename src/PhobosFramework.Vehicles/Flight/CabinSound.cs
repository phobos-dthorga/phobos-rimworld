using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Phobos.VehicleSystems;

/// <summary>
/// The sound inside an aircraft, heard while the player follows it between tiles on the world map (Framework 0.331.0; the C-27J's, the
/// owner's pick of 26 Nov 2026, a real recording of its cabin in flight): with the world map open and the aircraft alone selected, flying,
/// its Def's <c>cabinSound</c> plays on the camera, as if riding along; it stops when the map closes, the selection changes, it lands or
/// the game is paused. Agent's choice of when, for the owner to revise: the world map is the one place the game shows an aircraft from its
/// crew's side. Presentation only; nothing is saved.
/// </summary>
public sealed class GameComponent_PhobosCabinSound : GameComponent
{
    Sustainer? sustainer;
    SoundDef? playing;
    VehiclePawn? riding;

    public GameComponent_PhobosCabinSound(Game game)
    {
    }

    public override void GameComponentUpdate()
    {
        SoundDef? wanted = Wanted(out VehiclePawn? vehicle);
        if (wanted == null)
        {
            End();
            return;
        }
        if (sustainer is { Ended: false } && playing == wanted && riding == vehicle)
        {
            sustainer.Maintain();
            return;
        }
        End();
        sustainer = wanted.TrySpawnSustainer(SoundInfo.OnCamera(MaintenanceType.PerFrame));
        playing = wanted;
        riding = vehicle;
        if (sustainer != null && Seen.On && vehicle != null) Seen.Note("aircraft.cabin-sound", vehicle, wanted.defName);
    }

    /// <summary>The cabin sound to play now, and whose: the one selected world object, a player's aircraft in flight with one.</summary>
    static SoundDef? Wanted(out VehiclePawn? vehicle)
    {
        vehicle = null;
        if (Find.TickManager is not { Paused: false } || !WorldRendererUtility.WorldSelected) return null;
        if (Find.WorldSelector?.SingleSelectedObject is not AerialVehicleInFlight { Flying: true, IsPlayerControlled: true } flight) return null;
        vehicle = flight.Vehicle;
        return vehicle?.GetComp<CompPhobosFlight>()?.Props.cabinSound;
    }

    void End()
    {
        if (sustainer is { Ended: false }) sustainer.End();
        sustainer = null;
        playing = null;
        riding = null;
    }
}
