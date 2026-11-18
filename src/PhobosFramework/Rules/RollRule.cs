using System;

namespace Phobos.Framework.Rules;

/// <summary>Whether an aircraft can leave the ground at its hover margin, and how.</summary>
public enum RollVerdict
{
    /// <summary>It lifts straight up: no strip.</summary>
    Straight,
    /// <summary>It needs a strip to roll along.</summary>
    Rolls,
    /// <summary>It cannot leave the ground at all.</summary>
    Refused,
}

/// <summary>
/// Leaving the ground rolling (D'thorga Framework 0.151.0; docs/development/vehicle-flight-over-the-map.md, "Leaving the ground rolling", the
/// design's slice 4), from the hover margin alone (1 is just enough to hover). At 1 or more it lifts straight up; between its rolling share
/// and 1 it needs a strip, from its shortest roll just under 1 to its full roll at the rolling share, in a straight line (authored); below the
/// rolling share it cannot leave the ground. The rolling share is worked out from two published take-off weights, not chosen. Game-free.
/// </summary>
public static class RollRule
{
    /// <summary>
    /// The rolling share of a rotor-lifted aircraft from its published straight-up and rolling take-off weights: hover power rises with
    /// weight to the power 1.5 (momentum theory, the rule the hover check uses), so rolling needs (straight-up over rolling) to the 1.5 of the
    /// hover's power. The Osprey's 52,600 and 57,000 lb give 0.886 (our inference from the published figures).
    /// </summary>
    public static double RollingShareRotor(double straightUpKg, double rollingKg) =>
        straightUpKg <= 0 || rollingKg <= 0 ? double.NaN : Math.Pow(Math.Min(1, straightUpKg / rollingKg), 1.5);

    /// <summary>The same for a jet held up by its thrust: thrust needed is the weight, so the plain ratio. The Harrier's 9,415 and 14,100 kg
    /// give 0.668.</summary>
    public static double RollingShareThrust(double straightUpKg, double rollingKg) =>
        straightUpKg <= 0 || rollingKg <= 0 ? double.NaN : Math.Min(1, straightUpKg / rollingKg);

    /// <summary>What the margin allows: straight up at 1 or more, a roll down to the rolling share, else nothing. A margin that cannot be
    /// judged lifts straight up; an aircraft with no rolling share cannot roll.</summary>
    public static RollVerdict Judge(double margin, double rollingShare)
    {
        if (double.IsNaN(margin) || margin >= 1) return RollVerdict.Straight;
        if (double.IsNaN(rollingShare) || rollingShare <= 0 || rollingShare >= 1) return RollVerdict.Refused;
        return margin >= rollingShare ? RollVerdict.Rolls : RollVerdict.Refused;
    }

    /// <summary>
    /// The strip it needs at this margin, in cells: none at 1 or more; the shortest roll just under 1, rising in a straight line to the full
    /// roll at the rolling share (authored: the straight line). The record's worked table: full 20, shortest 5, share 0.886 give 9 cells at
    /// 0.97, 13 at 0.94, 17 at 0.91 and 20 at the share.
    /// </summary>
    public static int StripCells(double margin, double rollingShare, int shortestCells, int fullCells)
    {
        if (double.IsNaN(margin) || margin >= 1) return 0;
        double shortest = Math.Max(1, shortestCells), full = Math.Max(shortest, fullCells);
        double span = 1 - rollingShare;
        double t = span <= 0 ? 1 : Math.Min(1, Math.Max(0, (1 - margin) / span));
        return (int)Math.Round(shortest + (full - shortest) * t);
    }

    /// <summary>
    /// The share of its present weight a refused aircraft may keep and still roll: at the rolling share it just can. For rotors the margin
    /// follows weight to the power -1.5, so (margin over share) to the 2/3; held up by thrust, the plain ratio.
    /// </summary>
    public static double WeightShareToRoll(double margin, double rollingShare, bool rotor)
    {
        if (double.IsNaN(margin) || double.IsNaN(rollingShare) || rollingShare <= 0) return double.NaN;
        double ratio = Math.Max(0, margin) / rollingShare;
        return Math.Min(1, rotor ? Math.Pow(ratio, 2.0 / 3.0) : ratio);
    }
}
