using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Exhaust heat recovery on the generator set, game-free (owner, 11 Nov 2026; docs/development/vehicle-generator-set.md). A diesel set
/// throws away more energy as heat than it makes as electricity; a heat exchanger on its exhaust returns part of that to a room. Here the
/// heat follows the electricity being made (a share of the output watts), converted at the game's own heater's rate, and stops when the
/// room is warm enough, as the game's heater does. The share is authored; the rate and the target come from the game's heater.
/// </summary>
public static class HeatRecoveryRule
{
    /// <summary>Heat per second to push into the room: none when nothing is made, no exchanger is fitted, or the room is warm enough.</summary>
    public static double HeatPerSecond(double outputWatts, double recoveredShare, double heatPerSecondPerWatt, double roomCelsius, double targetCelsius)
    {
        if (outputWatts <= 0 || recoveredShare <= 0 || heatPerSecondPerWatt <= 0 || roomCelsius >= targetCelsius) return 0;
        return outputWatts * recoveredShare * heatPerSecondPerWatt;
    }
}
