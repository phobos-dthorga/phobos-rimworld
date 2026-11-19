using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework.Environment;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle comp with work that waits for a driver (Framework 0.177.0; docs/development/vehicles-at-work-tie-ins.md, VW3): a truck told
/// to Deliver or Gather with nobody aboard today, a farm machine given its order later. While it waits, a colonist with Drive work comes and
/// takes the driver's seat (<see cref="WorkGiver_PhobosFetchDriver"/>); the comp then starts its own work. It registers itself with
/// <see cref="DriverCalls"/> while it waits.
/// </summary>
public interface IVehicleWantsDriver
{
    /// <summary>The vehicle it belongs to.</summary>
    VehiclePawn Vehicle { get; }

    /// <summary>Whether it waits for a driver now: work ordered, the vehicle the player's and on a map, and not enough crew aboard to drive.</summary>
    bool WantsDriver { get; }
}

/// <summary>
/// Vehicles waiting for a driver, by map, and who may drive them (Framework 0.177.0). Shared by every errand of ours that a colonist must
/// come and drive, so farm machines use it too (owner, 19 Nov 2026: our own fallback, for the agricultural mod).
/// </summary>
public static class DriverCalls
{
    /// <summary>Starts or stops a comp's call for a driver. Call with the map it is on; again with false when it stops or leaves the map.</summary>
    public static void Call(Map? map, IVehicleWantsDriver caller, bool waiting)
    {
        if (waiting) MapRegistry<IVehicleWantsDriver>.Add(map, caller);
        else MapRegistry<IVehicleWantsDriver>.Remove(map, caller);
    }

    /// <summary>Everything calling for a driver on this map, waiting or not: test each one's WantsDriver.</summary>
    public static IReadOnlyList<IVehicleWantsDriver> On(Map? map) => MapRegistry<IVehicleWantsDriver>.On(map);

    /// <summary>Whether this vehicle has work waiting for a driver now.</summary>
    public static bool Waits(VehiclePawn vehicle)
    {
        IReadOnlyList<IVehicleWantsDriver> callers = On(vehicle.MapHeld);
        for (int i = 0; i < callers.Count; i++)
        {
            if (callers[i].Vehicle == vehicle && callers[i].WantsDriver) return true;
        }
        return false;
    }

    /// <summary>A free seat this colonist can drive from, or null: Vehicle Framework's own choice of seat, among the driving ones.</summary>
    public static VehicleRoleHandler? DriverSeat(VehiclePawn vehicle, Pawn pawn) => vehicle.GetNextAvailableHandler(pawn, HandlingType.Movement);

    /// <summary>
    /// Whether this colonist may drive this vehicle: Vehicles at Work's drivers list for it, when that mod is active and the player has set
    /// one; anyone otherwise.
    /// </summary>
    public static bool MayDrive(Pawn pawn, VehiclePawn vehicle) => VehiclesAtWorkDrivers.MayDrive(pawn, vehicle) ?? true;

    /// <summary>The name of the work that fetches drivers, as the work tab shows it: Vehicles at Work's Drive, or ours.</summary>
    public static string WorkLabel =>
        DefDatabase<WorkGiverDef>.GetNamedSilentFail("PhobosVehicleSystems_FetchDriver")?.workType?.labelShort
        ?? "PhobosDriverCall_WorkFallback".Translate().Resolve();

    /// <summary>A line for an errand's button: pressed with nobody aboard, it calls a driver.</summary>
    public static string WaitsLine => "PhobosDriverCall_WaitsLine".Translate(WorkLabel.Named("WORK")).Resolve();

    /// <summary>Whether a colonist is already on the way to drive it, so a second is not sent.</summary>
    public static bool DriverOnTheWay(VehiclePawn vehicle, Pawn? besides)
    {
        foreach (Pawn pawn in vehicle.Map.mapPawns.SpawnedPawnsInFaction(vehicle.Faction))
        {
            if (pawn != besides && pawn.CurJobDef == DriverCallDefOf.PhobosVehicleSystems_TakeTheWheel && pawn.CurJob.targetA.Thing == vehicle) return true;
        }
        return false;
    }
}

[DefOf]
public static class DriverCallDefOf
{
    public static JobDef PhobosVehicleSystems_TakeTheWheel = null!;

    static DriverCallDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(DriverCallDefOf));
}

/// <summary>
/// Sends a colonist with Drive work to take the wheel of a vehicle with work waiting for a driver (Framework 0.177.0). Its work type is
/// Vehicles at Work's Drive where that mod is active, ours otherwise (the Defs and Mods/VehiclesAtWork). Skipped at once when nothing waits.
/// </summary>
public class WorkGiver_PhobosFetchDriver : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        IReadOnlyList<IVehicleWantsDriver> callers = DriverCalls.On(pawn.Map);
        for (int i = 0; i < callers.Count; i++)
        {
            if (callers[i].WantsDriver) return false;
        }
        return true;
    }

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        IReadOnlyList<IVehicleWantsDriver> callers = DriverCalls.On(pawn.Map);
        for (int i = 0; i < callers.Count; i++)
        {
            if (callers[i].WantsDriver) yield return callers[i].Vehicle;
        }
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not VehiclePawn vehicle || vehicle.Faction != pawn.Faction || vehicle.IsForbidden(pawn) || !DriverCalls.Waits(vehicle)) return null;
        if (!DriverCalls.MayDrive(pawn, vehicle))
        {
            JobFailReason.Is("PhobosDriverCall_NotADriver".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
            return null;
        }
        if (DriverCalls.DriverSeat(vehicle, pawn) == null)
        {
            JobFailReason.Is("PhobosDriverCall_NoSeat".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
            return null;
        }
        if (DriverCalls.DriverOnTheWay(vehicle, pawn)) return null;
        return JobMaker.MakeJob(DriverCallDefOf.PhobosVehicleSystems_TakeTheWheel, vehicle);
    }
}

/// <summary>
/// Walks to the vehicle and takes a driver's seat in it, as Vehicle Framework's own boarding does at its last step (Toils_Board: the
/// vehicle's TryAddPawn with the seat). No reservation, as Vehicle Framework's own boarding job makes none; the work giver sends one at a time.
/// </summary>
public class JobDriver_PhobosTakeTheWheel : JobDriver
{
    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => !DriverCalls.Waits(Vehicle));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        Toil board = ToilMaker.MakeToil("PhobosTakeTheWheel");
        board.initAction = () =>
        {
            Pawn actor = board.actor;
            VehiclePawn vehicle = Vehicle;
            VehicleRoleHandler? seat = DriverCalls.DriverSeat(vehicle, actor);
            if (seat == null || !vehicle.TryAddPawn(actor, seat))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            Seen.Note("vehicles.driver-call.aboard", vehicle);
        };
        board.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return board;
    }
}

/// <summary>
/// Vehicles at Work's drivers list for a vehicle (its "Drivers" button), read by name (Framework 0.177.0, VW3): null when the mod is absent,
/// its helpers are renamed, or the player never opened the list for this vehicle.
/// </summary>
static class VehiclesAtWorkDrivers
{
    static readonly MethodInfo? WheelsOf = VehiclesAtWork.Active ? AccessTools.Method("WOR.Wheels:Of") : null;
    static readonly MethodInfo? UseOf = VehiclesAtWork.Active ? AccessTools.Method("WOR.Wheels:Use") : null;
    static readonly MethodInfo? MayDriveOf = VehiclesAtWork.Active ? AccessTools.Method("WOR.VehicleUse:MayDrive") : null;

    public static bool? MayDrive(Pawn pawn, VehiclePawn vehicle)
    {
        if (WheelsOf == null || UseOf == null || MayDriveOf == null) return null;
        object? wheels = WheelsOf.Invoke(null, new object?[] { vehicle.Map });
        if (wheels == null) return null;
        object? use = UseOf.Invoke(wheels, new object?[] { vehicle, false });
        if (use == null) return null;
        return MayDriveOf.Invoke(use, new object?[] { pawn }) as bool?;
    }
}
