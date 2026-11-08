using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>One step of a turbine's rating ladder: a share of maximum-continuous output, a speed, a temperature limit, a time limit.</summary>
public sealed class TurbineRating
{
    public string Name = "";
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
        var state = new TurbineState { Demand = demand, Rating = spec.Continuous.Name };
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
