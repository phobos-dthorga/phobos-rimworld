using System.Collections.Generic;
using PipeSystem;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;

namespace Phobos.Medical.Comps;

/// <summary>
/// A pipe-network store of liquefied gas that keeps cold on power. Without power its contents warm and boil off, vented a little at a
/// time, and it takes in no more (its liquefier is off; the owner, 7 Nov 2026): it still gives out what it holds. Pairs with Vanilla
/// Expanded Framework's resource storage comp on the same building; other mods can use it in XML.
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

    /// <summary>Stores whose cooler has no power, so they take in nothing. Read by the storage patch on every pipe-network pass, so a
    /// set lookup rather than a comp search. Derived, never saved: rebuilt as each store spawns and ticks.</summary>
    static readonly HashSet<CompResourceStorage> refusing = new();

    /// <summary>Whether this storage should take in nothing now.</summary>
    public static bool RefusesFilling(CompResourceStorage storage) => refusing.Contains(storage);

    CompResourceStorage? storage;
    CompPowerTrader? power;

    public CompProperties_PhobosBoilOff Props => (CompProperties_PhobosBoilOff)props;

    bool Powered => power == null || power.PowerOn;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        storage = parent.TryGetComp<CompResourceStorage>();
        power = parent.TryGetComp<CompPowerTrader>();
        UpdateRefusing();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        if (storage != null) refusing.Remove(storage);
    }

    void UpdateRefusing()
    {
        if (storage == null) return;
        if (Powered) refusing.Remove(storage); else refusing.Add(storage);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (storage == null || !parent.IsHashIntervalTick(IntervalTicks)) return;
        UpdateRefusing();
        float loss = BoilOffRule.LossForInterval(storage.AmountStored, Props.fractionPerDayUnpowered, IntervalTicks, Powered);
        if (loss > 0f) storage.DrawResource(loss);
    }

    public override string? CompInspectStringExtra()
    {
        if (storage == null) return null;
        return Powered
            ? "PhobosMedical_StoreCold".Translate().Resolve()
            : "PhobosMedical_StoreNotFilling".Translate().Resolve() + "\n"
              + "PhobosMedical_StoreBoilingOff".Translate(Props.fractionPerDayUnpowered.ToStringPercent().Named("SHARE")).Resolve();
    }
}
