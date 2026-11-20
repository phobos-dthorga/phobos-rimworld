using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Sends colonists who haul to push a vehicle to a dyno bay by hand (Framework 0.201.0; docs/development/vehicle-bay-trips.md, batch 4):
/// offered for a vehicle being pushed until as many are at it, or on their way, as it takes. Skipped at once when nothing is being pushed.
/// </summary>
public class WorkGiver_PhobosPushVehicle : WorkGiver_Scanner
{
    /// <summary>How often a pusher's job lets something more urgent take over, in ticks (authored): hunger and sleep come first.</summary>
    const int CheckOverrideTicks = 2000;

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        IReadOnlyList<CompPhobosBayTrip> trips = BayTrips.Of(pawn.Map);
        for (int i = 0; i < trips.Count; i++)
        {
            if (trips[i].BeingPushed) return false;
        }
        return true;
    }

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        IReadOnlyList<CompPhobosBayTrip> trips = BayTrips.Of(pawn.Map);
        for (int i = 0; i < trips.Count; i++)
        {
            if (trips[i].BeingPushed) yield return trips[i].Vehicle;
        }
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not VehiclePawn vehicle || vehicle.Faction != pawn.Faction || vehicle.IsForbidden(pawn)) return null;
        if (vehicle.GetComp<CompPhobosBayTrip>() is not { BeingPushed: true } trip) return null;
        if (GenHostility.AnyHostileActiveThreatToPlayer(pawn.Map)) return null;
        if (PushersComing(vehicle, pawn) >= trip.PushersNeeded)
        {
            JobFailReason.Is("PhobosBayTrip_EnoughHands".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
            return null;
        }
        Job job = JobMaker.MakeJob(BayTripDefOf.PhobosVehicleSystems_PushVehicle, vehicle);
        job.expiryInterval = CheckOverrideTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }

    /// <summary>How many others of the pawn's faction are pushing this vehicle or on their way to.</summary>
    static int PushersComing(VehiclePawn vehicle, Pawn besides)
    {
        int count = 0;
        foreach (Pawn other in vehicle.Map.mapPawns.SpawnedPawnsInFaction(besides.Faction))
        {
            if (other != besides && other.CurJobDef == BayTripDefOf.PhobosVehicleSystems_PushVehicle && other.CurJob.targetA.Thing == vehicle) count++;
        }
        return count;
    }
}

/// <summary>
/// Walks to the vehicle and pushes it: stands at a place beside its body (never where it is about to move), faces it and reports in every
/// few ticks, and follows as it moves. The vehicle moves itself a cell at a time while enough report (CompPhobosBayTrip.PushStep); the job
/// ends when it is no longer being pushed. Nothing is reserved: several push one vehicle.
/// </summary>
public class JobDriver_PhobosPushVehicle : JobDriver
{
    /// <summary>How often a pusher looks at where they should stand, in ticks.</summary>
    const int LookTicks = 15;

    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    CompPhobosBayTrip? Trip => Vehicle.GetComp<CompPhobosBayTrip>();

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => Trip is not { BeingPushed: true });
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil push = ToilMaker.MakeToil("PhobosPushVehicle");
        push.tickAction = () =>
        {
            Pawn actor = push.actor;
            if (!actor.IsHashIntervalTick(LookTicks) || Trip is not { } trip) return;
            IntVec3 spot = trip.PushSpotFor(actor);
            if (!spot.IsValid) return;
            if (actor.Position == spot)
            {
                if (actor.pather.Moving) return;
                actor.rotationTracker.FaceTarget(Vehicle);
                trip.NotePusher(actor);
                return;
            }
            if (!actor.pather.Moving || actor.pather.Destination.Cell != spot) actor.pather.StartPath(spot, PathEndMode.OnCell);
        };
        push.defaultCompleteMode = ToilCompleteMode.Never;
        push.handlingFacing = true;
        yield return push;
    }
}
