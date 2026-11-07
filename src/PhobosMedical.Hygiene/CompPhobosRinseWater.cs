using Phobos.Framework.Hygiene;
using Phobos.Medical.Comps;
using Verse;

namespace Phobos.Medical.Hygiene;

/// <summary>
/// Water for a safety shower from Dubs Bad Hygiene's plumbing, drawn one rinse at a time. A failed draw marks it dry for an in-game hour so
/// nobody keeps running to it; the reason is the same text the electrolyser uses. In Dubs' lite mode, with no plumbing, water stays assumed.
/// Added to the shower by a patch only while Dubs Bad Hygiene is active.
/// </summary>
public class CompProperties_PhobosRinseWater : CompProperties
{
    public CompProperties_PhobosRinseWater() => compClass = typeof(CompPhobosRinseWater);
}

public class CompPhobosRinseWater : ThingComp, IPhobosWaterSource
{
    const int DryForTicks = 2500;

    readonly PlumbingFeed feed = new();
    /// <summary>What the last draw found and when; derived, so never saved.</summary>
    WaterDraw last = WaterDraw.Flowing;
    int lastTick = -DryForTicks;

    public bool LooksDry => !PlumbingFeed.LiteMode && last != WaterDraw.Flowing && Find.TickManager.TicksGame - lastTick < DryForTicks;

    public bool TryDraw(float litres, out string reason)
    {
        reason = "";
        if (PlumbingFeed.LiteMode) return true;
        last = feed.TryPull(parent, litres, out _);
        lastTick = Find.TickManager.TicksGame;
        if (last == WaterDraw.Flowing) return true;
        reason = (last == WaterDraw.NoPlumbing ? "PhobosMedical_WaterNoPlumbing" : "PhobosMedical_WaterTowersEmpty").Translate().Resolve();
        return false;
    }
}
