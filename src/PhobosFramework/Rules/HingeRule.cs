using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// A part that swings flat about a hinge (docs/development/vehicle-animation-research.md). The game turns a picture only about the picture's own centre, so a part on a hinge also has to be moved. Angles are the game's: degrees clockwise seen from above, north at 0.
/// Offsets are in cells, x east and z north.
/// </summary>
public static class HingeRule
{
    public static (float x, float z) Rotate(float x, float z, float degrees)
    {
        double radians = degrees * Math.PI / 180.0;
        double cos = Math.Cos(radians), sin = Math.Sin(radians);
        return ((float)(x * cos + z * sin), (float)(-x * sin + z * cos));
    }

    /// <summary>How far to move a picture turned this far about its centre, so that the hinge at this offset from its centre stays where it was.</summary>
    public static (float x, float z) PivotOffset(float pivotX, float pivotZ, float degrees)
    {
        (float x, float z) turned = Rotate(pivotX, pivotZ, degrees);
        return (pivotX - turned.x, pivotZ - turned.z);
    }

    /// <summary>Eases a share of the movement in and out, so a part starts and stops gently.</summary>
    public static float Eased(float share)
    {
        float t = Math.Clamp(share, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>Points along the part, from the hinge out to its tip, at each step of the swing from at rest to the full angle. The part lies due south of its hinge at rest, in the vehicle's own frame.</summary>
    public static IEnumerable<(float x, float z)> Swept(float pivotX, float pivotZ, float reach, float angle, float stepDegrees = 10f, float stepCells = 0.5f)
    {
        int swings = Math.Max(1, (int)Math.Ceiling(Math.Abs(angle) / stepDegrees));
        int lengths = Math.Max(1, (int)Math.Ceiling(reach / stepCells));
        for (int i = 0; i <= swings; i++)
        {
            float degrees = angle * i / swings;
            for (int j = 0; j <= lengths; j++)
            {
                (float x, float z) arm = Rotate(0f, -reach * j / lengths, degrees);
                yield return (pivotX + arm.x, pivotZ + arm.z);
            }
        }
    }
}
