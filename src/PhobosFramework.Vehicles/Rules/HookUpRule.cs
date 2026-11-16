using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// A parked vehicle on a powered grid (Framework 0.120.0; owner, 17 Nov 2026: "yes, and it draws real watts"): the grid carries the vehicle's
/// electrical loads up to the hook-up's rating and charges its battery, and the colony's grid pays for both through a hidden consumer under
/// the vehicle. Game-free; the rating and the supply's efficiency are the rules Def's, authored.
/// </summary>
public static class HookUpRule
{
    /// <summary>The amps of the vehicle's loads the grid carries: all of them up to the hook-up's rating. The battery covers the rest.</summary>
    public static double CarriedAmps(double loadAmps, double ratingAmps) =>
        loadAmps <= 0 || ratingAmps <= 0 ? 0 : Math.Min(loadAmps, ratingAmps);

    /// <summary>The charger's amps at this state of charge: the full rate below 80 %, tapering to nothing at full, as the charge flow's curve does.</summary>
    public static double ChargeAmps(double chargerAmps, double charge)
    {
        if (chargerAmps <= 0 || charge >= 1) return 0;
        if (charge <= 0.8) return chargerAmps;
        return chargerAmps * (1 - charge) / 0.2;
    }

    /// <summary>The watts the grid gives: the amps it delivers at the vehicle's voltage, divided by the supply's efficiency.</summary>
    public static double GridWatts(double carriedAmps, double chargeAmps, double volts, double efficiency)
    {
        double amps = Math.Max(0, carriedAmps) + Math.Max(0, chargeAmps);
        if (amps <= 0 || volts <= 0) return 0;
        return amps * volts / Math.Min(1, Math.Max(0.05, efficiency));
    }
}
