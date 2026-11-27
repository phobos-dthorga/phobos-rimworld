using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// What priority a work type new to a save starts at for one pawn, game-free (Framework 0.354.0; docs/development/rearm-and-refuel-work.md).
/// A work type a mod adds part-way through a game starts switched off for everyone already in the colony, and with dozens of colonists
/// switching it on by hand is a chore. So it starts at the priority the pawn has for the first of its listed source work types that the
/// pawn can do at all: a colonist's Hauling, say, or a construction drone's Construction when it cannot haul. A source the pawn has
/// switched off gives 0, so a choice the player made (no hauling for this one) carries over instead of being skipped past.
/// </summary>
public static class WorkTypeSeedRule
{
    /// <summary>
    /// The starting priority: that of the first source the pawn is able to do, in the order listed; 0 (off) when it can do none, when
    /// its first able source is off, or when there are no sources.
    /// </summary>
    public static int StartingPriority(IEnumerable<(bool able, int priority)> sources)
    {
        foreach ((bool able, int priority) in sources)
        {
            if (able) return priority > 0 ? priority : 0;
        }
        return 0;
    }
}
