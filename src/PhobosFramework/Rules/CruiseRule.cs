using System;

namespace Phobos.Framework.Rules;

/// <summary>What holds an aircraft up between tiles, for what flying lower or higher than it was built for costs it.</summary>
public enum CruiseLift
{
    /// <summary>Rotors: a helicopter, or a tiltrotor flown as one. Thinner air costs power, so flying higher costs fuel.</summary>
    Rotor,
    /// <summary>A wing and a jet: thinner air is faster and cheaper, so flying lower is slower and costs fuel.</summary>
    Jet,
    /// <summary>A wing and a propeller (a turboprop, a tiltrotor cruising on its wing): as a jet, gaining less from height.</summary>
    Propeller,
}

/// <summary>Why a leg is flown at the height it is.</summary>
public enum CruiseLimit
{
    /// <summary>At the height the aircraft was built to cruise at: its author's speed and range hold.</summary>
    Design,
    /// <summary>Brought down to what the engines can climb to at this weight, with the engines left.</summary>
    Ceiling,
    /// <summary>Brought down to the oxygen line: someone aboard has no oxygen in a cabin that is not pressurised.</summary>
    Oxygen,
    /// <summary>Raised to clear the highest ground on the leg.</summary>
    Ground,
    /// <summary>The highest ground on the leg, with its clearance, is above what the engines can climb to.</summary>
    Refused,
}

/// <summary>One leg's height and what it does to the flight; never saved.</summary>
public readonly record struct CruiseLeg(
    double HeightMetres,
    CruiseLimit Limit,
    double DesignMetres,
    double CeilingMetres,
    double FloorMetres,
    double FuelFactor,
    double SpeedFactor)
{
    /// <summary>The leg can be flown.</summary>
    public bool Flyable => Limit != CruiseLimit.Refused;

    /// <summary>How far the ground and its clearance stand above the ceiling, in metres; 0 for a leg that can be flown.</summary>
    public double ShortMetres => Flyable ? 0 : Math.Max(0, FloorMetres - CeilingMetres);

    /// <summary>The fuel the leg burns over the same ground, against the author's rate: fuel per tick over speed.</summary>
    public double FuelPerDistance => SpeedFactor <= 0 ? FuelFactor : FuelFactor / SpeedFactor;

    /// <summary>A leg flown exactly as its author intended, for an aircraft the rule does not judge.</summary>
    public static CruiseLeg AsAuthored(double designMetres) => new(designMetres, CruiseLimit.Design, designMetres, designMetres, 0, 1, 1);
}

/// <summary>
/// A cruising height between tiles (D'thorga Framework 0.141.0; docs/development/vehicle-flight-over-the-map.md, "A cruising height between
/// tiles"). For each leg: the highest ground on the way and its clearance, the ceiling the engines can reach now, the oxygen line; the height
/// flown is the aircraft's design height brought down to the ceiling and the oxygen line and never below the ground plus its clearance; a
/// leg whose ground stands above the ceiling is refused. The height flown then sets fuel and speed against the author's figures, which hold
/// exactly at the design height. Game-free, and in the core because anything that flies between tiles can use it.
/// </summary>
public static class CruiseRule
{
    /// <summary>Height kept above the highest ground on a leg, metres: 1,000 ft, after the United States' instrument-flight rule of "1,000
    /// feet above the highest obstacle" (14 CFR 91.177, primary). Authored: a game has no obstacles to measure.</summary>
    public const double ClearanceMetres = 300;

    /// <summary>The same over mountainous ground, metres: the same rule's "2,000 feet" in mountainous areas (14 CFR 91.177).</summary>
    public const double MountainClearanceMetres = 600;

    /// <summary>The height above which a crew without oxygen may not stay for long, metres: 12,500 ft, past which United States rules
    /// allow a crew 30 minutes without oxygen (14 CFR 91.211, primary). An aircraft's own figure replaces it (the Osprey's 10,000 ft).</summary>
    public const double DefaultOxygenLineMetres = 3810;

    /// <summary>How much less a propeller gains from height than a jet, as a power on the density ratio (a jet's is 0.5). Authored.</summary>
    public const double PropellerPower = 0.25;

    /// <summary>Highest a leg is ever flown, metres: the top of the troposphere, where the standard atmosphere's formula stops holding.
    /// A published ceiling above it is read as this.</summary>
    public const double HighestMetres = AirRule.TroposphereTopMetres;

    /// <summary>The clearance kept above ground of this kind.</summary>
    public static double Clearance(bool mountainous) => mountainous ? MountainClearanceMetres : ClearanceMetres;

    /// <summary>The height a leg must be flown at to clear its ground: the highest of its ordinary ground plus 300 m and its mountainous
    /// ground plus 600 m (each NaN or below the sea for none).</summary>
    public static double Floor(double highestMetres, double highestMountainousMetres)
    {
        double floor = Math.Max(0, Clean(highestMetres)) + ClearanceMetres;
        double mountains = Clean(highestMountainousMetres);
        return mountains > double.NegativeInfinity ? Math.Max(floor, Math.Max(0, mountains) + MountainClearanceMetres) : floor;
    }

    static double Clean(double metres) => double.IsNaN(metres) ? double.NegativeInfinity : metres;

    /// <summary>
    /// What the engines can give against what level flight needs at a height, in the engine model's own terms: a turbine's output follows
    /// the air's pressure (built, Framework 0.132.0) and the power level flight needs rises as density falls, as the hover rule's does. Falls
    /// steadily with height. [Our inference, labelled in the record.]
    /// </summary>
    public static double Climb(double heightMetres) => AirRule.PressureRatio(heightMetres) * Math.Sqrt(AirRule.StandardDensityRatio(heightMetres));

    /// <summary>
    /// The ceiling now, metres: where <see cref="Climb"/> falls to the figure set by the published ceiling (taken at the heaviest weight with
    /// every engine), scaled by the weight's share of the heaviest to the power 1.5 and by the share of engines working. 0 when it cannot
    /// climb at all; never above <see cref="HighestMetres"/>.
    /// </summary>
    public static double CeilingNow(double publishedCeilingMetres, double weightShare, double engineShare)
    {
        if (engineShare <= 0 || double.IsNaN(publishedCeilingMetres) || publishedCeilingMetres <= 0) return 0;
        double needed = Climb(Math.Min(publishedCeilingMetres, HighestMetres)) * Math.Pow(Math.Max(0, weightShare), 1.5) / Math.Min(1, engineShare);
        if (needed >= 1) return 0;
        if (Climb(HighestMetres) >= needed) return HighestMetres;
        // Climb falls steadily with height, so halving the bracket finds it; 40 halvings put it well inside a millimetre.
        double low = 0, high = HighestMetres;
        for (int i = 0; i < 40; i++)
        {
            double mid = (low + high) / 2;
            if (Climb(mid) >= needed) low = mid;
            else high = mid;
        }
        return low;
    }

    /// <summary>The heaviest weight, as a share of the heaviest the aircraft is built for, at which the engines it has left still climb to
    /// this height: what it must come down to to fly a refused leg.</summary>
    public static double WeightShareFor(double heightMetres, double publishedCeilingMetres, double engineShare)
    {
        if (engineShare <= 0 || double.IsNaN(publishedCeilingMetres) || publishedCeilingMetres <= 0) return 0;
        double ratio = Climb(Math.Min(Math.Max(0, heightMetres), HighestMetres)) * Math.Min(1, engineShare)
            / Climb(Math.Min(publishedCeilingMetres, HighestMetres));
        return Math.Pow(Math.Max(0, ratio), 2.0 / 3.0);
    }

    /// <summary>
    /// What flying at a height does to fuel (per tick, as the author's world-flight rate is) and speed, against the design height, where
    /// both are exactly 1. Rotors: fuel by the square root of the design height's density over the height flown's, speed unchanged (momentum
    /// theory, as the hover rule). A jet: fuel by the square root of the height flown's density over the design height's, speed by its
    /// inverse (true against equivalent airspeed; the fuel half unverified). A propeller: the same with the power 0.25 (authored).
    /// </summary>
    public static (double Fuel, double Speed) Factors(CruiseLift lift, double designMetres, double flownMetres)
    {
        double design = AirRule.StandardDensityRatio(designMetres);
        double flown = AirRule.StandardDensityRatio(flownMetres);
        if (design <= 0 || flown <= 0) return (1, 1);
        switch (lift)
        {
            case CruiseLift.Rotor:
                return (Math.Sqrt(design / flown), 1);
            case CruiseLift.Jet:
            {
                double thicker = Math.Sqrt(flown / design);
                return (thicker, 1 / thicker);
            }
            default:
            {
                double thicker = Math.Pow(flown / design, PropellerPower);
                return (thicker, 1 / thicker);
            }
        }
    }

    /// <summary>
    /// One leg. <paramref name="oxygenLineMetres"/> is NaN when nobody aboard needs it (oxygen for all, a pressurised cabin, nobody
    /// breathing). <paramref name="floorMetres"/> is the ground plus its clearance (<see cref="Floor"/>).
    /// </summary>
    public static CruiseLeg Leg(CruiseLift lift, double designMetres, double publishedCeilingMetres, double weightShare, double engineShare,
        double oxygenLineMetres, double floorMetres)
    {
        double ceiling = CeilingNow(publishedCeilingMetres, weightShare, engineShare);
        double height = Math.Min(Math.Max(0, designMetres), HighestMetres);
        CruiseLimit limit = CruiseLimit.Design;
        if (ceiling < height)
        {
            height = ceiling;
            limit = CruiseLimit.Ceiling;
        }
        if (!double.IsNaN(oxygenLineMetres) && oxygenLineMetres < height)
        {
            height = Math.Max(0, oxygenLineMetres);
            limit = CruiseLimit.Oxygen;
        }
        if (floorMetres > ceiling)
        {
            // It cannot climb over the ground: the leg is refused. The figures are those of struggling across at the floor, for a leg it
            // finds itself on anyway (an engine lost on the way).
            height = Math.Min(floorMetres, HighestMetres);
            limit = CruiseLimit.Refused;
        }
        else if (height < floorMetres)
        {
            height = floorMetres;
            limit = CruiseLimit.Ground;
        }
        (double fuel, double speed) = Factors(lift, designMetres, height);
        return new CruiseLeg(height, limit, designMetres, ceiling, floorMetres, fuel, speed);
    }
}
