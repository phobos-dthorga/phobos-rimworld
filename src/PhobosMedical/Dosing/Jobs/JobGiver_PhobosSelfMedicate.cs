using Phobos.Dosing.Services;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Jobs;

/// <summary>
/// A pawn in serious pain goes and takes a painkiller, through the game's own ingest job. Sits at the Humanlike_PreMain think-tree hook,
/// after mental states, lord duties and emergency work and before needs, work and joy, so it runs for every humanlike choosing a job;
/// the service's first tests reject almost all of them at once. It decides nothing itself.
/// </summary>
public class JobGiver_PhobosSelfMedicate : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn) => DoseService.SelfJob(pawn);
}
