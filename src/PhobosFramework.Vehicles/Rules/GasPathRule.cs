using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The temperature of the gas along a turbine's path, and its colour on the systems schematic's cutaway (Framework 0.378.0;
/// docs/development/vehicle-schematic.md, SC8, the heat gradient). Game-free. Our engine model keeps one figure, the turbine temperature; the
/// rest of the path is shaped from it, AUTHORED: the compressor warms the air by up to 35 % of the way from the outside air to the turbine
/// figure, the burners run at the turbine figure, and the turbine stages take it down to 60 % of the way at the exhaust. The shape is real in
/// its direction (compressors heat air, burners are the hottest, turbines extract heat), its figures are not any engine's.
/// </summary>
public static class GasPathRule
{
    /// <summary>Where along the path (0 the intake, 1 the exhaust) the compressor ends and the turbine begins.</summary>
    public const double BurnersFrom = 0.4, BurnersTo = 0.6;
    /// <summary>How far toward the turbine figure the compressor's last stage is, and the exhaust.</summary>
    public const double CompressorRise = 0.35, ExhaustShare = 0.6;

    /// <summary>The gas temperature at <paramref name="position"/> along the path, from the outside air and the turbine figure.</summary>
    public static double Temperature(double ambient, double turbine, double position)
    {
        double p = Math.Max(0, Math.Min(1, position));
        double span = Math.Max(0, turbine - ambient);
        if (p < BurnersFrom) return ambient + span * CompressorRise * (p / BurnersFrom);
        if (p <= BurnersTo) return Math.Max(ambient, turbine);
        return turbine - span * (1 - ExhaustShare) * ((p - BurnersTo) / (1 - BurnersTo));
    }

    /// <summary>
    /// The colour for a gas temperature, red, green and blue from 0 to 1 (AUTHORED): cold blue to 0 °C, a pale steel blue at 200 °C, amber at
    /// 450 °C, orange 50 °C under the engine's top limit, red at the limit and over it.
    /// </summary>
    public static (double r, double g, double b) Colour(double celsius, double limit)
    {
        double top = limit > 500 ? limit : 800;
        (double t, double r, double g, double b)[] stops =
        {
            (0, 0.30, 0.52, 0.95),
            (200, 0.62, 0.78, 0.88),
            (450, 0.95, 0.80, 0.38),
            (top - 50, 0.98, 0.52, 0.18),
            (top, 0.95, 0.24, 0.18),
        };
        if (celsius <= stops[0].t) return (stops[0].r, stops[0].g, stops[0].b);
        for (int i = 1; i < stops.Length; i++)
        {
            if (celsius > stops[i].t) continue;
            double f = (celsius - stops[i - 1].t) / Math.Max(1e-6, stops[i].t - stops[i - 1].t);
            return (Lerp(stops[i - 1].r, stops[i].r, f), Lerp(stops[i - 1].g, stops[i].g, f), Lerp(stops[i - 1].b, stops[i].b, f));
        }
        (double _, double rr, double gg, double bb) = stops[stops.Length - 1];
        return (rr, gg, bb);
    }

    static double Lerp(double a, double b, double f) => a + (b - a) * f;
}
