using System.Collections.Generic;
using Phobos.Radio.Services;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.Radio.Work;

/// <summary>
/// Taking cover after a sighting on the radio (Radio 0.76.0; docs/development/radio-life-on-the-air.md, RN17): a colonist who does not fight
/// walks to the room indoors the sighting's look found and stays the rules' while, or until no threat is left on the map (looked at every 250
/// ticks). Afterwards they go back to whatever the game gives them next.
/// </summary>
public class JobDriver_PhobosTakeCover : JobDriver
{
    private const int LookTicks = 250;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
        Toil wait = ToilMaker.MakeToil("PhobosRadio_TakeCover");
        wait.initAction = () =>
        {
            if (Seen.Wants("radio.take-cover-arrived", pawn)) Seen.Note("radio.take-cover-arrived", pawn);
        };
        wait.tickIntervalAction = delta =>
        {
            if (pawn.IsHashIntervalTick(LookTicks, delta) && !GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map)) EndJobWith(JobCondition.Succeeded);
        };
        wait.defaultCompleteMode = ToilCompleteMode.Delay;
        wait.defaultDuration = RadioRulesDef.Current.coverTicks;
        wait.handlingFacing = false;
        wait.socialMode = RandomSocialMode.Quiet;
        yield return wait;
    }
}
