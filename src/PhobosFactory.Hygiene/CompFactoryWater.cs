using System.Collections.Generic;
using DubsBadHygiene;
using Phobos.Factory.Rules;
using PipeSystem;
using RimWorld;
using Verse;

namespace Phobos.Factory.Hygiene;

/// <summary>
/// Lets a factory machine draw water from Dubs Bad Hygiene's plumbing, from any pipe under it or against its side.
/// It is deliberately not one of Dubs' own pipe comps: a pipe comp would delete the plumbing under the machine, on
/// load too, and leave gaps in the player's pipe runs if this mod were ever removed. This way the machine only
/// reads the plumbing, so adding or removing the mod changes nothing on the map.
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
    private CompPipe? feed;
    private CompAdvancedResourceProcessor? processor;

    /// <summary>The chance the batch being made carries food poisoning, from the water used so far.</summary>
    public float BatchPoisonChance => WaterRule.BatchPoisonChance(batchRiskLitres, batchLitres);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        processor = parent.GetComp<CompAdvancedResourceProcessor>();
        feed = null;
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
        var net = FindNet(litres);
        if (net == null)
        {
            supply = Supply.NoPlumbing;
            return false;
        }
        if (!net.PullWater(litres, out var quality))
        {
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

    /// <summary>
    /// The plumbing to draw from: the last one used while it still has enough, else any within reach that has
    /// enough, else one with a water tower so the machine can say the tower is empty. Null if none has a tower.
    /// Looking again costs little and happens only while the machine is short of water.
    /// </summary>
    private PlumbingNet? FindNet(float litres)
    {
        if (feed != null && feed.parent.Spawned && !feed.closed && feed.pipeNet is { } current && current.WaterStorage >= litres)
        {
            return current;
        }
        feed = null;
        PlumbingNet? withEmptyTower = null;
        var map = parent.Map;
        foreach (var cell in FeedCells())
        {
            if (!cell.InBounds(map)) continue;
            var things = cell.GetThingList(map);
            for (int i = 0; i < things.Count; i++)
            {
                var pipe = things[i].TryGetComp<CompPipe>();
                // Dubs' water and sewage share one plumbing net, whose pipes are all in Sewage mode.
                if (pipe == null || pipe.mode != PipeType.Sewage || pipe.closed || pipe.pipeNet is not { } net) continue;
                if (net.WaterStorage >= litres)
                {
                    feed = pipe;
                    return net;
                }
                if (net.WaterTowers.Count > 0) withEmptyTower = net;
            }
        }
        return withEmptyTower;
    }

    private IEnumerable<IntVec3> FeedCells()
    {
        foreach (var cell in parent.OccupiedRect()) yield return cell;
        foreach (var cell in GenAdj.CellsAdjacentCardinal(parent)) yield return cell;
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
