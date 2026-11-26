using System.Collections.Generic;
using Phobos.Dosing.Defs;
using Phobos.Dosing.Services;
using Phobos.Framework.Health;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Dosing.Jobs;

/// <summary>
/// Doctor work: give a resting patient the best dose within reach for whichever care need they have (pain, a disease that is winning, a
/// condition an item cures, an outbreak they are unprotected against). Reads the per-map patient cache and
/// delegates the decision to the service; it mutates nothing itself. The cache lists only patients the rules would let someone dose now,
/// so a ward full of patients whose last dose is still working costs a doctor looking for work nothing; a right-click reads the patient
/// directly and still says why.
/// </summary>
public class WorkGiver_PhobosDose : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        // A forced check is the doctor's right-click: the game offers nothing at all when this skips, so the cached list (up to
        // 250 ticks old) must not hide a patient or their refusal there.
        !DosingMod.Settings.enabled || !DoseIndex.Any || pawn.Map == null || (!forced && PatientCache.Patients(pawn.Map).Count == 0);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => PatientCache.Patients(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient || patient == pawn) return false;
        if (!DoseService.IsPatient(patient, DosingMod.Settings, PhobosDosingRulesDef.Current))
        {
            if (forced) DoseService.ExplainNotPatient(patient, DosingMod.Settings, PhobosDosingRulesDef.Current);
            return false;
        }
        if (!FieldCare.CanReserve(pawn, patient, forced)) return false;
        return DoseService.MakeJob(pawn, patient, explain: forced) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) => t is Pawn patient ? DoseService.MakeJob(pawn, patient, explain: forced) : null;
}
