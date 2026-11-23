using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How one wear figure worked out for a vehicle's turbine is shared among its engines (Framework 0.247.0, per-engine realism step 4; the
/// owner's card of 23 Nov 2026): each engine wears by the work it does, so an engine covering for a weak or stopped one wears faster and
/// the stopped one not at all. Game-free.
/// </summary>
public static class EngineWearRule
{
    /// <summary>
    /// Each engine's multiplier on the vehicle's wear figure: its output over the mean output of the engines still giving any. A pair of
    /// equal engines gives 1 each, so each wears as the one figure did; an engine out gives 0. With none giving any (a hot start on the
    /// ground), each gives 1. The figure is a share of the work, an authored simplification: a damaged engine may in truth run hotter.
    /// </summary>
    public static double[] Shares(IReadOnlyList<double> outputs)
    {
        var shares = new double[outputs.Count];
        double sum = 0;
        int running = 0;
        foreach (double output in outputs)
        {
            if (output <= 0) continue;
            sum += output;
            running++;
        }
        for (int i = 0; i < shares.Length; i++)
        {
            shares[i] = running == 0 ? 1 : Math.Max(0, outputs[i]) / (sum / running);
        }
        return shares;
    }

    /// <summary>The engine most worn, by index, or -1 with none listed: the one a hot-section overhaul does and a worn hot section breaks.</summary>
    public static int MostWorn(IReadOnlyList<double> lives)
    {
        int worst = -1;
        for (int i = 0; i < lives.Count; i++)
        {
            if (worst < 0 || lives[i] > lives[worst]) worst = i;
        }
        return worst;
    }
}
