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
        var shortfall = new List<(T, int)>();
        foreach (T kind in order)
        {
            int missing = total[kind] - Math.Max(0, inStock(kind));
            if (missing > 0) shortfall.Add((kind, missing));
        }
        return shortfall;
    }
}
