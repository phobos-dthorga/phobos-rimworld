using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What a vehicle's electrical loads cost its engine (crosslink 5, Framework 0.70.0; docs/development/vehicle-system-crosslinks.md),
/// game-free. An alternator turns shaft power into electricity at roughly 55 to 70 % efficiency (secondary, approximate: Wikipedia,
/// "Alternator (automotive)"), so each watt the loads draw costs about 1.6 watts at the crankshaft; the engine burns fuel and makes
/// heat for that power as for any other.
/// </summary>
public static class AccessoryRules
{
    /// <summary>Electrical watts delivered per watt of shaft power taken: 0.6, inside the cited range (authored).</summary>
    public const double AlternatorEfficiency = 0.6;

    /// <summary>The share of the engine's rated power the loads take now: 0 to 1.</summary>
    public static double EngineShare(double loadAmps, double volts, double ratedKilowatts)
    {
        if (loadAmps <= 0 || volts <= 0 || ratedKilowatts <= 0) return 0;
        double shaftKilowatts = loadAmps * volts / AlternatorEfficiency / 1000.0;
        return Math.Min(1, shaftKilowatts / ratedKilowatts);
    }
}
