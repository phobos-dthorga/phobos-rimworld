using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The reverse-osmosis kit (Framework 0.318.0; docs/development/vehicle-road-work.md; the owner's idea and cards of 26 Nov 2026: the water
/// bowser and the Ullage, parked and on the road, a quarter run off as reject, a membrane cartridge, the Army's light purifier's rate).
/// While fitted and switched on, it passes the untreated water in a tank's treatable compartments through its membrane, at its stat's
/// litres an hour (TreatmentRule), drawing its watts: on a truck through its own electrics (the "waterTreatment" load), on a parked trailer
/// through the grid hook-up beneath it, on a trailer in tow through the towing vehicle's lead (the "trailerTreatment" tow load), map or
/// world map alike. The membrane wears by the litres it treats, a quantity of the vehicle's water treatment system, renewed by a service
/// that fits a membrane cartridge. Every rule is here; the tank's button and line only ask.
/// </summary>
public static class WaterTreatment
{
    /// <summary>The truck's own load, in its loadout.</summary>
    public const string LoadName = "waterTreatment";
    /// <summary>The membrane's wear, a quantity of the water treatment system.</summary>
    public const string MembraneQuantity = "membraneWear";

    static float HoursPerRareTick => GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;

    /// <summary>Litres an hour the fitted kit treats; 0 without it.</summary>
    public static float Rate(CompPhobosTank tank) => tank.Vehicle.GetStatValue(TankDefOf.Phobos_WaterTreatment);

    public static bool Fitted(CompPhobosTank tank) => Rate(tank) > 0f;

    static bool IsTrailer(CompPhobosTank tank) => tank.Vehicle.GetComp<CompPhobosTrailer>() != null;

    /// <summary>The membrane's wear, 0 new to 1 spent; 0 where the vehicle keeps no such system.</summary>
    public static float MembraneWear(CompPhobosTank tank) =>
        tank.Vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.TryGetQuantity(MembraneQuantity, out float wear) ? wear : 0f;

    /// <summary>Whether a compartment holds untreated water the kit can treat.</summary>
    static bool NeedsTreating(TankCompartment compartment) =>
        compartment.contents is { treatable: true } && compartment.amount > 0.5f && compartment.quality > 0;

    /// <summary>Whether the kit wants to run now: fitted, switched on, its membrane not spent, untreated water in the tank.</summary>
    public static bool Wants(CompPhobosTank tank) =>
        Fitted(tank) && tank.TreatOn && !TreatmentRule.Spent(MembraneWear(tank)) && tank.Compartments.Any(NeedsTreating);

    /// <summary>The watts it draws while it runs.</summary>
    public static float WattsWanted(CompPhobosTank tank) => Wants(tank) ? tank.Props.treatmentWatts : 0f;

    /// <summary>What a parked trailer asks of the grid hook-up beneath it, for its meter (the tank's own pump adds its own).</summary>
    public static float GridWatts(CompPhobosTank tank) => IsTrailer(tank) && Hitches.Towing(tank.Vehicle) == null ? WattsWanted(tank) : 0f;

    /// <summary>
    /// The tank's own look (CompPhobosTank.RareTank), on a map or, for a truck, in a caravan: a truck treats while its own load runs; a
    /// parked trailer while the grid carries it. A trailer in tow is treated by its towing vehicle's look instead (TrailerTreatmentInTow).
    /// </summary>
    public static void Step(CompPhobosTank tank, bool gridCarries)
    {
        if (!Wants(tank)) return;
        bool powered = IsTrailer(tank)
            ? Hitches.Towing(tank.Vehicle) == null && gridCarries
            : tank.Vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.LoadRunning(LoadName);
        if (powered) Treat(tank, HoursPerRareTick);
    }

    /// <summary>
    /// Treats for this long: the kit's litres shared across the compartments, first come first served; a quarter runs off; a compartment
    /// all through turns clean (quality 0, the clean level Dubs Bad Hygiene and our ports use); the membrane wears by the litres treated.
    /// </summary>
    public static void Treat(CompPhobosTank tank, float hours)
    {
        float litres = Rate(tank) * hours;
        float processed = 0f;
        bool cleaned = false;
        foreach (TankCompartment compartment in tank.Compartments)
        {
            if (litres - processed <= 0f) break;
            if (!NeedsTreating(compartment)) continue;
            TreatmentRule.Pass pass = TreatmentRule.Step(compartment.amount, compartment.treated, litres - processed);
            processed += pass.processed;
            compartment.amount = pass.amount;
            compartment.treated = pass.treated;
            if (!pass.clean) continue;
            compartment.quality = 0;
            compartment.treated = 0f;
            cleaned = true;
        }
        if (processed <= 0f) return;
        tank.AfterTreatment();
        if (tank.Vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems) systems.AddToQuantity(MembraneQuantity, TreatmentRule.Wear(processed));
        if (cleaned && Seen.Wants("vehicles.water-treated", tank.Vehicle))
            Seen.Note("vehicles.water-treated", tank.Vehicle, (tank.Vehicle.Spawned ? "on a map" : "on the road") + ", " + processed.ToString("0") + " L this look");
    }

    /// <summary>The line under the tank's button: what the kit is doing, and its membrane.</summary>
    public static string State(CompPhobosTank tank)
    {
        string membrane = (1f - MembraneWear(tank)).ToStringPercent();
        if (TreatmentRule.Spent(MembraneWear(tank))) return "PhobosTreatment_StateSpent".Translate().Resolve();
        if (!tank.TreatOn) return "PhobosTreatment_StateOff".Translate(membrane.Named("MEMBRANE")).Resolve();
        float untreated = tank.Compartments.Where(NeedsTreating).Sum(c => c.amount - c.treated);
        return untreated <= 0f
            ? "PhobosTreatment_StateClean".Translate(membrane.Named("MEMBRANE")).Resolve()
            : "PhobosTreatment_StateWorking".Translate(untreated.ToString("N0").Named("LITRES"), Rate(tank).ToString("0").Named("RATE"),
                membrane.Named("MEMBRANE")).Resolve();
    }

    /// <summary>The kit's switch, on the tank wherever it is.</summary>
    public static Gizmo? Toggle(CompPhobosTank tank)
    {
        if (!Fitted(tank) || tank.Vehicle.Faction != Faction.OfPlayer) return null;
        return new Command_Toggle
        {
            defaultLabel = "PhobosTreatment_Gizmo".Translate().Resolve(),
            defaultDesc = "PhobosTreatment_GizmoDesc".Translate(Rate(tank).ToString("0").Named("RATE"), tank.Props.treatmentWatts.ToString("0").Named("WATTS"),
                TreatmentRule.RejectShare.ToStringPercent().Named("REJECT")).Resolve() + "\n\n" + State(tank),
            // Its own picture, cut from the kit's cabinet (the owner's pick); a missing one falls back to ours, never the red X.
            icon = SystemIcons.For("Phobos/VehicleSystems/Tank/ReverseOsmosisKit"),
            isActive = () => tank.TreatOn,
            toggleAction = () => tank.TreatOn = !tank.TreatOn,
        };
    }
}

/// <summary>
/// A trailer's reverse-osmosis kit on the towing vehicle's lead (Framework 0.318.0), built as the trailer pump in tow is
/// (TrailerPumpInTow): one more load on that vehicle's electrics while the kit wants to run, its own watts at that vehicle's voltage. A
/// trailer in tow has no look of its own off the map, so the towing vehicle's look treats its water, on a map or the world map alike.
/// </summary>
public static class TrailerTreatmentInTow
{
    /// <summary>The load's name in the rules Def's tow loads.</summary>
    public const string LoadName = "trailerTreatment";

    static float HoursPerRareTick => GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;

    static IEnumerable<CompPhobosTank> Tanks(VehiclePawn tractor)
    {
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosTank>() is { } tank) yield return tank;
        }
    }

    public static float Watts(VehiclePawn tractor) => Tanks(tractor).Sum(WaterTreatment.WattsWanted);

    public static bool Any(VehiclePawn tractor) => Tanks(tractor).Any(WaterTreatment.Fitted);

    /// <summary>The towing vehicle's look found the load running: every kit behind it that wants to run treats for that look.</summary>
    public static void Power(VehiclePawn tractor)
    {
        foreach (CompPhobosTank tank in Tanks(tractor).ToList())
        {
            if (WaterTreatment.Wants(tank)) WaterTreatment.Treat(tank, HoursPerRareTick);
        }
    }
}

/// <summary>A trailer with the reverse-osmosis kit rides behind this vehicle; with <c>inUse</c>, one whose kit wants to run.</summary>
public class When_TowedTreatment : VehicleSystemCondition
{
    public bool inUse;

    protected override bool HoldsRaw(SystemContext context) =>
        context.Comp.Hitch is { Trailer: not null } && (inUse ? TrailerTreatmentInTow.Watts(context.Vehicle) > 0f : TrailerTreatmentInTow.Any(context.Vehicle));
}

/// <summary>What the kits behind draw now, in amps at this vehicle's voltage: their own watts, from their Defs.</summary>
public class Input_TowedTreatmentAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f ? TrailerTreatmentInTow.Watts(context.Vehicle) / volts : 0f;
    }
}

/// <summary>This vehicle's own reverse-osmosis kit wants to run now: for the truck's "waterTreatment" load.</summary>
public class When_TankTreating : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) =>
        context.Vehicle.GetComp<CompPhobosTank>() is { } tank && WaterTreatment.Wants(tank);
}

/// <summary>What this vehicle's own kit draws now, in amps at its voltage.</summary>
public class Input_TankTreatmentAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f && context.Vehicle.GetComp<CompPhobosTank>() is { } tank ? WaterTreatment.WattsWanted(tank) / volts : 0f;
    }
}
