namespace Phobos.Framework.Rules;

/// <summary>
/// What supplied oxygen does to a poison that clears through the lungs. Game-free. A half-life is a property of what the pawn
/// breathes, so the shortest one that applies wins; a treatment never slows clearance down.
/// </summary>
public static class BreathingRule
{
    /// <summary>The half-life in hours, from the air around the pawn and, if any, the oxygen being supplied.</summary>
    public static float ClearanceHalfLifeHours(float fromSurroundings, float? fromSuppliedOxygen)
    {
        if (fromSuppliedOxygen is not { } supplied || supplied <= 0f) return fromSurroundings;
        return supplied < fromSurroundings ? supplied : fromSurroundings;
    }

    /// <summary>The better of two supplied half-lives, either of which may be absent.</summary>
    public static float? Shortest(float? current, float candidate)
    {
        if (candidate <= 0f) return current;
        return current is { } held && held <= candidate ? held : candidate;
    }
}
