namespace Phobos.Framework.Rules;

/// <summary>
/// Whether a reading has passed a threshold set either way, game-free. A bound left unset (NaN) never trips, so one rule serves "due
/// once the hours reach 120" and "due once the oil falls to 60%". Vehicle services use it; nothing in it names a vehicle.
/// </summary>
public static class ThresholdRule
{
    public static bool Passes(double value, double atOrAbove, double atOrBelow) =>
        (!double.IsNaN(atOrAbove) && value >= atOrAbove) || (!double.IsNaN(atOrBelow) && value <= atOrBelow);
}
