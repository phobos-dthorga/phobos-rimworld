using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>Water a field kettle's batch takes from the plumbing it stands on (Framework 0.122.0; owner, 17 Nov 2026: 10 L a batch, authored).</summary>
public class CompProperties_PhobosKettleWater : CompProperties
{
    public float litresPerBatch = 10f;

    public CompProperties_PhobosKettleWater() => compClass = typeof(CompPhobosKettleWater);
}

/// <summary>
/// Billy's kettle with Dubs Bad Hygiene (docs/development/vehicle-field-kitchen.md): each big batch takes its water from the towers on the plumbing
/// beneath the kettle, which the trailer lays under everything it sets out. Its own tank, a water bowser or a shower trailer beside it, or a colony's
/// plumbing all serve. Without water its bills wait, and its inspect pane says why. In Dubs' lite mode, where there is no plumbing, nothing is asked.
/// </summary>
public class CompPhobosKettleWater : ThingComp, IFieldStoveGate
{
    public CompProperties_PhobosKettleWater Props => (CompProperties_PhobosKettleWater)props;

    PlumbingNet? Net => parent.TryGetComp<CompPipe>()?.pipeNet;

    public string? StoveRefusal()
    {
        if (PlumbingFeed.LiteMode || !parent.Spawned) return null;
        if (Net is not { } net || net.WaterTowers.Count == 0) return "PhobosKettleWater_NoPlumbing".Translate(Props.litresPerBatch.ToString("0").Named("LITRES")).Resolve();
        if (net.WaterStorage < Props.litresPerBatch) return "PhobosKettleWater_NotEnough".Translate(Props.litresPerBatch.ToString("0").Named("LITRES")).Resolve();
        return null;
    }

    public void BatchStarted()
    {
        if (PlumbingFeed.LiteMode || Net is not { } net) return;
        if (PlumbingFeed.TryPullFrom(net, Props.litresPerBatch, out _) == WaterDraw.Flowing && Seen.Wants("trailers.kettle-water-drawn", parent)) Seen.Note("trailers.kettle-water-drawn", parent, Props.litresPerBatch + " L");
    }
}
