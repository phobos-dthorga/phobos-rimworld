using System.Collections.Generic;
using Phobos.Radio.Services;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// A colonist talking another through their work by radio (Radio 0.23.0; docs/development/radio-advice-and-relays.md): on a hand-held where
/// they stand, or at a radio station's desk they walk to first. Target A is the person talked through (who may be away in a convoy); target
/// B the desk, when there is one. A call for a job on a map lasts until <see cref="RadioAdvice"/> ends it; a booked call (help given on the
/// road or in trade talks, which have no job of their own) lasts the Def's booked time, carried on the job's count.
/// </summary>
public class JobDriver_AdviseByRadio : JobDriver
{
    private bool talking;

    /// <summary>Whether they are at the radio and talking, not still walking to the desk.</summary>
    public bool Talking => talking;

    public bool Booked => job.def == RadioAdviceDefOf.PhobosRadio_AdviseByRadioBooked;

    public Pawn? Carer => job.targetA.Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool atDesk = job.targetB.HasThing;
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        }
        var talk = ToilMaker.MakeToil("PhobosRadio_AdviseByRadio");
        talk.initAction = () =>
        {
            talking = true;
            if (!Booked && Carer != null) RadioAdvice.Arrived(pawn, Carer);
        };
        if (atDesk)
        {
            talk.handlingFacing = true;
            talk.tickIntervalAction = _ => pawn.rotationTracker.FaceTarget(job.targetB);
        }
        if (Booked)
        {
            talk.defaultCompleteMode = ToilCompleteMode.Delay;
            talk.defaultDuration = System.Math.Max(60, job.count);
        }
        else talk.defaultCompleteMode = ToilCompleteMode.Never;
        talk.AddFinishAction(() =>
        {
            talking = false;
            if (!Booked) RadioAdvice.Stopped(pawn);
        });
        yield return talk;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref talking, "PhobosRadio_Talking");
    }
}
