using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Items;
using HarmonyLib;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

// People use parked vehicles (Framework 0.289.0; docs/development/vehicle-parked-use.md): the four reasons to climb in (PV1, PV2, PV3, PV6),
// the jobs, the recreation choices, and the fallbacks for food (PV4) and warmth and sleep. Water (PV5) is in the Dubs Bad Hygiene tie-in.

static class VisitShared
{
    internal static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>A recreation visit's end: recreation full (the game's own line), or the timetable moving on from recreation.</summary>
    internal static string? JoyDone(Pawn pawn)
    {
        if (pawn.needs?.joy is not { } joy || joy.CurLevel > 0.9999f) return "";
        if (pawn.timetable?.CurrentAssignment is { allowJoy: false }) return "";
        return null;
    }

    /// <summary>Whether the cab's air is inside this person's comfortable range now.</summary>
    internal static bool CabComfortable(CompPhobosVehicleSystems comp, Pawn pawn)
    {
        if (!comp.ClosedCab || !CabClimate.TryGet(comp.Vehicle, out float cab)) return false;
        FloatRange range = pawn.ComfortableTemperatureRange();
        return VisitRule.Comfortable(cab, range.min, range.max);
    }

    /// <summary>Whether the night heater may run for a visitor: fitted, its switch on, and the "Night heater at camp" setting on.</summary>
    internal static bool NightHeaterReady(CompPhobosVehicleSystems comp) =>
        VehicleSystemsMod.Settings.nightHeater && comp.LoadSwitchedOn(VisitWorker_Warm.NightHeaterLoad);
}

/// <summary>PV1: sit in with the radio on. Needs a stereo fitted and switched on, and a starting battery above the start line.</summary>
public class VisitWorker_Radio : VisitWorker
{
    public override bool Offers(CompPhobosVehicleSystems comp, Pawn pawn) =>
        pawn.needs?.joy != null && comp.Fittings.Grants("CabStereo") && comp.LoadSwitchedOn(LifeAboard.StereoLoad) && !BatteryLow(comp);

    public override string? Done(CompPhobosVehicleSystems comp, VehicleVisit visit)
    {
        if (BatteryLow(comp)) return "PhobosVisit_OutBattery";
        if (!comp.Fittings.Grants("CabStereo") || !comp.LoadSwitchedOn(LifeAboard.StereoLoad)) return "";
        // Out of power or shed (a flat battery, a busy alternator): nothing to listen to. Given the systems' first look after climbing in.
        if (Find.TickManager.TicksGame - visit.since > VehicleVisits.LookTicks * 2 && !comp.LoadRunning(LifeAboard.StereoLoad)) return "";
        return VisitShared.JoyDone(visit.pawn);
    }

    /// <summary>
    /// The owner's card of 25 Nov 2026: a visitor's radio stops at the starting battery's "barely enough to start" line, the camp lights'
    /// line, unless the setting "Visitors may flatten the battery" is on.
    /// </summary>
    internal static bool BatteryLow(CompPhobosVehicleSystems comp) =>
        comp.TryStartLine(VehicleVisits.StartingPower, out float reading, out double line)
        && VisitRule.RadioOffForTheStart(reading, line, VehicleSystemsMod.Settings.visitorsFlattenBattery);
}

/// <summary>
/// PV2: a hotbox session. With the Hotboxing setting on, a closed cab, and either a smoked drug in reach that their drug policy allows for
/// recreation, or a friend already in the cab. The smoke and the shared high are life aboard's.
/// </summary>
public class VisitWorker_Hotbox : VisitWorker
{
    public override bool Offers(CompPhobosVehicleSystems comp, Pawn pawn)
    {
        if (!VehicleSystemsMod.Settings.hotboxing || !comp.ClosedCab || pawn.needs?.joy == null || pawn.drugs == null || pawn.IsTeetotaler()) return false;
        return HasSmoke(comp.Vehicle, pawn) || VehicleVisits.FriendAboard(pawn, comp.Vehicle);
    }

    public override string? Done(CompPhobosVehicleSystems comp, VehicleVisit visit)
    {
        int now = Find.TickManager.TicksGame;
        if (VisitRule.SmokedAndShared(visit.smokedAt, now, VisitShared.Rules.hotboxStayHours, GenDate.TicksPerHour)) return "";
        // A stay's length gone with no smoke of their own, none in the cab and nothing left to light: nothing to share after all.
        if (visit.smokedAt < 0 && now - visit.since >= VisitShared.Rules.hotboxStayHours * GenDate.TicksPerHour
            && LifeAboard.SmokeAboard(comp.Vehicle) <= 0f && !HasSmoke(comp.Vehicle, visit.pawn)) return "";
        return VisitShared.JoyDone(visit.pawn);
    }

    /// <summary>A smoked drug in this person's packs or the vehicle's cargo that their policy allows for recreation now.</summary>
    internal static bool HasSmoke(VehiclePawn vehicle, Pawn pawn)
    {
        if (pawn.drugs?.CurrentPolicy is not { } policy || pawn.needs?.joy is not { } joy) return false;
        foreach (Thing thing in Reach(vehicle, pawn))
        {
            if (!LifeAboard.Smoked(thing.def)) continue;
            DrugPolicyEntry? entry = policy[thing.def];
            if (entry != null && entry.allowedForJoy && joy.CurLevel < entry.onlyIfJoyBelow) return true;
        }
        return false;
    }

    static IEnumerable<Thing> Reach(VehiclePawn vehicle, Pawn pawn)
    {
        if (pawn.inventory?.innerContainer is { } own)
        {
            for (int i = 0; i < own.Count; i++) yield return own[i];
        }
        if (vehicle.inventory?.innerContainer is { } cargo)
        {
            for (int i = 0; i < cargo.Count; i++) yield return cargo[i];
        }
    }
}

/// <summary>
/// PV3: warm up (or cool down) in the cab, for someone the weather is hurting when the game finds no safe room. A closed cab already in
/// their comfortable range, or one whose night heater may run for them (it burns its chemfuel while they warm up).
/// </summary>
public class VisitWorker_Warm : VisitWorker
{
    public const string NightHeaterLoad = "nightHeater";

    public override bool Offers(CompPhobosVehicleSystems comp, Pawn pawn) =>
        comp.ClosedCab && (VisitShared.CabComfortable(comp, pawn) || VisitShared.NightHeaterReady(comp));

    public override string? Done(CompPhobosVehicleSystems comp, VehicleVisit visit)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (VehicleVisits.ComfortableOutside(visit.pawn, vehicle.Position, vehicle.Map)) return "";
        if (!VisitShared.CabComfortable(comp, visit.pawn) && !VisitShared.NightHeaterReady(comp)) return "";
        return null;
    }
}

/// <summary>
/// PV6: a nap sitting up in a closed cab, for someone tired with no bed, whom the game would put on the ground. They rest at the rules' seat
/// figure while the vehicle stands still, and wake as the bunk's sleeper does.
/// </summary>
public class VisitWorker_Nap : VisitWorker
{
    public override bool Asleep => true;

    public override bool Offers(CompPhobosVehicleSystems comp, Pawn pawn) => comp.ClosedCab && pawn.needs?.rest != null;

    public override string? Done(CompPhobosVehicleSystems comp, VehicleVisit visit)
    {
        Pawn pawn = visit.pawn;
        if (pawn.needs?.rest is not { } rest || rest.CurLevel >= RestedLevel) return "";
        if (rest.CurLevel >= Need_Rest.DefaultFallAsleepMaxLevel && pawn.timetable?.CurrentAssignment is { allowRest: false }) return "";
        return null;
    }

    /// <summary>Rested enough to get up by themselves, as the cab bunk's sleeper (authored, near the game's own natural waking).</summary>
    const float RestedLevel = 0.98f;
}

/// <summary>A visit's walk and climb: to the vehicle, a moment climbing in, then aboard in a free passenger seat.</summary>
public class JobDriver_PhobosVisitVehicle : JobDriver
{
    const int ClimbTicks = 90;

    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        PhobosVehicleVisitDef? visit = VehicleVisits.DefForJob(job.def);
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => visit == null);
        // Checked once a second, as the bunk's walk is: the check tests the path.
        this.FailOn(() => pawn.IsHashIntervalTick(GenTicks.TicksPerRealSecond)
            && (Vehicle.GetComp<CompPhobosVehicleSystems>() is not { } comp || !VehicleVisits.MayUse(comp, pawn, climbIn: true)));
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(ClimbTicks).WithProgressBarToilDelay(TargetIndex.A);
        Toil climb = ToilMaker.MakeToil("ClimbInToVisit");
        climb.initAction = () => VehicleVisits.Begin(pawn, Vehicle, visit!);
        climb.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return climb;
    }
}

/// <summary>PV4: to the vehicle, a moment at its cargo, a portion of food taken out, then eaten by the game's own eating.</summary>
public class JobDriver_PhobosTakeFoodFromVehicle : JobDriver
{
    const int TakeTicks = 60;

    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;
    Thing Food => job.targetB.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => Food == null || Food.Destroyed || Vehicle.inventory?.innerContainer?.Contains(Food) != true);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(TakeTicks).WithProgressBarToilDelay(TargetIndex.A);
        Toil take = ToilMaker.MakeToil("TakeFoodFromVehicle");
        take.initAction = () =>
        {
            Thing source = Food;
            ThingDef def = source.def;
            int count = System.Math.Min(System.Math.Max(1, job.count), source.stackCount);
            string label = source.LabelNoCount;
            ThingOwner packs = pawn.inventory.innerContainer;
            // Counted by what left the cargo: a portion that merges into a stack the colonist already carries is still taken (ThingMoves).
            int moved = ThingMoves.Transfer(Vehicle.inventory.innerContainer, source, packs, count);
            if (moved <= 0) return;
            Thing? taken = packs.Contains(source) ? source : packs.FirstOrDefault(t => t.def == def);
            if (taken == null) return;
            Job eat = JobMaker.MakeJob(JobDefOf.Ingest, taken);
            eat.count = moved;
            pawn.jobs.jobQueue.EnqueueFirst(eat);
            if (Seen.Wants("vehicles.visit-food", Vehicle)) Seen.Note("vehicles.visit-food", Vehicle, pawn.LabelShort + ": " + label);
        };
        take.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return take;
    }
}

/// <summary>PV5: to the vehicle, and a drink from its crew water tank (the Dubs Bad Hygiene tie-in's rules).</summary>
public class JobDriver_PhobosDrinkFromVehicle : JobDriver
{
    const int DrinkTicks = 120;

    VehiclePawn Vehicle => (VehiclePawn)job.targetA.Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => LifeAboard.Drink == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(DrinkTicks).WithProgressBarToilDelay(TargetIndex.A);
        Toil drink = ToilMaker.MakeToil("DrinkFromVehicle");
        drink.initAction = () =>
        {
            if (LifeAboard.Drink?.Invoke(Vehicle, pawn) == true && Seen.Wants("vehicles.visit-drink", Vehicle)) Seen.Note("vehicles.visit-drink", Vehicle, pawn.LabelShort);
        };
        drink.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return drink;
    }
}

/// <summary>
/// PV1 and PV2 as recreation choices: the game weighs them against its own by recreation kind and tolerance. No chance at all while no
/// parked vehicle on the map offers the visit, so a colony without one pays one short list's count.
/// </summary>
public class JoyGiver_PhobosVehicleVisit : JoyGiver
{
    PhobosVehicleVisitDef? Visit => def.jobDef is { } job ? VehicleVisits.DefForJob(job) : null;

    public override float GetChance(Pawn pawn)
    {
        if (Visit is not { } visit || !VehicleVisits.AnyOffers(pawn, visit)) return 0f;
        float chance = base.GetChance(pawn);
        if (visit == VehicleVisitDefOf.PhobosVisit_Hotbox)
        {
            chance = (float)VisitRule.HotboxChance(chance, VehicleVisits.FriendHotboxing(pawn), VisitShared.Rules.hotboxFriendFactor);
        }
        return chance;
    }

    public override Job? TryGiveJob(Pawn pawn) => Visit is { } visit ? VehicleVisits.JobFor(pawn, visit) : null;
}

/// <summary>
/// PV4: a hungry colonist for whom the game found no food eats from a parked vehicle's cargo, as the game already lets them eat from a pack
/// animal's packs (it looks only at colony animals, so a vehicle was never asked). Runs only on the game's "nothing", after the other mods
/// that patch this giver; the food is the game's own choice (<c>FoodUtility.BestFoodInInventory</c>, food policy included).
/// </summary>
[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
public static class VisitFood
{
    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result != null || !VehicleVisits.MayVisit(pawn) || pawn.needs?.food is not { } food) return;
        if (VehicleVisits.Candidates(pawn.Map).Count == 0) return;
        FoodPreferability least = food.CurCategory >= HungerCategory.UrgentlyHungry ? FoodPreferability.DesperateOnlyForHumanlikes : FoodPreferability.RawBad;
        CompPhobosVehicleSystems? comp = VehicleVisits.Nearest(pawn, c => Best(c, pawn, least) != null, climbIn: false);
        if (comp == null || Best(comp, pawn, least) is not { } meal) return;
        Job job = JobMaker.MakeJob(VehicleVisitDefOf.PhobosVehicleSystems_TakeFoodFromVehicle, comp.Vehicle, meal);
        job.count = FoodUtility.WillIngestStackCountOf(pawn, meal.def, FoodUtility.GetNutrition(pawn, meal, meal.def));
        __result = job;
    }

    static Thing? Best(CompPhobosVehicleSystems comp, Pawn pawn, FoodPreferability least) =>
        FoodUtility.BestFoodInInventory(comp.Vehicle, pawn, least, FoodPreferability.MealLavish);
}

/// <summary>
/// PV3: someone the weather is hurting, for whom the game found no safe room, warms up (or cools down) in a closed cab. Runs only on the
/// game's "nothing", after its own injury test.
/// </summary>
[HarmonyPatch(typeof(JobGiver_SeekSafeTemperature), "TryGiveJob")]
public static class VisitWarm
{
    public static void Postfix(JobGiver_SeekSafeTemperature __instance, Pawn pawn, ref Job? __result)
    {
        if (__result != null || !VehicleVisits.MayVisit(pawn)) return;
        if (__instance.requiresInjury && !pawn.health.hediffSet.HasTemperatureInjury(TemperatureInjuryStage.Serious)) return;
        if (VehicleVisits.ComfortableOutside(pawn, pawn.Position, pawn.Map)) return;
        __result = VehicleVisits.JobFor(pawn, VehicleVisitDefOf.PhobosVisit_Warm);
    }
}

/// <summary>
/// PV6: a tired colonist with no bed, whom the game would put on the ground, naps sitting up in a closed cab instead. After the cab bunk's
/// and the deck tent's own postfixes (low priority), and never for someone who should rest for their health.
/// </summary>
[HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
public static class VisitNap
{
    [HarmonyPriority(Priority.Low)]
    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result == null || __result.def != JobDefOf.LayDown || __result.targetA.Thing is Building_Bed) return;
        if (!VehicleVisits.MayVisit(pawn) || HealthAIUtility.ShouldSeekMedicalRest(pawn)) return;
        if (VehicleVisits.JobFor(pawn, VehicleVisitDefOf.PhobosVisit_Nap) is { } nap) __result = nap;
    }
}
