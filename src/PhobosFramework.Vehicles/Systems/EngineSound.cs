using Phobos.Framework.Sounds;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An engine sound that follows the engine (docs/development/sound-catalogue.md; owner's choice, 10 Nov 2026): name it on a vehicle's
/// Def and our systems play it instead of a Vehicle Framework event loop, which nothing can change while it plays. Two loops, both
/// recorded steady near idle: <c>idleSound</c> and <c>loadSound</c> (the same engine labouring). Each loop's SoundDef maps the external
/// parameters <c>PhobosEnginePitch</c> (a pitch multiplier) and <c>PhobosEngineLoad</c> (0 to 1) onto its pitch and volume, so the idle
/// loop fades as the labouring one rises; the game reads them every frame. For other modders too: any vehicle with our systems may
/// carry it.
/// </summary>
public class PhobosEngineSoundExtension : DefModExtension
{
    public SoundDef? idleSound;
    public SoundDef? loadSound;
    /// <summary>The pitch multiplier at idle and at rated speed (authored; keep within about 0.8 to 1.4).</summary>
    public float idlePitch = 0.9f;
    public float ratedPitch = 1.35f;
    /// <summary>Engine speeds for a vehicle without a drivetrain model (or with it switched off in the settings).</summary>
    public float fallbackIdleRpm = 800f;
    public float fallbackCruiseRpm = 1600f;
    public float fallbackRatedRpm = 2400f;
    /// <summary>Silent while the engine turns for work, for a vehicle whose work has its own running sound (the generator set).</summary>
    public bool silentWhileWorking;
    /// <summary>Its sound in the air, heard from the ground (Framework 0.331.0; the C-27J's, the owner's picks of 26 Nov 2026): played in
    /// place of the idle and load loops once an aircraft on our flight is off the ground. Left out, the loops play in the air too, as the
    /// Osprey's do.</summary>
    public SoundDef? aloftSound;
    /// <summary>Its sound taxiing (Framework 0.331.0): played in place of the idle and load loops while it moves on the ground under its
    /// own power. Left out, the loops play.</summary>
    public SoundDef? taxiSound;
}

/// <summary>Plays one vehicle's engine sound, from its systems' engine speed and load. Presentation only; nothing is saved.</summary>
public sealed class EngineSound
{
    public const string PitchParam = "PhobosEnginePitch";
    public const string LoadParam = "PhobosEngineLoad";
    /// <summary>The share of the way to the new reading each tick (authored): pitch settles in about a third of a second, the blend in
    /// about half a second, so readings that change once a second glide.</summary>
    const float PitchFollow = 0.08f;
    const float LoadFollow = 0.05f;

    readonly PhobosEngineSoundExtension props;
    readonly SustainedSound idle = new();
    readonly SustainedSound load = new();
    readonly SustainedSound aloft = new();
    readonly SustainedSound taxi = new();
    readonly CompPhobosFlight? flight;
    EngineVoice voice;
    double pitch = double.NaN;
    double blend;

    EngineSound(PhobosEngineSoundExtension props, CompPhobosFlight? flight)
    {
        this.props = props;
        this.flight = flight;
    }

    /// <summary>The engine sound for this vehicle, or null if its Def names none.</summary>
    public static EngineSound? For(VehiclePawn vehicle) =>
        vehicle.def.GetModExtension<PhobosEngineSoundExtension>() is { } props ? new EngineSound(props, vehicle.GetComp<CompPhobosFlight>()) : null;

    public void Tick(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        bool running = comp.EngineRunning && vehicle.Spawned && !(props.silentWhileWorking && comp.WorkTurningEngine);
        bool onGround = flight == null || flight.State == FlightState.Ground;
        EngineVoice now = EngineSoundRule.Voice(running, flight?.Climb ?? 0f, onGround && vehicle.vehiclePather?.Moving == true,
            props.aloftSound != null, props.taxiSound != null);
        if (now != voice)
        {
            if (Seen.On && now is EngineVoice.Aloft or EngineVoice.Taxi) Seen.Note("vehicle.engine-voice", vehicle, now == EngineVoice.Aloft ? "aloft" : "taxi");
            voice = now;
        }
        bool on = now == EngineVoice.Engine;
        idle.Set(props.idleSound, on, vehicle);
        load.Set(props.loadSound, on, vehicle);
        aloft.Set(props.aloftSound, now == EngineVoice.Aloft, vehicle);
        taxi.Set(props.taxiSound, now == EngineVoice.Taxi, vehicle);
        if (!on)
        {
            pitch = double.NaN;
            blend = 0;
            return;
        }
        DrivetrainSpec? spec = comp.Powertrain?.Spec;
        double idleRpm = spec?.IdleRpm ?? props.fallbackIdleRpm;
        double ratedRpm = spec?.RatedRpm ?? props.fallbackRatedRpm;
        double rpm = comp.EngineRpm(props.fallbackIdleRpm, props.fallbackCruiseRpm);
        pitch = EngineSoundRule.Approach(pitch, EngineSoundRule.Pitch(rpm, idleRpm, ratedRpm, props.idlePitch, props.ratedPitch), PitchFollow);
        blend = EngineSoundRule.Approach(blend, Mathf.Clamp01(comp.EngineLoad), LoadFollow);
        idle.SetParam(PitchParam, (float)pitch);
        idle.SetParam(LoadParam, (float)blend);
        load.SetParam(PitchParam, (float)pitch);
        load.SetParam(LoadParam, (float)blend);
    }

    public void End()
    {
        idle.End();
        load.End();
        aloft.End();
        taxi.End();
        voice = EngineVoice.Silent;
    }
}
