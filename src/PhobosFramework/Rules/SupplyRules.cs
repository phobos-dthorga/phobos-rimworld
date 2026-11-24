using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// What several jobs that each need supplies still lack, game-free: their needs added up by kind, less what is in stock, in the order
/// each kind was first asked for. For a vehicle with two services due at once (an engine service and a hot-section overhaul), so the
/// haulers fetch everything and the vehicle says everything it waits for.
/// </summary>
public static class SupplyRules
{
    public static List<(T kind, int count)> Shortfall<T>(IEnumerable<IEnumerable<(T kind, int count)>> needs, Func<T, int> inStock) where T : notnull
    {
        var shortfall = new List<(T, int)>();
        foreach ((T kind, int needed) in Totals(needs))
        {
            int missing = needed - Math.Max(0, inStock(kind));
            if (missing > 0) shortfall.Add((kind, missing));
        }
        return shortfall;
    }

    /// <summary>Several jobs' needs added up by kind, in the order each kind was first asked for.</summary>
    public static List<(T kind, int count)> Totals<T>(IEnumerable<IEnumerable<(T kind, int count)>> needs) where T : notnull
    {
        var order = new List<T>();
        var total = new Dictionary<T, int>();
        foreach (IEnumerable<(T kind, int count)> need in needs)
        {
            foreach ((T kind, int count) in need)
            {
                if (count <= 0) continue;
                if (!total.ContainsKey(kind))
                {
                    total[kind] = 0;
                    order.Add(kind);
                }
                total[kind] += count;
            }
        }
        var totals = new List<(T, int)>(order.Count);
        foreach (T kind in order) totals.Add((kind, total[kind]));
        return totals;
    }

    /// <summary>
    /// How much of each held lot the waiting jobs cannot use, lot by lot in the order held (Framework 0.286.2; the owner's HEMTT of 25 Nov
    /// 2026 kept 975 steel laid out for five fittings that needed about a hundred): the earlier lots of a kind are kept up to what the jobs
    /// need of it, and the rest of that kind is surplus. One figure per lot, nothing for a lot that is all kept.
    /// </summary>
    public static List<int> Surplus<T>(IReadOnlyList<(T kind, int count)> held, IEnumerable<IEnumerable<(T kind, int count)>> needs) where T : notnull
    {
        var allowance = new Dictionary<T, int>();
        foreach ((T kind, int count) in Totals(needs)) allowance[kind] = count;
        var surplus = new List<int>(held.Count);
        foreach ((T kind, int count) in held)
        {
            int lot = Math.Max(0, count);
            int keep = Math.Min(lot, allowance.TryGetValue(kind, out int left) ? left : 0);
            if (keep > 0) allowance[kind] = left - keep;
            surplus.Add(lot - keep);
        }
        return surplus;
    }
}
