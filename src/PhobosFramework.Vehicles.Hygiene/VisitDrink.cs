using DubsBadHygiene;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>
/// People use parked vehicles, PV5 (Framework 0.289.0; docs/development/vehicle-parked-use.md): a thirsty colonist for whom Dubs Bad Hygiene
/// found nothing to drink walks to a parked vehicle of the colony and drinks from its crew water tank, by the riders' own drink (the rules'
/// litres, Dubs' contamination check). Runs only on Dubs' "nothing", so taps, basins and bottles come first; its urgent giver inherits this
/// method, so both are covered. Never in Dubs' lite mode, where no tank exists.
/// </summary>
[HarmonyPatch(typeof(JobGiver_DrinkWater), nameof(JobGiver_DrinkWater.TryGiveJob))]
public static class VisitDrink
{
    public static void Postfix(Pawn pawn, ref Job? __result)
    {
        if (__result != null || LifeAboard.Drink == null || !VehicleVisits.MayVisit(pawn)) return;
        if (pawn.needs?.TryGetNeed<Need_Thirst>() is not { } thirst || thirst.CurCategory == ThirstCategory.Hydrated) return;
        if (VehicleVisits.Candidates(pawn.Map).Count == 0) return;
        float litres = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().caravanDrinkLitres;
        CompPhobosVehicleSystems? comp = VehicleVisits.Nearest(pawn,
            c => c.parent.GetComp<CompPhobosCrewWater>() is { Fitted: true } tank && tank.Litres >= litres, climbIn: false);
        if (comp != null) __result = JobMaker.MakeJob(VehicleVisitDefOf.PhobosVehicleSystems_DrinkFromVehicle, comp.Vehicle);
    }
}
