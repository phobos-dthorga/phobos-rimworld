using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>The right-click order to purge an empty tank of what it last held. Says why not when it cannot be done.</summary>
public class FloatMenuOptionProvider_PhobosPurgeTank : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && base.SelectedPawnValid(pawn, context);

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer) return null!;
        CompPhobosTank? tank = vehicle.GetComp<CompPhobosTank>();
        if (tank == null || !tank.NeedsPurge) return null!;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (!VehicleWater.CanDraw(vehicle, tank.Props.purgeLitres, out string? why))
        {
            return new FloatMenuOption("PhobosVehicleSystems_TankCannotPurge".Translate(name, (why ?? "").Named("REASON")), null);
        }
        Pawn worker = context.FirstSelectedPawn;
        IntVec3 cell = vehicle.SurroundingCells.Where(c => c.InBounds(vehicle.Map) && c.Standable(vehicle.Map) && worker.CanReach(c, PathEndMode.OnCell, Danger.Some))
            .OrderBy(c => c.DistanceToSquared(worker.Position)).FirstOrFallback(IntVec3.Invalid);
        if (!cell.IsValid) return new FloatMenuOption("PhobosVehicleSystems_TankCannotPurgeNoPath".Translate(name), null);
        string label = "PhobosVehicleSystems_TankPurgeOption".Translate(name, tank.Residue!.label.Named("CONTENTS"));
        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
        {
            Job job = JobMaker.MakeJob(TankDefOf.PhobosVehicleSystems_PurgeTank, vehicle, cell);
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }), worker, vehicle);
    }
}

/// <summary>Walks to the tanker, works for the purge's time, then clears the residue.</summary>
public class JobDriver_PhobosPurgeTank : JobDriver
{
    const TargetIndex VehicleIndex = TargetIndex.A;
    const TargetIndex CellIndex = TargetIndex.B;

    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(VehicleIndex).Thing;
    CompPhobosTank? Tank => Vehicle.GetComp<CompPhobosTank>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(VehicleIndex);
        this.FailOn(() => Tank?.NeedsPurge != true);
        yield return Toils_Goto.GotoCell(CellIndex, PathEndMode.OnCell);
        Toil work = Toils_General.Wait(Tank?.Props.purgeTicks ?? 10000, VehicleIndex);
        work.WithProgressBarToilDelay(VehicleIndex);
        yield return work;
        yield return new Toil { initAction = () => Tank?.FinishPurge(), defaultCompleteMode = ToilCompleteMode.Instant };
    }
}
