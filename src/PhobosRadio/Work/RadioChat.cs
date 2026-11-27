using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// Chatting with a radio friend as recreation (Radio 0.29.0; docs/development/radio-friends-school-medevac.md). Target A is the station's
/// desk when the colonist has no hand-held in touch; target C their bed when they chat lying down, as the game's in-bed television does.
/// The friendship grows each tick; the mood lift and the goodwill come when the chat ends. It ends when recreation is full, after the
/// job's time, or when the radio loses touch.
/// </summary>
public class JobDriver_PhobosRadioChat : JobDriver
{
    private int chatted;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    public override string GetReport()
    {
        string friend = RadioFriends.Of(pawn)?.Friend?.LabelShort ?? "?";
        return "PhobosRadioFriend_Report".Translate(friend.Named("FRIEND")).Resolve();
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        Toil chat;
        if (job.targetC.Thing is Building_Bed)
        {
            this.KeepLyingDown(TargetIndex.C);
            yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.C);
            yield return Toils_Bed.GotoBed(TargetIndex.C);
            chat = Toils_LayDown.LayDown(TargetIndex.C, hasBed: true, lookForOtherJobs: false);
            chat.AddFailCondition(() => !chat.actor.Awake());
        }
        else
        {
            if (job.targetA.HasThing)
            {
                this.FailOnDespawnedOrNull(TargetIndex.A);
                yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            }
            chat = ToilMaker.MakeToil("PhobosRadio_RadioChat");
            if (job.targetA.HasThing)
            {
                chat.handlingFacing = true;
                chat.AddPreTickIntervalAction(_ => pawn.rotationTracker.FaceTarget(job.targetA));
            }
        }
        chat.AddPreTickIntervalAction(delta =>
        {
            chatted += delta;
            RadioFriends.Chatting(pawn, delta);
            // Out of touch (a hand-held walked out of reach, a desk lost power): the chat ends, counted so far.
            if (pawn.IsHashIntervalTick(250, delta) && !InTouch())
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob);
        });
        chat.AddFinishAction(() =>
        {
            RadioFriends.Chatted(pawn, chatted);
            chatted = 0;
        });
        chat.defaultCompleteMode = ToilCompleteMode.Delay;
        chat.defaultDuration = job.def.joyDuration;
        chat.socialMode = RandomSocialMode.Off;
        yield return chat;
    }

    private bool InTouch() =>
        job.targetA.Thing is { } desk ? desk.Spawned && desk.TryGetComp<CompPowerTrader>() is not { PowerOn: false } : RadioAdvice.InTouch(pawn);

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref chatted, "PhobosRadio_Chatted");
    }
}
