using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The wider way round for a vehicle that tows (Framework 0.277.0; docs/development/systems/hitches-and-trailers.md, "The wider way round"),
/// game-free. A trailer cuts the inside of every corner, so a vehicle that tows pays more to drive near ground a trailer cannot stand on,
/// and swings out from walls, fences and rock as a driver with a trailer would. The cost falls with the distance from that ground, in whole
/// cells counted the way a vehicle drives (a diagonal step is one). The costs are authored, not measured: they only make a path dearer, never
/// close it.
/// </summary>
public static class TowRouteRule
{
    /// <summary>
    /// The extra cost of each cell of a map for a vehicle that tows: <paramref name="costs"/>[0] on a cell next to ground a trailer cannot
    /// stand on (<paramref name="blocked"/>, or off the map), [1] one further out, and so on; nought on that ground itself (the path finder
    /// already keeps off it) and beyond the last. Cells by row: index = x + z * width.
    /// </summary>
    public static short[] ClearanceCosts(bool[] blocked, int width, int height, IReadOnlyList<int> costs)
    {
        if (blocked.Length != width * height) throw new ArgumentException("The grid is not width by height.", nameof(blocked));
        var result = new short[blocked.Length];
        int reach = costs.Count;
        if (reach == 0) return result;
        int[] distance = Distances(blocked, width, height, reach + 1);
        for (int i = 0; i < result.Length; i++)
        {
            int d = distance[i];
            if (d >= 1 && d <= reach) result[i] = (short)Math.Clamp(costs[d - 1], 0, short.MaxValue);
        }
        return result;
    }

    /// <summary>
    /// How far each cell is from the nearest blocked cell or the map's edge, counting a diagonal step as one, up to <paramref name="cap"/>: two
    /// passes over the grid (a chamfer distance with equal weights, which is exact for this measure). Nought on a blocked cell; a cell on the
    /// map's edge is one from the ground beyond it.
    /// </summary>
    public static int[] Distances(bool[] blocked, int width, int height, int cap)
    {
        var d = new int[width * height];
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = x + z * width;
                if (blocked[i]) { d[i] = 0; continue; }
                int best = Math.Min(cap, EdgeDistance(x, z, width, height));
                if (x > 0) best = Math.Min(best, d[i - 1] + 1);
                if (z > 0)
                {
                    best = Math.Min(best, d[i - width] + 1);
                    if (x > 0) best = Math.Min(best, d[i - width - 1] + 1);
                    if (x < width - 1) best = Math.Min(best, d[i - width + 1] + 1);
                }
                d[i] = best;
            }
        }
        for (int z = height - 1; z >= 0; z--)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                int i = x + z * width;
                if (d[i] == 0) continue;
                int best = d[i];
                if (x < width - 1) best = Math.Min(best, d[i + 1] + 1);
                if (z < height - 1)
                {
                    best = Math.Min(best, d[i + width] + 1);
                    if (x < width - 1) best = Math.Min(best, d[i + width + 1] + 1);
                    if (x > 0) best = Math.Min(best, d[i + width - 1] + 1);
                }
                d[i] = best;
            }
        }
        return d;
    }

    static int EdgeDistance(int x, int z, int width, int height) => 1 + Math.Min(Math.Min(x, z), Math.Min(width - 1 - x, height - 1 - z));

    /// <summary>
    /// The hitch points of a vehicle driving from one point to the next, in <paramref name="steps"/> even steps, the last on the end point: the
    /// trailer is dragged a short way at a time, as it is tick by tick in play, rather than a whole cell at once.
    /// </summary>
    public static IEnumerable<(double X, double Z)> Between(double fromX, double fromZ, double toX, double toZ, int steps)
    {
        int n = Math.Max(1, steps);
        for (int i = 1; i <= n; i++)
        {
            double t = (double)i / n;
            yield return (fromX + (toX - fromX) * t, fromZ + (toZ - fromZ) * t);
        }
    }
}
