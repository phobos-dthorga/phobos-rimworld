using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a destroyed vehicle's hull yields, and what salvaged parts pay for, game-free (Framework 0.187.0; docs/development/vehicle-emergent-play.md,
/// EM2 and EM3). Vehicle Framework already gives back a quarter of a vehicle's materials where it is destroyed; taking the hull apart
/// gives another quarter of its worth as salvaged parts, so a wreck is worth a detour and never more than half of what it cost. A salvaged
/// part pays for a part fitted at the salvaged grade, at the same worth as the new materials it stands in for. Every figure is authored.
/// </summary>
public static class SalvageRule
{
    /// <summary>The share of a vehicle's worth its hull gives as salvaged parts (authored, beside Vehicle Framework's own quarter).</summary>
    public const double HullShare = 0.25;

    /// <summary>The share of that a burnt-out hull still gives (Framework 0.245.0; vehicle-fire.md, FR13; authored): a fire takes the rubber,
    /// wiring, seals, glass and plastics, and softens springs and castings, so much less is fit to use again.</summary>
    public const double BurntShare = 0.5;

    /// <summary>The share of a vehicle's worth its hull gives as salvaged parts, burnt out or not.</summary>
    public static double HullShareOf(bool burntOut) => burntOut ? HullShare * BurntShare : HullShare;

    /// <summary>One salvaged part's worth, silver; the item's market value in XML matches it (authored).</summary>
    public const double PartValue = 60;

    /// <summary>The salvaged parts a hull of this worth gives: at least one for anything worth anything.</summary>
    public static int HullParts(double vehicleValue, double share = HullShare, double partValue = PartValue) =>
        vehicleValue <= 0 || share <= 0 || partValue <= 0 ? 0 : Math.Max(1, (int)Math.Round(vehicleValue * share / partValue));

    /// <summary>The salvaged parts that pay for new materials of this worth: rounded up, at least one.</summary>
    public static int PartsToPay(double newValue, double partValue = PartValue) =>
        newValue <= 0 || partValue <= 0 ? 0 : Math.Max(1, (int)Math.Ceiling(newValue / partValue));
}
