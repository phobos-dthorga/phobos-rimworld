using System;

namespace Phobos.Framework.Rules;

/// <summary>What is being dropped by parachute: each kind has its own canopy, heights, descent and drift.</summary>
public enum ParachuteLoad
{
    /// <summary>A person on a static-line personnel parachute (the T-11).</summary>
    Person,
    /// <summary>A bundle of cargo on a cargo parachute (the Container Delivery System: an A-22 bag under a G-12).</summary>
    Bundle,
    /// <summary>A vehicle on a heavy-drop platform under a cluster of canopies (a Type V platform under G-11s).</summary>
    Vehicle,
}

/// <summary>
/// Parachute airdrops (D'thorga Framework 0.288.0; docs/development/airdrop.md): how far a load drifts, how long it falls, and how likely a
/// person is to be hurt landing. Heights in feet and wind in knots, as the sources give them; distances out in metres. Game-free.
/// Sources: the US Army pathfinder handbook (Oct 2022) for heights, wind limits and the drift formula; Knapik and colleagues (2016) and the
/// US Army for injury rates. Every figure not from them is marked authored.
/// </summary>
public static class ParachuteRule
{
    /// <summary>A foot in metres.</summary>
    public const double MetresPerFoot = 0.3048;

    /// <summary>A knot in metres a second.</summary>
    public const double MetresPerSecondPerKnot = 0.514444;

    /// <summary>One kind of load's figures.</summary>
    public readonly struct Figures
    {
        /// <summary>The height it is planned to drop from, feet above the ground.</summary>
        public readonly double PlannedFeet;
        /// <summary>The lowest it may be dropped from, feet.</summary>
        public readonly double MinimumFeet;
        /// <summary>Its rate of descent under a full canopy, feet a second.</summary>
        public readonly double DescentFeetPerSecond;
        /// <summary>The drift constant K of D = K x A x V.</summary>
        public readonly double DriftConstant;
        /// <summary>The highest surface wind it is dropped in, knots.</summary>
        public readonly double WindLimitKnots;

        public Figures(double plannedFeet, double minimumFeet, double descentFeetPerSecond, double driftConstant, double windLimitKnots)
        {
            PlannedFeet = plannedFeet;
            MinimumFeet = minimumFeet;
            DescentFeetPerSecond = descentFeetPerSecond;
            DriftConstant = driftConstant;
            WindLimitKnots = windLimitKnots;
        }
    }

    /// <summary>
    /// A person: planned at 1,000 ft, 800 ft at the least (the handbook's tactical-training minimum), the T-11's 19 ft/s at the heaviest
    /// jumper (secondary), K 3.0 and 13 kt (the handbook).
    /// </summary>
    public static readonly Figures Person = new(1000, 800, 19, 3.0, 13);

    /// <summary>
    /// A bundle: planned at 600 ft (the handbook), 400 ft at the least (authored: the handbook's nearest figure is the G-14's 300 to 400 ft),
    /// the G-12's 27.6 ft/s at 2,200 lb (an old Army test report), K 1.5 and 13 kt (the handbook).
    /// </summary>
    public static readonly Figures Bundle = new(600, 400, 27.6, 1.5, 13);

    /// <summary>
    /// A vehicle: planned at 1,100 ft (the handbook), 700 ft at the least (the G-11's lowest, Mills), 27.6 ft/s (authored: the G-11's rate
    /// was not found, so the G-12's is used), K 1.5 and 13 kt (the handbook).
    /// </summary>
    public static readonly Figures Vehicle = new(1100, 700, 27.6, 1.5, 13);

    /// <summary>The figures for one kind of load.</summary>
    public static Figures For(ParachuteLoad load) => load switch
    {
        ParachuteLoad.Person => Person,
        ParachuteLoad.Bundle => Bundle,
        _ => Vehicle,
    };

    /// <summary>The most cargo one bundle carries, kilograms (the A-22's 2,200 lb, rounded; authored).</summary>
    public const double BundleKilograms = 1000;

    // ---- Falling ----

    /// <summary>
    /// How far a load drifts on the wind, metres: D = K x A x V, with A the height in hundreds of feet and V the wind in knots (the handbook's
    /// computed-air-release-point formula). The forward throw from the aircraft is left out (a simplification).
    /// </summary>
    public static double DriftMetres(double driftConstant, double heightFeet, double windKnots) =>
        Math.Max(0, driftConstant) * Math.Max(0, heightFeet) / 100 * Math.Max(0, windKnots);

    /// <summary>How long a load takes to come down from a height under its canopy, real seconds.</summary>
    public static double DescentSeconds(double heightFeet, double descentFeetPerSecond) =>
        Math.Max(0, heightFeet) / Math.Max(0.1, descentFeetPerSecond);

    /// <summary>
    /// The share of real drift a map shows (authored, 0.2): our flight draws 1.5 m a cell, and a real drift of 240 m (a jump from 1,000 ft in
    /// 8 kt) would cross most of a map.
    /// </summary>
    public const double MapDriftShare = 0.2;

    /// <summary>The share of real falling time a map takes (authored, 0.5): RimWorld's time runs faster than real.</summary>
    public const double MapDescentTimeShare = 0.5;

    /// <summary>
    /// The random spread of where a load lands around its drifted point, cells (authored: a tenth of the drift and a cell and a half; no
    /// unguided miss figure that fits a map was found).
    /// </summary>
    public static double ScatterCells(double driftCells) => 0.1 * Math.Max(0, driftCells) + 1.5;

    /// <summary>The height a static-line canopy needs to open, feet (authored, about 150 ft): below it a load falls as from its height.</summary>
    public const double OpeningFeet = 150;

    /// <summary>Whether a canopy opens in time from this height.</summary>
    public static bool Opens(double heightFeet) => heightFeet >= OpeningFeet;

    // ---- People landing ----

    /// <summary>The T-11's injuries per jump: 5.3 per 1,000 (Knapik and colleagues, 2016).</summary>
    public const double BaseInjuryChance = 5.3 / 1000;

    /// <summary>A night jump's risk against a day jump's: 1.76 times (Knapik and colleagues).</summary>
    public const double NightFactor = 1.76;

    /// <summary>Extra equipment against none: 2.60 times (Knapik and colleagues).</summary>
    public const double HeavyKitFactor = 2.60;

    /// <summary>Gear and inventory over this many kilograms counts as extra equipment (authored).</summary>
    public const double HeavyKitKilograms = 25;

    /// <summary>A person's first jumps count as a novice's (authored: five, the basic airborne course's count).</summary>
    public const int NoviceJumps = 5;

    /// <summary>A novice's risk against a trained jumper's (authored: no sourced figure was found).</summary>
    public const double NoviceFactor = 2;

    /// <summary>Landing somewhere it was not meant to (a roof, water, the map's edge): twice the risk (authored).</summary>
    public const double OffTargetFactor = 2;

    /// <summary>
    /// The wind's share of the risk: 1 below 6 kt, 1.55 from 6 to 11 kt, 1.88 at 11 kt or more (Knapik and colleagues); past the wind limit it
    /// goes on rising by 0.15 a knot (authored).
    /// </summary>
    public static double WindFactor(double windKnots, double windLimitKnots)
    {
        if (windKnots < 6) return 1;
        if (windKnots < 11) return 1.55;
        double over = Math.Max(0, windKnots - windLimitKnots);
        return 1.88 + 0.15 * over;
    }

    /// <summary>
    /// The risk from being dropped too low: 1 at or above the minimum, rising to ten times at the height a canopy needs to open (authored), as
    /// it has less and less time to slow the load.
    /// </summary>
    public static double LowFactor(double heightFeet, double minimumFeet)
    {
        if (heightFeet >= minimumFeet) return 1;
        double span = Math.Max(1, minimumFeet - OpeningFeet);
        double below = Math.Min(1, (minimumFeet - heightFeet) / span);
        return 1 + 9 * below;
    }

    /// <summary>A person's chance of an injury landing, at most one.</summary>
    public static double InjuryChance(bool night, double windKnots, double windLimitKnots, bool heavyKit, bool novice, double heightFeet, double minimumFeet,
        bool offTarget)
    {
        double chance = BaseInjuryChance * WindFactor(windKnots, windLimitKnots) * LowFactor(heightFeet, minimumFeet);
        if (night) chance *= NightFactor;
        if (heavyKit) chance *= HeavyKitFactor;
        if (novice) chance *= NoviceFactor;
        if (offTarget) chance *= OffTargetFactor;
        return Math.Min(1, chance);
    }

    /// <summary>The share of injuries to the head; the rest are to an ankle, a foot or a leg (authored split; the Army names the head and the
    /// ankles as the most injured).</summary>
    public const double HeadInjuryShare = 0.2;

    /// <summary>The harm points of a landing injury (authored: a sprain or a crack, rarely worse).</summary>
    public const double InjuryHarm = 10;

    // ---- Cargo and vehicles landing ----

    /// <summary>The chance a bundle or a platform lands hard (authored: the one malfunction figure found was unreadable).</summary>
    public const double HardLandingChance = 0.02;

    /// <summary>The fall a hard landing does a vehicle's parts, as the sling's drop from this height, metres (authored).</summary>
    public const double HardLandingMetres = 10;

    /// <summary>The share of a bundle's contents' hit points a hard landing takes, at most (authored).</summary>
    public const double HardLandingItemShare = 0.5;

    /// <summary>Crew inside a dropped vehicle: each takes a person's landing risk this many times over (authored, after the Kentavr and
    /// Reaktavr drops of 1973 and 1976).</summary>
    public const double CrewInsideFactor = 3;
}
