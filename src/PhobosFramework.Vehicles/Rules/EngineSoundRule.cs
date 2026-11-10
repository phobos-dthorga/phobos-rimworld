using System;

namespace Phobos.Framework.Vehicles.Rules;

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

    /// <summary>One step of a smooth follow: moves <paramref name="current"/> a share of the way to <paramref name="target"/>, so a
    /// reading that changes once a second glides instead of jumping.</summary>
    public static double Approach(double current, double target, double share) =>
        double.IsNaN(current) ? target : current + (target - current) * Math.Clamp(share, 0, 1);
}
