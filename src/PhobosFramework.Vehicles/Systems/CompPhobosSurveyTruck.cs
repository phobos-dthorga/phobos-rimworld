using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A sensor upgrade's bias: what it makes a deep find more likely to be.</summary>
public class SurveySensorWeight
{
    public VehicleStatDef? stat;
    public ThingDef? ore;
    public float factor = 1f;
}

/// <summary>Something more a set-up truck can show. Subclass it in any mod and name it in XML with Class="Namespace.ClassName".</summary>
public abstract class SurveyExtra
{
    public virtual void DrawSelected(CompPhobosSurveyTruck truck) { }

    public virtual string? InspectLine(CompPhobosSurveyTruck truck) => null;
}

public class CompProperties_PhobosSurveyTruck : CompProperties_PhobosRigWork
{
    /// <summary>A tenth of the game's ground-penetrating and long-range scanners' own figures (3 and 6 days, 4 and 8, from their Defs): the owner's order of magnitude, 9 Nov 2026.</summary>
    public float mtbDays = 0.3f;
    public float guaranteedDays = 0.6f;
    public float longRangeMtbDays = 0.4f;
    public float longRangeGuaranteedDays = 0.8f;
    /// <summary>The owner's curve.</summary>
    public float curveStep = 1.5f;
    public float curveCap = 8f;
    /// <summary>Game watts drawn from a grid. Authored: the console is the game's scanner; the array is priced so one generator set can run it.</summary>
    public float consoleWatts = 700f;
    public float arrayWatts = 1000f;
    public float idleFuelPerDay = 8f;
    /// <summary>The engine's speed while recharging, as a share of the way from idle to rated: about 1,200 rpm.</summary>
    public float highIdleShare = 0.33f;
    public List<SurveySensorWeight> sensorWeights = new();
    /// <summary>What other mods' tie-ins show while the truck is set up.</summary>
    public List<SurveyExtra> extras = new();

    public CompProperties_PhobosSurveyTruck() => compClass = typeof(CompPhobosSurveyTruck);
}

/// <summary>How many finds a survey truck has made on this map, which slows the next (the owner's curve). The game's own scanners are not counted.</summary>
public sealed class MapComponent_PhobosSurvey : MapComponent
{
    public int finds;

    public MapComponent_PhobosSurvey(Map map) : base(map) { }

    public override void ExposeData() => Scribe_Values.Look(ref finds, "PhobosSurveyFinds");
}

/// <summary>
/// The survey truck (docs/development/vehicle-survey-truck.md). The game's scanners only work as buildings, so this runs their routine itself: the same rolls, on the same deep resource grid,
/// with the same ore weights and lump sizes, on a colonist's Intellectual work. Gizmos read this and call it.
/// </summary>
public sealed class CompPhobosSurveyTruck : CompPhobosRigWork
{
    const int RollIntervalTicks = 59;
    const float TicksPerDay = GenDate.TicksPerDay;
    const string ArrayLoad = "surveyArray";
    const string ChargeQuantity = "charge";
    const string BatteryParameter = "batteryAmpHours";

    float daysWorking;
    bool longRange;
    ThingDef? target;
    bool recharging;

    int rollTicks;
    SurveyPower power = new(true, false, false, false, SurveyNote.None);

    public CompProperties_PhobosSurveyTruck Props => (CompProperties_PhobosSurveyTruck)props;
    protected override string DeployedSaveKey => "PhobosSurveyDeployed";
    public override JobDef WorkJob => RigDefOf.PhobosVehicleSystems_Survey;
    public override SkillDef WorkSkill => SkillDefOf.Intellectual;
    public override float LearnPerTick => 0.035f;
    public override string SetUpDescription => "PhobosVehicleSystems_SurveyGizmoSetUpDesc".Translate().Resolve();
    public override string PackUpDescription => "PhobosVehicleSystems_SurveyGizmoPackUpDesc".Translate().Resolve();

    public override bool WantsEngine => deployed && power.EngineWanted;
    public override float EngineDemand => Demand(power.Recharging || power.Fast ? Props.highIdleShare : 0f);
    protected override VehicleStatDef WorkStat => RigDefOf.Phobos_ScanRate;
    protected override float FuelPerDay => Props.idleFuelPerDay;
    /// <summary>Drawn whenever it is set up, as the game's scanner draws whenever it is on: a net that cannot carry it then shows as not plugged in, rather than flickering.</summary>
    protected override float DrawWatts => FastFactor > 1f && Systems?.SwitchOn(ArrayLoad) == true ? Props.arrayWatts : Props.consoleWatts;

    public bool LongRangeFitted => Vehicle.GetStatValue(RigDefOf.Phobos_SurveyLongRange) > 0f;
    public bool LongRange => longRange && LongRangeFitted;
    public ThingDef Target => target ?? ThingDefOf.MineableGold;
    float FastFactor => Vehicle.GetStatValue(RigDefOf.Phobos_SurveyFastFactor);
    float BatteryCapacity => Vehicle.GetStatValue(RigDefOf.Phobos_BatteryCapacity);
    int FindsHere => Vehicle.Map.GetComponent<MapComponent_PhobosSurvey>()?.finds ?? 0;
    float CurveNow => LongRange ? 1f : SurveyRule.TimeFactor(FindsHere, Props.curveStep, Props.curveCap);
    float GuaranteedDays => (LongRange ? Props.longRangeGuaranteedDays : Props.guaranteedDays) * CurveNow;

    /// <summary>Whether the truck's own electrics are carrying this right now.</summary>
    public bool Drawing(string what) => deployed && !PluggedIn && what switch
    {
        "Console" => Working && power.CanScan,
        "Array" => Working && power.Fast,
        _ => false,
    };

    static readonly string[] ConsoleKey = { "Console" };
    protected override string[] WorkSoundKeys => ConsoleKey;

    /// <summary>The console while someone surveys, the array while it is powered for a fast survey, the dish while it reaches far.</summary>
    protected override void SyncWorkSound()
    {
        base.SyncWorkSound();
        bool on = deployed && Working;
        VehicleWorkSounds.Loop(Vehicle, "Array", on && power.Fast);
        VehicleWorkSounds.Loop(Vehicle, "Dish", on && LongRange);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref daysWorking, "PhobosSurveyDays");
        Scribe_Values.Look(ref longRange, "PhobosSurveyLongRange");
        Scribe_Defs.Look(ref target, "PhobosSurveyTarget");
        Scribe_Values.Look(ref recharging, "PhobosSurveyRecharging");
    }

    protected override void RareWork()
    {
        CompPhobosVehicleSystems? systems = Systems;
        float capacity = BatteryCapacity;
        if (systems == null) return;
        if (capacity > 0f) systems.SetParameterFloor(BatteryParameter, capacity);
        float charge = systems.TryGetQuantity(ChargeQuantity, out float value) ? value : 1f;
        power = SurveyRule.Decide(PluggedIn, capacity > 0f, HasFuel, FastFactor > 1f, systems.SwitchOn(ArrayLoad), Working, charge, recharging);
        recharging = power.Recharging;
    }

    public override string? WhyNoWork()
    {
        string? why = base.WhyNoWork();
        if (why != null) return why;
        if (RoofUtility.IsAnyCellUnderRoof(Vehicle)) return "PhobosVehicleSystems_SurveyRoofed".Translate().Resolve();
        if (!LongRange && !Vehicle.Map.Biome.hasBedrock) return "PhobosVehicleSystems_SurveyNoBedrock".Translate().Resolve();
        return power.CanScan ? null : NoteLine();
    }

    string? NoteLine() => power.Note switch
    {
        SurveyNote.NoPower => "PhobosVehicleSystems_SurveyNoPower".Translate().Resolve(),
        SurveyNote.ArrayRecharging => "PhobosVehicleSystems_SurveyArrayRecharging".Translate().Resolve(),
        SurveyNote.ArrayNoPower => "PhobosVehicleSystems_SurveyArrayNoPower".Translate().Resolve(),
        _ => null,
    };

    public override void Worked(Pawn worker, int delta)
    {
        MarkWorked();
        float speed = worker.GetStatValue(StatDefOf.ResearchSpeed) * Vehicle.GetStatValue(RigDefOf.Phobos_ScanRate) * (power.Fast ? FastFactor : 1f) * WorkPace;
        daysWorking += speed * delta / TicksPerDay;
        rollTicks += delta;
        if (rollTicks < RollIntervalTicks) return;
        rollTicks -= RollIntervalTicks;
        if (speed <= 0f) return;
        float mtbDays = (LongRange ? Props.longRangeMtbDays : Props.mtbDays) * CurveNow;
        if (daysWorking < GuaranteedDays && !Rand.MTBEventOccurs(mtbDays / speed, TicksPerDay, RollIntervalTicks)) return;
        DoFind(worker);
        daysWorking = 0f;
    }

    void DoFind(Pawn worker)
    {
        VehicleWorkSounds.Play(Vehicle, "Find");
        if (LongRange) FindOnTheWorld(worker);
        else FindHere(worker);
    }

    void FindOnTheWorld(Pawn worker)
    {
        var slate = new Slate();
        slate.Set("map", Vehicle.Map);
        slate.Set("targetMineable", Target);
        slate.Set("targetMineableThing", Target.building.mineableThing);
        slate.Set("worker", worker);
        if (!QuestScriptDefOf.LongRangeMineralScannerLump.CanRun(slate, Vehicle.Map)) return;
        Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(QuestScriptDefOf.LongRangeMineralScannerLump, slate);
        Find.LetterStack.ReceiveLetter(quest.name, quest.description, LetterDefOf.PositiveEvent, null, null, quest);
    }

    /// <summary>The game's deep find, with the sensor upgrades' bias on which ore it is.</summary>
    void FindHere(Pawn worker)
    {
        Map map = Vehicle.Map;
        if (!CellFinderLoose.TryFindRandomNotEdgeCellWith(10, x => CanScatterAt(x, map), map, out IntVec3 centre))
        {
            Tell("PhobosVehicleSystems_SurveyNoGround".Translate().Resolve(), MessageTypeDefOf.NeutralEvent);
            return;
        }
        ThingDef ore = DefDatabase<ThingDef>.AllDefs.RandomElementByWeight(def => def.deepCommonality * SensorFactor(def));
        int cells = Mathf.CeilToInt(ore.deepLumpSizeRange.RandomInRange);
        foreach (IntVec3 cell in GridShapeMaker.IrregularLump(centre, map, cells))
        {
            if (CanScatterAt(cell, map) && !cell.InNoBuildEdgeArea(map)) map.deepResourceGrid.SetAt(cell, ore, ore.deepCountPerCell);
        }
        MapComponent_PhobosSurvey? record = map.GetComponent<MapComponent_PhobosSurvey>();
        if (record != null) record.finds++;
        Find.LetterStack.ReceiveLetter("PhobosVehicleSystems_SurveyFoundLabel".Translate(ore.LabelCap.Named("ORE")),
            "PhobosVehicleSystems_SurveyFoundText".Translate(ore.label.Named("ORE"), worker.Named("FINDER")),
            LetterDefOf.PositiveEvent, new LookTargets(centre, map));
    }

    float SensorFactor(ThingDef ore)
    {
        float factor = 1f;
        foreach (SurveySensorWeight weight in Props.sensorWeights)
        {
            if (weight.ore == ore && weight.stat != null && Vehicle.GetStatValue(weight.stat) > 0f) factor *= weight.factor;
        }
        return factor;
    }

    /// <summary>The game's test for ground a lump can go in.</summary>
    static bool CanScatterAt(IntVec3 cell, Map map)
    {
        TerrainDef? terrain = map.terrainGrid.BaseTerrainAt(cell);
        if (terrain != null && terrain.IsWater && terrain.passability == Traversability.Impassable) return false;
        return cell.GetAffordances(map).Contains(ThingDefOf.DeepDrill.terrainAffordanceNeeded) && map.deepResourceGrid.ThingDefAt(cell) == null;
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (!deployed) return;
        Vehicle.Map.deepResourceGrid.MarkForDraw();
        foreach (SurveyExtra extra in Props.extras) extra.DrawSelected(this);
    }

    /// <summary>Whether a survey truck is set up on this map, for whatever asks after a powered deep scanner.</summary>
    public static bool AnySetUp(Map map)
    {
        foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (pawn is VehiclePawn vehicle && vehicle.GetComp<CompPhobosSurveyTruck>() is { Deployed: true }) return true;
        }
        return false;
    }

    public void SetLongRange(bool on)
    {
        longRange = on;
        daysWorking = 0f;
    }

    public void SetTarget(ThingDef mineable) => target = mineable;

    protected override IEnumerable<Gizmo> ExtraGizmos()
    {
        if (LongRangeFitted)
        {
            yield return RigGizmos.LongRangeToggle(this);
            if (LongRange) yield return RigGizmos.TargetButton(this);
        }
        if (DebugSettings.ShowDevGizmos && deployed)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Find now",
                action = () => DoFind(PawnsFinder.AllMaps_FreeColonists.RandomElement()),
            };
        }
    }

    public override string? CompInspectStringExtra()
    {
        if (!deployed) return null;
        var lines = new List<string>
        {
            (LongRange ? "PhobosVehicleSystems_SurveyLongRangeLine".Translate(Target.building.mineableThing.label.Named("ORE")) : "PhobosVehicleSystems_SurveyDeepLine".Translate()).Resolve(),
        };
        if (Overdrive) lines.Add("PhobosVehicleSystems_OverdriveLine".Translate().Resolve());
        string? note = NoteLine();
        if (note != null) lines.Add(note);
        else if (power.Recharging) lines.Add("PhobosVehicleSystems_SurveyRecharging".Translate().Resolve());
        lines.Add("PhobosVehicleSystems_SurveyProgress".Translate((daysWorking / GuaranteedDays).ToStringPercent().Named("PROGRESS")).Resolve());
        foreach (SurveyExtra extra in Props.extras)
        {
            string? line = extra.InspectLine(this);
            if (line != null) lines.Add(line);
        }
        return string.Join("\n", lines);
    }
}
