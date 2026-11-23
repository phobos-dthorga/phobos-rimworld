using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using RimWorld;
using Verse;

namespace Phobos.Radio;

/// <summary>"In touch with home": reads the net's cache; never computes a link itself.</summary>
public class ThoughtWorker_RadioInTouch : ThoughtWorker
{
    private const int StageInTouch = 0;
    private const int StageFaint = 1;

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!RadioMod.Settings.inTouchThought || !p.Spawned || p.Map == null) return ThoughtState.Inactive;
        var net = RadioNet.For(p.Map);
        if (!net.TryGetBest(p, out var link) || link == null) return ThoughtState.Inactive;
        return link.Outcome switch
        {
            // In touch only on a channel (Radio 0.7.0): with every channel taken, the net is busy and there is no mood.
            LinkOutcome.Clean or LinkOutcome.Workable => net.ServedBy(p) != null ? ThoughtState.ActiveAtStage(StageInTouch) : ThoughtState.Inactive,
            LinkOutcome.Marginal => ThoughtState.ActiveAtStage(StageFaint),
            _ => ThoughtState.Inactive,
        };
    }
}
