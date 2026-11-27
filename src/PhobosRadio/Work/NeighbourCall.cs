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
public class JobDriver_PhobosNeighbourCall : JobDriver
{
    private int talked;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool atDesk = job.targetA.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        }
        else this.FailOn(() => pawn.IsHashIntervalTick(250) && !RadioAdvice.InTouch(pawn));
        Toil talk = ToilMaker.MakeToil("PhobosRadio_NeighbourCall");
        talk.tickIntervalAction = delta =>
        {
            if (atDesk) pawn.rotationTracker.FaceTarget(job.targetA);
            talked += delta;
            RadioNeighbours.Talking(pawn, delta);
        };
        talk.handlingFacing = atDesk;
        talk.defaultCompleteMode = ToilCompleteMode.Delay;
        talk.defaultDuration = System.Math.Max(60, job.count);
        talk.socialMode = RandomSocialMode.Off;
        talk.AddFinishAction(() => RadioNeighbours.Ended(pawn, done: talked >= job.count - 1));
        yield return talk;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref talked, "PhobosRadio_Talked");
    }
}
