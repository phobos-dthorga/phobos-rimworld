using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Crew machines on vehicles (Framework 0.265.0; docs/development/vehicle-crew-machines.md, CM2 and CM5; the owner's go of 23 Nov 2026). Two
/// fittings, each fitted from the machine itself, packed (PackedThings): a sleep accelerator on the cab bunk, and a vitals monitor on the
/// litters. Each works only while its load runs, so only with power and someone to serve, and each gives the game's own machine's figures,
/// held in the rules Def. Nothing here is saved.
/// </summary>
public static class CrewMachines
{
    /// <summary>The ability the bunk's sleep accelerator grants, and its load's name in the fitting.</summary>
    public const string BunkAcceleratorAbility = "BunkAccelerator";
    public const string BunkAcceleratorLoad = "bunkAccelerator";
    /// <summary>The ability the litters' vitals monitor grants, and its load's name.</summary>
    public const string LitterMonitorAbility = "LitterMonitor";
    public const string LitterMonitorLoad = "litterMonitor";

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Whether a vehicle's machine is fitted, its part standing, and its load running (switched on, with power, someone served).</summary>
    static bool Running(VehiclePawn? vehicle, string ability, string load) =>
        vehicle?.GetComp<CompPhobosVehicleSystems>() is { Usable: true } systems && systems.Fittings.Grants(ability) && systems.LoadRunning(load);

    /// <summary>The vehicle whose bunk this is: the bunk is held by the vehicle's camp, never spawned.</summary>
    static VehiclePawn? VehicleOfBunk(Thing? bunk) =>
        bunk?.def == VehicleCampDefOf.PhobosCamp_CabBunk && bunk.ParentHolder is CompPhobosCamp camp ? camp.Vehicle : null;

    /// <summary>Whether this bunk's sleep accelerator is working now.</summary>
    public static bool BunkAccelerated(Thing? bunk) => Running(VehicleOfBunk(bunk), BunkAcceleratorAbility, BunkAcceleratorLoad);

    /// <summary>
    /// The vehicle whose accelerated bunk this person sleeps in, or null: a rider whose caravan bed it is, or the bunk's sleeper aboard a
    /// parked vehicle. A spawned person is never in a bunk, so the common case costs one check.
    /// </summary>
    public static VehiclePawn? AcceleratedBunkOf(Pawn pawn)
    {
        if (pawn.Spawned || pawn.Dead) return null;
        if (pawn.CurrentCaravanBed() is { } bed) return BunkAccelerated(bed) ? VehicleOfBunk(bed) : null;
        if (pawn.GetVehicle() is not { } vehicle || vehicle.GetComp<CompPhobosCamp>() is not { SleeperAsleep: true } camp || camp.BunkSleeper != pawn) return null;
        return Running(vehicle, BunkAcceleratorAbility, BunkAcceleratorLoad) ? vehicle : null;
    }

    public static float BunkRestBonus => Rules.bunkAcceleratorRest;
    public static float BunkHungerFactor => 1f + Rules.bunkAcceleratorHunger;

    /// <summary>What the litters' monitor adds to a tend of this patient now: the rules' figure on a litter whose monitor runs, else 0.</summary>
    public static float LitterTendBonus(Pawn? patient) =>
        patient?.ParentHolder is CompPhobosLitter litter && Running(litter.Vehicle, LitterMonitorAbility, LitterMonitorLoad) ? Rules.litterMonitorTendQuality : 0f;
}

/// <summary>
/// The cab bunk rests its sleeper faster while its sleep accelerator works (CM2). On the game's bed rest stat, added by XML, so every place
/// that rests someone in the bunk (a caravan's bed, a parked cab) and the stat's explanation agree. Acts only on the cab bunk.
/// </summary>
public class StatPart_PhobosBunkAccelerator : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (CrewMachines.BunkAccelerated(req.Thing)) val += CrewMachines.BunkRestBonus;
    }

    public override string? ExplanationPart(StatRequest req) =>
        CrewMachines.BunkAccelerated(req.Thing) ? "PhobosCrewMachines_StatBunkAccelerator".Translate() + ": " + CrewMachines.BunkRestBonus.ToStringByStyle(ToStringStyle.PercentZero, ToStringNumberSense.Offset) : null;
}

/// <summary>
/// Someone sleeping in an accelerated cab bunk grows hungry faster (CM2), as in a bed beside the game's own sleep accelerator. The game
/// applies a bed's hunger figure only to someone lying in a bed on a map, and a bunk's sleeper is aboard a vehicle or in a caravan, so the
/// figure is applied here. A postfix; it never skips the original, and costs one check for anyone on a map.
/// </summary>
[HarmonyPatch(typeof(Need_Food), nameof(Need_Food.FoodFallPerTickAssumingCategory))]
public static class CrewMachinesBunkHunger
{
    public static void Postfix(Pawn ___pawn, ref float __result)
    {
        if (___pawn == null || CrewMachines.AcceleratedBunkOf(___pawn) is not { } vehicle) return;
        __result *= CrewMachines.BunkHungerFactor;
        if (Seen.Wants("vehicles.bunk-accelerator", vehicle)) Seen.Note("vehicles.bunk-accelerator", vehicle, ___pawn.LabelShort);
    }
}

/// <summary>
/// A patient on a vehicle's litter is tended better while the litters' vitals monitor works (CM5), as the game's monitor does beside a bed.
/// The game adds a bed's bonus only to someone lying in a bed, and a litter is held by the vehicle, so the bonus is added here, within the
/// medicine's own cap as the game's sum keeps it. A postfix on the overload every tend goes through; it never skips the original.
/// </summary>
[HarmonyPatch(typeof(TendUtility), nameof(TendUtility.CalculateBaseTendQuality), new[] { typeof(Pawn), typeof(Pawn), typeof(float), typeof(float) })]
public static class CrewMachinesLitterTend
{
    public static void Postfix(Pawn patient, float medicineQualityMax, ref float __result)
    {
        float bonus = CrewMachines.LitterTendBonus(patient);
        if (bonus <= 0f) return;
        __result = Mathf.Min(__result + bonus, medicineQualityMax);
        if (patient.ParentHolder is CompPhobosLitter litter && Seen.Wants("vehicles.litter-monitor", litter.Vehicle)) Seen.Note("vehicles.litter-monitor", litter.Vehicle, patient.LabelShort);
    }
}
