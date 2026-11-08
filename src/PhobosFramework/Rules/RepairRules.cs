using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a share of a repair's full cost comes to, game-free. The player sets the shares (vehicle health, slice 3): a share of 0 costs
/// nothing, and anything above 0 costs at least one of each thing, so a small part is never free by rounding.
/// </summary>
public static class CostShareRule
{
    /// <summary>From this share up, the whole part is asked for instead of a share of its materials.</summary>
    public const double WholeShare = 0.995;

    public static bool WantsWhole(double share) => share >= WholeShare;

    public static int Count(int full, double share)
    {
        if (full <= 0 || share <= 0) return 0;
        return Math.Max(1, (int)Math.Ceiling(full * share - 1e-9));
    }

    /// <summary>A cost that follows a part's size: so much per point of its health, then the share.</summary>
    public static int PerHealth(double perHealth, double health, double share)
    {
        double full = perHealth * health * share;
        if (full <= 0) return 0;
        return Math.Max(1, (int)Math.Ceiling(full - 1e-9));
    }
}

/// <summary>How long until a quantity reaches a level at the rate it is moving, game-free: an estimate, for a danger clock.</summary>
public static class DangerClockRule
{
    public const double Never = double.PositiveInfinity;

    /// <summary>Hours until value reaches target at perHour; <see cref="Never"/> when it is not heading there.</summary>
    public static double Hours(double value, double target, double perHour)
    {
        if (perHour == 0 || value == target) return Never;
        double hours = (target - value) / perHour;
        return hours > 0 ? hours : Never;
    }

    /// <summary>The rate per hour between two samples taken the given hours apart.</summary>
    public static double Rate(double older, double newer, double hoursApart) => hoursApart > 0 ? (newer - older) / hoursApart : 0;
}
