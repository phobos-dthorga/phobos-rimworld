using System.Collections.Generic;
using Phobos.Medical.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Jobs;

/// <summary>
/// Doctor work: use a hand-held device (a field oxygen kit, a resuscitator) on a patient who needs it. Reads the per-map patient
/// cache and delegates every decision to <see cref="DeviceService"/>; it changes nothing itself.
/// </summary>
public class WorkGiver_PhobosUseDevice : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    // A forced check is the doctor's right-click: the game offers nothing at all when this skips, so the cached list (a few seconds
    // old) must not hide a patient or the reason they get nothing.
    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        DeviceService.Devices.Count == 0 || pawn.Map == null || (!forced && DeviceService.PatientsNeedingDevices(pawn.Map).Count == 0);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => DeviceService.PatientsNeedingDevices(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient || patient == pawn) return false;
        if (!pawn.CanReserve(patient, 1, -1, null, forced)) return false;
        return DeviceService.MakeJob(pawn, patient, explain: forced) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        t is Pawn patient ? DeviceService.MakeJob(pawn, patient) : null;
}
