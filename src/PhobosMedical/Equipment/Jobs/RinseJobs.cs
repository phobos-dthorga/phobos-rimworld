using System.Collections.Generic;
using Phobos.Medical.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Medical.Jobs;

/// <summary>
/// Anyone who can walk, with acid on their skin, runs to the nearest safety shower. Inserted by XML into the game's constant think tree for
/// people, after moving away from heat (it interrupts work; the game's gate skips drafted, downed, burning, asleep and broken pawns).
/// </summary>
public class JobGiver_PhobosRinseSelf : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn) => RinseService.SelfRinseJob(pawn);
}

/// <summary>Stand under the shower and rinse for its full time. Target A is the station.</summary>
public class JobDriver_PhobosRinse : JobDriver
{
    Thing Station => job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Station, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
        yield return Toils_General.Do(() =>
        {
            if (!RinseService.TryDrawWater(Station, pawn)) EndJobWith(JobCondition.Incompletable);
        });
        yield return Toils_General.Wait(RinseService.RinseTicks(Station)).WithProgressBarToilDelay(TargetIndex.A);
        yield return Toils_General.Do(() => RinseService.Finish(pawn));
    }
}

/// <summary>Doctor work: draw water at a safety shower and rinse the acid off a patient who can't walk. All decisions are <see cref="RinseService"/>'s.</summary>
public class WorkGiver_PhobosRinsePatient : WorkGiver_Scanner
{
    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);
    public override PathEndMode PathEndMode => PathEndMode.Touch;
    public override Danger MaxPathDanger(Pawn pawn) => Danger.Deadly;

    public override bool ShouldSkip(Pawn pawn, bool forced = false) =>
        pawn.Map == null || RinseService.StationDefs.Count == 0 || (!forced && RinseService.PatientsNeedingRinse(pawn.Map).Count == 0);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => RinseService.PatientsNeedingRinse(pawn.Map);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) => JobOnThing(pawn, t, forced) != null;

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn patient) return null;
        Job? job = RinseService.RinsePatientJob(pawn, patient);
        if (job == null && forced && patient != pawn && RinseService.IsPatientNeedingRinse(patient) && pawn.CanReserve(patient))
        {
            JobFailReason.Is("PhobosMedical_NoRinseStation".Translate());
        }
        return job;
    }
}

/// <summary>Target A is the station, B the patient: draw water at the station, carry it over and rinse.</summary>
public class JobDriver_PhobosRinsePatient : JobDriver
{
    Thing Station => job.targetA.Thing;
    Pawn Patient => job.targetB.Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Station, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        this.FailOn(() => Patient.Dead || !RinseService.NeedsRinse(Patient));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Do(() =>
        {
            if (!RinseService.TryDrawWater(Station, Patient)) EndJobWith(JobCondition.Incompletable);
        });
        yield return Toils_General.Wait(RinseService.FillTicks(Station), TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        Toil rinse = Toils_General.Wait(RinseService.RinseTicks(Station), TargetIndex.B).WithProgressBarToilDelay(TargetIndex.B);
        rinse.activeSkill = () => SkillDefOf.Medicine;
        yield return rinse;
        yield return Toils_General.Do(() => RinseService.Finish(Patient));
    }
}
