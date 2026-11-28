using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

[DefOf]
public static class RadioWatchDefOf
{
    public static JobDef PhobosRadio_RadioWatch = null!;

    static RadioWatchDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadioWatchDefOf));
}

/// <summary>
/// Radio watch (Radio 0.73.0; docs/development/radio-life-on-the-air.md, RN4): a colonist with the "Radio watch" work keeps watch at a
/// working station's desk when nobody else on the map is listening, for the rules' stint at a time. Reads the kept answer to who is
/// listening; nothing is worked out here.
/// </summary>
public class WorkGiver_RadioWatch : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Undefined);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !pawn.Spawned || !RadioWatch.WantsWatcher(pawn.Map, pawn);

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => RadioNet.For(pawn.Map).Stations.Select(station => (Thing)station.parent);

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) =>
        t.Spawned && RadioFlare.Works(t) && !t.IsForbidden(pawn) && !RadioNet.For(pawn.Map).WantsOperator(t) && RadioSeats.CanSit(pawn, t);

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        Job job = JobMaker.MakeJob(RadioWatchDefOf.PhobosRadio_RadioWatch, t);
        job.expiryInterval = RadioWatch.StintTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }
}

/// <summary>
/// Keeping radio watch at a desk (Radio 0.73.0): the colonist sits at a seat and listens, counted as listening while there. They leave
/// when someone else is listening again (looked at every 250 ticks, from the kept answer) or when a vehicle needs the console. Listening
/// takes no channel. Holds a seat, not the station, so others may still call from a terminal's other seat.
/// </summary>
public class JobDriver_PhobosRadioWatch : JobDriver
{
    private const int LookTicks = 250;
    private bool atDesk;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>Whether they have reached the desk and are listening, not still walking.</summary>
    public bool AtDesk => atDesk;

    public Thing? Station => job?.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    /// <summary>A watch begun: the next look counts this colonist as on watch, so a second colonist does not set off too.</summary>
    public override void Notify_Starting()
    {
        base.Notify_Starting();
        RadioWatch.MarkDirty(pawn.Map);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !RadioFlare.Works(job.targetA.Thing));
        yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        Toil watch = ToilMaker.MakeToil("PhobosRadio_RadioWatch");
        watch.initAction = () =>
        {
            atDesk = true;
            RadioSeats.NoteSeated(pawn, job.targetA.Thing);
            RadioWatch.MarkDirty(pawn.Map);
            if (Seen.Wants("radio.watch-on", pawn)) Seen.Note("radio.watch-on", pawn, job.targetA.Thing.LabelShort);
        };
        watch.tickIntervalAction = delta =>
        {
            Thing desk = job.targetA.Thing;
            RadioSeats.AtDesk(pawn, desk, delta);
            if (!pawn.IsHashIntervalTick(LookTicks, delta)) return;
            if (RadioNet.For(pawn.Map).WantsOperator(desk) || !RadioWatch.WantsWatcher(pawn.Map, pawn)) EndJobWith(JobCondition.Succeeded);
        };
        watch.handlingFacing = true;
        watch.defaultCompleteMode = ToilCompleteMode.Never;
        watch.socialMode = RandomSocialMode.Normal;
        watch.AddFinishAction(() =>
        {
            atDesk = false;
            if (pawn.Spawned) RadioWatch.MarkDirty(pawn.Map);
        });
        yield return watch;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref atDesk, "PhobosRadio_AtDesk");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
