using System.Collections.Generic;
using Phobos.Framework.Health;
using Phobos.Medical.Defs;
using Phobos.Medical.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Jobs;

/// <summary>
/// Doctor work: use a hand-held device (a field oxygen kit, a resuscitator) on a patient who needs it. Reads the per-map patient
/// cache and delegates every decision to <see cref="DeviceService"/>; it changes nothing itself. A <see cref="PhobosDeviceWorkExtension"/>
/// on the WorkGiverDef makes it one lane: some devices only, or a lane in another mod's work type (Search and Rescue's field rescue).
/// </summary>
public class WorkGiver_PhobosUseDevice : WorkGiver_Scanner
{
    PhobosDeviceWorkExtension? Lane => def?.GetModExtension<PhobosDeviceWorkExtension>();

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    // A forced check is the doctor's right-click: the game offers nothing at all when this skips, so the cached list (a few seconds
    // old) must not hide a patient or the reason they get nothing.
    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        DeviceService.Devices.Count == 0 || pawn.Map == null || !DeviceService.MayWorkLane(pawn, Lane)
        || (!forced && DeviceService.PatientsNeedingDevices(pawn.Map).Count == 0);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => DeviceService.PatientsNeedingDevices(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient || patient == pawn) return false;
        if (!FieldCare.CanReserve(pawn, patient, forced)) return false;
        return DeviceService.MakeJob(pawn, patient, explain: forced, lane: Lane?.needs) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        t is Pawn patient ? DeviceService.MakeJob(pawn, patient, lane: Lane?.needs) : null;
}
