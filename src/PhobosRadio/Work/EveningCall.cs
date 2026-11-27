using System.Collections.Generic;
using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// An evening call to someone close who is away (Radio 0.63.0; docs/development/radio-life-on-the-air.md, RN8): at a station's desk (target A)
/// or where the colonist stands with a hand-held in touch, for the job's count in ticks. Whom they call is handed over as the job starts
/// (<see cref="Pending"/>) and saved with the job. A desk with no channel for the call hangs up at once, and the caller could not get through.
/// </summary>
public class JobDriver_PhobosEveningCall : JobDriver, IRadioDeskCall
{
    /// <summary>Whom the next call is to, set by the talk pass just around starting the job; read once, as the job starts.</summary>
    internal static (Pawn Away, int Tie, bool Faint)? Pending;

    private Pawn? away;
    private int tie = (int)TalkRule.Tie.Friend;
    private bool faint;
    private int talked;
    private bool talking;
    private IntVec3 seat = IntVec3.Invalid;

    /// <summary>Talking at a desk takes a channel there, as every call at a desk does (Radio 0.51.0).</summary>
    public Thing? CallingFrom => talking && job.targetA.HasThing ? job.targetA.Thing : null;

    public override void Notify_Starting()
    {
        base.Notify_Starting();
        if (Pending is not { } pending) return;
        away = pending.Away;
        tie = pending.Tie;
        faint = pending.Faint;
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        !job.targetA.HasThing || RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    public override string GetReport() => "PhobosRadioTalk_EveningCallReport".Translate((away?.LabelShort ?? "?").Named("AWAY")).Resolve();

    protected override IEnumerable<Toil> MakeNewToils()
    {
        bool atDesk = job.targetA.HasThing;
        this.FailOn(() => away == null || away.Dead || away.Destroyed);
        if (atDesk)
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        }
        else this.FailOn(() => pawn.IsHashIntervalTick(250) && !RadioAdvice.InTouch(pawn));
        Toil talk = ToilMaker.MakeToil("PhobosRadio_EveningCall");
        talk.initAction = () =>
        {
            talking = true;
            if (atDesk) RadioSeats.NoteSeated(pawn, job.targetA.Thing);
            if (away != null) RadioTalk.CallBegan(pawn, away, faint, atDesk ? job.targetA.Thing : null);
        };
        talk.tickIntervalAction = delta =>
        {
            if (atDesk && RadioSeats.HungUpBusy(this, pawn, job.targetA.Thing, delta)) return;
            if (atDesk) RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
            talked += delta;
        };
        talk.handlingFacing = atDesk;
        talk.defaultCompleteMode = ToilCompleteMode.Delay;
        talk.defaultDuration = System.Math.Max(60, job.count);
        talk.socialMode = RandomSocialMode.Off;
        talk.AddFinishAction(() =>
        {
            if (!talking) return;
            talking = false;
            RadioTalk.CallEnded(pawn, away, (TalkRule.Tie)tie, talked, System.Math.Max(60, job.count), faint);
        });
        yield return talk;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref away, "PhobosRadio_Away");
        Scribe_Values.Look(ref tie, "PhobosRadio_Tie", (int)TalkRule.Tie.Friend);
        Scribe_Values.Look(ref faint, "PhobosRadio_Faint");
        Scribe_Values.Look(ref talked, "PhobosRadio_Talked");
        Scribe_Values.Look(ref talking, "PhobosRadio_Talking");
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
    }
}
