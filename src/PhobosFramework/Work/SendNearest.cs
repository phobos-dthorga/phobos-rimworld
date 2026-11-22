using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Framework.Work;

/// <summary>
/// "Send someone": a button that needs a person sends the nearest of the player's colonists who can do it, instead of asking the player
/// to select one and right-click (Framework 0.241.0; the owner's request of 23 Nov 2026, first for a vehicle's extinguisher: one button,
/// keyboard-friendly, with the right-click kept for choosing who goes). Any button of ours whose work needs a person may use it.
/// </summary>
public static class SendNearest
{
    /// <summary>
    /// The player's colonist on the target's map nearest to it who is free to go: spawned, not downed, not in a mental state, not drafted
    /// unless <paramref name="draftedToo"/>, able by <paramref name="able"/>, and able to reach it. Null when there is none.
    /// </summary>
    public static Pawn? Find(Thing target, Predicate<Pawn> able, PathEndMode endMode = PathEndMode.Touch, bool draftedToo = false)
    {
        Map? map = target?.MapHeld;
        if (map == null) return null;
        Pawn? best = null;
        float bestDistance = float.MaxValue;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.Downed || pawn.InMentalState || (!draftedToo && pawn.Drafted) || !able(pawn)) continue;
            float distance = pawn.Position.DistanceToSquared(target!.PositionHeld);
            if (distance >= bestDistance || !pawn.CanReach(target, endMode, Danger.Deadly)) continue;
            best = pawn;
            bestDistance = distance;
        }
        return best;
    }

    /// <summary>Gives the colonist the job as if the player had ordered it, putting aside what they were doing. Says whether it was taken.</summary>
    public static bool Order(Pawn pawn, Job job) => pawn?.jobs != null && pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
}
