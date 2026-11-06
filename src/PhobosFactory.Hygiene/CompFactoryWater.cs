using System.Collections.Generic;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Phobos.Factory.Rules;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory.Hygiene;

/// <summary>
/// Lets a factory machine draw water from Dubs Bad Hygiene's plumbing, through D'thorga Framework's <see cref="PlumbingFeed"/>
/// (any pipe under it or against its side; it only reads the plumbing, so adding or removing the mod changes nothing on the map).
/// Only the current batch's water tally is saved. <see cref="FactoryWater"/> decides when to draw.
/// </summary>
public class CompFactoryWater : ThingComp
{
    private enum Supply
    {
        NotDrawnYet,
        Flowing,
        NoPlumbing,
        TowersEmpty,
    }

    private float batchLitres;
    private float batchRiskLitres;

    private Supply supply;
    private ContaminationLevel lastQuality;
    private readonly PlumbingFeed feed = new();
    private CompAdvancedResourceProcessor? processor;

    /// <summary>The chance the batch being made carries food poisoning, from the water used so far.</summary>
    public float BatchPoisonChance => WaterRule.BatchPoisonChance(batchRiskLitres, batchLitres);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        processor = parent.GetComp<CompAdvancedResourceProcessor>();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref batchLitres, "PhobosFactory_WaterBatchLitres");
        Scribe_Values.Look(ref batchRiskLitres, "PhobosFactory_WaterBatchRiskLitres");
    }

    /// <summary>Draws this step's water and adds it to the batch's tally. False if there isn't enough.</summary>
    public bool TryDraw(float litres, WaterUseExtension use)
    {
        switch (feed.TryPull(parent, litres, out var quality))
        {
            case WaterDraw.NoPlumbing:
                supply = Supply.NoPlumbing;
                return false;
            case WaterDraw.TowersEmpty:
                supply = Supply.TowersEmpty;
                return false;
        }
        supply = Supply.Flowing;
        lastQuality = quality;
        batchLitres += litres;
        batchRiskLitres += litres * use.PoisonChance(quality);
        return true;
    }

    /// <summary>Forgets the batch's water, and what the last draw found, so no old "no water" lingers.</summary>
    public void ClearBatch()
    {
        batchLitres = 0f;
        batchRiskLitres = 0f;
        supply = Supply.NotDrawnYet;
    }

    public override string? CompInspectStringExtra()
    {
        var use = processor?.Process?.Def.GetModExtension<WaterUseExtension>();
        if (use == null || !FactoryWater.Required) return null;
        var litres = use.litresPerBatch.ToString("0.#").Named("LITRES");
        switch (supply)
        {
            case Supply.NoPlumbing:
                return "PhobosFactory_WaterNoPlumbing".Translate().Resolve();
            case Supply.TowersEmpty:
                return "PhobosFactory_WaterTowersEmpty".Translate().Resolve();
            case Supply.NotDrawnYet:
                return "PhobosFactory_WaterNeeded".Translate(litres).Resolve();
        }
        float chance = FactoryMod.Settings.dirtyWaterPoisons ? use.PoisonChance(lastQuality) : 0f;
        var chanceText = chance.ToStringPercent().Named("CHANCE");
        return lastQuality switch
        {
            ContaminationLevel.Contaminated when chance > 0f => "PhobosFactory_WaterContaminatedRisk".Translate(litres, chanceText).Resolve(),
            ContaminationLevel.Contaminated => "PhobosFactory_WaterContaminated".Translate(litres).Resolve(),
            ContaminationLevel.Untreated when chance > 0f => "PhobosFactory_WaterUntreatedRisk".Translate(litres, chanceText).Resolve(),
            ContaminationLevel.Untreated => "PhobosFactory_WaterUntreated".Translate(litres).Resolve(),
            _ => "PhobosFactory_WaterTreated".Translate(litres).Resolve(),
        };
    }
}
