using System.Collections.Generic;
using Phobos.Radio.Services;
using Phobos.Radio.UI;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// A colonist calling another faction from a radio station's desk (Radio 0.53.0; docs/development/radio-comms-console.md): they take a
/// seat, and once seated, with a channel free and the faction still in reach, the call opens; the call holds the channel a moment after.
/// Target A is the desk; the faction is the job's comms target, as the game's console job keeps it.
/// </summary>
public class JobDriver_PhobosCallFaction : JobDriver, IRadioDeskCall
{
    /// <summary>How long the colonist stays on the line once the call opens, holding the channel (authored: a game minute or so).</summary>
    const int OnTheLineTicks = 120;

    private IntVec3 seat = IntVec3.Invalid;
    private bool calling;

    public Thing? CallingFrom => calling ? job.targetA.Thing : null;

    Faction? Faction => job.commTarget as Faction;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => RadioSeats.Claim(pawn, job, job.targetA.Thing, ref seat, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => !Phobos.Radio.Services.RadioFlare.Works(job.targetA.Thing));
        yield return RadioSeats.GoTo(job.targetA.Thing, () => seat);
        Toil call = ToilMaker.MakeToil("PhobosRadio_CallFaction");
        call.initAction = () =>
        {
            Thing desk = job.targetA.Thing;
            RadioSeats.NoteSeated(pawn, desk);
            if (!RadioNet.For(pawn.Map).DeskHasRoom(desk))
            {
                Messages.Message("PhobosRadio_DeskCallBusy".Translate(pawn.LabelShort.Named("PAWN"), desk.LabelShort.Named("DESK")).Resolve(),
                    new LookTargets(pawn), MessageTypeDefOf.RejectInput, historical: false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (Faction is not { } faction || RadioDiplomacy.Reach(pawn.Map, faction) is not { } link)
            {
                Messages.Message("PhobosRadioCall_LostTouch".Translate(pawn.LabelShort.Named("PAWN"), (Faction?.Name ?? "?").Named("FACTION")).Resolve(),
                    new LookTargets(pawn), MessageTypeDefOf.RejectInput, historical: false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            calling = true;
            RadioCallDialog.Open(pawn, faction, link);
        };
        call.tickIntervalAction = delta => RadioSeats.AtDesk(pawn, job.targetA.Thing, delta);
        call.handlingFacing = true;
        call.defaultCompleteMode = ToilCompleteMode.Delay;
        call.defaultDuration = OnTheLineTicks;
        call.AddFinishAction(() => calling = false);
        yield return call;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref seat, "PhobosRadio_Seat", IntVec3.Invalid);
        Scribe_Values.Look(ref calling, "PhobosRadio_Calling");
    }
}

/// <summary>
/// The station's right-click options for a selected colonist (Radio 0.53.0): one call for each faction its radio reaches, each refused with
/// its reason when it cannot be made now. Presentation only: reach is the service's; the order is the colonist's job.
/// </summary>
public static class CallFactionMenu
{
    public static IEnumerable<FloatMenuOption> For(Thing desk, Pawn pawn)
    {
        if (!pawn.IsColonistPlayerControlled || desk.Map != pawn.Map) yield break;
        string? refused = Refusal(desk, pawn);
        bool any = false;
        foreach (Faction faction in Find.FactionManager.AllFactionsVisibleInViewOrder)
        {
            if (faction.IsPlayer || RadioDiplomacy.Reach(desk.Map, faction) is not { } link) continue;
            any = true;
            string label = "PhobosRadioCall_MenuCall".Translate(faction.Name.Named("FACTION"), link.Settlement.LabelCap.Named("SETTLEMENT"),
                LinkText.OutcomeLabel(RadioDiplomacy.Line(link)).Named("LINE")).Resolve();
            if (refused != null)
            {
                yield return new FloatMenuOption(label + ": " + refused, null);
                continue;
            }
            Faction called = faction;
            yield return new FloatMenuOption(label, () =>
            {
                Job job = JobMaker.MakeJob(RadioDiplomacyDefOf.PhobosRadio_CallFaction, desk);
                job.commTarget = called;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
        if (!any) yield return new FloatMenuOption("PhobosRadioCall_MenuNone".Translate(), null);
    }

    /// <summary>Why this colonist cannot call from this desk now, or null.</summary>
    static string? Refusal(Thing desk, Pawn pawn)
    {
        if (!Phobos.Radio.Services.RadioFlare.Works(desk)) return "CannotUseNoPower".Translate();
        if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking)) return "PhobosRadioCall_CannotTalk".Translate(pawn.LabelShort.Named("PAWN")).Resolve();
        if (pawn.skills?.GetSkill(SkillDefOf.Social) is { TotallyDisabled: true }) return "CannotPrioritizeWorkTypeDisabled".Translate(SkillDefOf.Social.LabelCap);
        if (!RadioSeats.CanSit(pawn, desk)) return "PhobosRadioCall_NoSeat".Translate();
        if (!RadioNet.For(desk.Map).DeskHasRoom(desk)) return "PhobosRadioCall_NoChannel".Translate();
        return null;
    }
}
