using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

public class CompProperties_PhobosDrillRig : CompProperties_PhobosRigWork
{
    /// <summary>Authored. On the engine: 40 chemfuel a day. On a grid: 300 game watts, the deep drill's 200 and the rig's hydraulics.</summary>
    public float drillFuelPerDay = 40f;
    public float electricWatts = 300f;
    /// <summary>The rate at night with no work lights fitted.</summary>
    public float unlitFactor = 0.8f;
    /// <summary>Deep resources that are piped and never come up as items (owner, 9 Nov 2026): skipped here.</summary>
    public List<ThingDef> pipedResources = new();

    public CompProperties_PhobosDrillRig() => compClass = typeof(CompPhobosDrillRig);
}

/// <summary>
/// The drill rig (docs/development/vehicle-drill-rig.md). The game's deep drill only works as a building, so this runs its routine itself: the same 10,000 work a portion, the same portion sizes and
/// yield, on the same grid. A portion is the game's, at most the game's rate. Gizmos read this and call it.
/// </summary>
public sealed class CompPhobosDrillRig : CompPhobosRigWork
{
    /// <summary>The game's own figures (CompDeepDrill and CompCreatesInfestations).</summary>
    const float WorkPerPortion = 10000f;
    const int InsectRefireTicks = 420000;
    const float InsectQuietCells = 10f;
    const float RareTickDays = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;

    float portionProgress;
    float portionYield;
    int lastInsectsTick = -999999;

    bool orePresent;

    public CompProperties_PhobosDrillRig Props => (CompProperties_PhobosDrillRig)props;
    protected override string DeployedSaveKey => "PhobosDrillDeployed";
    public override JobDef WorkJob => RigDefOf.PhobosVehicleSystems_Drill;
    public override SkillDef WorkSkill => SkillDefOf.Mining;
    public override float LearnPerTick => 0.065f;
    public override EffecterDef? WorkEffect => EffecterDefOf.Drill;
    public override string SetUpDescription => "PhobosVehicleSystems_RigGizmoSetUpDesc".Translate().Resolve();
    public override string PackUpDescription => "PhobosVehicleSystems_RigGizmoPackUpDesc".Translate().Resolve();

    bool ElectricFitted => Vehicle.GetStatValue(RigDefOf.Phobos_ElectricDrill) > 0f;
    bool ElectricNow => ElectricFitted && PluggedIn;
    protected override bool WantsPowerPoint => ElectricFitted;
    public override bool WantsEngine => deployed && Working && !ElectricNow;
    public override float EngineDemand => 0.5f;
    protected override float FuelPerDay => Props.drillFuelPerDay;
    protected override float DrawWatts => ElectricFitted ? Props.electricWatts : 0f;
    float Reach => Vehicle.GetStatValue(RigDefOf.Phobos_DrillReach);

    /// <summary>The cell behind the middle of the truck that the string goes down.</summary>
    public IntVec3 Hole
    {
        get
        {
            CellRect footprint = Vehicle.OccupiedRect();
            IntVec3 rear = -Vehicle.Rotation.FacingCell;
            IntVec3 cell = Vehicle.Position;
            while (footprint.Contains(cell)) cell += rear;
            return cell;
        }
    }

    protected override IntVec3 WorkSpot => Hole;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref portionProgress, "PhobosDrillProgress");
        Scribe_Values.Look(ref portionYield, "PhobosDrillYield");
        Scribe_Values.Look(ref lastInsectsTick, "PhobosDrillInsectsTick", -999999);
    }

    public override string? CannotDeploy()
    {
        string? why = base.CannotDeploy();
        if (why != null) return why;
        return Hole.InBounds(Vehicle.Map) ? null : "PhobosVehicleSystems_RigNoHole".Translate().Resolve();
    }

    public override string? WhyNoWork()
    {
        string? why = base.WhyNoWork();
        if (why != null) return why;
        if (!NextOre(out _, out _, out _) && DeepDrillUtility.GetBaseResource(Vehicle.Map, Hole) == null) return "PhobosVehicleSystems_RigNothingToDrill".Translate().Resolve();
        return !ElectricNow && !HasFuel ? "PhobosVehicleSystems_RigNoFuel".Translate().Resolve() : null;
    }

    /// <summary>The first ore within reach of the hole that can come up as items, as the game's drill looks for it, out to the mast's own reach.</summary>
    bool NextOre(out ThingDef? ore, out int count, out IntVec3 cell)
    {
        Map map = Vehicle.Map;
        IntVec3 hole = Hole;
        int cells = GenRadial.NumCellsInRadius(Reach);
        for (int i = 0; i < cells; i++)
        {
            IntVec3 candidate = hole + GenRadial.RadialPattern[i];
            if (!candidate.InBounds(map)) continue;
            ThingDef? found = map.deepResourceGrid.ThingDefAt(candidate);
            if (found == null || Props.pipedResources.Contains(found)) continue;
            ore = found;
            count = map.deepResourceGrid.CountAt(candidate);
            cell = candidate;
            return true;
        }
        ore = DeepDrillUtility.GetBaseResource(map, hole);
        count = int.MaxValue;
        cell = hole;
        return false;
    }

    protected override void RareWork()
    {
        orePresent = NextOre(out _, out _, out _);
        if (Working) MaybeInsects();
    }

    public override void Worked(Pawn worker, int delta)
    {
        MarkWorked();
        float factor = Vehicle.GetStatValue(RigDefOf.Phobos_DrillRate);
        if (!orePresent) factor *= Vehicle.GetStatValue(RigDefOf.Phobos_BedrockFactor);
        if (Vehicle.GetStatValue(RigDefOf.Phobos_WorkLights) <= 0f && When_Night.IsNight(Vehicle)) factor *= Props.unlitFactor;
        float work = worker.GetStatValue(StatDefOf.DeepDrillingSpeed) * factor * delta;
        portionProgress += work;
        portionYield += work * worker.GetStatValue(StatDefOf.MiningYield) / WorkPerPortion;
        if (portionProgress <= WorkPerPortion) return;
        ProducePortion(worker);
        portionProgress = 0f;
        portionYield = 0f;
    }

    void ProducePortion(Pawn worker)
    {
        bool isOre = NextOre(out ThingDef? ore, out int count, out IntVec3 cell);
        if (ore == null) return;
        Map map = Vehicle.Map;
        int amount = Mathf.Min(count, ore.deepCountPerPortion);
        if (isOre) map.deepResourceGrid.SetAt(cell, ore, count - amount);
        Thing thing = ThingMaker.MakeThing(ore);
        thing.stackCount = Mathf.Max(1, GenMath.RoundRandom(amount * portionYield));
        IntVec3 hole = Hole;
        CellRect footprint = Vehicle.OccupiedRect();
        GenPlace.TryPlaceThing(thing, hole, map, ThingPlaceMode.Near, null, p => p != hole && !footprint.Contains(p));
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, worker.Named(HistoryEventArgsNames.Doer)));
        orePresent = NextOre(out _, out _, out _);
        if (!isOre || orePresent) return;
        ThingDef? bedrock = DeepDrillUtility.GetBaseResource(map, hole);
        Tell(bedrock != null ? "PhobosVehicleSystems_RigSpent".Translate().Resolve() : "PhobosVehicleSystems_RigSpentNothing".Translate().Resolve(), MessageTypeDefOf.TaskCompletion);
    }

    // ---- Insects: the game's rule, kept to. It lives on a deep drill building and its storyteller, so the rig rolls it itself. ----

    bool InsectsRecently => Find.TickManager.TicksGame <= lastInsectsTick + InsectRefireTicks;

    void MaybeInsects()
    {
        if (InsectsRecently || SomethingElseHadInsects() || Faction.OfInsects == null) return;
        float chance = Find.Storyteller.difficulty.deepDrillInfestationChanceFactor;
        float baseMtb = Find.Storyteller.def.comps.OfType<StorytellerCompProperties_DeepDrillInfestation>().FirstOrDefault()?.baseMtbDaysPerDrill ?? -1f;
        if (chance <= 0f || baseMtb <= 0f || !Rand.MTBEventOccurs(baseMtb / chance, GenDate.TicksPerDay, GenTicks.TickRareInterval)) return;
        BringInsects();
    }

    bool SomethingElseHadInsects()
    {
        Map map = Vehicle.Map;
        IntVec3 here = Vehicle.Position;
        foreach (Thing thing in map.listerThings.ThingsInGroup(ThingRequestGroup.CreatesInfestations))
        {
            if (thing.Position.InHorDistOf(here, InsectQuietCells) && thing.TryGetComp<CompCreatesInfestations>().CantFireBecauseCreatedInfestationRecently) return true;
        }
        return map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Any(p => p != Vehicle && p is VehiclePawn other && other.Position.InHorDistOf(here, InsectQuietCells)
            && other.GetComp<CompPhobosDrillRig>() is { InsectsRecently: true });
    }

    void BringInsects()
    {
        Map map = Vehicle.Map;
        IntVec3 hole = Hole;
        IntVec3 cell = CellFinder.FindNoWipeSpawnLocNear(hole, map, ThingDefOf.TunnelHiveSpawner, Rot4.North, 2, x => x.Walkable(map) && x.GetFirstThing(map, ThingDefOf.Hive) == null && x.GetFirstThing(map, ThingDefOf.TunnelHiveSpawner) == null);
        if (cell == hole) return;
        var spawner = (TunnelHiveSpawner)ThingMaker.MakeThing(ThingDefOf.TunnelHiveSpawner);
        spawner.spawnHive = false;
        spawner.insectsPoints = Mathf.Clamp(StorytellerUtility.DefaultThreatPointsNow(map) * Rand.Range(0.3f, 0.6f), 200f, 1000f);
        spawner.spawnedByInfestationThingComp = true;
        GenSpawn.Spawn(spawner, cell, map, WipeMode.FullRefund);
        lastInsectsTick = Find.TickManager.TicksGame;
        Find.LetterStack.ReceiveLetter("PhobosVehicleSystems_RigInsectsLabel".Translate(), "PhobosVehicleSystems_RigInsectsText".Translate(Vehicle.LabelShort.Named("VEHICLE")), LetterDefOf.ThreatSmall, new TargetInfo(spawner.Position, map));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (!deployed) return;
        GenDraw.DrawRadiusRing(Hole, Reach);
        if (Vehicle.Map.deepResourceGrid.AnyActiveDeepScannersOnMap()) Vehicle.Map.deepResourceGrid.MarkForDraw();
    }

    public override string? CompInspectStringExtra()
    {
        if (!deployed) return null;
        return (orePresent
            ? "PhobosVehicleSystems_RigProgressOre".Translate((portionProgress / WorkPerPortion).ToStringPercent().Named("PROGRESS"))
            : "PhobosVehicleSystems_RigProgressStone".Translate((portionProgress / WorkPerPortion).ToStringPercent().Named("PROGRESS"))).Resolve();
    }
}
