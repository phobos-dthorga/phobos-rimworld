using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How one wear figure worked out for a vehicle's system is shared among the parts that each keep their own (Framework 0.247.0 for engines,
/// per-engine realism step 4; generic since 0.248.0, round two, the owner's card of 23 Nov 2026: "any system figure can be marked per
/// part"). Engines wear by the work each does; wheel parts by their axle, the front axle doing more. Game-free.
/// </summary>
public static class PartWearRule
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

    /// <summary>
    /// Each wheel part's multiplier on the vehicle's wear figure, by its axle: the front axle's parts weigh <paramref name="frontWeight"/>,
    /// every other axle's 1, then all are scaled so their mean is 1, so the vehicle's one figure still stands for the average part. Braking
    /// shifts weight forward, so front brakes do more of the work (a gameplay figure: authored per system, not measured). Parts on one axle
    /// share alike. <paramref name="along"/> is each part's place along the vehicle, larger further forward, as <see cref="AxleRule.Group"/>.
    /// </summary>
    public static double[] AxleShares(IReadOnlyList<float> along, double frontWeight, float tolerance)
    {
        var shares = new double[along.Count];
        if (along.Count == 0) return shares;
        List<List<int>> axles = AxleRule.Group(along, tolerance);
        double front = axles.Count > 1 ? Math.Max(0, frontWeight) : 1;
        double sum = 0;
        for (int a = 0; a < axles.Count; a++)
        {
            foreach (int part in axles[a])
            {
                shares[part] = a == 0 ? front : 1;
                sum += shares[part];
            }
        }
        double mean = sum / along.Count;
        for (int i = 0; i < shares.Length; i++) shares[i] = mean <= 0 ? 1 : shares[i] / mean;
        return shares;
    }

    /// <summary>The part most worn, by index, or -1 with none listed: the one a restore sees to first and a worn stage breaks.</summary>
    public static int MostWorn(IReadOnlyList<double> figures)
    {
        int worst = -1;
        for (int i = 0; i < figures.Count; i++)
        {
            if (worst < 0 || figures[i] > figures[worst]) worst = i;
        }
        return worst;
    }

    /// <summary>The parts a restore does, by index: the most worn alone, or with <paramref name="axles"/> given, every part on its axle.</summary>
    public static List<int> RestoreSet(IReadOnlyList<double> figures, IReadOnlyList<float>? axlePlaces, float tolerance)
    {
        int worst = MostWorn(figures);
        if (worst < 0) return new List<int>();
        if (axlePlaces == null || axlePlaces.Count != figures.Count) return new List<int> { worst };
        foreach (List<int> axle in AxleRule.Group(axlePlaces, tolerance))
        {
            if (axle.Contains(worst)) return axle;
        }
        return new List<int> { worst };
    }
}
