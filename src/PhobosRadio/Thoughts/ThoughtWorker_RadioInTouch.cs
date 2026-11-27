using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>"In touch with home": reads the net's cache, or since Radio 0.34.0 a cross-band vehicle's kept answer; never computes a link itself.</summary>
public class ThoughtWorker_RadioInTouch : ThoughtWorker
{
    private const int StageInTouch = 0;
    private const int StageFaint = 1;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!RadioMod.Settings.inTouchThought || !p.Spawned || p.Map == null) return ThoughtState.Inactive;
        var net = RadioNet.For(p.Map);
        if (!net.TryGetBest(p, out var link) || link == null)
        {
            // Radio 0.34.0: no station of theirs serves them here (a camp, a site), but a vehicle's dual-band radio may: its kept answer.
            if (net.ServedBy(p) != null || AwayReach.KeptMarginDb(p) is not { } margin) return ThoughtState.Inactive;
            return RadioRulesDef.Current.MarginBands.Classify(margin) switch
            {
                LinkOutcome.Clean or LinkOutcome.Workable => ThoughtState.ActiveAtStage(StageInTouch),
                LinkOutcome.Marginal => ThoughtState.ActiveAtStage(StageFaint),
                _ => ThoughtState.Inactive,
            };
        }
        return link.Outcome switch
        {
            // In touch only on a channel (Radio 0.7.0): with every channel taken, the net is busy and there is no mood.
            LinkOutcome.Clean or LinkOutcome.Workable => net.ServedBy(p) != null ? ThoughtState.ActiveAtStage(StageInTouch) : ThoughtState.Inactive,
            LinkOutcome.Marginal => ThoughtState.ActiveAtStage(StageFaint),
            _ => ThoughtState.Inactive,
        };
    }
}
