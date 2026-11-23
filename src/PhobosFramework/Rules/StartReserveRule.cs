using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// A load that keeps enough in the starting battery to start the engine, game-free (Framework 0.276.3; the owner's card of 24 Nov 2026,
/// "House battery, plus a floor": camp lights with no house battery go out before the battery is too low to start the engine). The line
/// is never a figure of its own: it is read from the stages of the reading the starter depends on (a vehicle's starting power), where
/// one stage refuses a start and the stage just above it warns that there is barely enough. The load goes out on entering that warning
/// band, so the engine still starts, and comes back only once charging lifts the reading past the line again: a warming day alone does
/// not bring it back, so it does not flick on and off by the weather.
/// </summary>
public static class StartReserveRule
{
    /// <summary>Two stage edges closer than this are the same edge (the Defs' figures are single-precision).</summary>
    const double SameEdge = 1e-6;

    /// <summary>One stage of the reading: its range, NaN for an open end, and whether it refuses a start.</summary>
    public readonly struct Stage
    {
        public readonly double From;
        public readonly double To;
        public readonly bool RefusesStart;

        public Stage(double from, double to, bool refusesStart)
        {
            From = from;
            To = to;
            RefusesStart = refusesStart;
        }
    }

    /// <summary>
    /// The reading below which the load goes out: the top of the stage just above the highest stage that refuses a start (the "barely
    /// enough to start" band), or the top of the refusing stage itself where no band sits above it. NaN when no stage refuses a start
    /// or the refusing stage has no top: there is then no line, and the load is never held.
    /// </summary>
    public static double Line(IReadOnlyList<Stage> stages)
    {
        double noStart = double.NaN;
        for (int i = 0; i < stages.Count; i++)
        {
            if (!stages[i].RefusesStart || double.IsNaN(stages[i].To)) continue;
            if (double.IsNaN(noStart) || stages[i].To > noStart) noStart = stages[i].To;
        }
        if (double.IsNaN(noStart)) return double.NaN;
        for (int i = 0; i < stages.Count; i++)
        {
            Stage stage = stages[i];
            if (stage.RefusesStart || double.IsNaN(stage.From) || double.IsNaN(stage.To)) continue;
            if (Math.Abs(stage.From - noStart) < SameEdge && stage.To > noStart) return stage.To;
        }
        return noStart;
    }

    /// <summary>
    /// Whether the load is held off now: never with a house battery to carry it, nor without a line; below the line always; and once held,
    /// until the battery is charging (the engine or a grid putting charge in) with the reading back at or past the line.
    /// </summary>
    /// <param name="houseBattery">A house battery is fitted, so the load draws on it and never the starting battery.</param>
    /// <param name="reading">The reading the starter depends on now (the starting power).</param>
    /// <param name="line">From <see cref="Line"/>.</param>
    /// <param name="charging">The starting battery is gaining charge now.</param>
    /// <param name="heldBefore">The answer at the last look.</param>
    public static bool Held(bool houseBattery, double reading, double line, bool charging, bool heldBefore)
    {
        if (houseBattery || double.IsNaN(line)) return false;
        if (reading < line) return true;
        return heldBefore && !charging;
    }
}
