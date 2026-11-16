using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How far a vehicle's picture spreads past the cells it occupies, game-free (Framework 0.77.0, the owner's report of 11 Nov 2026: a
/// tuning rig could not reach aircraft). A helicopter three cells wide is drawn twelve wide, so its rotor sweeps four and a half cells past
/// its footprint each side, and a station placed clear of it is out of a reach measured from the footprint. Measuring from the drawn
/// extent instead puts anything under the wings or the rotor within reach.
/// </summary>
public static class ReachRule
{
    /// <summary>Whole cells the picture spreads past the footprint on each side, along one axis: none when it is drawn no wider.</summary>
    public static int Margin(double drawnCells, int footprintCells) =>
        drawnCells <= footprintCells ? 0 : (int)Math.Ceiling((drawnCells - footprintCells) / 2.0 - 1e-6);

    /// <summary>
    /// How many tiles an aircraft can still fly on the fuel aboard (Framework 0.114.1; the owner's report of 16 Nov 2026: no range line on an
    /// aircraft in flight). Vehicle Framework's own launch planner (CompVehicleLauncher.FuelNeededToLaunchAtDist, read in its 1.7 build) needs
    /// rate x multiplier x tiles / (0.001 x flight speed) fuel for a flight of so many tiles; this is that, turned round. 0 when it cannot fly
    /// or burns nothing it can measure.
    /// </summary>
    public static double FlightTiles(double fuel, double flightSpeed, double ratePerTick, double worldMultiplier)
    {
        double burn = ratePerTick * worldMultiplier;
        if (fuel <= 0 || flightSpeed <= 0 || burn <= 0) return 0;
        return fuel * 0.001 * flightSpeed / burn;
    }
}
