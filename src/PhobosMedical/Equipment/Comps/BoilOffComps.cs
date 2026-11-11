using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PipeSystem;
using Phobos.Medical.Rules;
using RimWorld;
using Verse;
using Phobos.Framework.Interface;
using Phobos.Framework.Sounds;

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
    /// <summary>Loops while it boils off (gas venting from its relief valve); null for silence.</summary>
    public SoundDef? boilingSound;
    /// <summary>Frost over the store while it holds liquid and its cooler runs (owner, 12 Nov 2026, after DMS2's frosting Pressure Chamber):
    /// a mote Def faded in and out, or null for none. On a real store the ambient-air vaporiser fins and the pipework ice up as cold gas
    /// warms through them, while the vacuum-insulated tank stays mostly clear, and the picture follows that.</summary>
    public ThingDef? frostMote;
    /// <summary>Hours for the frost to gather fully, and to melt away once the store stops (authored: frost is slow to come and slower to go).</summary>
    public float frostBuildHours = 1f;
    public float frostMeltHours = 3f;

    public CompProperties_PhobosBoilOff() => compClass = typeof(CompPhobosBoilOff);
}

public class CompPhobosBoilOff : ThingComp
{
    const int IntervalTicks = 250;

    /// <summary>Stores whose cooler has no power, so they take in nothing. Read by the storage patch on every pipe-network pass, so a
    /// set lookup rather than a comp search. Derived, never saved: rebuilt as each store spawns and ticks.</summary>
    static readonly ConditionalWeakTable<CompResourceStorage, object> refusing = new();
    static readonly object Refuses = new();

    /// <summary>Whether this storage should take in nothing now.</summary>
    public static bool RefusesFilling(CompResourceStorage storage) => refusing.TryGetValue(storage, out _);

    CompResourceStorage? storage;
    CompPowerTrader? power;
    readonly SustainedSound boiling = new();
    FadingOverlay? frost;

    const float TicksPerHour = 2500f;

    public CompProperties_PhobosBoilOff Props => (CompProperties_PhobosBoilOff)props;

    bool Powered => power == null || power.PowerOn;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        storage = parent.TryGetComp<CompResourceStorage>();
        power = parent.TryGetComp<CompPowerTrader>();
        UpdateRefusing();
        if (Props.frostMote != null) frost = new FadingOverlay(Props.frostMote);
        // Motes are never saved: after a load the frost starts at what the store's state calls for.
        UpdateFrost(0f, snap: true);
    }

    /// <summary>Frost while the store holds liquid and is kept cold.</summary>
    bool Frosting => storage != null && Powered && storage.AmountStored >= 1f;

    void UpdateFrost(float ticks, bool snap = false) =>
        frost?.Update(parent, Frosting, ticks, Props.frostBuildHours * TicksPerHour, Props.frostMeltHours * TicksPerHour, snap);

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        boiling.End();
        frost?.End();
        base.PostDeSpawn(map, mode);
        if (storage != null) refusing.Remove(storage);
    }

    void UpdateRefusing()
    {
        if (storage == null) return;
        if (Powered) refusing.Remove(storage); else refusing.AddOrUpdate(storage, Refuses);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (storage == null || !parent.IsHashIntervalTick(IntervalTicks)) return;
        UpdateRefusing();
        float loss = BoilOffRule.LossForInterval(storage.AmountStored, Props.fractionPerDayUnpowered, IntervalTicks, Powered);
        if (loss > 0f) storage.DrawResource(loss);
        boiling.Set(Props.boilingSound, loss > 0f, parent);
        UpdateFrost(IntervalTicks);
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
