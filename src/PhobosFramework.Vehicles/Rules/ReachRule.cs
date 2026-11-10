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
}
