using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The camp accessories' arithmetic (Framework 0.129.0; docs/development/vehicle-accessories.md, the power budget): what a vehicle's power outlet
/// gives the camp and draws from the vehicle, which cargo food its fridge keeps cold, and whether a camp load still runs on the house battery.
/// Game-free; every figure it is given is the rules Def's, authored.
/// </summary>
public static class CampRule
{
    /// <summary>The outlet's most watts on this system's voltage: the 12 V figure, scaled by the voltage (a 24 V truck's inverter is twice the size).</summary>
    public static double OutletRating(double wattsAt12Volts, double systemVolts) => systemVolts <= 0 ? 0 : wattsAt12Volts * systemVolts / 12.0;

    /// <summary>What the outlet gives the camp now: what the camp's devices ask beyond its other sources, up to the outlet's rating. Never below 0.</summary>
    public static double OutletWatts(double campAsksWatts, double ratingWatts) => Math.Max(0, Math.Min(campAsksWatts, ratingWatts));

    /// <summary>What giving those watts draws from the vehicle's electrics, in amps at its voltage through the inverter's efficiency.</summary>
    public static double OutletAmps(double watts, double systemVolts, double inverterEfficiency) =>
        watts <= 0 || systemVolts <= 0 || inverterEfficiency <= 0 ? 0 : watts / systemVolts / inverterEfficiency;

    /// <summary>
    /// Which food the fridge keeps cold: the stacks closest to spoiling first, each whole stack or not at all, until the next would pass its
    /// load (the owner's choice, 17 Nov 2026: automatic, no dialog). Returns the indexes of the stacks taken, in the order given.
    /// </summary>
    /// <param name="stacks">Each stack's hours until it spoils at the temperature it is kept at now, and its kilograms.</param>
    public static List<int> FridgeTakes(IReadOnlyList<(double hoursToSpoil, double kilograms)> stacks, double loadKilograms)
    {
        var taken = new List<int>();
        double used = 0;
        foreach (int i in Enumerable.Range(0, stacks.Count).OrderBy(i => stacks[i].hoursToSpoil).ThenBy(i => i))
        {
            double kg = Math.Max(0, stacks[i].kilograms);
            if (used + kg > loadKilograms + 1e-9) continue;
            used += kg;
            taken.Add(i);
        }
        taken.Sort();
        return taken;
    }

    /// <summary>The fridge's temperature: its own, or the air's when the air is colder (a fridge does not heat).</summary>
    public static double FridgeTemperature(double fridgeDegrees, double airDegrees) => Math.Min(fridgeDegrees, airDegrees);

    /// <summary>Whether a camp load runs on the house battery: only above its low-voltage cut-off, as a real one's does.</summary>
    public static bool HouseBatteryRuns(double houseCharge, double cutoff) => houseCharge > cutoff;
}
