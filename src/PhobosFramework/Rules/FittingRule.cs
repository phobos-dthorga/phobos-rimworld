using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a fitting on one part of several is worth, and who may do the work, game-free (vehicle tuning and upgrades; agent rule). Each of
/// a role's parts gives its root of the listed factor, so fitting all of them gives exactly that factor whatever the number of parts.
/// </summary>
public static class FittingRule
{
    /// <summary>The factor one fitted part gives, among this many parts of its role.</summary>
    public static double PartShare(double factor, int partsOfRole)
    {
        if (factor <= 0) return 0;
        return partsOfRole <= 1 ? factor : Math.Pow(factor, 1.0 / partsOfRole);
    }

    /// <summary>A mechanic at or above the job's minimum skill always succeeds; one below it may not try (owner: no random failure).</summary>
    public static bool SkilledEnough(int skill, int minimum) => skill >= minimum;

    /// <summary>What a grade adds to a part's maximum health (negative for a poorer part): health is additive, so this is the difference.</summary>
    public static double GradeHealthModifier(double baseHealth, double healthFactor) => baseHealth * (healthFactor - 1);

    /// <summary>How severe a fault a hit gives a part of this grade.</summary>
    public static double FaultSeverity(double severity, double faultFactor) => severity * faultFactor;

    /// <summary>The fuel a tuned engine burns per hour beyond what Vehicle Framework already burned at this share of its rate.</summary>
    public static double TunedFuelShare(double modelShare, double factor) => modelShare * factor;

    /// <summary>
    /// What extra tanks add to a vehicle's fuel capacity (the long range family, Framework 0.103.0): the capacity it has without them, times
    /// the tanks' factor less one. Capacity is additive in Vehicle Framework, so this is the difference, as a grade's health is. Nothing
    /// for a vehicle with no tank or a factor of one or less.
    /// </summary>
    public static double FuelCapacityOffset(double capacityWithout, double factor) =>
        capacityWithout <= 0 || factor <= 1 ? 0 : capacityWithout * (factor - 1);

    /// <summary>The fuel left above a tank's capacity once a tank comes off (never below nothing).</summary>
    public static double FuelAbove(double fuel, double capacity) => Math.Max(0, fuel - Math.Max(0, capacity));

    /// <summary>Of the fuel above capacity, the whole units a mechanic drains into containers; the fraction left over is lost in the
    /// draining (a simplification: the game counts fuel in whole items).</summary>
    public static int WholeUnits(double amount) => amount <= 0 ? 0 : (int)Math.Floor(amount + 1e-6);

    /// <summary>
    /// Wear left after one more part of a role is renewed (owner's test of 13 Nov 2026: new tyres left the tread as worn as before). A vehicle
    /// keeps one wear figure for all its wheels, the average; parts already renewed count as unworn, so the wear sits on the others. Renewing
    /// one of the <paramref name="notYetRenewed"/> parts takes away its share: 4 wheels in turn leave 3/4, 2/4, 1/4 and none of the wear.
    /// A simplification: it assumes the wear was even across the parts when the first was renewed.
    /// </summary>
    public static double WearAfterRenewing(double wear, int notYetRenewed) =>
        notYetRenewed <= 1 ? 0 : Math.Max(0, wear) * (notYetRenewed - 1) / notYetRenewed;
}
