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
        if (!RadioNet.For(p.Map).TryGetBest(p, out var link) || link == null) return ThoughtState.Inactive;
        return link.Outcome switch
        {
            LinkOutcome.Clean or LinkOutcome.Workable => ThoughtState.ActiveAtStage(StageInTouch),
            LinkOutcome.Marginal => ThoughtState.ActiveAtStage(StageFaint),
            _ => ThoughtState.Inactive,
        };
    }
}
