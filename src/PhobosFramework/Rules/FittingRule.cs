using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a fitting on one part of several is worth, and who may do the work, game-free (vehicle tuning and upgrades; agent rule). Each of
/// a role's parts gives its root of the listed factor, so fitting all of them gives exactly that factor whatever the number of parts.
/// </summary>
public static class FittingRule
{
    /// <summary>The factor one fitted part gives, among this many parts of its role.</summary>
    public static double PartShare(double factor, int partsOfRole)
    {
        if (factor <= 0) return 0;
        return partsOfRole <= 1 ? factor : Math.Pow(factor, 1.0 / partsOfRole);
    }

    /// <summary>A mechanic at or above the job's minimum skill always succeeds; one below it may not try (owner: no random failure).</summary>
    public static bool SkilledEnough(int skill, int minimum) => skill >= minimum;

    /// <summary>The fuel a tuned engine burns per hour beyond what Vehicle Framework already burned at this share of its rate.</summary>
    public static double TunedFuelShare(double modelShare, double factor) => modelShare * factor;
}
