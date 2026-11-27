using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// An hour on the radio for a neighbour who asked (Radio 0.32.0; docs/development/radio-friends-school-medevac.md): at the station's desk
/// (target A) or where the colonist stands with a hand-held, for the job's count in ticks. Done in full, the neighbour's goodwill rises.
/// </summary>
public class JobDriver_PhobosNeighbourCall : JobDriver, IRadioDeskCall
{
    private int talked;
    private bool talking;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>Radio 0.51.0: talking at a desk takes a channel there.</summary>
    public Thing? CallingFrom => talking && job.targetA.HasThing ? job.targetA.Thing : null;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !job.targetA.HasThing || RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool atDesk = job.targetA.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        }
        else this.FailOn(() => pawn.IsHashIntervalTick(250) && !RadioAdvice.InTouch(pawn));
        Toil talk = ToilMaker.MakeToil("PhobosRadio_NeighbourCall");
        talk.initAction = () =>
        {
            talking = true;
            if (atDesk) RadioSeats.NoteSeated(pawn, job.targetA.Thing);
        };
        talk.tickIntervalAction = delta =>
        {
            if (atDesk && RadioSeats.HungUpBusy(this, pawn, job.targetA.Thing, delta)) return;
            if (atDesk) RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
            talked += delta;
            RadioNeighbours.Talking(pawn, delta);
        };
        talk.handlingFacing = atDesk;
        talk.defaultCompleteMode = ToilCompleteMode.Delay;
        talk.defaultDuration = System.Math.Max(60, job.count);
        talk.socialMode = RandomSocialMode.Off;
        talk.AddFinishAction(() =>
        {
            talking = false;
            RadioNeighbours.Ended(pawn, done: talked >= job.count - 1);
        });
        yield return talk;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref talked, "PhobosRadio_Talked");
        Scribe_Values.Look(ref talking, "PhobosRadio_Talking");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
