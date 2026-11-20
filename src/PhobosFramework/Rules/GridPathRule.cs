using System;
using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// A way across a grid of cells by steps north, south, east and west, game-free (Framework 0.201.0; docs/development/vehicle-bay-trips.md):
/// for a thing moved a cell at a time by something other than its own engine, such as a broken vehicle shunted by a recovery vehicle or
/// pushed by hand. The caller says which cells it may stand on; the search is A* with every step costing the same, so the way found is a
/// shortest one, and it gives up after a set number of cells looked at, so a walled-in goal costs a bounded amount.
/// </summary>
public static class GridPathRule
{
    static readonly (int Dx, int Dz)[] Steps = { (0, 1), (1, 0), (0, -1), (-1, 0) };

    /// <summary>
    /// The cells to step through from the start to the goal, the start left out and the goal last; empty when the start is the goal;
    /// null when there is no way, or none was found within <paramref name="maxNodes"/> cells looked at. The start is never tested: the
    /// thing stands there already. Each other cell is tested once.
    /// </summary>
    public static List<(int X, int Z)>? Find(int startX, int startZ, int goalX, int goalZ, Func<int, int, bool> passable, int maxNodes)
    {
        var start = (startX, startZ);
        var goal = (goalX, goalZ);
        if (start == goal) return new List<(int X, int Z)>();
        var cost = new Dictionary<(int, int), int> { [start] = 0 };
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var blocked = new HashSet<(int, int)>();
        var done = new HashSet<(int, int)>();
        var open = new PriorityQueue<(int X, int Z), int>();
        open.Enqueue(start, Distance(start, goal));
        int looked = 0;
        while (open.TryDequeue(out (int X, int Z) current, out _))
        {
            if (current == goal) return Trace(cameFrom, start, goal);
            if (!done.Add(current)) continue;
            if (++looked > maxNodes) return null;
            int next = cost[current] + 1;
            foreach ((int dx, int dz) in Steps)
            {
                var cell = (current.X + dx, current.Z + dz);
                if (blocked.Contains(cell) || done.Contains(cell)) continue;
                if (!cost.TryGetValue(cell, out int known))
                {
                    if (!passable(cell.Item1, cell.Item2))
                    {
                        blocked.Add(cell);
                        continue;
                    }
                }
                else if (known <= next) continue;
                cost[cell] = next;
                cameFrom[cell] = current;
                open.Enqueue(cell, next + Distance(cell, goal));
            }
        }
        return null;
    }

    static int Distance((int X, int Z) a, (int X, int Z) b) => Math.Abs(a.X - b.X) + Math.Abs(a.Z - b.Z);

    static List<(int X, int Z)> Trace(Dictionary<(int, int), (int, int)> cameFrom, (int, int) start, (int, int) goal)
    {
        var path = new List<(int X, int Z)>();
        (int, int) cell = goal;
        while (cell != start)
        {
            path.Add(cell);
            cell = cameFrom[cell];
        }
        path.Reverse();
        return path;
    }
}
