using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The one place power changes units (Framework 0.130.0; docs/development/vehicle-engine-loads.md, step S3): amps and volts to watts, watts to
/// kilowatts, power in for power out through a machine's efficiency, and the game's own watts to real ones. Game-free.
/// </summary>
public static class PowerUnits
{
    /// <summary>
    /// How many real watts one of the game's watts stands for: 1, the agent default every power figure of ours has used. The game's watts are
    /// not real watts (a real 100 kW generator head would burn far more than the game's generator), but the owner kept the generator set's
    /// fuel as it is (18 Nov 2026), so the conversion stays at 1 and is named here, where one change would move every figure that crosses
    /// between the game's power grid and a vehicle's electrics: the outlet, the grid hook-up and the generator set.
    /// </summary>
    public const double RealWattsPerGameWatt = 1;

    public static double Watts(double amps, double volts) => amps * volts;

    public static double Amps(double watts, double volts) => volts <= 0 ? 0 : watts / volts;

    public static double Kilowatts(double watts) => watts / 1000.0;

    /// <summary>The power a machine takes in to give <paramref name="output"/> at <paramref name="efficiency"/>, the efficiency held at or
    /// above <paramref name="floor"/> so a figure read off a curve's end never asks for an endless amount, and at or below 1.</summary>
    public static double Input(double output, double efficiency, double floor = 0.05) =>
        Math.Max(0, output) / Math.Min(1, Math.Max(floor, efficiency));

    public static double FromGameWatts(double gameWatts) => gameWatts * RealWattsPerGameWatt;

    public static double ToGameWatts(double watts) => watts / RealWattsPerGameWatt;
}
