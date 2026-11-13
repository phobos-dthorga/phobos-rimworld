using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

[DefOf]
public static class VehicleSystemsJobDefOf
{
    public static JobDef PhobosVehicleSystems_LoadServiceSupplies = null!;
    public static JobDef PhobosVehicleSystems_ServiceVehicle = null!;
    public static JobDef PhobosVehicleSystems_JumpStart = null!;
    public static JobDef PhobosVehicleSystems_RepairWithSupplies = null!;
    public static JobDef PhobosVehicleSystems_Diagnose = null!;
    public static JobDef PhobosVehicleSystems_SlingHookUp = null!;
    public static JobDef PhobosVehicleSystems_HitchUp = null!;
    public static JobDef PhobosVehicleSystems_TrailerSetUp = null!;

    static VehicleSystemsJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(VehicleSystemsJobDefOf));
}

static class ServiceTargets
{
    public static IEnumerable<Thing> Vehicles(Pawn pawn)
    {
        foreach (Pawn other in pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction))
        {
            if (other is VehiclePawn vehicle && vehicle.GetComp<CompPhobosVehicleSystems>()?.Usable == true) yield return vehicle;
        }
    }

    public static bool Workable(Pawn pawn, VehiclePawn vehicle, bool forced) =>
        vehicle.Spawned && !vehicle.Drafted && !vehicle.vehiclePather.Moving && !vehicle.IsForbidden(pawn) && (forced || !vehicle.IsBurning());
}

/// <summary>Hauls the first supply a due service lacks into the vehicle's cargo (oil, coolant, parts). Hauling work.</summary>
public class WorkGiver_PhobosLoadServiceSupplies : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ServiceTargets.Vehicles(pawn);

    public override bool ShouldSkip(Pawn pawn, bool forced = false) => !ServiceTargets.Vehicles(pawn).GetEnumerator().MoveNext();

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not VehiclePawn vehicle || t.Faction != pawn.Faction || !ServiceTargets.Workable(pawn, vehicle, forced)) return null;
        CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
        if (comp == null) return null;
        List<(ThingDef def, int count)> missing = comp.MissingSupplies();
        if (missing.Count == 0) return null;
        Thing? supply = null;
        int count = 0;
        // The first thing that can be found: one part nobody has made must not hold up everything else the vehicle is waiting for.
        foreach ((ThingDef wanted, int wantedCount) in missing)
        {
            supply = Find(pawn, wanted);
            count = wantedCount;
            if (supply != null) break;
            // Our part is not to be had: another mod's, where one would do.
            foreach ((ThingDef other, int otherCount) in comp.Repairs.AlternativesFor(wanted))
            {
                supply = Find(pawn, other);
                count = otherCount;
                if (supply != null) break;
            }
            if (supply != null) break;
        }
        if (supply == null)
        {
            JobFailReason.Is("PhobosVehicleSystems_NoSupply".Translate(missing[0].def.label.Named("SUPPLY")));
            return null;
        }
        IntVec3 cell = vehicle.SurroundingCells.Where(c => c.Standable(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .RandomElementWithFallback(IntVec3.Invalid);
        if (!cell.IsValid) return null;
        Job job = JobMaker.MakeJob(VehicleSystemsJobDefOf.PhobosVehicleSystems_LoadServiceSupplies, vehicle, cell, supply);
        job.count = System.Math.Min(count, supply.stackCount);
        return job;
    }

    static Thing? Find(Pawn pawn, ThingDef def) => GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def),
        PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
}

/// <summary>Carries the supply to the vehicle and puts it in its cargo.</summary>
public class JobDriver_PhobosLoadServiceSupplies : JobDriver
{
    const TargetIndex VehicleIndex = TargetIndex.A;
    const TargetIndex CellIndex = TargetIndex.B;
    const TargetIndex SupplyIndex = TargetIndex.C;

    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(VehicleIndex).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) =>
        pawn.Reserve(job.GetTarget(SupplyIndex), job, 1, job.count, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(VehicleIndex);
        this.FailOn(() => Vehicle.Drafted);
        yield return Toils_Goto.GotoThing(SupplyIndex, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(SupplyIndex);
        yield return Toils_Haul.StartCarryThing(SupplyIndex, putRemainderInQueue: false, subtractNumTakenFromJobCount: false);
        yield return Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell);
        var stow = new Toil
        {
            initAction = () =>
            {
                Thing? carried = pawn.carryTracker.CarriedThing;
                if (carried == null) return;
                pawn.carryTracker.innerContainer.TryTransferToContainer(carried, Vehicle.inventory.innerContainer, carried.stackCount);
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
        yield return stow;
    }
}

/// <summary>Services a vehicle whose due service has its supplies aboard: Construction work at the vehicle, then the restores.</summary>
public class WorkGiver_PhobosServiceVehicle : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_ServiceVehicle;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ServiceTargets.Vehicles(pawn);

    public override bool CanBeWorkedOn(VehiclePawn vehicle) =>
        !vehicle.Drafted && vehicle.GetComp<CompPhobosVehicleSystems>() is { RepairsPaused: false } comp && comp.ReadyService() != null;
}

/// <summary>
/// Work at a vehicle that may take longer than Vehicle Framework's own repair step. Its base class ends the work toil after 2,000 ticks
/// unless told otherwise, whatever work is left, and the job then repeats for ever without finishing; and it does not save the work
/// done, so a load would finish the job at once. Read from Vehicle Framework 1.6 (JobDriver_WorkVehicle), 9 Nov 2026.
/// </summary>
public abstract class PhobosVehicleWorkDriver : JobDriver_WorkVehicle, Phobos.Framework.Interface.IDrawsOwnWorkBar
{
    readonly Phobos.Framework.Rules.ProgressWatch watch = new();

    protected override ToilCompleteMode ToilCompleteMode => ToilCompleteMode.Never;

    protected CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    /// <summary>With no time limit on the work, a mechanic who cannot advance it (a work speed of nothing) would stand there for ever.</summary>
    bool Stalled()
    {
        if (pawn.pather.Moving)
        {
            watch.Reset();
            return false;
        }
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        if (!watch.Note(Find.TickManager.TicksGame, Work, rules.repairStallTicks, rules.repairStepGapTicks)) return false;
        Systems?.PauseRepairs(pawn);
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(Stalled);
        foreach (Toil toil in WorkBars.WithBar(base.MakeNewToils(), EffecterDef != null, GetProgressPct)) yield return toil;
    }

    /// <summary>The job ended and did nothing: repairs wait, rather than a mechanic being sent straight back.</summary>
    protected void Finished(Pawn actor, bool done)
    {
        if (!done) Systems?.PauseRepairs(actor);
        ReadyForNextToil();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        float work = Work;
        Scribe_Values.Look(ref work, "PhobosVehicleSystems_Work");
        if (Scribe.mode == LoadSaveMode.LoadingVars) Work = work;
    }
}

public class JobDriver_PhobosServiceVehicle : PhobosVehicleWorkDriver
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_ServiceVehicle;

    protected override float TotalWork => (Vehicle.GetComp<CompPhobosVehicleSystems>()?.ReadyService()?.Def.service?.work ?? 1f) * (Systems?.BayWorkFactor ?? 1f);

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    protected override void WorkComplete(Pawn actor)
    {
        Finished(actor, Systems?.CompleteService(actor) == true);
    }
}

/// <summary>Diagnoses a vehicle the player asked about, standing within reach of a working tuning rig (Framework 0.73.0).</summary>
public class WorkGiver_PhobosDiagnose : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_Diagnose;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ServiceTargets.Vehicles(pawn);

    public override bool CanBeWorkedOn(VehiclePawn vehicle) =>
        !vehicle.Drafted && vehicle.GetComp<CompPhobosVehicleSystems>() is { RepairsPaused: false } comp && comp.Diagnostics.Ready;
}

public class JobDriver_PhobosDiagnose : PhobosVehicleWorkDriver
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_Diagnose;

    protected override float TotalWork => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().diagnoseWork * (Systems?.BayWorkFactor ?? 1f);

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Driven off from the rig, or the request withdrawn: stop without a report.
        this.FailOn(() => Systems?.Diagnostics.Ready != true);
        foreach (Toil toil in base.MakeNewToils()) yield return toil;
    }

    protected override void WorkComplete(Pawn actor) => Finished(actor, Systems?.Diagnostics.Complete(actor) == true);
}

/// <summary>Does a repair whose supplies are aboard: fits a new part, puts a serious condition right, or patches a leak.</summary>
public class WorkGiver_PhobosRepairWithSupplies : VehicleWorkGiver
{
    public override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_RepairWithSupplies;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => ServiceTargets.Vehicles(pawn);

    public override bool CanBeWorkedOn(VehiclePawn vehicle) =>
        !vehicle.Drafted && vehicle.GetComp<CompPhobosVehicleSystems>() is { RepairsPaused: false } comp && comp.Repairs.Ready != null;

    /// <summary>A job may name a minimum skill: a mechanic below it is refused and told why (owner: no random failure).</summary>
    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is VehiclePawn vehicle && vehicle.GetComp<CompPhobosVehicleSystems>() is { } comp && comp.Repairs.Ready is { } ready && comp.Repairs.ReadyFor(pawn) == null)
        {
            JobFailReason.Is("PhobosVehicleSystems_FitNeedsSkill".Translate(ready.MinSkill.Named("SKILL")));
            return null;
        }
        return base.JobOnThing(pawn, t, forced);
    }
}

public class JobDriver_PhobosRepairWithSupplies : PhobosVehicleWorkDriver
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_RepairWithSupplies;

    string? workKey;

    RepairWork? Current => workKey == null ? null : Systems?.Repairs.Find(workKey);

    protected override float TotalWork => (Current is { } work && Systems is { } systems ? systems.Repairs.WorkFor(work) : 1f) * (Systems?.BayWorkFactor ?? 1f);

    /// <summary>The job is tied to one piece of work, so what was cancelled is not finished, and another job is not done on its time.</summary>
    protected override void ResetWork()
    {
        workKey = Systems?.Repairs.ReadyFor(pawn)?.Key;
        if (workKey == null)
        {
            EndJobWith(JobCondition.Incompletable);
            return;
        }
        base.ResetWork();
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        // Cancelled, or its part is gone: stop. Nothing is wrong with the vehicle, so repairs do not stand down.
        this.FailOn(() => workKey != null && Current == null);
        foreach (Toil toil in base.MakeNewToils()) yield return toil;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref workKey, "PhobosVehicleSystems_WorkKey");
    }

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    protected override void WorkComplete(Pawn actor)
    {
        // A job saved before work was tied to a key has none: it does what is ready, as it did.
        if (workKey == null) Finished(actor, Systems?.Repairs.CompleteReady(actor) == true);
        else
        {
            Systems?.Repairs.Complete(workKey, actor);
            ReadyForNextToil();
        }
    }
}
