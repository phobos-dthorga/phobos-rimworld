using PipeSystem;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>
/// A pipe-network store of liquefied gas that keeps cold on power. Without power its contents warm and boil off, vented a little at a
/// time. Pairs with Vanilla Expanded Framework's resource storage comp on the same building; other mods can use it in XML.
/// </summary>
public class CompProperties_PhobosBoilOff : CompProperties
{
    /// <summary>The share of what is stored that boils away per day without power. Authored.</summary>
    public float fractionPerDayUnpowered = 0.02f;

    public CompProperties_PhobosBoilOff() => compClass = typeof(CompPhobosBoilOff);
}

public class CompPhobosBoilOff : ThingComp
{
    const int IntervalTicks = 250;

    CompResourceStorage? storage;
    CompPowerTrader? power;

    public CompProperties_PhobosBoilOff Props => (CompProperties_PhobosBoilOff)props;

    bool Powered => power == null || power.PowerOn;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        storage = parent.TryGetComp<CompResourceStorage>();
        power = parent.TryGetComp<CompPowerTrader>();
    }

    public override void CompTick()
    {
        base.CompTick();
        if (storage == null || !parent.IsHashIntervalTick(IntervalTicks)) return;
        float loss = BoilOffRule.LossForInterval(storage.AmountStored, Props.fractionPerDayUnpowered, IntervalTicks, Powered);
        if (loss > 0f) storage.DrawResource(loss);
    }

    public override string? CompInspectStringExtra()
    {
        if (storage == null) return null;
        return Powered
            ? "PhobosMedical_StoreCold".Translate()
            : "PhobosMedical_StoreBoilingOff".Translate(Props.fractionPerDayUnpowered.ToStringPercent().Named("SHARE"));
    }
}
