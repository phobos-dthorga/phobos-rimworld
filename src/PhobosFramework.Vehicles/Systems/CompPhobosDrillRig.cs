using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Makes a truck a drill rig: set up over the ground and worked by a colonist (Mining), it drills deep resources as the game's
/// deep drill does, and piped resources go into a buffer for a tanker or a pipe net. Goes on the VehicleDef, which needs
/// <c>Phobos_DrillRate</c> and <c>Phobos_DrillReach</c> in its <c>vehicleStats</c> besides the set-up stats every rig needs
/// (<see cref="CompProperties_PhobosRigWork"/>).</summary>
public class CompProperties_PhobosDrillRig : CompProperties_PhobosRigWork
{
    /// <summary>Authored. On the engine: 40 chemfuel a day. On a grid: 300 game watts, the deep drill's 200 and the rig's hydraulics.</summary>
    public float drillFuelPerDay = 40f;
    /// <summary>The share of the engine's rated power drilling takes (Framework 0.131.0; authored, as the 0.5 written in code before it):
    /// the compressor and the mast's hydraulics. With fuel following engine load this sets the fuel, in place of drillFuelPerDay.</summary>
    public float engineShare = 0.5f;
    public float electricWatts = 300f;
    /// <summary>The rate at night with no work lights fitted: its own upgrade or the work lamps family (Framework 0.103.0).</summary>
    public float unlitFactor = 0.8f;
    /// <summary>Deep resources that are piped and never come up as items (owner, 9 Nov 2026): skipped until a head is fitted, then raised into the buffer.</summary>
    public List<ThingDef> pipedResources = new();
    /// <summary>The work to raise one unit of a piped resource: a tenth of the deepchem pumpjack's 60 ticks a unit (the owner's order of magnitude, 9 Nov 2026).</summary>
    public float pipedWorkPerUnit = 6f;
    /// <summary>How far from the hole, in cells, a storage building may stand and still be filled straight from the rig (Framework 0.126.0,
    /// UH3). Authored: a short chute from the rig's shaker, enough for a hopper or a bulk bunker set beside the rig.</summary>
    public float storageReach = 3f;
    /// <summary>Where the buffer can empty itself, besides a tanker drawing from it.</summary>
    public List<RigBufferOutlet> bufferOutlets = new();
    /// <summary>Other work the rig can do at the hole, offered by other mods' tie-ins, tried when no ore is in reach.</summary>
    public List<RigSideJob> sideJobs = new();

    public CompProperties_PhobosDrillRig() => compClass = typeof(CompPhobosDrillRig);
}

/// <summary>Somewhere a rig's buffer can run to. Subclass it in any mod and name it in XML with Class="Namespace.ClassName".</summary>
public abstract class RigBufferOutlet
{
    /// <summary>Takes up to this much from the rig's buffer and returns what really went.</summary>
    public abstract float Give(CompPhobosDrillRig rig, float most);
}

/// <summary>Work a rig does at its hole in place of drilling, such as sinking an oil well. Subclass it in any mod and name it in XML with Class="Namespace.ClassName". It holds no state: its progress is kept on the rig.</summary>
public abstract class RigSideJob
{
    /// <summary>Whether this job is what the ground at the hole calls for.</summary>
    public abstract bool Wanted(CompPhobosDrillRig rig);

    /// <summary>Why it cannot go on now, or null.</summary>
    public abstract string? WhyNot(CompPhobosDrillRig rig);

    public abstract void Worked(CompPhobosDrillRig rig, Pawn worker, int delta);

    public abstract string? InspectLine(CompPhobosDrillRig rig);

    /// <summary>How far the job has got, 0 to 1, for the worker's progress bar; null (the default) shows none.</summary>
    public virtual float? Progress(CompPhobosDrillRig rig) => null;
}

/// <summary>
/// The drill rig (docs/development/vehicle-drill-rig.md). The game's deep drill only works as a building, so this runs its routine itself: the same 10,000 work a portion, the same portion sizes and
/// yield, on the same grid. A portion is the game's, at most the game's rate. Gizmos read this and call it.
/// </summary>
public sealed class CompPhobosDrillRig : CompPhobosRigWork
{
    /// <summary>
    /// On the world map, with Dubs Bad Hygiene (Framework 0.317.0; EM34; docs/development/vehicle-road-work.md): drill a water bore at a
    /// halt, or stop it. The rules are RoadWork's; a refusal (a dry tile, no hand, a wrecked bit) greys the button with its reason.
    /// </summary>
    public override IEnumerable<Gizmo> CompCaravanGizmos()
    {
        foreach (Gizmo gizmo in base.CompCaravanGizmos()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Vehicle.GetVehicleCaravan() is not { } convoy) yield break;
        RoadHaltJob? busy = WorldComponent_PhobosRoadWork.Current?.JobOn(Vehicle);
        string busyText = "PhobosRoadWork_Busy".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (CaravanWater.HasSource)
        {
            bool boring = busy is RoadBore;
            var bore = new Command_Action
            {
                defaultLabel = (boring ? "PhobosRoadWork_BoreStopGizmo" : "PhobosRoadWork_BoreGizmo").Translate().Resolve(),
                defaultDesc = boring ? "PhobosRoadWork_BoreStopGizmoDesc".Translate().Resolve()
                    : "PhobosRoadWork_BoreGizmoDesc".Translate(RoadWork.Rainfall(convoy).ToString("0").Named("RAIN"),
                        BoreRule.LitresPerHour(RoadWork.Rainfall(convoy)).ToString("0").Named("LITRES")).Resolve(),
                icon = SystemIcons.For("Phobos/VehicleSystems/RoadWork/Bore"),
                action = boring ? () => RoadWork.StopBore(convoy) : () => RoadWork.StartBore(convoy, Vehicle),
            };
            if (!boring && (busy != null ? busyText : RoadWork.BoreRefusal(convoy, Vehicle, starting: true)) is { } refused) bore.Disable(refused);
            yield return bore;
        }
        // Framework 0.322.0 (EM35): prove a find the survey halt read here. Borrows the game's deep drill picture until its own is drawn.
        bool proving = busy is RoadTestHole;
        string desc = "PhobosRoadWork_TestHoleGizmoDesc".Translate(RoadHaltRule.TestHoleHours.ToString("0").Named("HOURS")).Resolve();
        if (VehicleSystemsMod.Settings.testHoleInsects) desc += " " + "PhobosRoadWork_TestHoleInsectsDesc".Translate().Resolve();
        var hole = new Command_Action
        {
            defaultLabel = (proving ? "PhobosRoadWork_TestHoleStopGizmo" : "PhobosRoadWork_TestHoleGizmo").Translate().Resolve(),
            defaultDesc = proving ? "PhobosRoadWork_TestHoleStopGizmoDesc".Translate().Resolve() : desc,
            icon = SystemIcons.For("Phobos/VehicleSystems/RoadWork/TestHole"),
            action = proving ? () => RoadWork.Stop(busy!, "PhobosRoadWork_TestHoleStopped".Translate().Resolve()) : () => RoadWork.StartTestHole(convoy, Vehicle),
        };
        if (!proving && (busy != null ? busyText : RoadWork.TestHoleRefusal(convoy, Vehicle, starting: true)) is { } holeRefused) hole.Disable(holeRefused);
        yield return hole;
        // Framework 0.324.0 (EM36): break stone out with the hammer bit. Its own icon since Framework 0.337.1 (the owner's pick of 27 Nov 2026).
        bool quarrying = busy is RoadQuarry;
        var quarry = new Command_Action
        {
            defaultLabel = (quarrying ? "PhobosRoadWork_QuarryStopGizmo" : "PhobosRoadWork_QuarryGizmo").Translate().Resolve(),
            defaultDesc = quarrying ? "PhobosRoadWork_QuarryStopGizmoDesc".Translate().Resolve() : "PhobosRoadWork_QuarryGizmoDesc".Translate().Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/RoadWork/Quarry"),
            action = quarrying ? () => RoadWork.Stop(busy!, "PhobosRoadWork_QuarryStopped".Translate(((RoadQuarry)busy!).made.Named("COUNT")).Resolve())
                : () => RoadWork.StartQuarry(convoy, Vehicle),
        };
        if (!quarrying && (busy != null ? busyText : RoadWork.QuarryRefusal(convoy, Vehicle, starting: true)) is { } quarryRefused) quarry.Disable(quarryRefused);
        yield return quarry;
    }

    /// <summary>The game's own figures (CompDeepDrill and CompCreatesInfestations).</summary>
    const float WorkPerPortion = 10000f;
    const int InsectRefireTicks = 420000;
    const float InsectQuietCells = 10f;
    const float RareTickDays = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;

    float portionProgress;
    float portionYield;
    int lastInsectsTick = -999999;
    float buffer;
    ThingDef? bufferKind;

    bool orePresent;
    bool nextIsPiped;
    float pipedProgress;
    RigSideJob? sideJob;

    /// <summary>A side job's progress and steps, kept here so a save holds them.</summary>
    public float SideProgress;
    public int SideSteps;

    public CompProperties_PhobosDrillRig Props => (CompProperties_PhobosDrillRig)props;
    protected override string DeployedSaveKey => "PhobosDrillDeployed";
    public override JobDef WorkJob => RigDefOf.PhobosVehicleSystems_Drill;
    public override SkillDef WorkSkill => SkillDefOf.Mining;
    public override float LearnPerTick => 0.065f;
    /// <summary>The game's drill, or only its dust when the rig has its own drilling sound.</summary>
    public override EffecterDef? WorkEffect => VehicleWorkSounds.Find(Vehicle, "Work") != null ? VehicleWorkEffecterDefOf.PhobosVehicleSystems_DrillDust : EffecterDefOf.Drill;
    static readonly string[] ElectricKeys = { "WorkElectric", "Work" };
    protected override string[] WorkSoundKeys => ElectricNow ? ElectricKeys : base.WorkSoundKeys;
    public override string SetUpDescription => "PhobosVehicleSystems_RigGizmoSetUpDesc".Translate().Resolve();
    public override string PackUpDescription => "PhobosVehicleSystems_RigGizmoPackUpDesc".Translate().Resolve();

    bool ElectricFitted => Vehicle.GetStatValue(RigDefOf.Phobos_ElectricDrill) > 0f;
    bool ElectricNow => ElectricFitted && PluggedIn;
    protected override bool WantsPowerPoint => ElectricFitted;
    public override bool WantsEngine => deployed && Working && !ElectricNow;
    public override float EngineDemand => Demand(Props.engineShare);
    protected override VehicleStatDef WorkStat => RigDefOf.Phobos_DrillRate;
    protected override float FuelPerDay => Props.drillFuelPerDay;
    protected override float DrawWatts => ElectricFitted ? Props.electricWatts : 0f;
    float Reach => Vehicle.GetStatValue(RigDefOf.Phobos_DrillReach);
    /// <summary>The head's buffer: above zero only with the head fitted.</summary>
    float BufferCapacity => Vehicle.GetStatValue(RigDefOf.Phobos_RigBuffer);
    public float Buffer => buffer;
    public ThingDef? BufferKind => bufferKind;
    bool BufferFull => buffer > BufferCapacity - 1f;

    /// <summary>Where a rig touches pipes: its own cells and those around it.</summary>
    public IEnumerable<IntVec3> ContactCells()
    {
        foreach (IntVec3 cell in Vehicle.OccupiedRect()) yield return cell;
        foreach (IntVec3 cell in Vehicle.SurroundingCells) yield return cell;
    }

    /// <summary>Takes up to this much of this resource out of the buffer, for a tanker or a pipe. Returns what came.</summary>
    public float TakeFromBuffer(ThingDef kind, float most)
    {
        if (kind != bufferKind || buffer <= 0f) return 0f;
        float taken = Mathf.Min(most, buffer);
        buffer -= taken;
        if (buffer <= 0f) bufferKind = null;
        return taken;
    }

    /// <summary>The cell behind the middle of the truck that the string goes down.</summary>
    public IntVec3 Hole => RearCell;

    protected override IntVec3 WorkSpot => Hole;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref portionProgress, "PhobosDrillProgress");
        Scribe_Values.Look(ref portionYield, "PhobosDrillYield");
        Scribe_Values.Look(ref lastInsectsTick, "PhobosDrillInsectsTick", -999999);
        Scribe_Values.Look(ref buffer, "PhobosDrillBuffer");
        Scribe_Defs.Look(ref bufferKind, "PhobosDrillBufferKind");
        Scribe_Values.Look(ref SideProgress, "PhobosDrillSideProgress");
        Scribe_Values.Look(ref SideSteps, "PhobosDrillSideSteps");
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
        ReadNext();
        if (sideJob != null) return sideJob.WhyNot(this) ?? (!ElectricNow && !HasFuel ? "PhobosVehicleSystems_RigNoFuel".Translate().Resolve() : null);
        bool found = NextOre(out ThingDef? ore, out _, out _);
        if (!found && DeepDrillUtility.GetBaseResource(Vehicle.Map, Hole) == null) return "PhobosVehicleSystems_RigNothingToDrill".Translate().Resolve();
        if (found && ore != null && Piped(ore) && (BufferFull || (bufferKind != null && bufferKind != ore))) return "PhobosVehicleSystems_RigBufferFull".Translate().Resolve();
        return !ElectricNow && !HasFuel ? "PhobosVehicleSystems_RigNoFuel".Translate().Resolve() : null;
    }

    bool Piped(ThingDef ore) => Props.pipedResources.Contains(ore);

    /// <summary>The first ore within reach of the hole, as the game's drill looks for it, out to the mast's own reach. Piped ones count only with somewhere to put them.</summary>
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
            if (found == null || (Piped(found) && BufferCapacity <= 0f)) continue;
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

    /// <summary>After a side job changes what is at the hole.</summary>
    public void Reread() => ReadNext();

    public new void Tell(string text, MessageTypeDef type) => base.Tell(text, type);

    protected override void RareWork()
    {
        ReadNext();
        RunOutlets();
        if (Working && sideJob == null) MaybeInsects();
    }

    void ReadNext()
    {
        orePresent = NextOre(out ThingDef? ore, out _, out _);
        nextIsPiped = orePresent && ore != null && Piped(ore);
        sideJob = orePresent ? null : Props.sideJobs.FirstOrDefault(job => job.Wanted(this));
    }

    void RunOutlets()
    {
        foreach (RigBufferOutlet outlet in Props.bufferOutlets)
        {
            if (buffer < 1f) return;
            outlet.Give(this, buffer);
        }
    }

    /// <summary>A side job's own progress; the unit of deepchem being raised; or the portion of ore or stone being drilled.</summary>
    public override float? StepProgress
    {
        get
        {
            if (sideJob != null) return sideJob.Progress(this);
            if (nextIsPiped) return Props.pipedWorkPerUnit > 0f ? pipedProgress / Props.pipedWorkPerUnit : null;
            return portionProgress / WorkPerPortion;
        }
    }

    public override void Worked(Pawn worker, int delta)
    {
        MarkWorked();
        if (sideJob != null)
        {
            sideJob.Worked(this, worker, delta);
            return;
        }
        float factor = Vehicle.GetStatValue(RigDefOf.Phobos_DrillRate) * WorkPace;
        if (!orePresent) factor = DrillRule.StoneRate(factor, Vehicle.GetStatValue(RigDefOf.Phobos_BedrockFactor));
        // Framework 0.173.2: dark by the sky, so an eclipse slows unlit drilling as night does.
        if (!WorkLamps.Fitted(Vehicle) && When_Dark.IsDark(Vehicle)) factor *= Props.unlitFactor;
        float work = worker.GetStatValue(StatDefOf.DeepDrillingSpeed) * factor * delta;
        if (nextIsPiped)
        {
            RaisePiped(work);
            return;
        }
        portionProgress += work;
        portionYield += work * worker.GetStatValue(StatDefOf.MiningYield) / WorkPerPortion;
        if (portionProgress <= WorkPerPortion) return;
        ProducePortion(worker);
        portionProgress = 0f;
        portionYield = 0f;
    }

    /// <summary>Raises a piped resource a unit at a time into the buffer: never an item (owner, 9 Nov 2026).</summary>
    void RaisePiped(float work)
    {
        pipedProgress += work;
        while (pipedProgress >= Props.pipedWorkPerUnit)
        {
            pipedProgress -= Props.pipedWorkPerUnit;
            if (!NextOre(out ThingDef? ore, out int count, out IntVec3 cell) || ore == null || !Piped(ore) || BufferFull || (bufferKind != null && bufferKind != ore))
            {
                ReadNext();
                return;
            }
            Vehicle.Map.deepResourceGrid.SetAt(cell, ore, count - 1);
            buffer += 1f;
            bufferKind = ore;
            // Seen in play (Framework 0.293.0, row 15): a piped resource raised into the buffer, never an item.
            Seen.Note("vehicles.drill-piped", Vehicle, ore.defName);
        }
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
        // Into storage of the rig's side within reach of the hole first, nearest first (Framework 0.126.0, UH3); otherwise beside the hole as before.
        IEnumerable<IntVec3> reach = GenRadial.RadialCellsAround(hole, Props.storageReach, true).Where(c => !footprint.Contains(c));
        StorageNearby.Place(thing, map, reach, Vehicle.Faction, hole, p => p != hole && !footprint.Contains(p), out Building_Storage? into);
        if (into != null) Seen.Note("vehicles.rig-into-storage", Vehicle, into.LabelShort);
        // Seen in play (Framework 0.293.0, row 14): a portion drilled, wherever it went.
        Seen.Note("vehicles.drill-portion", Vehicle, ore.defName);
        VehicleWorkSounds.Play(Vehicle, "Portion");
        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.Mined, worker.Named(HistoryEventArgsNames.Doer)));
        ReadNext();
        if (!isOre || orePresent) return;
        ThingDef? bedrock = DeepDrillUtility.GetBaseResource(map, hole);
        VehicleWorkSounds.Play(Vehicle, "Spent");
        Tell(bedrock != null ? "PhobosVehicleSystems_RigSpent".Translate().Resolve() : "PhobosVehicleSystems_RigSpentNothing".Translate().Resolve(), MessageTypeDefOf.TaskCompletion);
    }

    // ---- Insects: the game's rule, kept to. It lives on a deep drill building and its storyteller, so the rig rolls it itself. ----

    bool InsectsRecently => Find.TickManager.TicksGame <= lastInsectsTick + InsectRefireTicks;

    void MaybeInsects()
    {
        if (InsectsRecently || SomethingElseHadInsects() || Faction.OfInsects == null) return;
        float chance = Find.Storyteller.difficulty.deepDrillInfestationChanceFactor;
        float baseMtb = Find.Storyteller.def.comps.OfType<StorytellerCompProperties_DeepDrillInfestation>().FirstOrDefault()?.baseMtbDaysPerDrill ?? -1f;
        // The odds scale with the rig's rate (owner, 9 Nov 2026): a rig ten times faster meets the hives ten times sooner, the same risk per portion of ore.
        float mtb = DrillRule.InsectMtbDays(baseMtb / chance, Vehicle.GetStatValue(RigDefOf.Phobos_DrillRate) * WorkPace);
        if (chance <= 0f || baseMtb <= 0f || !Rand.MTBEventOccurs(mtb, GenDate.TicksPerDay, GenTicks.TickRareInterval)) return;
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
        var lines = new List<string>();
        if (deployed && Overdrive) lines.Add("PhobosVehicleSystems_OverdriveLine".Translate().Resolve());
        string? side = deployed ? sideJob?.InspectLine(this) : null;
        if (side != null) lines.Add(side);
        else if (deployed && !nextIsPiped)
        {
            lines.Add((orePresent
                ? "PhobosVehicleSystems_RigProgressOre".Translate((portionProgress / WorkPerPortion).ToStringPercent().Named("PROGRESS"))
                : "PhobosVehicleSystems_RigProgressStone".Translate((portionProgress / WorkPerPortion).ToStringPercent().Named("PROGRESS"))).Resolve());
        }
        if (BufferCapacity > 0f && (deployed || buffer >= 1f))
        {
            lines.Add("PhobosVehicleSystems_RigBufferLine".Translate(buffer.ToString("N0").Named("AMOUNT"), BufferCapacity.ToString("N0").Named("CAPACITY")).Resolve());
        }
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }
}
