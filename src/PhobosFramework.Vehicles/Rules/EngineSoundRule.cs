using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Which of an engine's voices plays (Framework 0.331.0): none, the idling and labouring loops, the sound of it in the air, or
/// the sound of it taxiing.</summary>
public enum EngineVoice
{
    Silent,
    Engine,
    Aloft,
    Taxi,
}

/// <summary>
/// How an engine's sound follows the engine (docs/development/sound-catalogue.md; the owner's choice of 10 Nov 2026: an idling and a
/// labouring loop per vehicle, blended by load, pitched by rpm). Game-free. The pitch is a multiplier on each loop's natural pitch;
/// the loops are recorded steady near idle, so idle plays a little under 1 and rated speed well over it. Beyond about 1.4 an engine
/// starts to sound like tape played fast, so the figures stay inside that (authored).
/// </summary>
public static class EngineSoundRule
{
    /// <summary>The pitch at an engine speed: <paramref name="idlePitch"/> at idle, <paramref name="ratedPitch"/> at rated speed, in a
    /// straight line between, and held at the ends beyond them.</summary>
    public static double Pitch(double rpm, double idleRpm, double ratedRpm, double idlePitch, double ratedPitch)
    {
        if (double.IsNaN(rpm) || ratedRpm <= idleRpm) return idlePitch;
        double share = Math.Clamp((rpm - idleRpm) / (ratedRpm - idleRpm), 0, 1);
        return idlePitch + (ratedPitch - idlePitch) * share;
    }

    /// <summary>Off the ground for the sound of its flight: this share of its flight height (authored, 27 Nov 2026: 3 m of the C-27J's
    /// 60, so the sound changes as the wheels leave the strip, not while it rolls).</summary>
    public const double AloftClimb = 0.05;

    /// <summary>
    /// Which voice plays (Framework 0.331.0; the C-27J first, the owner's picks of 26 Nov 2026): the aircraft's own sound in the air in place
    /// of the engine loops once it is off the ground, and its taxiing sound in place of them while it moves on the ground under its own
    /// power. A voice the vehicle has no sound for falls back to the engine loops, so a vehicle with neither sounds as before.
    /// </summary>
    public static EngineVoice Voice(bool running, double climb, bool taxiing, bool hasAloft, bool hasTaxi)
    {
        if (!running) return EngineVoice.Silent;
        if (hasAloft && climb >= AloftClimb) return EngineVoice.Aloft;
        if (hasTaxi && taxiing && climb <= 0) return EngineVoice.Taxi;
        return EngineVoice.Engine;
    }

    /// <summary>One step of a smooth follow: moves <paramref name="current"/> a share of the way to <paramref name="target"/>, so a
    /// reading that changes once a second glides instead of jumping.</summary>
    public static double Approach(double current, double target, double share) =>
        double.IsNaN(current) ? target : current + (target - current) * Math.Clamp(share, 0, 1);
}
