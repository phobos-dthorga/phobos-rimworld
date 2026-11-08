namespace Phobos.Framework.Rules;

/// <summary>
/// What a healed condition leaves behind (a burned eye's scarring), game-free: nothing below a threshold of the worst it ever reached,
/// then a lasting severity that rises from <see cref="Faintest"/> at the threshold to 1 at full severity.
/// </summary>
public static class LastingRule
{
    /// <summary>The least a lasting condition is given when it is given at all (authored): a faint trace.</summary>
    public const double Faintest = 0.1;

    public static double SeverityFor(double peak, double from)
    {
        if (peak < from) return 0;
        if (from >= 1) return 1;
        double share = (peak - from) / (1 - from);
        double severity = Faintest + (1 - Faintest) * share;
        return severity > 1 ? 1 : severity;
    }
}
