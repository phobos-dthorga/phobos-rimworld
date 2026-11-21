using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Goes into a vehicle's cabin (target A, the vehicle), carrying a downed person first where target B names one: walks to the vehicle, waits at
/// its door, and comes through at the cabin's way out, laying anyone carried on a free litter. Stops if the vehicle takes off or leaves.
/// </summary>
public class JobDriver_PhobosEnterCabin : JobDriver
{
    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(TargetIndex.A).Thing;
    Pawn? Carried => job.GetTarget(TargetIndex.B).Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => Carried == null || pawn.Reserve(Carried, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => !VehicleCabins.Offered(Vehicle) || VehicleCabins.EnterRefusal(Vehicle, 0) != null);
        if (Carried != null)
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
        }
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil wait = Toils_General.Wait(VehicleCabins.PassTicks, TargetIndex.A);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;
        yield return new Toil
        {
            initAction = () =>
            {
                if (VehicleCabins.Of(Vehicle) is not { } comp || VehicleCabins.Open(comp) is not { } cabin || VehicleCabins.WayOut(cabin) is not { } wayOut)
                {
                    Messages.Message("PhobosCabin_CouldNotOpen".Translate(Vehicle.LabelShort.Named("VEHICLE")), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                bool carrying = pawn.carryTracker?.CarriedThing is Pawn;
                // Several may walk up together: the last ones find it full at the door (Framework 0.211.1).
                if (VehicleCabins.EnterRefusal(Vehicle, carrying ? 2 : 1) is string full)
                {
                    Messages.Message("PhobosCabin_CannotEnter".Translate(Vehicle.LabelShort.Named("VEHICLE"), full.Named("REASON")), Vehicle, MessageTypeDefOf.RejectInput, historical: false);
                    Seen.Note("vehicles.cabin-full", Vehicle);
                    return;
                }
                if (!VehicleCabins.Move(pawn, cabin, wayOut.Position, Vehicle)) return;
                Seen.Note("vehicles.cabin-entered", Vehicle, variant: carrying ? "carrying" : "walking");
                if (carrying) VehicleCabins.SettleCarried(pawn);
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}

/// <summary>Leaves a cabin by its way out (target A), carrying a downed person first where target B names one, and comes out beside the vehicle.</summary>
public class JobDriver_PhobosLeaveCabin : JobDriver
{
    Building_PhobosCabinWayOut WayOut => (Building_PhobosCabinWayOut)job.GetTarget(TargetIndex.A).Thing;
    Pawn? Carried => job.GetTarget(TargetIndex.B).Thing as Pawn;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => Carried == null || pawn.Reserve(Carried, job, errorOnFailed: errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => VehicleCabins.LeaveRefusal(WayOut.Vehicle) != null);
        if (Carried != null)
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
        }
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell);
        Toil wait = Toils_General.Wait(VehicleCabins.PassTicks, TargetIndex.A);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;
        yield return new Toil
        {
            initAction = () =>
            {
                VehiclePawn vehicle = WayOut.Vehicle!;
                bool carrying = pawn.carryTracker?.CarriedThing is Pawn;
                if (!VehicleCabins.Move(pawn, vehicle.Map, VehicleCabins.BesideVehicle(vehicle), vehicle)) return;
                Seen.Note("vehicles.cabin-left", vehicle, variant: carrying ? "carrying" : "walking");
                if (carrying) VehicleCabins.SettleCarried(pawn);
            },
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}

/// <summary>The right-click orders on a vehicle: go into its cabin. Says why not when it cannot be done.</summary>
public class FloatMenuOptionProvider_PhobosEnterCabin : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.Faction == Faction.OfPlayer && base.SelectedPawnValid(pawn, context);

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is not VehiclePawn vehicle || !VehicleCabins.Offered(vehicle)) return null!;
        NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
        if (VehicleCabins.EnterRefusal(vehicle) is string why) return new FloatMenuOption("PhobosCabin_CannotEnter".Translate(name, why.Named("REASON")), null);
        List<Pawn> going = context.ValidSelectedPawns.Where(p => p.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly)).ToList();
        if (going.Count == 0) return new FloatMenuOption("PhobosCabin_CannotEnter".Translate(name, "PhobosCabin_NoPath".Translate().Named("REASON")), null);
        return new FloatMenuOption("PhobosCabin_Enter".Translate(name), () =>
        {
            foreach (Pawn pawn in going)
            {
                Job job = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_EnterCabin, vehicle);
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }, MenuOptionPriority.High);
    }
}

/// <summary>
/// The right-click orders on a downed person: carry them into a vehicle's cabin (on the map), or out of the cabin they lie in. One person at a
/// time, by a colonist who can carry.
/// </summary>
public class FloatMenuOptionProvider_PhobosCarryCabin : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && pawn.Faction == Faction.OfPlayer && base.SelectedPawnValid(pawn, context);

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is VehiclePawn || !clickedPawn.Downed || clickedPawn == context.FirstSelectedPawn) yield break;
        Pawn carrier = context.FirstSelectedPawn;
        NamedArgument patient = clickedPawn.LabelShort.Named("PAWN");
        if (!carrier.CanReach(clickedPawn, PathEndMode.ClosestTouch, Danger.Deadly)) yield break;
        // Out of the cabin they lie in.
        if (VehicleCabins.WayOut(clickedPawn.Map) is { } wayOut)
        {
            if (VehicleCabins.LeaveRefusal(wayOut.Vehicle) is string why)
            {
                yield return new FloatMenuOption("PhobosCabin_CannotCarryOut".Translate(patient, why.Named("REASON")), null);
                yield break;
            }
            yield return new FloatMenuOption("PhobosCabin_CarryOut".Translate(patient), () =>
            {
                Job job = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_LeaveCabin, wayOut, clickedPawn);
                job.count = 1;
                job.playerForced = true;
                carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
            yield break;
        }
        // Into each of the player's vehicles with a cabin on this map: with the setting off, nobody is carried in (carrying out, above, still
        // works, so nobody is left inside; Framework 0.211.1).
        if (!VehicleCabins.Enabled) yield break;
        foreach (Pawn other in clickedPawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (other is not VehiclePawn vehicle || !VehicleCabins.Offered(vehicle)) continue;
            NamedArgument name = vehicle.LabelShort.Named("VEHICLE");
            if (VehicleCabins.EnterRefusal(vehicle, 2) is string why)
            {
                yield return new FloatMenuOption("PhobosCabin_CannotCarryIn".Translate(patient, name, why.Named("REASON")), null);
                continue;
            }
            if (!carrier.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly)) continue;
            yield return new FloatMenuOption("PhobosCabin_CarryIn".Translate(patient, name), () =>
            {
                Job job = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_EnterCabin, vehicle, clickedPawn);
                job.count = 1;
                job.playerForced = true;
                carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }
    }
}

/// <summary>The right-click order on a cabin's way out: leave the cabin. Says why not while the vehicle is in the air.</summary>
public class FloatMenuOptionProvider_PhobosLeaveCabin : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.Faction == Faction.OfPlayer && base.SelectedPawnValid(pawn, context);

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Building_PhobosCabinWayOut wayOut) return null!;
        if (VehicleCabins.LeaveRefusal(wayOut.Vehicle) is string why) return new FloatMenuOption("PhobosCabin_CannotLeave".Translate(why.Named("REASON")), null);
        List<Pawn> going = context.ValidSelectedPawns.ToList();
        return new FloatMenuOption("PhobosCabin_Leave".Translate(wayOut.Vehicle!.LabelShort.Named("VEHICLE")), () =>
        {
            foreach (Pawn pawn in going)
            {
                Job job = JobMaker.MakeJob(VehicleCabinDefOf.PhobosVehicleSystems_LeaveCabin, wayOut);
                job.playerForced = true;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }, MenuOptionPriority.High);
    }
}
