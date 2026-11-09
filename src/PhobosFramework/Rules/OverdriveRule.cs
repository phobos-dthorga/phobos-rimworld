namespace Phobos.Framework.Rules;

/// <summary>Why overdrive had to stop, or nothing.</summary>
public enum OverdriveStop
{
    None,
    PartWorn,
    EngineHot,
}

/// <summary>
/// Overdrive (docs/development/vehicle-drill-rig.md, vehicle-survey-truck.md): a working vehicle runs faster and pays in fuel, heat and the wear of its working parts, the vehicle's answer to
/// Vanilla Expanded's overclock, whose price is a tripled breakdown chance. It cuts itself out before it wrecks anything.
/// </summary>
public static class OverdriveRule
{
    public static float Rate(float rate, bool on, float factor) => on ? rate * factor : rate;

    public static float FuelPerDay(float fuelPerDay, bool on, float fuelFactor) => on ? fuelPerDay * fuelFactor : fuelPerDay;

    /// <summary>Health the working parts lose over this many hours of overdrive.</summary>
    public static float Wear(float perHour, float hours) => perHour * hours;

    /// <param name="lowestPartShare">The worst working part's health as a share of its maximum.</param>
    /// <param name="coolant">The coolant temperature, or NaN for a vehicle with no cooling system.</param>
    public static OverdriveStop MustStop(float lowestPartShare, float offBelowShare, float coolant, float hotAbove)
    {
        if (lowestPartShare < offBelowShare) return OverdriveStop.PartWorn;
        if (!float.IsNaN(coolant) && coolant >= hotAbove) return OverdriveStop.EngineHot;
        return OverdriveStop.None;
    }
}
