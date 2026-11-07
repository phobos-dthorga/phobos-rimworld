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
        if (comp == null || !comp.MissingSupply(out ThingDef? def, out int count) || def == null) return null;
        Thing? supply = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.ClosestTouch,
            TraverseParms.For(pawn), 9999f, x => !x.IsForbidden(pawn) && pawn.CanReserve(x));
        if (supply == null)
        {
            JobFailReason.Is("PhobosVehicleSystems_NoSupply".Translate(def.label.Named("SUPPLY")));
            return null;
        }
        IntVec3 cell = vehicle.SurroundingCells.Where(c => c.Standable(pawn.Map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .RandomElementWithFallback(IntVec3.Invalid);
        if (!cell.IsValid) return null;
        Job job = JobMaker.MakeJob(VehicleSystemsJobDefOf.PhobosVehicleSystems_LoadServiceSupplies, vehicle, cell, supply);
        job.count = System.Math.Min(count, supply.stackCount);
        return job;
    }
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
        !vehicle.Drafted && vehicle.GetComp<CompPhobosVehicleSystems>()?.ReadyService() != null;
}

public class JobDriver_PhobosServiceVehicle : JobDriver_WorkVehicle
{
    protected override JobDef JobDef => VehicleSystemsJobDefOf.PhobosVehicleSystems_ServiceVehicle;

    protected override float TotalWork => Vehicle.GetComp<CompPhobosVehicleSystems>()?.ReadyService()?.Def.service?.work ?? 1f;

    protected override StatDef Stat => StatDefOf.ConstructionSpeed;

    protected override SkillDef Skill => SkillDefOf.Construction;

    protected override void WorkComplete(Pawn actor)
    {
        Vehicle.GetComp<CompPhobosVehicleSystems>()?.CompleteService(actor);
        ReadyForNextToil();
    }
}
