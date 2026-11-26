using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// Work a halted convoy does on the world map, game-free (docs/development/vehicle-road-work.md): the survey halt (EM31) and the test hole
/// (EM35), beside the water bore's <see cref="BoreRule"/>. Every figure is the agent's, for the owner to revise.
/// </summary>
public static class RoadHaltRule
{
    /// <summary>Hours of console work, at the plain pace, for a firm read of the tile under a halted convoy.</summary>
    public const float SurveyHaltHours = 6f;
    /// <summary>Hours of drilling, at the plain pace, to prove a read find with a test hole: about a working day.</summary>
    public const float TestHoleHours = 12f;
    /// <summary>Portions of the read ore a test hole brings up when the insects' setting is on (and so the risk of them is taken).</summary>
    public const int TestHolePortions = 4;
    /// <summary>Portions with the insects' setting off: a little less (the owner's card of 26 Nov 2026), for a hole drilled with care.</summary>
    public const int TestHolePortionsCareful = 3;

    /// <summary>Hours worked so far after one step at a hand's pace, never past the job's total.</summary>
    public static float Worked(float soFar, float hours, float pace, float total) => Math.Min(total, soFar + Math.Max(0f, hours * pace));

    /// <summary>Portions a finished test hole gives.</summary>
    public static int Portions(bool insectsOn) => insectsOn ? TestHolePortions : TestHolePortionsCareful;

    /// <summary>
    /// How much of an ore a read tile holds, in units: the lump's cells, picked within the game's own lump size range by a value fixed for
    /// the tile (0 to 1), times the game's units a cell. The same tile always reads the same: the ground is what it is.
    /// </summary>
    public static int Units(float lumpMin, float lumpMax, float tileValue, int perCell)
    {
        float cells = lumpMin + (Math.Max(lumpMax, lumpMin) - lumpMin) * Math.Clamp(tileValue, 0f, 1f);
        return Math.Max(1, (int)Math.Ceiling(cells)) * Math.Max(1, perCell);
    }
}
