using System.Collections.Generic;
using Phobos.Medical.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Jobs;

/// <summary>
/// Doctor work, just above the game's own rescue: carry a downed patient who needs oxygen to a free pressure chamber instead of the
/// nearest bed. Delegates every decision to <see cref="ChamberService"/>; when no chamber is free the game's rescue goes on as usual.
/// </summary>
public class WorkGiver_PhobosTakeToChamber : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.OnCell;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        pawn.Map == null || (!forced && ChamberService.PatientsForChamber(pawn.Map).Count == 0);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ChamberService.PatientsForChamber(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient || patient == pawn) return false;
        if (!pawn.CanReserve(patient, 1, -1, null, forced)) return false;
        return ChamberService.MakeJob(pawn, patient) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        t is Pawn patient ? ChamberService.MakeJob(pawn, patient) : null;
}
