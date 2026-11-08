using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The arithmetic of a thing that holds a few uses (a booster pack's starts, a kit's refills), game-free: a use takes one whole
/// charge, and recharging over time adds charge at a set number of hours per use, never past full.
/// </summary>
public static class ChargeRules
{
    /// <summary>Whole uses left in a charge level (a part-charged use does not count until it is complete).</summary>
    public static int WholeUses(double charge) => charge <= 0 ? 0 : (int)Math.Floor(charge + 1e-9);

    /// <summary>Whether one use can be had.</summary>
    public static bool CanUse(double charge) => WholeUses(charge) >= 1;

    /// <summary>The charge after one use, or the same charge when there is no whole use left.</summary>
    public static double AfterUse(double charge) => CanUse(charge) ? Math.Max(0, charge - 1) : charge;

    /// <summary>The charge after recharging for some hours at a number of hours per use; zero or less hours per use never charges.</summary>
    public static double Recharged(double charge, double max, double hours, double hoursPerUse)
    {
        if (hoursPerUse <= 0 || hours <= 0) return Math.Min(charge, max);
        return Math.Min(max, charge + hours / hoursPerUse);
    }
}
