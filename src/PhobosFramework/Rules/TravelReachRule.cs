using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// Where a traveller can get on a budget, game-free (Framework 0.85.0, the range on the world map): the cheapest cost to every node within
/// the budget from a start, over any graph given as a neighbour list and a cost per step, and the edges where the reachable area ends. A
/// shortest-path search (Dijkstra) with its own small heap, because the game's runtime has no priority queue of .NET's.
/// </summary>
public static class TravelReachRule
{
    /// <summary>
    /// The cheapest cost from <paramref name="start"/> to every node reachable for at most <paramref name="budget"/>, the start at 0.
    /// <paramref name="cost"/> gives a step's cost, or a negative or infinite one for a step that cannot be taken. The search stops after
    /// <paramref name="maxNodes"/> nodes, so a huge budget cannot stall the game.
    /// </summary>
    public static Dictionary<int, double> Within(int start, double budget, Action<int, List<int>> neighbours, Func<int, int, double> cost, int maxNodes)
    {
        var best = new Dictionary<int, double> { [start] = 0 };
        var done = new HashSet<int>();
        var heap = new MinHeap();
        heap.Push(0, start);
        var around = new List<int>();
        while (heap.Count > 0 && done.Count < maxNodes)
        {
            (double spent, int node) = heap.Pop();
            if (!done.Add(node) || spent > best[node]) continue;
            around.Clear();
            neighbours(node, around);
            foreach (int next in around)
            {
                if (done.Contains(next)) continue;
                double step = cost(node, next);
                if (step < 0 || double.IsInfinity(step) || double.IsNaN(step)) continue;
                double total = spent + step;
                if (total > budget) continue;
                if (best.TryGetValue(next, out double known) && known <= total) continue;
                best[next] = total;
                heap.Push(total, next);
            }
        }
        return best;
    }

    /// <summary>Every pair of a reached node and a neighbour that was not reached: the edges along which the reachable area ends.</summary>
    public static List<(int inside, int outside)> Boundary(IReadOnlyDictionary<int, double> reached, Action<int, List<int>> neighbours)
    {
        var edges = new List<(int, int)>();
        var around = new List<int>();
        foreach (int node in reached.Keys)
        {
            around.Clear();
            neighbours(node, around);
            foreach (int next in around)
            {
                if (!reached.ContainsKey(next)) edges.Add((node, next));
            }
        }
        return edges;
    }

    /// <summary>A binary heap of (cost, node), cheapest first.</summary>
    sealed class MinHeap
    {
        readonly List<(double cost, int node)> items = new();

        public int Count => items.Count;

        public void Push(double cost, int node)
        {
            items.Add((cost, node));
            int i = items.Count - 1;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (items[parent].cost <= items[i].cost) break;
                (items[parent], items[i]) = (items[i], items[parent]);
                i = parent;
            }
        }

        public (double cost, int node) Pop()
        {
            (double cost, int node) top = items[0];
            int last = items.Count - 1;
            items[0] = items[last];
            items.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int left = i * 2 + 1, right = left + 1, smallest = i;
                if (left < items.Count && items[left].cost < items[smallest].cost) smallest = left;
                if (right < items.Count && items[right].cost < items[smallest].cost) smallest = right;
                if (smallest == i) break;
                (items[smallest], items[i]) = (items[i], items[smallest]);
                i = smallest;
            }
            return top;
        }
    }
}
