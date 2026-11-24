using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>
/// Sleeping inside a parked vehicle (Framework 0.215.0; docs/development/vehicle-accessories.md, B4; the owner, 13 Nov 2026: wanted, last).
/// A colonist assigned to a vehicle's cab bunk goes to it at bedtime instead of a bed, climbs in, sleeps in the cab's warmth and climbs out
/// rested or when their day begins. Kept by the vehicles as sleepers are assigned, so the bedtime check reads a short list. Emptied when another
/// game is loaded.
/// </summary>
public static class BunkSleepers
{
    static readonly Dictionary<Pawn, CompPhobosCamp> assigned = new();
    static Game? game;

    static void Check()
    {
        if (game == Current.Game) return;
        assigned.Clear();
        game = Current.Game;
    }

    internal static void Note(CompPhobosCamp camp, Pawn? before, Pawn? now)
    {
        Check();
        if (before != null && assigned.TryGetValue(before, out CompPhobosCamp? was) && was == camp) assigned.Remove(before);
        if (now != null) assigned[now] = camp;
    }

    /// <summary>The vehicle whose bunk this colonist sleeps in, or null.</summary>
    public static CompPhobosCamp? BunkOf(Pawn pawn)
    {
        Check();
        return assigned.TryGetValue(pawn, out CompPhobosCamp? camp) && camp.BunkSleeper == pawn ? camp : null;
    }
}

/// <summary>
/// At bedtime, a colonist assigned to a cab bunk goes to it instead of the bed the game chose. The game decides when to sleep (its needs,
/// timetable, duties, mental states and emergencies all come first, as for any bed); this postfix only changes where, and only when the
/// bunk can take them now. It never sends someone who should rest for their health: patients keep their beds and doctors.
/// </summary>
[HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
public static class BunkBedtime
{
    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result == null || pawn.Faction != Faction.OfPlayer || BunkSleepers.BunkOf(pawn) is not { } camp) return;
        if (HealthAIUtility.ShouldSeekMedicalRest(pawn)) return;
        if (camp.BedtimeRefusal(pawn) != null) return;
        __result = JobMaker.MakeJob(VehicleCampDefOf.PhobosVehicleSystems_BunkSleep, camp.Vehicle);
    }
}

/// <summary>Walks to the vehicle, takes a moment to climb in, and lies down in the cab bunk.</summary>
public class JobDriver_PhobosBunkSleep : JobDriver
{
    const int ClimbTicks = 90;

    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        // Checked once a second, not every tick (fault-read 25 Nov 2026, C10): the refusal scans for threats and tests the path.
        this.FailOn(() => pawn.IsHashIntervalTick(GenTicks.TicksPerRealSecond)
            && (Vehicle.GetComp<CompPhobosCamp>() is not { } camp || camp.BedtimeRefusal(pawn) != null));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(ClimbTicks).WithProgressBarToilDelay(TargetIndex.A);
        var climb = ToilMaker.MakeToil("ClimbIntoBunk");
        climb.initAction = () => Vehicle.GetComp<CompPhobosCamp>()?.ClimbIn(pawn);
        climb.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return climb;
    }
}
