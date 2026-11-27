using System.Collections.Generic;
using System.Text;
using Phobos.Framework.Environment;
using Phobos.Framework.Upgrades;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>Makes a building a service yard's fuel pump: from its own tank it fills the vehicles standing in its yard, emptiest first,
/// and with its charging cable fitted (a building upgrade) charges electric ones from the grid. It works only as yard equipment: the
/// building needs <c>PhobosYardEquipmentExtension</c> and must stand in or touch a service yard with a dyno bay, and needs a
/// <c>CompProperties_Refuelable</c> (its tank) and <c>tickerType</c> Rare; power is optional, and the cable needs
/// <c>CompProperties_PhobosBuildingUpgrades</c>.</summary>
public sealed class CompProperties_PhobosFuelPump : CompProperties
{
    /// <summary>How much fuel the pump moves in an hour (authored [A]: the depot bay pump's 30).</summary>
    public float pumpPerHour = 30f;

    /// <summary>
    /// The watts the charging cable draws for each unit of a vehicle's charge rate. Derived from Vehicle Framework's own charging
    /// (CompFueledTravel.CompTick, read 21 Nov 2026): an electric vehicle on a powered net takes its charge rate in watt-days from the net's
    /// batteries every 120 ticks, which is 60,000 / 120 = 500 W for each unit of rate. The cable charges at that rate and bills the same
    /// power through the pump's own draw, so the grid pays as it would for the game's own charging.
    /// </summary>
    public float chargeWattsPerRate = 500f;

    public CompProperties_PhobosFuelPump() => compClass = typeof(CompPhobosFuelPump);
}

/// <summary>
/// A service yard's fuel pump (Framework 0.210.0; docs/development/vehicle-service-yard.md; owner, 20 Nov 2026: "It could also have the role
/// of refilling fuel too"): its own tank, filled by haulers as the depot bay's is, fills the vehicles standing in its yard one at a time, the
/// emptiest first, at the depot bay pump's rate. With its charging cable fitted (a building upgrade), it also charges electric vehicles in
/// the yard from the colony's power, one at a time. Which vehicle is next is the yard's choice (<see cref="MapComponent_PhobosServiceYards"/>);
/// the filling is done here on the rare tick.
/// </summary>
public sealed class CompPhobosFuelPump : ThingComp
{
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;
    /// <summary>Vehicle Framework charges an electric vehicle by its rate once every this many ticks (CompFueledTravel.TicksToCharge).</summary>
    const float VehicleFrameworkChargeTicks = 120f;

    CompRefuelable? tank;
    CompPowerTrader? power;
    CompBreakdownable? breakdown;
    CompPhobosBuildingUpgrades? upgrades;

    /// <summary>Set by the yard's look: the vehicle to fill, the vehicle to charge, and one of a fuel no pump here holds. Derived, never saved.</summary>
    internal VehiclePawn? FuelTarget;
    internal VehiclePawn? ChargeTarget;
    internal VehiclePawn? WrongFuel;

    /// <summary>What the pump last did, for the inspect pane. Derived, never saved.</summary>
    string? fuelStatus;
    string? chargeStatus;

    public CompProperties_PhobosFuelPump Props => (CompProperties_PhobosFuelPump)props;

    public static IReadOnlyList<CompPhobosFuelPump> On(Map map) => MapRegistry<CompPhobosFuelPump>.On(map);

    public bool Working => (power == null || power.PowerOn) && breakdown is not { BrokenDown: true };

    /// <summary>Whether its charging cable is fitted.</summary>
    public bool Charges => upgrades?.Grants(ServiceYards.ChargesVehicles) == true;

    /// <summary>The fuel its tank holds.</summary>
    public ThingDef? Fuel => tank?.Props.fuelFilter.AnyAllowedDef;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        tank = parent.GetComp<CompRefuelable>();
        power = parent.GetComp<CompPowerTrader>();
        breakdown = parent.GetComp<CompBreakdownable>();
        upgrades = parent.GetComp<CompPhobosBuildingUpgrades>();
        MapRegistry<CompPhobosFuelPump>.Add(parent.Map, this);
        ServiceYards.Dirty(parent.Map);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapRegistry<CompPhobosFuelPump>.Remove(map, this);
        ServiceYards.Dirty(map);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        fuelStatus = null;
        chargeStatus = null;
        float chargeWatts = 0f;
        if (Working)
        {
            Fill();
            chargeWatts = Charge();
        }
        // The cable's draw rides on the pump's own: the grid pays for what it gives, and a grid that cannot pay switches the pump off.
        if (power != null) power.PowerOutput = -(power.Props.PowerConsumption + chargeWatts);
    }

    void Fill()
    {
        if (FuelTarget is not { Spawned: true } vehicle || vehicle.CompFueledTravel is not { } fuel || tank == null) return;
        if (!tank.HasFuel)
        {
            fuelStatus = "PhobosFuelPump_TankEmpty".Translate().Resolve();
            return;
        }
        float amount = UnityEngine.Mathf.Min(fuel.FuelCapacity - fuel.Fuel, tank.Fuel, Props.pumpPerHour * HoursPerRareTick);
        if (amount <= 0f) return;
        tank.ConsumeFuel(amount);
        fuel.Refuel(amount);
        fuelStatus = "PhobosFuelPump_Filling".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Seen.Wants("vehicles.service-yard.fuel", vehicle)) Seen.Note("vehicles.service-yard.fuel", vehicle, amount.ToString("0.0"));
    }

    /// <summary>Charges the vehicle whose turn it is, at Vehicle Framework's own rate; returns the watts it draws for it.</summary>
    float Charge()
    {
        if (!Charges || ChargeTarget is not { Spawned: true } vehicle || vehicle.CompFueledTravel is not { Props.electricPowered: true } battery) return 0f;
        float rate = battery.Props.chargeRate;
        float amount = UnityEngine.Mathf.Min(battery.FuelCapacity - battery.Fuel, rate * GenTicks.TickRareInterval / VehicleFrameworkChargeTicks);
        if (amount <= 0f || rate <= 0f) return 0f;
        battery.Refuel(amount);
        chargeStatus = "PhobosFuelPump_Charging".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Seen.Wants("vehicles.service-yard.charge", vehicle)) Seen.Note("vehicles.service-yard.charge", vehicle, amount.ToString("0.0"));
        return rate * Props.chargeWattsPerRate;
    }

    public override string? CompInspectStringExtra()
    {
        var text = new StringBuilder();
        if (ServiceYards.For(parent.Map)?.OwnerOf(parent) is not { Works: true }) text.Append("PhobosFuelPump_NoYard".Translate().Resolve());
        else if (!Working) text.Append("PhobosFuelPump_Off".Translate().Resolve());
        else
        {
            text.Append(fuelStatus ?? "PhobosFuelPump_Idle".Translate().Resolve());
            if (chargeStatus != null) text.AppendLine().Append(chargeStatus);
            if (WrongFuel != null)
            {
                text.AppendLine().Append("PhobosFuelPump_WrongFuel".Translate(WrongFuel.LabelShort.Named("VEHICLE"),
                    (WrongFuel.CompFueledTravel?.Props.fuelType?.label ?? "?").Named("FUEL")).Resolve());
            }
        }
        return text.ToString();
    }
}
