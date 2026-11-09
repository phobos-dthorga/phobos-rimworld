using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using SmashTools;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

[DefOf]
public static class RigDefOf
{
    public static VehicleStatDef Phobos_ScanRate = null!;
    public static VehicleStatDef Phobos_SurveyLongRange = null!;
    public static VehicleStatDef Phobos_SurveyFastFactor = null!;
    public static VehicleStatDef Phobos_BatteryCapacity = null!;
    public static VehicleStatDef Phobos_WorkLights = null!;
    public static VehicleStatDef Phobos_DrillRate = null!;
    public static VehicleStatDef Phobos_DrillReach = null!;
    public static VehicleStatDef Phobos_BedrockFactor = null!;
    public static VehicleStatDef Phobos_ElectricDrill = null!;
    public static VehicleStatDef Phobos_RigBuffer = null!;
    public static VehicleStatDef Phobos_WellKit = null!;
    public static VehicleStatDef Phobos_Hydrogeology = null!;
    public static JobDef PhobosVehicleSystems_Survey = null!;
    public static JobDef PhobosVehicleSystems_Drill = null!;

    static RigDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RigDefOf));
}

public class CompProperties_PhobosRigWork : VehicleCompProperties
{
    /// <summary>The small building that plugs a set-up vehicle into a grid.</summary>
    public ThingDef? powerPointDef;
    public GraphicDataOverlay? setUpOverlay;
    /// <summary>The parts that swing out when it sets up, each its own picture.</summary>
    public List<SetUpMovingPart> movingParts = new();
    /// <summary>Authored, as the tanker's: below these it will not set up, and below the last it will not work.</summary>
    public float stabilityFloor = 0.5f;
    public float controlFloor = 0.5f;
    public float workControlFloor = 0.35f;
    /// <summary>Parts without which the work stops, whatever the averages say.</summary>
    public List<string> vitalParts = new();
    /// <summary>Overdrive (owner, 9 Nov 2026: the vehicle's overclock). Authored: the rate, what the fuel is multiplied by, the health the working parts lose an hour, the share of
    /// health at which it cuts out, and the coolant temperature that stops it (the cooling system's "overheating" stage starts at 120).</summary>
    public float overdriveFactor = 2f;
    public float overdriveFuelFactor = 2.5f;
    public float overdrivePartDamagePerHour = 2f;
    public float overdriveOffBelow = 0.5f;
    public float overdriveHotAbove = 120f;
}

/// <summary>
/// A vehicle that sets up and is worked by a colonist standing beside it: the survey truck, the drill rig. It holds what they share: the operator's spot, the floors, the engine that
/// turns with nobody driving, and the power point that makes it a real load on a grid (the game's power code takes only buildings, as with the generator set's cable end).
/// </summary>
public abstract class CompPhobosRigWork : CompPhobosSetUp, IVehicleEngineDemand
{
    const int WorkedWindowTicks = 300;
    const float DaysPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerDay;
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;
    const string CoolantQuantity = "coolantTemperature";

    Building? powerPoint;
    int lastWorkedTick = -99999;
    bool overdrive;

    public CompProperties_PhobosRigWork RigProps => (CompProperties_PhobosRigWork)props;
    protected override GraphicDataOverlay? SetUpLook => RigProps.setUpOverlay;
    protected override List<SetUpMovingPart>? MovingParts => RigProps.movingParts;

    public abstract JobDef WorkJob { get; }
    public abstract SkillDef WorkSkill { get; }
    public abstract float LearnPerTick { get; }
    public virtual EffecterDef? WorkEffect => null;
    public abstract string SetUpDescription { get; }
    public abstract string PackUpDescription { get; }

    public abstract bool WantsEngine { get; }
    public abstract float EngineDemand { get; }
    /// <summary>The stat the working parts feed: what overdrive wears.</summary>
    protected abstract VehicleStatDef WorkStat { get; }

    public bool Overdrive => overdrive;
    /// <summary>What the work rate is multiplied by now.</summary>
    public float OverdriveFactor => OverdriveRule.Rate(1f, overdrive, RigProps.overdriveFactor);
    /// <summary>What a subclass's engine demand becomes under overdrive: flat out.</summary>
    protected float Demand(float usual) => overdrive ? 1f : usual;
    protected abstract float FuelPerDay { get; }
    protected abstract float DrawWatts { get; }
    protected virtual bool WantsPowerPoint => true;

    /// <summary>Where the work happens, for choosing the operator's spot.</summary>
    protected virtual IntVec3 WorkSpot => Vehicle.Position;

    public abstract void Worked(Pawn worker, int delta);
    protected abstract void RareWork();

    /// <summary>Someone has been working here in the last few seconds.</summary>
    public bool Working => Find.TickManager.TicksGame - lastWorkedTick <= WorkedWindowTicks;

    protected void MarkWorked() => lastWorkedTick = Find.TickManager.TicksGame;

    /// <summary>On a grid that has power to give.</summary>
    public bool PluggedIn => powerPoint is { Spawned: true } && powerPoint.GetComp<CompPowerTrader>() is { PowerOn: true };

    protected CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    protected bool HasFuel => Vehicle.CompFueledTravel is not { EmptyTank: true };

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref powerPoint, "PhobosRigPowerPoint");
        Scribe_Values.Look(ref overdrive, "PhobosRigOverdrive");
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (deployed) ShowSetUp(true);
        });
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Leaving the map, for a caravan or a launch, or being destroyed, packs it up.
        ClearSetUp();
        base.PostDeSpawn(map, mode);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (deployed) RunRare();
    }

    void RunRare()
    {
        using (PhobosTiming.Measure("vehicle rig work"))
        {
            EnsurePowerPoint();
            RareWork();
            RunOverdrive();
            CompPowerTrader? trader = powerPoint?.GetComp<CompPowerTrader>();
            if (trader != null) trader.PowerOutput = -DrawWatts * OverdriveFactor;
            if (WantsEngine && HasFuel) Vehicle.CompFueledTravel?.ConsumeFuel(OverdriveRule.FuelPerDay(FuelPerDay, overdrive, RigProps.overdriveFuelFactor) * DaysPerRareTick);
        }
    }

    void EnsurePowerPoint()
    {
        if (!WantsPowerPoint)
        {
            RemovePowerPoint();
            return;
        }
        if (powerPoint is { Spawned: true } || RigProps.powerPointDef == null) return;
        IntVec3 cell = BestAdjacentCell();
        if (!cell.IsValid) return;
        Thing placed = ThingMaker.MakeThing(RigProps.powerPointDef);
        placed.SetFaction(Faction.OfPlayer);
        powerPoint = (Building)GenSpawn.Spawn(placed, cell, Vehicle.Map);
    }

    void RemovePowerPoint()
    {
        if (powerPoint is { Destroyed: false }) powerPoint.Destroy(DestroyMode.Vanish);
        powerPoint = null;
    }

    /// <summary>The parts that feed the work stat: what overdrive wears, and what stops it when worn.</summary>
    IEnumerable<VehicleComponent> WorkingParts() => Vehicle.statHandler.components.Where(p => p.props.categories != null && p.props.categories.Contains(WorkStat));

    void RunOverdrive()
    {
        if (!overdrive) return;
        if (Working)
        {
            float wear = OverdriveRule.Wear(RigProps.overdrivePartDamagePerHour, HoursPerRareTick);
            foreach (VehicleComponent part in WorkingParts())
            {
                if (part.Health > 0f) part.SetHealth(Mathf.Max(0f, part.Health - wear));
            }
        }
        float lowest = WorkingParts().Select(p => p.HealthPercent).DefaultIfEmpty(1f).Min();
        float coolant = Systems != null && Systems.TryGetQuantity(CoolantQuantity, out float value) ? value : float.NaN;
        OverdriveStop stop = OverdriveRule.MustStop(lowest, RigProps.overdriveOffBelow, coolant, RigProps.overdriveHotAbove);
        if (stop == OverdriveStop.None) return;
        overdrive = false;
        Tell((stop == OverdriveStop.PartWorn ? "PhobosVehicleSystems_OverdriveStoppedWorn" : "PhobosVehicleSystems_OverdriveStoppedHot").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.CautionInput);
    }

    /// <summary>Why overdrive cannot be switched on now, or null.</summary>
    public string? CannotOverdrive()
    {
        float lowest = WorkingParts().Select(p => p.HealthPercent).DefaultIfEmpty(1f).Min();
        if (lowest < RigProps.overdriveOffBelow) return "PhobosVehicleSystems_OverdriveWorn".Translate().Resolve();
        return null;
    }

    public void SetOverdrive(bool on)
    {
        if (on)
        {
            string? refused = CannotOverdrive();
            if (refused != null)
            {
                Tell(refused, MessageTypeDefOf.RejectInput);
                return;
            }
        }
        overdrive = on;
        Tell((on ? "PhobosVehicleSystems_OverdriveOn" : "PhobosVehicleSystems_OverdriveOff").Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.SilentInput);
    }

    void ClearSetUp()
    {
        deployed = false;
        lastWorkedTick = -99999;
        ShowSetUp(false);
        RemovePowerPoint();
    }

    // ---- Services the gizmos and the job call ----

    /// <summary>Why it cannot set up now, or null.</summary>
    public virtual string? CannotDeploy()
    {
        if (!Vehicle.Spawned) return "PhobosVehicleSystems_RigNotOnMap".Translate().Resolve();
        if (Vehicle.Drafted) return "PhobosVehicleSystems_RigStopFirst".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigStability) < RigProps.stabilityFloor) return "PhobosVehicleSystems_RigOutriggers".Translate().Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < RigProps.controlFloor) return "PhobosVehicleSystems_RigControls".Translate().Resolve();
        return MovingPartsRefusal();
    }

    /// <summary>Why nobody can work it now, or null.</summary>
    public virtual string? WhyNoWork()
    {
        if (!deployed) return "PhobosVehicleSystems_RigNotSetUp".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Vehicle.GetStatValue(GeneratorDefOf.Phobos_RigControl) < RigProps.workControlFloor) return "PhobosVehicleSystems_RigWorkControls".Translate().Resolve();
        foreach (string key in RigProps.vitalParts)
        {
            VehicleComponent? part = Vehicle.statHandler.GetComponent(key);
            if (part != null && part.Health <= 0f) return "PhobosVehicleSystems_RigPartGone".Translate(part.props.label.Named("PART")).Resolve();
        }
        return null;
    }

    /// <summary>A free cell beside the vehicle for the operator, as near the work as can be; invalid if there is none.</summary>
    public IntVec3 OperatorCell(Pawn pawn)
    {
        IntVec3 spot = WorkSpot;
        return Vehicle.OccupiedRect().ExpandedBy(1).ClipInsideMap(Vehicle.Map).EdgeCells
            .Where(c => c.Standable(Vehicle.Map) && pawn.CanReserveAndReach(c, PathEndMode.OnCell, Danger.Deadly))
            .OrderBy(c => c.DistanceToSquared(spot)).FirstOrFallback(IntVec3.Invalid);
    }

    public void SetUp()
    {
        string? refused = CannotDeploy();
        if (refused != null)
        {
            Tell(refused, MessageTypeDefOf.RejectInput);
            return;
        }
        deployed = true;
        StartMotion(true);
        ShowSetUp(true);
        RunRare();
        Tell("PhobosVehicleSystems_RigSetUp".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    public void PackUp()
    {
        ClearSetUp();
        StartMotion(false);
        Tell("PhobosVehicleSystems_RigPackedUp".Translate(Vehicle.LabelShort.Named("VEHICLE")), MessageTypeDefOf.SilentInput);
    }

    protected override string SetUpRefusal => "PhobosVehicleSystems_RigSetUpRefusal".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();

    protected void Tell(string text, MessageTypeDef type) => Messages.Message(text, Vehicle, type, historical: false);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer) yield break;
        yield return RigGizmos.SetUpButton(this);
        yield return RigGizmos.OverdriveToggle(this);
        foreach (Gizmo gizmo in ExtraGizmos()) yield return gizmo;
    }

    protected virtual IEnumerable<Gizmo> ExtraGizmos() => Enumerable.Empty<Gizmo>();
}

/// <summary>Finds deployed rigs for colonists to work. One subclass per kind, because each is a different kind of work.</summary>
public abstract class WorkGiver_PhobosRigWork : WorkGiver_Scanner
{
    protected abstract CompPhobosRigWork? WorkOf(VehiclePawn vehicle);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Pawn other in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
        {
            if (other is VehiclePawn vehicle && WorkOf(vehicle) is { Deployed: true }) yield return vehicle;
        }
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !PotentialWorkThingsGlobal(pawn).Any();

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not VehiclePawn vehicle || t.Faction != pawn.Faction || vehicle.IsForbidden(pawn)) return null;
        CompPhobosRigWork? work = WorkOf(vehicle);
        if (work == null || !work.Deployed) return null;
        string? why = work.WhyNoWork();
        if (why != null)
        {
            JobFailReason.Is(why);
            return null;
        }
        IntVec3 cell = work.OperatorCell(pawn);
        return cell.IsValid ? JobMaker.MakeJob(work.WorkJob, vehicle, cell) : null;
    }
}

public class WorkGiver_PhobosSurvey : WorkGiver_PhobosRigWork
{
    protected override CompPhobosRigWork? WorkOf(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosSurveyTruck>();
}

public class WorkGiver_PhobosDrill : WorkGiver_PhobosRigWork
{
    protected override CompPhobosRigWork? WorkOf(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosDrillRig>();
}

/// <summary>A colonist stands beside a set-up vehicle and works it until it can no longer be worked.</summary>
public class JobDriver_PhobosRigWork : JobDriver
{
    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    CompPhobosRigWork? Work => Vehicle.AllComps.OfType<CompPhobosRigWork>().FirstOrDefault();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetB, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => Work == null || Work.WhyNoWork() != null);
        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        CompPhobosRigWork? rig = Work;
        Toil work = ToilMaker.MakeToil("PhobosRigWork");
        work.tickIntervalAction = delta =>
        {
            if (rig == null) return;
            rig.Worked(work.actor, delta);
            work.actor.skills?.Learn(rig.WorkSkill, rig.LearnPerTick * delta);
        };
        work.defaultCompleteMode = ToilCompleteMode.Never;
        if (rig?.WorkEffect != null) work.WithEffect(rig.WorkEffect, TargetIndex.A);
        work.activeSkill = () => rig?.WorkSkill;
        yield return work;
    }
}
