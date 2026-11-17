using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Refuelling from cargo on the world map (Framework 0.137.0; docs/development/vehicle-cargo-refuel.md; owner, 18 Nov 2026). A vehicle
/// whose player has switched it on tops its tank up from the fuel the caravan carries when the tank falls to a quarter, so the caravan
/// is not halted to refuel by hand; each top-up costs a short stop. It uses Vehicle Framework's own way of taking fuel from a caravan's
/// cargo (every vehicle's and pack animal's), so what it pours is what its refuel button would pour. The player's own caravans only.
/// </summary>
public static class CargoRefuel
{
    /// <summary>Whether this vehicle could top up from cargo at all: it burns a fuel that is carried as items.</summary>
    public static bool Burns(VehiclePawn vehicle) =>
        vehicle.CompFueledTravel is { Props.fuelType: not null } fuel && !fuel.Props.electricPowered;

    /// <summary>The fuel of the vehicle's kind the caravan carries, in units (every vehicle's cargo and every pawn's).</summary>
    public static int Carried(VehiclePawn vehicle)
    {
        if (!Burns(vehicle)) return 0;
        int total = 0;
        foreach (Thing thing in CompFueledTravel.AllFuelFromInventory(vehicle)) total += thing.stackCount;
        return total;
    }

    /// <summary>
    /// Tops up every vehicle of the caravan that wants it, the emptiest first, while the caravan is on its way somewhere (a standing
    /// caravan burns nothing on the world map). The pouring holds the caravan up for as long as it takes, added to the leg it is on.
    /// </summary>
    public static void Tick(VehicleCaravan caravan)
    {
        VehicleCaravan_PathFollower? pather = caravan.vehiclePather;
        if (pather == null || !pather.Moving || caravan.Faction != Faction.OfPlayer) return;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        List<VehiclePawn>? wanting = null;
        foreach (VehiclePawn vehicle in caravan.VehiclesListForReading)
        {
            if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { RefuelFromCargo: true } || !Burns(vehicle)) continue;
            CompFueledTravel fuel = vehicle.CompFueledTravel;
            if (!CargoFuelRule.ShouldTopUp(fuel.Fuel, fuel.FuelCapacity, rules.refuelFromCargoBelow)) continue;
            (wanting ??= new List<VehiclePawn>()).Add(vehicle);
        }
        if (wanting == null) return;
        double stopTicks = 0;
        foreach (VehiclePawn vehicle in wanting.OrderBy(v => v.CompFueledTravel.FuelPercent))
        {
            CompFueledTravel fuel = vehicle.CompFueledTravel;
            int carried = Carried(vehicle);
            int pour = CargoFuelRule.TopUpAmount(fuel.Fuel, fuel.FuelCapacity, carried);
            CompPhobosVehicleSystems comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (pour <= 0)
            {
                // Said once a dry spell, not every look: the cans are empty.
                if (carried <= 0 && !comp.CargoFuelGoneTold)
                {
                    comp.CargoFuelGoneTold = true;
                    Messages.Message("PhobosVehicleSystems_CargoRefuelGone".Translate(vehicle.LabelShort.Named("VEHICLE"),
                        fuel.Props.fuelType.label.Named("FUEL")).Resolve(), caravan, MessageTypeDefOf.CautionInput, historical: false);
                }
                continue;
            }
            comp.CargoFuelGoneTold = false;
            fuel.ConsumeFuelFromInventory(pour);
            stopTicks += CargoFuelRule.StopTicks(pour, fuel.FuelCapacity, rules.refuelStopHoursPerTank, GenDate.TicksPerHour);
            Messages.Message("PhobosVehicleSystems_CargoRefuelled".Translate(vehicle.LabelShort.Named("VEHICLE"), pour.Named("AMOUNT"),
                fuel.Props.fuelType.label.Named("FUEL"), Carried(vehicle).Named("LEFT")).Resolve(), caravan, MessageTypeDefOf.SilentInput, historical: false);
            if (SeenInPlay.On) Seen.Note("vehicles.cargo-refuel", vehicle, pour + " " + fuel.Props.fuelType.defName);
        }
        // The stop: Vehicle Framework pays a leg's cost at one a tick, so the pouring's ticks added to it hold the caravan up as long.
        if (stopTicks > 0) pather.nextTileCostLeft += (float)stopTicks;
    }

    /// <summary>The toggle for one vehicle of a caravan: shown while the caravan carries its fuel, or while it is on (so it can be
    /// switched off once the cans are empty). Presentation only: it flips the vehicle's switch.</summary>
    public static Gizmo? Toggle(VehiclePawn vehicle, CompPhobosVehicleSystems comp)
    {
        if (!Burns(vehicle)) return null;
        int carried = Carried(vehicle);
        if (carried <= 0 && !comp.RefuelFromCargo) return null;
        string fuelName = vehicle.CompFueledTravel.Props.fuelType.label;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        return new Command_Toggle
        {
            defaultLabel = "PhobosVehicleSystems_CargoRefuelLabel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            defaultDesc = "PhobosVehicleSystems_CargoRefuelDesc".Translate(fuelName.Named("FUEL"), carried.Named("CARRIED"),
                rules.refuelFromCargoBelow.ToStringPercent().Named("BELOW")).Resolve(),
            // Vehicle Framework's own picture for refuelling from cargo, which its fuel bar uses (a required mod's icon, confirmed to exist).
            icon = VehicleTex.RefuelFromCargo,
            isActive = () => comp.RefuelFromCargo,
            toggleAction = () => comp.RefuelFromCargo = !comp.RefuelFromCargo,
        };
    }
}
