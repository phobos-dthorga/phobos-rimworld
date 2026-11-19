using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The numbers of one plant's harvest, as the game works them out for a colonist cutting it by hand (Framework 0.182.0;
/// docs/development/farming-batch-one.md, "The harvest steps"; the game's JobDriver_PlantWork, read 20 Nov 2026): whether the harvest
/// fails, and how many units a plant that yields gives. A machine's driver stands in for the colonist, so a machine harvests by the same
/// rule as a hand. Game-free: the caller reads the stat and the plant, and supplies the rolls.
/// </summary>
public static class HarvestRule
{
    /// <summary>
    /// Whether this plant's harvest fails and yields nothing: only for a humanlike harvester, a crop that can fail and a plant without
    /// blight, when the roll (nought to one) is above the harvester's yield stat. A stat of one or more never fails.
    /// </summary>
    public static bool Fails(bool humanlike, bool cropCanFail, bool blighted, float yieldStat, float roll) =>
        humanlike && cropCanFail && !blighted && roll > yieldStat;

    /// <summary>
    /// How many units a plant gives: what the plant yields now, times the harvester's stat only when the stat is above one, the fraction
    /// left over settled by the roll (nought to one) as the game's random rounding does, so three and a half is four half the time.
    /// </summary>
    public static int Yield(int plantYieldNow, float yieldStat, float roundingRoll)
    {
        if (plantYieldNow <= 0) return 0;
        if (yieldStat <= 1f) return plantYieldNow;
        float exact = plantYieldNow * yieldStat;
        int whole = (int)Math.Floor(exact);
        return roundingRoll < exact - whole ? whole + 1 : whole;
    }

    /// <summary>
    /// The second product of a dual crop (barley's hay, oats' raw oats), by the arithmetic Vanilla Expanded Framework's own harvest patch
    /// uses for a hand (its public source, Source/VEF/Plants/Harmony/Plant_PlantCollected.cs, read 20 Nov 2026; the framework is CC
    /// BY-NC-ND, so this is the rule written afresh, none of its code). It has a roll of its own, apart from the main harvest's: nothing for a blighted plant or
    /// when the chance roll (nought to one) is not below the harvester's stat. Otherwise the authored amount times how far the plant has
    /// grown, cut down to a whole number, then times the stat only when the stat is above one, rounded by the second roll.
    /// </summary>
    public static int SecondProduct(bool blighted, int authoredAmount, float growth, float yieldStat, float chanceRoll, float roundingRoll)
    {
        if (blighted || authoredAmount <= 0 || !(chanceRoll < yieldStat)) return 0;
        int grown = (int)(authoredAmount * growth);
        if (grown <= 0) return 0;
        if (yieldStat <= 1f) return grown;
        float exact = grown * yieldStat;
        int whole = (int)Math.Floor(exact);
        return roundingRoll < exact - whole ? whole + 1 : whole;
    }

    /// <summary>
    /// The Plants experience a driver earns for one plant harvested by machine: the share of what cutting it by hand would have given
    /// (the hand's experience a tick times the plant's harvest work in ticks). Authored: a machine driver still learns, slower than a hand.
    /// </summary>
    public static float DriverExperience(float handExperiencePerTick, float harvestWorkTicks, float machineShare) =>
        Math.Max(0f, handExperiencePerTick) * Math.Max(0f, harvestWorkTicks) * Math.Clamp(machineShare, 0f, 1f);
}
