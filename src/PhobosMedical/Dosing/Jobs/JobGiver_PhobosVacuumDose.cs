using Phobos.Dosing.Services;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Jobs;

/// <summary>
/// Someone caught in vacuum takes a vacuum-resisting dose they carry (Medical 0.30.0). Inserted into the game's constant think tree just
/// ahead of its search for air (JobGiver_FindOxygen), by an XML patch that does nothing without Odyssey: the constant tree interrupts
/// whatever the person is doing, and the search for air would otherwise always come first. It decides nothing itself.
/// </summary>
public class JobGiver_PhobosVacuumDose : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn) => ExposureService.VacuumJob(pawn);
}
