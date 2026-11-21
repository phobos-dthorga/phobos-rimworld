using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Fire wearing a vehicle's parts (Framework 0.221.0; docs/development/vehicle-fire.md, FR2, FR3, FR5, FR6; owner, 22 Nov 2026),
/// game-free. A part's role wears a share of its health each hour by the size of the fire (FR2: until now any fire wore at the full
/// rate). A soft part (rubber, hoses, belts) also burns from a ground fire under a vehicle that cannot itself catch fire (FR5, the
/// owner's "soft parts only"). Every figure is authored.
/// </summary>
public static class FireWearRule
{
    /// <summary>The share of a part's health lost per hour: the role's rate at full fire, times the fire's size, never under the
    /// smallest share of the rate a burning vehicle wears at.</summary>
    public static double SharePerHour(double rateAtFullFire, double fire, double leastShareOfRate)
    {
        if (rateAtFullFire <= 0 || fire <= 0) return 0;
        double share = Math.Min(fire, 1);
        return rateAtFullFire * Math.Max(share, Math.Max(leastShareOfRate, 0));
    }

    /// <summary>
    /// The fire a part feels: the fire on the vehicle; for a soft part also a ground fire under it, but only where the vehicle cannot catch
    /// fire itself (its author made it fireproof) and the setting allows.
    /// </summary>
    public static double FireFelt(double fireOnVehicle, double groundFireUnder, bool soft, bool vehicleCanCatch, bool groundSettingOn) =>
        soft && !vehicleCanCatch && groundSettingOn ? Math.Max(fireOnVehicle, groundFireUnder) : fireOnVehicle;
}
