using System;
using System.Collections.Generic;

namespace Phobos.Radio.Physics;

/// <summary>
/// A radio relay chain, game-free (Radio 0.22.0; the relay design of radio-autonomy, docs/development/radio-advice-and-relays.md, built
/// ahead by road-work-three): every hop is its own link, decided by the one link budget, and <b>a chain is as good as its worst hop</b>.
/// The best chain from a start to any target, through relays, in at most a set number of hops, is the one whose worst hop has the most
/// to spare. Nodes are numbers; the caller says which are targets, which may pass a call on, and what each hop has to spare (null where
/// there is no link). Small by design: a handful of nodes and three hops, so a plain search by hop count is enough.
/// </summary>
public static class RelayRule
{
    /// <summary>The best chain found: whether any reaches a target, its worst hop's margin, and the nodes from the start to the target.</summary>
    public readonly record struct Chain(bool Found, double WorstMarginDb, IReadOnlyList<int> Path)
    {
        /// <summary>How many relays the call passes through: the nodes between the start and the target.</summary>
        public int Relays => Found ? Math.Max(0, Path.Count - 2) : 0;
    }

    public static readonly Chain None = new(false, double.NegativeInfinity, Array.Empty<int>());

    /// <summary>
    /// The chain from <paramref name="start"/> to any target in at most <paramref name="maxHops"/> hops whose worst hop has the most to spare.
    /// A target is never passed through; a node that cannot relay is never passed through. Each hop is asked at most once.
    /// </summary>
    public static Chain Best(int nodes, int start, Func<int, bool> isTarget, Func<int, bool> canRelay, Func<int, int, double?> hop, int maxHops)
    {
        if (nodes <= 0 || start < 0 || start >= nodes || maxHops <= 0) return None;
        var asked = new Dictionary<(int, int), double?>();
        double? Hop(int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!asked.TryGetValue(key, out var margin)) asked[key] = margin = hop(a, b);
            return margin;
        }

        // One layer a hop count: worst[k][n] is the best worst-hop margin reaching n in exactly k hops, came[k][n] the node before it, so a
        // chain read back from layer k is always k hops, never more.
        var worst = new double[maxHops + 1][];
        var came = new int[maxHops + 1][];
        for (var k = 0; k <= maxHops; k++)
        {
            worst[k] = new double[nodes];
            came[k] = new int[nodes];
            for (var i = 0; i < nodes; i++)
            {
                worst[k][i] = double.NegativeInfinity;
                came[k][i] = -1;
            }
        }
        worst[0][start] = double.PositiveInfinity;
        for (var k = 1; k <= maxHops; k++)
        {
            for (var from = 0; from < nodes; from++)
            {
                if (double.IsNegativeInfinity(worst[k - 1][from])) continue;
                if (from != start && (isTarget(from) || !canRelay(from))) continue;
                for (var to = 0; to < nodes; to++)
                {
                    if (to == from || to == start) continue;
                    if (Hop(from, to) is not { } margin) continue;
                    var chain = Math.Min(worst[k - 1][from], margin);
                    if (chain <= worst[k][to]) continue;
                    worst[k][to] = chain;
                    came[k][to] = from;
                }
            }
        }
        int bestNode = -1, bestHops = -1;
        for (var k = 1; k <= maxHops; k++)
        {
            for (var i = 0; i < nodes; i++)
            {
                if (i == start || !isTarget(i) || double.IsNegativeInfinity(worst[k][i])) continue;
                // The most to spare wins; between equals, the fewer hops.
                if (bestNode < 0 || worst[k][i] > worst[bestHops][bestNode]) (bestNode, bestHops) = (i, k);
            }
        }
        if (bestNode < 0) return None;
        var path = new List<int>();
        for (int n = bestNode, k = bestHops; k >= 0 && n >= 0; n = came[k][n], k--) path.Insert(0, n);
        return new Chain(true, worst[bestHops][bestNode], path);
    }
}
