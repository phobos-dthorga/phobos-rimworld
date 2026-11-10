using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>What happens as a pawn steps from one cell to the next across a protective-gear line.</summary>
public enum CrossingAction
{
    /// <summary>Nothing: the step goes ahead.</summary>
    Continue,
    /// <summary>Stop at the line and put on gear first: the pawn is stepping in without enough protection.</summary>
    SuitUp,
    /// <summary>Take off the gear put on at the line: the pawn is stepping out wearing it.</summary>
    StepOff,
}

/// <summary>
/// The rules of a protective-gear line (docs/development/radiation-control-line.md), game-free: whether a pawn is protected enough, what
/// a step across the line means, and which gear to fetch. Protection is a stat the gear raises, judged by its effect and never by its
/// name (owner rule, 5 Nov 2026): Rimatomics cuts radiation by the game's toxic environment resistance, so any gear that raises it counts.
/// </summary>
public static class ProtectionRule
{
    /// <summary>Stat values are sums of floats; a pawn exactly at the required figure counts as protected.</summary>
    public const float Tolerance = 0.0001f;

    public static bool IsProtected(float value, float required) => value >= required - Tolerance;

    /// <summary>
    /// Stepping in unprotected means suiting up; stepping out in gear put on at the line means taking it off. Every other step goes
    /// ahead: walking about inside, walking about outside, and walking in already protected by what the pawn wears anyway.
    /// </summary>
    public static CrossingAction Decide(bool fromControlled, bool toControlled, bool protectedNow, bool wearsLineGear)
    {
        if (!fromControlled && toControlled && !protectedNow) return CrossingAction.SuitUp;
        if (fromControlled && !toControlled && wearsLineGear) return CrossingAction.StepOff;
        return CrossingAction.Continue;
    }

    /// <summary>
    /// Which gear to fetch, as indices into the candidates, in the order to put it on; empty when no choice reaches the requirement.
    /// Each candidate's gain is what wearing it adds to the stat after whatever it would replace is taken off; its cost is the walk to
    /// it. Greedy: the biggest gain first, the nearer of two equal gains first, skipping any that can't be worn with one already chosen,
    /// stopping as soon as the requirement is met, so a pawn fetches as few items as it can.
    /// </summary>
    public static IReadOnlyList<int> Choose(float current, float required, IReadOnlyList<float> gains, IReadOnlyList<float> costs,
        Func<int, int, bool> wearableTogether)
    {
        if (IsProtected(current, required)) return Array.Empty<int>();
        var order = new List<int>();
        for (int i = 0; i < gains.Count; i++)
        {
            if (gains[i] > Tolerance) order.Add(i);
        }
        order.Sort((a, b) => gains[a] != gains[b] ? gains[b].CompareTo(gains[a]) : costs[a].CompareTo(costs[b]));
        var chosen = new List<int>();
        float value = current;
        foreach (int candidate in order)
        {
            bool fits = true;
            foreach (int already in chosen)
            {
                if (!wearableTogether(candidate, already)) { fits = false; break; }
            }
            if (!fits) continue;
            chosen.Add(candidate);
            value += gains[candidate];
            if (IsProtected(value, required)) return chosen;
        }
        return Array.Empty<int>();
    }
}
