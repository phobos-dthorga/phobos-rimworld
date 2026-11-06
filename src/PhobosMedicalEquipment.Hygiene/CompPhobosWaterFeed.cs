using Phobos.Framework.Hygiene;
using Phobos.Medical.Comps;
using RimWorld;
using Verse;

namespace Phobos.Medical.Hygiene;

/// <summary>
/// Water from Dubs Bad Hygiene's plumbing for a producer that splits it (the oxygen electrolyser; the owner, 7 Nov 2026). While it runs
/// it draws its water on each rare tick, and without water it shuts the production gate, so the electrolyser stops and idles. Water
/// quality doesn't matter here (a real electrolyser needs pure water; a simplification). In Dubs' lite mode, with no plumbing at all,
/// water stays assumed. Added to the electrolyser by a patch only while Dubs Bad Hygiene is active.
/// </summary>
public class CompProperties_PhobosWaterFeed : CompProperties
{
    /// <summary>
    /// Litres an in-game hour while running. One oxygen canister an hour is about 0.3 cubic metres, about 0.43 kg of oxygen; splitting
    /// water for it (2 H2O gives 2 H2 + O2, 36 g of water for 32 g of oxygen) takes about 0.48 kg, so about half a litre (our arithmetic).
    /// </summary>
    public float litresPerHour = 0.5f;

    public CompProperties_PhobosWaterFeed() => compClass = typeof(CompPhobosWaterFeed);
}

public class CompPhobosWaterFeed : ThingComp, IPhobosProductionGate
{
    const int RareTickInterval = 250;
    const int TicksPerHour = 2500;

    readonly PlumbingFeed feed = new();
    /// <summary>What the last draw found; derived, so never saved. Starts flowing, so a loaded electrolyser doesn't flicker off before its first check.</summary>
    WaterDraw supply = WaterDraw.Flowing;

    CompProperties_PhobosWaterFeed Props => (CompProperties_PhobosWaterFeed)props;

    public bool AllowsProduction => PlumbingFeed.LiteMode || supply == WaterDraw.Flowing;

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (PlumbingFeed.LiteMode || !parent.Spawned || !Running) return;
        WaterDraw found = feed.TryPull(parent, Props.litresPerHour * RareTickInterval / TicksPerHour, out _);
        if (found == supply) return;
        supply = found;
        // Any signal the trader doesn't know makes it look again at whether it can run and whether to idle.
        parent.BroadcastCompSignal("PhobosWaterSupplyChanged");
    }

    /// <summary>Switched on and powered, so it would be splitting water now if it had any.</summary>
    bool Running =>
        FlickUtility.WantsToBeOn(parent) && (parent.TryGetComp<CompPowerTrader>() is not CompPowerTrader power || power.PowerOn);

    public override string? CompInspectStringExtra()
    {
        if (PlumbingFeed.LiteMode) return null;
        return supply switch
        {
            WaterDraw.NoPlumbing => "PhobosMedical_WaterNoPlumbing".Translate().Resolve(),
            WaterDraw.TowersEmpty => "PhobosMedical_WaterTowersEmpty".Translate().Resolve(),
            _ => "PhobosMedical_WaterFlowing".Translate(Props.litresPerHour.ToString("0.#").Named("LITRES")).Resolve(),
        };
    }
}
