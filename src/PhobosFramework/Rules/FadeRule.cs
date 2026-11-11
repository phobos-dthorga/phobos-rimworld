namespace Phobos.Framework.Rules;

/// <summary>
/// How strongly a picture that comes and goes with a building's state shows, 0 to 1 (owner, 12 Nov 2026: the liquid oxygen store's frost,
/// "faded in and out by code"). Game-free. It builds at one rate while wanted and fades at another, so frost can gather over an hour and
/// melt over three; a time of zero or less jumps straight there.
/// </summary>
public static class FadeRule
{
    /// <summary>The new strength after <paramref name="ticks"/> ticks, moving towards 1 when wanted and towards 0 when not.</summary>
    public static float Step(float level, bool wanted, float ticks, float buildTicks, float fadeTicks)
    {
        float target = wanted ? 1f : 0f;
        float span = wanted ? buildTicks : fadeTicks;
        if (span <= 0f) return target;
        float change = ticks / span;
        float next = wanted ? level + change : level - change;
        return next < 0f ? 0f : next > 1f ? 1f : next;
    }
}
