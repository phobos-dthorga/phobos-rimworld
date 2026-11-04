using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Jobs;

/// <summary>
/// Doctor work: give a resting patient in severe pain the best dose within reach. Reads the per-map patient cache and
/// delegates the decision to the service; it mutates nothing itself.
/// </summary>
public class WorkGiver_PhobosDose : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        !DosingMod.Settings.enabled || DoseIndex.Options.Count == 0 || pawn.Map == null || PatientCache.Patients(pawn.Map).Count == 0;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => PatientCache.Patients(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient || patient == pawn) return false;
        if (!DoseService.IsPatient(patient, DosingMod.Settings, PhobosDosingRulesDef.Current)) return false;
        if (!pawn.CanReserve(patient, 1, -1, null, forced)) return false;
        return DoseService.MakeJob(pawn, patient) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) => t is Pawn patient ? DoseService.MakeJob(pawn, patient) : null;
}
