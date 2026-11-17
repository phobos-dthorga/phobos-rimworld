using System;
using Phobos.Framework.Rules;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What a vehicle's electrical loads cost its engine (crosslink 5, Framework 0.70.0; docs/development/vehicle-crosslinks.md), game-free.
/// An alternator turns shaft power into electricity at about 55 % (Remy's white paper, Bradfield 2015: 55 % typical, measured duty-cycle
/// averages of 54 to 60.5 %), so each watt the loads draw costs nearly two at the crankshaft; the engine burns fuel and makes heat for that
/// power as for any other. Since Framework 0.130.0 the efficiency is the caller's, from the rules Def's curve on the alternator's speed.
/// </summary>
public static class AccessoryRules
{
    /// <summary>The efficiency used when none is given: Remy's typical figure.</summary>
    public const double TypicalAlternatorEfficiency = 0.55;

    /// <summary>The share of the engine's rated power the loads take now: 0 to 1.</summary>
    /// <param name="alternatorEfficiency">Electrical watts out per shaft watt in, 0 to 1.</param>
    public static double EngineShare(double loadAmps, double volts, double ratedKilowatts, double alternatorEfficiency = TypicalAlternatorEfficiency)
    {
        if (loadAmps <= 0 || volts <= 0 || ratedKilowatts <= 0) return 0;
        double shaftKilowatts = ShaftKilowatts(PowerUnits.Watts(loadAmps, volts), alternatorEfficiency);
        return Math.Min(1, shaftKilowatts / ratedKilowatts);
    }

    /// <summary>The shaft power in kW an alternator takes to deliver these electrical watts. A tenth is the floor on the efficiency, so a
    /// curve read off its end can never ask for infinite power.</summary>
    public static double ShaftKilowatts(double electricalWatts, double alternatorEfficiency) =>
        PowerUnits.Kilowatts(PowerUnits.Input(electricalWatts, alternatorEfficiency, AlternatorEfficiencyFloor));

    const double AlternatorEfficiencyFloor = 0.1;
}
