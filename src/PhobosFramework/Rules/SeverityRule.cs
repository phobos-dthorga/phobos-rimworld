namespace Phobos.Framework.Rules;

/// <summary>
/// A condition's severity, game-free: a share from 0 to 1 that hits and causes add to, that may fade with time, and that a repair
/// clears. Vehicle conditions use it (docs/development/vehicle-health-design.md, slice 2); nothing in it names a vehicle.
/// </summary>
public static class SeverityRule
{
    /// <summary>The share of full health at which a part counts as fully repaired. Vehicle Framework rounds a part's health share to
    /// hundredths and never repairs one above 99.5% (read from its source), so a repair can stop just short of full.</summary>
    public const double RepairedShare = 0.995;

    /// <summary>Below this a condition is gone: what fading leaves at the end is too slight to show or to matter.</summary>
    public const double Gone = 0.001;

    /// <summary>The severity after something adds to it and the hours pass: adds first, then fades, held between 0 and 1.</summary>
    public static double Next(double severity, double added, double fadePerHour, double hours)
    {
        double next = severity + (added > 0 ? added : 0);
        if (fadePerHour > 0 && hours > 0) next -= fadePerHour * hours;
        if (next < 0) return 0;
        return next > 1 ? 1 : next;
    }

    /// <summary>What a hit adds: so much severity for each point of health the part lost (a part that lost nothing adds nothing).</summary>
    public static double FromHit(double perHealthLost, double healthLost) =>
        perHealthLost > 0 && healthLost > 0 ? perHealthLost * healthLost : 0;

    /// <summary>Whether a repair clears the condition: the part's health rose and now counts as full. A condition on a part that
    /// was already full (one a cause gave it, not a hit) is not cleared by standing there.</summary>
    public static bool ClearedByRepair(bool clearsOnRepair, double healthBefore, double healthAfter, double maxHealth) =>
        clearsOnRepair && maxHealth > 0 && healthAfter > healthBefore && healthAfter / maxHealth >= RepairedShare;

    public static bool IsGone(double severity) => severity < Gone;

    /// <summary>What a field fix leaves of a condition: half, enough to bring most of them down a stage. It never clears one.</summary>
    public static double FieldFixed(double severity) => severity * 0.5;
}
