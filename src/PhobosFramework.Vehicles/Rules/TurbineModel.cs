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

    public TurbineRating Continuous => Ratings[0];
    public TurbineRating Top => Ratings[Ratings.Count - 1];
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
    /// published for industrial machines).</summary>
    public static double Derate(TurbineSpec spec, double ambient) => Math.Max(0.2, 1 - spec.DeratePerDegree * Math.Max(0, ambient - 15));

    /// <summary>Air density at an ambient temperature, sea-level pressure (ideal gas).</summary>
    public static double Density(double ambient) => SeaLevelDensity * 288.15 / (273.15 + ambient);

    /// <summary>Works the turbine through at a demand (a share of maximum continuous).</summary>
    public static TurbineState Evaluate(TurbineSpec spec, double demand, double ambient, double condition, int working, int engines, bool running)
    {
        var state = new TurbineState { Demand = demand, Rating = spec.Continuous.Name, OverTorque = running ? OverTorque(spec, demand) : -1 };
        if (!running || working <= 0)
        {
            state.Temperature = ambient;
            state.OverContinuous = ambient - spec.Continuous.TemperatureLimit;
            state.OverTop = ambient - spec.Top.TemperatureLimit;
            return state;
        }
        double share = (double)working / Math.Max(1, engines);
        state.Available = Derate(spec, ambient) * Math.Max(0.05, condition) * share;
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
                if (rating == spec.Continuous) continue;
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
    public static double LiftPowerAvailable(TurbineSpec spec, double ambient, double condition, double engineShare) =>
        Math.Min(Derate(spec, ambient) * Math.Max(0, condition) * engineShare * spec.Top.Share, spec.DriveLimitShare);

    /// <summary>
    /// The thrust each rating must give for a thrust-lifted craft built to lift <paramref name="liftToWeight"/> times its maximum gross
    /// weight on every thruster (authored per craft), kN in total.
    /// </summary>
    public static double ThrustForLiftToWeight(double liftToWeight, double maxGrossKg) => liftToWeight * maxGrossKg * DrivetrainModel.Gravity / 1000;

    /// <summary>The share of full-power fuel burned at a demand: the idle share plus the rest in proportion (a Willans shape).</summary>
    public static double FuelFraction(double idleShare, double demand) => idleShare + (1 - idleShare) * Math.Min(1, Math.Max(0, demand));

    /// <summary>Thrust a rating gives at an ambient temperature, kN.</summary>
    public static double ThrustKn(TurbineSpec spec, TurbineRating rating, double ambient, double condition) =>
        rating.ThrustKn * Derate(spec, ambient) * Math.Max(0, condition);

    /// <summary>
    /// A jet's vertical-landing check: the best rating the aircraft may use (wet only with water aboard) against its weight. Returns
    /// thrust ÷ weight for the best dry and the best wet rating; at or above 1 it can hover.
    /// </summary>
    public static (double dry, double wet) HoverMargin(TurbineSpec spec, double massKg, double ambient, double condition)
    {
        double weightKn = massKg * DrivetrainModel.Gravity / 1000;
        double dry = 0, wet = 0;
        foreach (TurbineRating rating in spec.Ratings)
        {
            if (rating.ThrustKn <= 0) continue;
            double margin = ThrustKn(spec, rating, ambient, condition) / Math.Max(0.1, weightKn);
            if (rating.Wet) wet = Math.Max(wet, margin);
            else dry = Math.Max(dry, margin);
        }
        return (dry, Math.Max(dry, wet));
    }

    /// <summary>
    /// A helicopter's hover power as a share of all engines' maximum-continuous power: ideal momentum theory, power rising with weight
    /// to the 1.5 power and with the inverse square root of air density. <paramref name="atMaxGross"/> is the hover power needed at
    /// maximum gross weight in sea-level air at 15 °C (authored per aircraft).
    /// </summary>
    public static double HoverPowerNeeded(double atMaxGross, double weightShare, double ambient) =>
        atMaxGross * Math.Pow(Math.Max(0, weightShare), 1.5) * Math.Sqrt(Density(15) / Density(ambient));

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
