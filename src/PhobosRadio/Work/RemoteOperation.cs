using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

[DefOf]
public static class RemoteOperationDefOf
{
    public static JobDef PhobosRadio_OperateStation = null!;

    static RemoteOperationDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RemoteOperationDefOf));
}

/// <summary>
/// The operator's work (Radio 0.12.0; docs/development/autonomous-vehicles.md, step 5; the owner's card of 24 Nov 2026: "a colonist works
/// the console"): a station that serves a vehicle asked to be driven from it, with nobody working it, wants a colonist beside it. The
/// "Remote operation" work type, so the player can put one colonist on it. It reads the net's last pass; nothing is worked out here.
/// </summary>
public class WorkGiver_RemoteOperation : WorkGiver_Scanner
{
    /// <summary>How long one stint at the console lasts before the colonist looks again at what else needs them (authored).</summary>
    private const int StintTicks = 2000;

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => RadioNet.For(pawn.Map).StationsWantingOperator.Count == 0;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => RadioNet.For(pawn.Map).StationsWantingOperator;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        RadioNet.For(pawn.Map).WantsOperator(t) && !t.IsForbidden(pawn) && pawn.CanReserve(t, 1, -1, null, forced) && RadioSeats.CanSit(pawn, t);

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var job = JobMaker.MakeJob(RemoteOperationDefOf.PhobosRadio_OperateStation, t);
        job.expiryInterval = StintTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }
}

/// <summary>
/// A colonist working a radio station so it can drive vehicles (Radio 0.12.0): they walk to it, stand at it facing it, and stay while it
/// serves a vehicle asked to be driven, until food, sleep or something more urgent calls them away. While they are at it the net counts
/// them as its operator, at its next pass. Holds the station, so one colonist drives from it at a time; since Radio 0.51.0 they sit in one
/// of its seats, and a terminal's other seat stays free for someone else's call.
/// </summary>
public class JobDriver_RemoteOperation : JobDriver
{
    private bool atConsole;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>Whether they have reached the station and are working it, not still walking.</summary>
    public bool AtConsole => atConsole;

    public Thing? Station => job?.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed) && RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !RadioNet.For(pawn.Map).HasVehiclesToDrive(job.targetA.Thing));
        yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        var work = ToilMaker.MakeToil("PhobosRadio_OperateStation");
        work.initAction = () =>
        {
            atConsole = true;
            RadioSeats.NoteSeated(pawn, job.targetA.Thing);
            RadioNet.For(pawn.Map).MarkDirty();
            if (Seen.Wants("radio.operator", pawn)) Seen.Note("radio.operator", pawn, "at " + job.targetA.Thing.LabelShort);
        };
        work.handlingFacing = true;
        work.tickIntervalAction = delta => RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
        work.defaultCompleteMode = ToilCompleteMode.Never;
        work.AddFinishAction(() =>
        {
            atConsole = false;
            if (pawn.Spawned) RadioNet.For(pawn.Map).MarkDirty();
        });
        yield return work;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref atConsole, "PhobosRadio_AtConsole");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
