using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Refuelling from cargo on the world map (Framework 0.137.0; docs/development/vehicle-cargo-refuel.md; owner, 18 Nov 2026). A vehicle
/// whose player has switched it on tops its tank up from the fuel the caravan carries when the tank falls to a quarter, so the caravan
/// is not halted to refuel by hand; each top-up costs a short stop. It uses Vehicle Framework's own way of taking fuel from a caravan's
/// cargo (every vehicle's and pack animal's), so what it pours is what its refuel button would pour. The player's own caravans only.
/// Since 0.139.0 an aircraft on a journey does the same by setting down on the way and pouring from the fuel it carries aboard.
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
        return MakeToggle(vehicle, comp, "PhobosVehicleSystems_CargoRefuelDesc".Translate(fuelName.Named("FUEL"), carried.Named("CARRIED"),
            rules.refuelFromCargoBelow.ToStringPercent().Named("BELOW")).Resolve());
    }

    static Command_Toggle MakeToggle(VehiclePawn vehicle, CompPhobosVehicleSystems comp, string description) => new()
    {
        defaultLabel = "PhobosVehicleSystems_CargoRefuelLabel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(),
        defaultDesc = description,
        // Vehicle Framework's own picture for refuelling from cargo, which its fuel bar uses (a required mod's icon, confirmed to exist).
        icon = VehicleTex.RefuelFromCargo,
        isActive = () => comp.RefuelFromCargo,
        toggleAction = () => comp.RefuelFromCargo = !comp.RefuelFromCargo,
    };

    // ---- Aircraft: setting down on a journey to refuel (Framework 0.139.0; docs/development/vehicle-cargo-refuel.md) ----

    /// <summary>Whether this vehicle flies between places on the world map (it has Vehicle Framework's launcher).</summary>
    public static bool Flies(VehiclePawn vehicle) => vehicle.CompVehicleLauncher != null;

    /// <summary>The aircraft's fuel carried aboard: in its own cargo and in the pockets of everyone aboard. This is what goes with it on a
    /// flight. Vehicle Framework's own lookup finds nothing in flight (it reads a caravan's or a spawned vehicle's), so this is ours.</summary>
    static List<Thing> FuelAboard(VehiclePawn vehicle)
    {
        var found = new List<Thing>();
        ThingDef? fuelType = vehicle.CompFueledTravel?.Props.fuelType;
        if (fuelType == null) return found;
        if (vehicle.inventory?.innerContainer is { } hold)
        {
            foreach (Thing thing in hold) if (thing.def == fuelType) found.Add(thing);
        }
        foreach (Pawn pawn in vehicle.AllPawnsAboard)
        {
            if (pawn.inventory?.innerContainer is not { } pockets) continue;
            foreach (Thing thing in pockets) if (thing.def == fuelType) found.Add(thing);
        }
        return found;
    }

    /// <summary>The aircraft's fuel carried aboard, in units.</summary>
    public static int CarriedAboard(VehiclePawn vehicle)
    {
        if (!Burns(vehicle)) return 0;
        int total = 0;
        foreach (Thing thing in FuelAboard(vehicle)) total += thing.stackCount;
        return total;
    }

    /// <summary>Takes this much of the fuel carried aboard out of the cargo and pockets it sits in. What is poured is used up.</summary>
    static void TakeAboard(VehiclePawn vehicle, int count)
    {
        foreach (Thing thing in FuelAboard(vehicle))
        {
            if (count <= 0) break;
            int take = System.Math.Min(count, thing.stackCount);
            thing.SplitOff(take).Destroy(DestroyMode.Vanish);
            count -= take;
        }
    }

    /// <summary>The toggle for an aircraft on a map or in flight: shown while it carries its fuel aboard, or while it is on. Presentation
    /// only: it flips the vehicle's switch.</summary>
    public static Gizmo? AircraftToggle(VehiclePawn vehicle, CompPhobosVehicleSystems comp)
    {
        if (!Flies(vehicle) || !Burns(vehicle)) return null;
        int carried = CarriedAboard(vehicle);
        // Framework 0.199.0: also while a depot of ours holds its fuel, since the switch lets it stop at one on the way.
        if (carried <= 0 && !comp.RefuelFromCargo && !Depots.AnyHolding(vehicle.CompFueledTravel.Props.fuelType)) return null;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        return MakeToggle(vehicle, comp, "PhobosVehicleSystems_FlightRefuelDesc".Translate(
            vehicle.CompFueledTravel.Props.fuelType.label.Named("FUEL"), carried.Named("CARRIED"),
            rules.refuelFromCargoBelow.ToStringPercent().Named("BELOW")).Resolve());
    }

    /// <summary>Why an aircraft cannot set down on this tile to refuel, or null when it can. It needs open land: not water, not a
    /// mountain range, and not ground held by a faction hostile to the player (agent defaults, 18 Nov 2026).</summary>
    public static string? NoLandingReason(PlanetTile tile)
    {
        if (!tile.Valid || Find.WorldGrid == null || tile.LayerDef == null || tile.LayerDef.isSpace)
            return "PhobosVehicleSystems_FlightRefuelNoLandingSpace".Translate().Resolve();
        Tile ground = Find.WorldGrid[tile];
        if (ground.WaterCovered) return "PhobosVehicleSystems_FlightRefuelNoLandingWater".Translate().Resolve();
        if (ground.hilliness == Hilliness.Impassable) return "PhobosVehicleSystems_FlightRefuelNoLandingMountains".Translate().Resolve();
        foreach (WorldObject thing in Find.WorldObjects.ObjectsAt(tile))
        {
            if (thing.Faction != null && thing.Faction.HostileTo(Faction.OfPlayer))
                return "PhobosVehicleSystems_FlightRefuelNoLandingHostile".Translate(thing.LabelShort.Named("PLACE")).Resolve();
        }
        return null;
    }

    /// <summary>
    /// The look at an aircraft flying a journey, every 250 ticks: with its switch on, fuel aboard and its tank at a quarter or less, it
    /// sets down on the tile beneath it, pours, and stays down while the crew work (Vehicle Framework's in-flight flag held off, so it
    /// neither moves nor burns; its journey is untouched and resumes where it stopped). Over water, mountains or an enemy's ground it
    /// flies on and tries again at the next look. Recon circling is left alone.
    /// </summary>
    public static void FlightLook(AerialVehicleInFlight flight, VehiclePawn vehicle, CompPhobosVehicleSystems comp)
    {
        if (!comp.RefuelFromCargo || comp.LandedRefuelUntil > 0 || !Burns(vehicle)) return;
        if (flight.flightPath is not { Empty: false, Circling: false } || flight.recon) return;
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        // Framework 0.199.0 (DP3(b)): passing a depot of ours that holds its fuel, below half a tank, it sets down there and fills up.
        if (fuel.FuelPercent < Depots.AircraftStopBelow && Depots.StopFor(vehicle, (PlanetTile)WorldHelper.GetNearestTile(flight.DrawPos)) is { } depot
            && StopAtDepot(flight, vehicle, comp, depot, rules))
        {
            return;
        }
        if (!CargoFuelRule.ShouldTopUp(fuel.Fuel, fuel.FuelCapacity, rules.refuelFromCargoBelow)) return;
        string fuelName = fuel.Props.fuelType.label;
        int carried = CarriedAboard(vehicle);
        if (carried <= 0)
        {
            if (!comp.CargoFuelGoneTold)
            {
                comp.CargoFuelGoneTold = true;
                Messages.Message("PhobosVehicleSystems_CargoRefuelGone".Translate(vehicle.LabelShort.Named("VEHICLE"), fuelName.Named("FUEL")).Resolve(),
                    flight, MessageTypeDefOf.CautionInput, historical: false);
            }
            return;
        }
        comp.CargoFuelGoneTold = false;
        PlanetTile below = (PlanetTile)WorldHelper.GetNearestTile(flight.DrawPos);
        // Framework 0.140.0: nor where it could not hover at its weight in that tile's air, under the refuse setting (LandingCheck).
        LandingCheck.Outcome landing = LandingCheck.Outcome.Fine;
        string? refusal = NoLandingReason(below) ?? LandingCheck.StopRefusal(vehicle, below, out landing);
        if (refusal != null)
        {
            if (!comp.LandingRefusedTold)
            {
                comp.LandingRefusedTold = true;
                Messages.Message("PhobosVehicleSystems_FlightRefuelCannotLand".Translate(vehicle.LabelShort.Named("VEHICLE"), refusal.Named("REASON")).Resolve(),
                    flight, MessageTypeDefOf.CautionInput, historical: false);
            }
            return;
        }
        comp.LandingRefusedTold = false;
        int pour = CargoFuelRule.TopUpAmount(fuel.Fuel, fuel.FuelCapacity, carried);
        if (pour <= 0) return;
        TakeAboard(vehicle, pour);
        fuel.Refuel(pour);
        double stopTicks = CargoFuelRule.StopTicks(pour, fuel.FuelCapacity, rules.refuelStopHoursPerTank, GenDate.TicksPerHour)
            + System.Math.Max(0f, rules.refuelLandingHours) * GenDate.TicksPerHour;
        comp.LandedRefuelUntil = Find.TickManager.TicksGame + System.Math.Max(1, (int)System.Math.Ceiling(stopTicks));
        comp.LandedHoldApplied = true;
        vehicle.CompVehicleLauncher.inFlight = false;
        Messages.Message("PhobosVehicleSystems_FlightRefuelled".Translate(vehicle.LabelShort.Named("VEHICLE"), pour.Named("AMOUNT"), fuelName.Named("FUEL"),
            CarriedAboard(vehicle).Named("LEFT"), ((int)stopTicks).ToStringTicksToPeriod().Named("TIME")).Resolve(),
            flight, MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("aircraft.landed-refuel", vehicle)) Seen.Note("aircraft.landed-refuel", vehicle, pour + " " + fuel.Props.fuelType.defName);
        // Beyond the engines under allow at a cost or warn only: it set down all the same, and that comes at its price.
        LandingCheck.Apply(vehicle, landing, flight);
    }

    /// <summary>Sets down at a depot and fills the tank from it (Framework 0.199.0, DP3(b)), as a stop for the fuel aboard does. False when
    /// it cannot hover down there, or the depot gave nothing: it flies on as before.</summary>
    static bool StopAtDepot(AerialVehicleInFlight flight, VehiclePawn vehicle, CompPhobosVehicleSystems comp, PhobosSlingWreck depot, PhobosVehicleRulesDef rules)
    {
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        if (LandingCheck.StopRefusal(vehicle, depot.Tile, out LandingCheck.Outcome landing) != null) return false;
        int got = Depots.Draw(depot, fuel.Props.fuelType, (int)(fuel.FuelCapacity - fuel.Fuel));
        if (got <= 0) return false;
        fuel.Refuel(got);
        double stopTicks = CargoFuelRule.StopTicks(got, fuel.FuelCapacity, rules.refuelStopHoursPerTank, GenDate.TicksPerHour)
            + System.Math.Max(0f, rules.refuelLandingHours) * GenDate.TicksPerHour;
        comp.LandedRefuelUntil = Find.TickManager.TicksGame + System.Math.Max(1, (int)System.Math.Ceiling(stopTicks));
        comp.LandedHoldApplied = true;
        vehicle.CompVehicleLauncher.inFlight = false;
        Messages.Message("PhobosDepot_AircraftStopped".Translate(vehicle.LabelShort.Named("VEHICLE"), depot.LabelCap.Named("DEPOT"), got.Named("AMOUNT"),
            fuel.Props.fuelType.label.Named("FUEL"), ((int)stopTicks).ToStringTicksToPeriod().Named("TIME")).Resolve(), flight, MessageTypeDefOf.SilentInput, historical: false);
        if (Seen.Wants("vehicles.depot", vehicle, "aircraft-stop")) Seen.Note("vehicles.depot", vehicle, got + " units", "aircraft-stop");
        LandingCheck.Apply(vehicle, landing, flight);
        depot.RemoveIfEmpty();
        return true;
    }

    /// <summary>
    /// Every tick while an aircraft is down for fuel: keeps it down (after a load, Vehicle Framework's resumed journey puts it back in
    /// the air, so the stop is put back once), lets the player's own new order end the stop early, and lifts off when the time is up.
    /// </summary>
    public static void HoldTick(AerialVehicleInFlight flight, VehiclePawn vehicle, CompPhobosVehicleSystems comp)
    {
        if (comp.LandedRefuelUntil <= 0) return;
        CompVehicleLauncher launcher = vehicle.CompVehicleLauncher;
        if (!comp.LandedHoldApplied)
        {
            launcher.inFlight = false;
            comp.LandedHoldApplied = true;
            return;
        }
        if (launcher.inFlight)
        {
            // The player gave it a new destination while it was down (Vehicle Framework's launch puts it in the air): the stop ends there.
            comp.LandedRefuelUntil = 0;
            return;
        }
        if (Find.TickManager.TicksGame < comp.LandedRefuelUntil) return;
        comp.LandedRefuelUntil = 0;
        if (flight.flightPath is { Empty: false })
        {
            launcher.inFlight = true;
            Messages.Message("PhobosVehicleSystems_FlightRefuelLiftOff".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(),
                flight, MessageTypeDefOf.SilentInput, historical: false);
            Seen.Note("aircraft.landed-refuel-lift-off", vehicle);
        }
        else
        {
            // Nowhere left to go (should not happen: a stop is only made on a journey): it stays where it is, as a caravan.
            flight.SwitchToCaravan();
        }
    }

    /// <summary>
    /// Whether the fuel aboard lets an aircraft with its switch on reach a journey its tank alone cannot (Framework 0.139.0), and how many
    /// times it would set down on the way; null when it cannot, or when the switch is off. Used by the destination checks.
    /// </summary>
    public static int? StopsForJourney(VehiclePawn vehicle, double cost)
    {
        if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { RefuelFromCargo: true } || !Burns(vehicle) || !Flies(vehicle)) return null;
        CompFueledTravel fuel = vehicle.CompFueledTravel;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        return CargoFuelRule.StopsForJourney(cost, fuel.Fuel, fuel.FuelCapacity, CarriedAboard(vehicle), rules.refuelFromCargoBelow);
    }
}
