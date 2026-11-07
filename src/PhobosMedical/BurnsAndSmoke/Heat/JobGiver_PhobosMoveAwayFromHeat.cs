using Phobos.BurnsAndSmoke.Defs;
using Phobos.BurnsAndSmoke.Services;
using Verse;
using Verse.AI;

namespace Phobos.BurnsAndSmoke.Heat;

/// <summary>
/// Sends a pawn out of air hot enough to burn them, at a sprint. Inserted by XML into the game's constant think trees for people and
/// animals, beside its own "flee an explosion" and "flee danger" (the constant trees run every 30 ticks and interrupt the current job;
/// they have no insert tags, so a patch adds it there). The game's own gate already skips drafted, downed, burning, sleeping and
/// mentally broken pawns. Decisions live in <see cref="HotCellService"/>.
/// </summary>
public class JobGiver_PhobosMoveAwayFromHeat : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!HotCellService.ShouldMoveAway(pawn) || !HotCellService.TryFindCoolCell(pawn, out IntVec3 cell)) return null;
        Job job = JobMaker.MakeJob(BurnsDefOf.PhobosBurns_MoveAwayFromHeat, cell);
        job.locomotionUrgency = LocomotionUrgency.Sprint;
        return job;
    }
}
