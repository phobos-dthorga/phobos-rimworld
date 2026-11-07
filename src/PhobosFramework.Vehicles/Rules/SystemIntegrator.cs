using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Which way a flow moves its quantity.</summary>
public enum FlowKind
{
    /// <summary>Adds <see cref="FlowSample.Rate"/> per hour.</summary>
    Into,
    /// <summary>Removes <see cref="FlowSample.Rate"/> per hour.</summary>
    From,
    /// <summary>Closes the gap to <see cref="FlowSample.Target"/> at <see cref="FlowSample.Rate"/> per hour (Newton's law of cooling).</summary>
    Toward,
}

/// <summary>
/// One flow as the game side has already evaluated it this interval: the inputs multiplied out into a rate, the target read. The
/// integrator only needs numbers, so it runs (and is tested) without the game.
/// </summary>
public sealed class FlowSample
{
    public int Quantity;
    public FlowKind Kind;
    /// <summary>Per hour: units for Into and From, the fraction of the gap closed for Toward. Never negative.</summary>
    public double Rate;
    public double Target;
    /// <summary>The flow opens as its quantity rises past this (a thermostat). NaN: always open.</summary>
    public double OpenAbove = double.NaN;
    /// <summary>The flow opens as its quantity falls past this. NaN: always open.</summary>
    public double OpenBelow = double.NaN;
    /// <summary>How far past the threshold the flow is fully open; 0 opens it at once.</summary>
    public double Band;
}

/// <summary>
/// Advances a system's quantities through its flows. Game-free: compiled into the tests without the game. A "toward" flow uses the
/// exact exponential step, so a fast cooling rate can never overshoot its target; sources and sinks are linear within a step; the
/// interval is cut into steps no longer than <see cref="DefaultStepHours"/> so a thermostat opening part-way is followed.
/// </summary>
public static class SystemIntegrator
{
    /// <summary>The longest single step (agent default): 1.5 game minutes, so a thermostat band of a few degrees is resolved.</summary>
    public const double DefaultStepHours = 0.025;

    public static void Advance(double[] values, double[] min, double[] max, IReadOnlyList<FlowSample> flows, double hours,
        double stepHours = DefaultStepHours)
    {
        if (hours <= 0 || flows.Count == 0) return;
        if (stepHours <= 0) stepHours = DefaultStepHours;
        int steps = (int)Math.Ceiling(hours / stepHours);
        double dt = hours / steps;
        var change = new double[values.Length];
        for (int s = 0; s < steps; s++)
        {
            Array.Clear(change, 0, change.Length);
            foreach (FlowSample flow in flows)
            {
                if (flow.Quantity < 0 || flow.Quantity >= values.Length || flow.Rate <= 0) continue;
                double value = values[flow.Quantity];
                double open = Opening(value, flow);
                if (open <= 0) continue;
                switch (flow.Kind)
                {
                    case FlowKind.Into:
                        change[flow.Quantity] += flow.Rate * open * dt;
                        break;
                    case FlowKind.From:
                        change[flow.Quantity] -= flow.Rate * open * dt;
                        break;
                    case FlowKind.Toward:
                        change[flow.Quantity] += (flow.Target - value) * (1 - Math.Exp(-flow.Rate * open * dt));
                        break;
                }
            }
            for (int q = 0; q < values.Length; q++)
            {
                values[q] = Clamp(values[q] + change[q], min[q], max[q]);
            }
        }
    }

    /// <summary>How far open a gated flow is at this value, 0 to 1.</summary>
    public static double Opening(double value, FlowSample flow)
    {
        double open = 1;
        if (!double.IsNaN(flow.OpenAbove)) open = Math.Min(open, Ramp(value - flow.OpenAbove, flow.Band));
        if (!double.IsNaN(flow.OpenBelow)) open = Math.Min(open, Ramp(flow.OpenBelow - value, flow.Band));
        return open;
    }

    static double Ramp(double past, double band)
    {
        if (past <= 0) return 0;
        if (band <= 0) return 1;
        return past >= band ? 1 : past / band;
    }

    public static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
}
