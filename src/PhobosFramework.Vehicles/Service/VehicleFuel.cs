using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The one way our code burns or gives back a vehicle's fuel (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S2): the
/// engine model's correction, standing work, heaters, a holed tank and a helicopter's own runs all come through here, saying what the fuel
/// was for, so the vehicle can tell the player what it burns and why, and the world map's range can use the same figures the burn does.
/// Vehicle Framework's own flat burn stays its own; <see cref="FrameworkPerDay"/> reads it. Calls <see cref="CompFueledTravel.ConsumeFuel"/>
/// and never patches it: leaks, electric discharge and other mods' aircraft go through it too.
/// </summary>
public static class VehicleFuel
{
    /// <summary>Burns up to <paramref name="amount"/> for <paramref name="use"/>; returns what was burned (never more than the tank holds).</summary>
    public static float Burn(VehiclePawn vehicle, float amount, FuelUse use, CompPhobosVehicleSystems? comp = null)
    {
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel == null || amount <= 0f || fuel.EmptyTank) return 0f;
        float burned = Mathf.Min(amount, fuel.Fuel);
        fuel.ConsumeFuel(burned);
        (comp ?? vehicle.GetComp<CompPhobosVehicleSystems>())?.FuelLog.Note(use, burned);
        return burned;
    }

    /// <summary>Gives back fuel the engine model says Vehicle Framework's flat rate over-burned (step S2: the one kind of refund).</summary>
    public static void GiveBack(VehiclePawn vehicle, float amount, FuelUse use, CompPhobosVehicleSystems? comp = null)
    {
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel == null || amount <= 0f) return;
        fuel.Refuel(amount);
        (comp ?? vehicle.GetComp<CompPhobosVehicleSystems>())?.FuelLog.Note(use, -amount);
    }

    /// <summary>Vehicle Framework's own burn now, per day: on a map while drafted (full moving, half standing), on the world map while its
    /// caravan moves. Nothing on the battery (the generator set's hybrid stops it, BatteryDrive).</summary>
    public static double FrameworkPerDay(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        CompFueledTravel? fuel = vehicle.CompFueledTravel;
        if (fuel == null || fuel.EmptyTank || comp.BatteryDrive is { OnBattery: true }) return 0;
        if (vehicle.Spawned) return FuelRateRule.FrameworkPerDay(fuel.FuelEfficiency, vehicle.Drafted, vehicle.vehiclePather is { Moving: true });
        Vehicles.World.VehicleCaravan? caravan = vehicle.GetVehicleCaravan();
        bool moving = caravan?.vehiclePather != null && caravan.vehiclePather.MovingNow;
        return moving ? fuel.ConsumptionRateWorldPerTick * FuelRateRule.TicksPerDay : 0;
    }

    /// <summary>What the vehicle burns now, per day, for each use that burns anything, the engine's own counting Vehicle Framework's flat
    /// rate and our correction to it; and the total.</summary>
    public static double PerDay(CompPhobosVehicleSystems comp, List<(FuelUse use, double perDay)> parts)
    {
        parts.Clear();
        double engine = FrameworkPerDay(comp) + comp.FuelLog.PerDay(FuelUse.Engine);
        double total = 0;
        if (engine > MinShownPerDay)
        {
            parts.Add((FuelUse.Engine, engine));
            total += engine;
        }
        foreach (FuelUse use in Uses)
        {
            double perDay = comp.FuelLog.PerDay(use);
            if (perDay <= MinShownPerDay) continue;
            parts.Add((use, perDay));
            total += perDay;
        }
        return total;
    }

    /// <summary>Below this a use is not worth naming (authored: a twentieth of a unit a day).</summary>
    public const double MinShownPerDay = 0.05;

    static readonly FuelUse[] Uses = { FuelUse.Work, FuelUse.Heater, FuelUse.Leak, FuelUse.Flight };

    /// <summary>
    /// The factor on Vehicle Framework's world-map rate while the caravan moves (step S6): the same one the engine model's correction puts
    /// on the burn (<see cref="Powertrain.ApplyFuel"/>), at the nominal cruise when the fuel follows the model. The world map's range reads
    /// this, so it shows what the vehicle will really burn: tunes, a clogged filter and the loads included.
    /// </summary>
    public static double WorldMoveFactor(CompPhobosVehicleSystems? comp)
    {
        if (comp?.Powertrain is not { Full: true, Ground: true } train) return 1;
        if (comp.Vehicle.CompFueledTravel is not { } fuel || fuel.Props.electricPowered) return 1;
        double factor = comp.Fittings.TunedFuelFactor * comp.EngineFuelFactor;
        if (VehicleFittings.FuelFollowsModel) factor *= train.CruiseFuelShare(comp.LedgerExtraShare);
        return factor;
    }
}

/// <summary>
/// What a vehicle burned lately for each use (step S2), as one fading figure per use (<see cref="FuelRateRule"/>), so its rates can be shown
/// without storing each burn. Derived, never saved: after a load the rates build up again within an hour or so.
/// </summary>
public sealed class VehicleFuelLog
{
    static readonly int Count = System.Enum.GetValues(typeof(FuelUse)).Length;
    readonly double[] figures = new double[Count];
    readonly int[] lastTicks = new int[Count];

    public void Note(FuelUse use, double amount)
    {
        if (use == FuelUse.Spilled || Current.Game == null) return;
        int i = (int)use;
        int now = Find.TickManager.TicksGame;
        figures[i] = FuelRateRule.Add(figures[i], lastTicks[i], now, amount);
        lastTicks[i] = now;
    }

    public double PerDay(FuelUse use)
    {
        if (Current.Game == null) return 0;
        int i = (int)use;
        return FuelRateRule.PerDay(figures[i], lastTicks[i], Find.TickManager.TicksGame);
    }
}
