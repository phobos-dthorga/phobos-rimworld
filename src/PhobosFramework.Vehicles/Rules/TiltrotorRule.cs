using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How a tiltrotor's nacelles and rotors are drawn as they tip from upright to forward (docs/development/vehicle-osprey.md, "Tilting
/// nacelles"; the owner asked to see them tilt, 18 Nov 2026). Game-free.
/// <list type="bullet">
/// <item>The nacelle is drawn from a few pictures at set angles; the nearest is shown.</item>
/// <item>The rotor, seen from straight above, is a disc tilted from flat by the nacelle's angle: it shows shortened along the aircraft's
/// length by the cosine of that angle. That is plain projection, not an approximation.</item>
/// <item>The rotor moves forward as the nacelle tips, by its reach times the sine of the angle.</item>
/// </list>
/// Angles are degrees from upright (0, a helicopter) to forward (90, an aeroplane).
/// </summary>
public static class TiltrotorRule
{
    /// <summary>The thinnest a rotor disc is drawn, as a share of its width, so a rotor fully forward still shows as a line.</summary>
    public const float ThinnestDisc = 0.06f;

    /// <summary>The index of the picture whose angle is nearest; the lower one wins a tie. -1 with no pictures.</summary>
    public static int NearestPicture(float tiltDegrees, IReadOnlyList<float> pictureAngles)
    {
        int best = -1;
        float bestGap = float.MaxValue;
        for (int i = 0; i < pictureAngles.Count; i++)
        {
            float gap = Math.Abs(pictureAngles[i] - tiltDegrees);
            if (gap < bestGap)
            {
                best = i;
                bestGap = gap;
            }
        }
        return best;
    }

    /// <summary>How long the rotor disc looks along the aircraft's length, as a share of its width: cos of the tilt.</summary>
    public static float DiscLength(float tiltDegrees) => Math.Max(ThinnestDisc, (float)Math.Cos(Radians(Clamp(tiltDegrees))));

    /// <summary>How far forward of its upright place the rotor is drawn, in the same units as <paramref name="reach"/>.</summary>
    public static float RotorForward(float tiltDegrees, float reach) => reach * (float)Math.Sin(Radians(Clamp(tiltDegrees)));

    /// <summary>
    /// The tilt through a VTOL take-off or landing, from how far along each part of it is (0 to 1). A take-off goes straight up with the
    /// nacelles upright, then tips them forward through the climb away; a landing tips them back during the approach and comes straight
    /// down upright.
    /// </summary>
    public static float LaunchTilt(bool landing, float verticalProgress, float climbProgress)
    {
        if (landing) return climbProgress >= 1f ? 0f : 90f * (1f - Clamp01(climbProgress));
        return verticalProgress < 1f ? 0f : 90f * Clamp01(climbProgress);
    }

    /// <summary>The still blades' or spinning disc's turn, in degrees, at a tick: left and right rotors turn opposite ways.</summary>
    public static float SpinAngle(int ticks, float degreesPerTick, float startDegrees, bool reversed)
    {
        // In double: as a float the product loses whole degrees after some months of play, and the disc turns in jumps (fault-read T6).
        float turned = (float)((double)ticks * degreesPerTick % 360.0);
        return (startDegrees + (reversed ? -turned : turned) + 720f) % 360f;
    }

    static float Clamp(float tiltDegrees) => Math.Max(0f, Math.Min(90f, tiltDegrees));
    static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    static double Radians(float degrees) => degrees * Math.PI / 180.0;
}
