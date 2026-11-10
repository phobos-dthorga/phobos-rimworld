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
    double pitch = double.NaN;
    double blend;

    EngineSound(PhobosEngineSoundExtension props) => this.props = props;

    /// <summary>The engine sound for this vehicle, or null if its Def names none.</summary>
    public static EngineSound? For(VehiclePawn vehicle) =>
        vehicle.def.GetModExtension<PhobosEngineSoundExtension>() is { } props ? new EngineSound(props) : null;

    public void Tick(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        bool on = comp.EngineRunning && vehicle.Spawned && !(props.silentWhileWorking && comp.WorkTurningEngine);
        idle.Set(props.idleSound, on, vehicle);
        load.Set(props.loadSound, on, vehicle);
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
    }
}
