using System.Collections.Generic;
using Phobos.Framework.Health;
using Phobos.Radiation.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radiation.Jobs;

/// <summary>
/// Doctor work: give a dosed person the radiation medicine they need (D'thorga Medical 0.24.0, RM5). Delegates the choice to
/// <see cref="RadiationMedicine"/>; mutates nothing itself.
/// </summary>
public class WorkGiver_PhobosRadiationMedicine : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !RadiationPart.On || pawn.Map == null;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => RadiationMedicine.Patients(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => t is Pawn patient && RadiationMedicine.MakeJob(pawn, patient, forced) != null;

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false) => t is Pawn patient ? RadiationMedicine.MakeJob(pawn, patient, forced) : null;
}

/// <summary>
/// Carry one radiation medicine to the patient, who takes it: the game's own feed-a-patient toils, as the dosing part's job, so the
/// item's outcomes apply as its Def says. Target A is the item, B the patient. Gives up once the patient no longer needs it.
/// </summary>
public class JobDriver_PhobosRadiationMedicine : JobDriver
{
    const TargetIndex ItemInd = TargetIndex.A;
    const TargetIndex PatientInd = TargetIndex.B;

    Thing Item => job.targetA.Thing;
    Pawn Patient => job.targetB.Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!FieldCare.Reserve(pawn, Patient, job, errorOnFailed)) return false;
        if (!pawn.Reserve(Item, job, 10, 1, null, errorOnFailed)) return false;
        job.count = 1;
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(PatientInd);
        this.FailOn(() => Patient.Dead || RadiationMedicine.Needed(Patient) != Item.def);
        yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch).FailOnForbidden(ItemInd);
        yield return Toils_Ingest.PickupIngestible(ItemInd, Patient);
        yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.Touch);
        yield return Toils_Ingest.ChewIngestible(Patient, 1f, ItemInd).FailOnCannotTouch(PatientInd, PathEndMode.Touch);
        yield return Toils_Ingest.FinalizeIngest(Patient, ItemInd);
    }
}
