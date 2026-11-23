using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>One step of a turbine's rating ladder: a share of maximum-continuous output, a speed, a temperature limit, a time limit.</summary>
public sealed class TurbineRating
{
    public string Name = "";
    /// <summary>What the player reads for the rating; presentation only.</summary>
    public string Label = "";
    public double Share = 1;
    public double SpeedPercent = 100;
    public double TemperatureLimit = 775;
    /// <summary>How long the rating may be held, in minutes; 0 for no limit (maximum continuous).</summary>
    public double Minutes;
    /// <summary>For thrust engines: lift or thrust at this rating at sea level and 15 °C, in kN (0 when unknown).</summary>
    public double ThrustKn;
    /// <summary>Whether the rating needs water injection.</summary>
    public bool Wet;
    /// <summary>Whether the engine's control allows the rating only with another engine out (Framework 0.250.0; per-engine realism round two,
    /// the owner's card of 23 Nov 2026): the one-engine-out contingency ratings of twin-engined helicopters. A GE T700's control arms it when
    /// the other engine's torque falls to 51 % or below (TM 1-1520-238-10).</summary>
    public bool EngineOutOnly;
}

/// <summary>A gas turbine's figures. Game-free; every default is authored unless the design names a source.</summary>
public sealed class TurbineSpec
{
    public double IdleSpeedPercent = 60;
    public double IdleTemperatureAboveAmbient = 363;
    public double DeratePerDegree = 0.007;
    public double StartSpeedPercent = 15;
    public List<TurbineRating> Ratings = new() { new TurbineRating { Name = "maxContinuous" } };
    /// <summary>
    /// What the rotor drive (the main transmission and rotor masts) accepts, as a share of all engines' combined maximum-continuous
    /// output, already scaled by the drive's condition; infinite when no drive limits it. Helicopters' transmissions are often rated
    /// below their engines' total, so the gearbox, not the engines, sets the limit.
    /// </summary>
    public double DriveLimitShare = double.PositiveInfinity;
    /// <summary>The engine's output against what its ratings' shares assume, 1 as designed: a re-engining gives more for the same demand, so
    /// it runs cooler and has more in reserve (aircraft service-life upgrades). The ratings' thrust is scaled by the same factor.</summary>
    public double OutputFactor = 1;

    /// <summary>Whether another engine is out now, so ratings kept for that are allowed (Framework 0.250.0); set by the caller before each
    /// evaluation, as <see cref="DriveLimitShare"/> is.</summary>
    public bool EngineOut;

    public TurbineRating Continuous => Ratings[0];

    /// <summary>The highest rating allowed now: the last one, unless it is kept for an engine out and none is.</summary>
    public TurbineRating Top
    {
        get
        {
            for (int i = Ratings.Count - 1; i > 0; i--)
            {
                if (Usable(Ratings[i])) return Ratings[i];
            }
            return Ratings[0];
        }
    }

    /// <summary>Whether a rating may be used now.</summary>
    public bool Usable(TurbineRating rating) => !rating.EngineOutOnly || EngineOut;
}

/// <summary>What the turbine is doing now.</summary>
public struct TurbineState
{
    public double Demand;
    public double Available;
    public double SpeedPercent;
    public double Temperature;
    /// <summary>Degrees above the maximum-continuous limit (negative when inside it).</summary>
    public double OverContinuous;
    /// <summary>Degrees above the top rating's limit.</summary>
    public double OverTop;
    /// <summary>How far demand exceeds what the rotor drive accepts (demand ÷ limit − 1; negative inside it, −1 with no limit).</summary>
    public double OverTorque;
    public string Rating;
}

/// <summary>
/// Gas turbines worked out from demand, not simulated: available output after the ambient derate, hot-section condition and the
/// engines still working; spool speed from JSBSim's thrust-against-core-speed square law; turbine temperature on a line from idle to
/// each rating's limit; hover margins for jets (thrust against weight) and helicopters (momentum theory). Design:
/// docs/development/vehicle-turbine-design.md. Game-free.
/// </summary>
public static class TurbineModel
{
    public const double SeaLevelDensity = 1.225;

    /// <summary>The share of rated output left at an ambient temperature: 0.7% less per °C above 15 °C (authored, inside the 0.5 to 0.9%
    /// published for industrial machines). Since Framework 0.132.0 thin air takes its share too: <paramref name="pressureRatio"/> is the
    /// air's pressure against sea level (AirRule.PressureRatio), 1 at the sea, and output falls in step with it.</summary>
    public static double Derate(TurbineSpec spec, double ambient, double pressureRatio = 1) =>
        Math.Max(0.2, 1 - spec.DeratePerDegree * Math.Max(0, ambient - 15)) * Math.Min(1, Math.Max(0.05, pressureRatio));

    /// <summary>Air density at an ambient temperature, sea-level pressure (ideal gas).</summary>
    public static double Density(double ambient) => SeaLevelDensity * 288.15 / (273.15 + ambient);

    /// <summary>An engine below this share of its working order is out (Framework 0.106.0's rule, kept).</summary>
    public const double EngineOutBelow = 0.2;

    /// <summary>What a damaged engine still gives just above <see cref="EngineOutBelow"/>, as a share of a sound one's output (AUTHORED).</summary>
    public const double DamagedEngineFloor = 0.5;

    /// <summary>
    /// The share of its rated output one engine gives at a working order (Framework 0.247.0, per-engine realism; owner's card, 23 Nov 2026).
    /// Out below <see cref="EngineOutBelow"/>; above it, from <see cref="DamagedEngineFloor"/> rising in a line to all of it when sound. A shot-up
    /// turbine's torn blades and seals lose it compression, so it gives less before it stops (the shape and figures are AUTHORED).
    /// </summary>
    public static double EngineOutput(double workingOrder)
    {
        if (workingOrder <= EngineOutBelow) return 0;
        double t = Math.Min(1, (workingOrder - EngineOutBelow) / (1 - EngineOutBelow));
        return DamagedEngineFloor + (1 - DamagedEngineFloor) * t;
    }

    /// <summary>
    /// All the engines' output as a share of every engine sound: each by its working order when <paramref name="byHealth"/>, else each in or
    /// out as before Framework 0.247.0. 1 with no engines listed, so a vehicle without engine roles is unchanged.
    /// </summary>
    public static double OutputShare(IReadOnlyList<double> workingOrders, bool byHealth)
    {
        if (workingOrders.Count == 0) return 1;
        double sum = 0;
        foreach (double order in workingOrders) sum += byHealth ? EngineOutput(order) : order > EngineOutBelow ? 1 : 0;
        return sum / workingOrders.Count;
    }

    /// <summary>Works the turbine through at a demand (a share of maximum continuous).</summary>
    /// <param name="pressureRatio">The air's pressure against sea level (1 at the sea): thin air leaves less to give.</param>
    public static TurbineState Evaluate(TurbineSpec spec, double demand, double ambient, double condition, int working, int engines, bool running,
        double pressureRatio = 1) =>
        Evaluate(spec, demand, ambient, condition, (double)Math.Max(0, working) / Math.Max(1, engines), running, pressureRatio);

    /// <summary>Works the turbine through at a demand with the engines giving <paramref name="share"/> of their full output between them
    /// (Framework 0.247.0: a damaged engine gives part of its share).</summary>
    public static TurbineState Evaluate(TurbineSpec spec, double demand, double ambient, double condition, double share, bool running,
        double pressureRatio = 1)
    {
        var state = new TurbineState { Demand = demand, Rating = spec.Continuous.Name, OverTorque = running ? OverTorque(spec, demand) : -1 };
        if (!running || share <= 0)
        {
            state.Temperature = ambient;
            state.OverContinuous = ambient - spec.Continuous.TemperatureLimit;
            state.OverTop = ambient - spec.Top.TemperatureLimit;
            return state;
        }
        state.Available = Derate(spec, ambient, pressureRatio) * Math.Max(0.05, condition) * share * Math.Max(0.01, spec.OutputFactor);
        double ratio = demand / Math.Max(0.01, state.Available);
        state.SpeedPercent = spec.IdleSpeedPercent + (100 - spec.IdleSpeedPercent) * Math.Sqrt(Math.Min(1.2, Math.Max(0, ratio)));
        double idleTemperature = ambient + spec.IdleTemperatureAboveAmbient;
        if (ratio <= 1)
        {
            state.Temperature = idleTemperature + (spec.Continuous.TemperatureLimit - idleTemperature) * Math.Max(0, ratio);
        }
        else
        {
            TurbineRating below = spec.Continuous;
            state.Temperature = spec.Top.TemperatureLimit + (ratio - spec.Top.Share) * 400;
            foreach (TurbineRating rating in spec.Ratings)
            {
                if (rating == spec.Continuous || !spec.Usable(rating)) continue;
                if (ratio <= rating.Share)
                {
                    double t = (ratio - below.Share) / Math.Max(0.001, rating.Share - below.Share);
                    state.Temperature = below.TemperatureLimit + (rating.TemperatureLimit - below.TemperatureLimit) * t;
                    state.Rating = rating.Name;
                    break;
                }
                below = rating;
                state.Rating = rating.Name;
            }
            state.SpeedPercent = Math.Min(spec.Top.SpeedPercent, state.SpeedPercent);
        }
        state.OverContinuous = state.Temperature - spec.Continuous.TemperatureLimit;
        state.OverTop = state.Temperature - spec.Top.TemperatureLimit;
        return state;
    }

    /// <summary>Demand against the rotor drive's limit: demand ÷ limit − 1 (−1 with no limit, so it never reads as over).</summary>
    public static double OverTorque(TurbineSpec spec, double demand) =>
        double.IsInfinity(spec.DriveLimitShare) ? -1 : demand / Math.Max(0.01, spec.DriveLimitShare) - 1;

    /// <summary>What can hold a helicopter up: the engines' output after the derate, condition and engines out, capped by the rotor drive.</summary>
    public static double LiftPowerAvailable(TurbineSpec spec, double ambient, double condition, double engineShare, double pressureRatio = 1) =>
        Math.Min(Derate(spec, ambient, pressureRatio) * Math.Max(0, condition) * engineShare * spec.Top.Share * Math.Max(0.01, spec.OutputFactor),
            spec.DriveLimitShare);

    /// <summary>
    /// The thrust each rating must give for a thrust-lifted craft built to lift <paramref name="liftToWeight"/> times its maximum gross
    /// weight on every thruster (authored per craft), kN in total.
    /// </summary>
    public static double ThrustForLiftToWeight(double liftToWeight, double maxGrossKg) => liftToWeight * maxGrossKg * DrivetrainModel.Gravity / 1000;

    /// <summary>The share of maximum-continuous fuel burned at a demand: the idle share plus the rest in proportion (a Willans shape).
    /// Above maximum continuous it goes on rising, as far as <paramref name="maxDemand"/> (the top rating's share).</summary>
    public static double FuelFraction(double idleShare, double demand, double maxDemand = 1) =>
        idleShare + (1 - idleShare) * Math.Min(Math.Max(1, maxDemand), Math.Max(0, demand));

    /// <summary>What a worn hot section adds to the fuel burned for the same work (Framework 0.132.0): nothing new, and
    /// <paramref name="penalty"/> of the lost condition after that (condition 0.9 with a penalty of 0.3 burns 3 % more).</summary>
    public static double WearFuelFactor(double condition, double penalty) => 1 + Math.Max(0, penalty) * Math.Min(1, Math.Max(0, 1 - condition));

    /// <summary>
    /// The fuel an aircraft burns at a demand, as a share of what it burns at its reference demand in good order (Framework 0.132.0): the
    /// author's rate holds at the reference (a cruise, or flying about on a map), and a hover, a climb, an idle, the loads and a worn
    /// engine move it from there. 1 when the reference burns nothing.
    /// </summary>
    public static double FuelShare(double idleShare, double demand, double referenceDemand, double maxDemand, double wearFactor)
    {
        double reference = FuelFraction(idleShare, referenceDemand, maxDemand);
        return reference <= 0 ? 1 : FuelFraction(idleShare, demand, maxDemand) * Math.Max(1, wearFactor) / reference;
    }

    /// <summary>
    /// What our code must still burn (positive) or hand back (negative) so that an aircraft has burned what its engines owe since the last
    /// look, given what its tank is seen to have lost to everything else meanwhile (the game's own burn, its flight framework's). While
    /// the tank leaks the loss seen is not all engine, so nothing is ever handed back then.
    /// </summary>
    public static double FuelCorrection(double owed, double seenBurned, bool leaking)
    {
        double extra = Math.Max(0, owed) - Math.Max(0, seenBurned);
        return leaking && extra < 0 ? 0 : extra;
    }

    /// <summary>Thrust a rating gives at an ambient temperature and air pressure (a share of sea level's), kN.</summary>
    public static double ThrustKn(TurbineSpec spec, TurbineRating rating, double ambient, double condition, double pressureRatio = 1) =>
        rating.ThrustKn * Derate(spec, ambient, pressureRatio) * Math.Max(0, condition);

    /// <summary>
    /// A jet's vertical-landing check: the best rating the aircraft may use (wet only with water aboard) against its weight. Returns
    /// thrust ÷ weight for the best dry and the best wet rating; at or above 1 it can hover.
    /// </summary>
    public static (double dry, double wet) HoverMargin(TurbineSpec spec, double massKg, double ambient, double condition, double pressureRatio = 1)
    {
        double weightKn = massKg * DrivetrainModel.Gravity / 1000;
        double dry = 0, wet = 0;
        foreach (TurbineRating rating in spec.Ratings)
        {
            if (rating.ThrustKn <= 0 || !spec.Usable(rating)) continue;
            double margin = ThrustKn(spec, rating, ambient, condition, pressureRatio) / Math.Max(0.1, weightKn);
            if (rating.Wet) wet = Math.Max(wet, margin);
            else dry = Math.Max(dry, margin);
        }
        return (dry, Math.Max(dry, wet));
    }

    /// <summary>
    /// A helicopter's hover power as a share of all engines' maximum-continuous power: ideal momentum theory, power rising with weight
    /// to the 1.5 power and with the inverse square root of air density. <paramref name="atMaxGross"/> is the hover power needed at
    /// maximum gross weight in sea-level air at 15 °C (authored per aircraft). <paramref name="pressureRatio"/> is the air's pressure
    /// against sea level (Framework 0.132.0): thin air at height asks more, as hot air does.
    /// </summary>
    public static double HoverPowerNeeded(double atMaxGross, double weightShare, double ambient, double pressureRatio = 1) =>
        atMaxGross * Math.Pow(Math.Max(0, weightShare), 1.5) * Math.Sqrt(Density(15) / (Density(ambient) * Math.Min(1, Math.Max(0.05, pressureRatio))));

    /// <summary>
    /// A flight phase's demand that follows the hover figure (Framework 0.141.0; docs/development/vehicle-flight-over-the-map.md, "Flight phases
    /// for the engine model"): its authored demand holds at the heaviest weight in sea-level air at 15 °C, and from there it moves as the
    /// power a hover needs moves, with weight, heat and height. A rotor flying slowly or hovering in turn on station, or flying forward, needs
    /// a share of the hover's power that stays roughly in step with it (our inference, from momentum theory: the induced power behind both
    /// follows weight to the 1.5 and density to the -0.5).
    /// </summary>
    public static double FollowHover(double authoredDemand, double hoverNow, double hoverNominal) =>
        hoverNominal <= 0 ? authoredDemand : authoredDemand * Math.Max(0, hoverNow) / hoverNominal;

    /// <summary>Hot-section life used per hour above the continuous limit: doubling every 25 °C over it (authored; creep is strongly
    /// temperature-dependent, but no figure is cited yet). Zero inside the limit.</summary>
    public static double LifePerHour(double overContinuous) => overContinuous <= 0 ? 0 : 0.01 * Math.Pow(2, overContinuous / 25);
}

/// <summary>
/// Bleed air: air a gas turbine's compressor gives up for other work, so the engine works harder for the same thrust or power
/// (docs/development/vehicle-turbine-design.md). The Harrier's reaction control (its puffer jets) bleeds in a hover, more in gusty air:
/// NASA measured 4.5 to 9.2 lb/s on a YAV-8B in hover, above 6 lb/s 60% of the time, and found the Pegasus held its thrust for bleed up
/// to the mean by running hotter, losing thrust only past its temperature limit (Borchers and colleagues, NASA TM-104021, 1994). So
/// bleed raises the engine's demand here rather than cutting thrust; the turbine model then heats, wears and reaches its limits as
/// before. Game-free.
/// </summary>
public static class BleedRule
{
    /// <summary>The wind speed at which gusts add nothing (calm air), in the game's wind units (authored).</summary>
    public const double CalmWind = 0.5;
    /// <summary>The wind speed above calm at which gusts add their full share (authored).</summary>
    public const double FullGustSpan = 1.0;

    /// <summary>How far into gusty air the wind is, 0 (calm) to 1.</summary>
    public static double GustShare(double windSpeed)
    {
        double share = (windSpeed - CalmWind) / FullGustSpan;
        return share < 0 ? 0 : share > 1 ? 1 : share;
    }

    /// <summary>
    /// The extra share of the engine's work bled off: the reaction controls' share while hovering, raised by up to
    /// <paramref name="gustFactor"/> in gusty air (NASA's peaks were about half again the usual bleed), plus the cabin's share while it
    /// is heated or conditioned with bleed air.
    /// </summary>
    public static double Share(bool hovering, double reactionControl, double gustFactor, double windSpeed, bool cabinOn, double cabin)
    {
        double share = 0;
        if (hovering && reactionControl > 0) share += reactionControl * (1 + gustFactor * GustShare(windSpeed));
        if (cabinOn && cabin > 0) share += cabin;
        return share;
    }

    /// <summary>The engine's demand with the bleed: the same thrust or power asks the engine for more.</summary>
    public static double Demand(double demand, double share) => demand * (1 + (share > 0 ? share : 0));
}
