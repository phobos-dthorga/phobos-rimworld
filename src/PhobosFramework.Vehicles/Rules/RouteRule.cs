using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The route level of working without a crew (Framework 0.281.0; docs/development/autonomous-vehicles.md, step 7a; the owner's choices of 18
/// and 25 Nov 2026): a follower keeps behind the vehicle ahead of it in its convoy, and a vehicle on a loop drives from point to point,
/// pausing at its two stops. Game-free and tested; every distance is in cells, the figures the rules Def's (authored).
/// </summary>
public static class RouteRule
{
    /// <summary>Where a follower aims: this far behind the vehicle ahead, against the way that vehicle faces (its facing as a step, x and z).</summary>
    public static (int x, int z) FollowTarget((int x, int z) ahead, (int x, int z) facing, int gap) =>
        (ahead.x - facing.x * gap, ahead.z - facing.z * gap);

    /// <summary>How far behind the vehicle ahead a follower keeps: half of each vehicle's length and a gap between them.</summary>
    public static int FollowGap(int aheadLength, int ownLength, int gapCells) =>
        (aheadLength + 1) / 2 + (ownLength + 1) / 2 + Math.Max(0, gapCells);

    /// <summary>
    /// Whether to give a new drive: none if already near the target; otherwise one when there is no drive in hand or the drive in hand ends
    /// too far from where the target has moved to, so a moving leader is not chased with a fresh order every look.
    /// </summary>
    public static bool NeedsOrder(double distanceToTarget, bool driving, double orderToTarget, double nearCells, double slackCells) =>
        distanceToTarget > nearCells && (!driving || orderToTarget > slackCells);

    /// <summary>Whether another follower may join a convoy that has this many already (the owner, 18 Nov 2026: at most four).</summary>
    public static bool MayJoin(int followers, int most) => MayJoin(followers, 1, most);

    /// <summary>
    /// Whether this many vehicles may join a convoy that has this many followers already (Framework 0.284.0): a vehicle joins with its own
    /// followers behind it, and all of them count against the limit.
    /// </summary>
    public static bool MayJoin(int followers, int joining, int most) => followers + Math.Max(1, joining) <= Math.Max(0, most);

    /// <summary>
    /// Whether a loop vehicle waiting at a stop charges its battery with its engine (Framework 0.286.0; the owner's card of 25 Nov 2026):
    /// it starts below <paramref name="startBelow"/> and keeps on until above <paramref name="stopAbove"/>, so it does not flick on and off
    /// at one line. A vehicle with no battery never does (<paramref name="charge"/> NaN).
    /// </summary>
    public static bool ChargesAtStop(bool charging, double charge, double startBelow, double stopAbove)
    {
        if (double.IsNaN(charge)) return false;
        return charging ? charge <= Math.Max(startBelow, stopAbove) : charge < startBelow;
    }

    /// <summary>
    /// Whether a loop vehicle leaves a stop (Framework 0.299.0; the owner's cards of 26 Nov 2026): once it has done its own work there, emptied
    /// at the drop-off or loaded anything at the pick-up, it goes at once; otherwise, or when its own work left the job half done (storage
    /// full, nothing in reach), it waits out the stop's time for colonists.
    /// </summary>
    public static bool LeavesStop(bool workedItself, bool dropOff, bool emptied, bool loadedAny, bool waitOver) =>
        waitOver || workedItself && (dropOff ? emptied : loadedAny);

    /// <summary>The point after this one on a loop, coming back to the first.</summary>
    public static int Next(int index, int count) => count <= 0 ? 0 : (index + 1) % count;

    /// <summary>Whether the point is reached: within this many cells.</summary>
    public static bool Reached(double distance, double arriveCells) => distance <= arriveCells;

    /// <summary>
    /// Where a waypoint goes in a loop: before the point whose leg it lengthens least, so a waypoint clicked beside the way out falls on
    /// the way out. Returns the index to insert at (1 to count; count closes the loop's last leg back to the first point).
    /// </summary>
    public static int InsertAt(IReadOnlyList<(int x, int z)> points, (int x, int z) waypoint)
    {
        if (points.Count < 2) return points.Count;
        var best = 1;
        var bestAdded = double.MaxValue;
        for (var i = 0; i < points.Count; i++)
        {
            var from = points[i];
            var to = points[(i + 1) % points.Count];
            var added = Distance(from, waypoint) + Distance(waypoint, to) - Distance(from, to);
            if (added >= bestAdded) continue;
            bestAdded = added;
            best = i + 1;
        }
        return best;
    }

    /// <summary>The straight distance between two cells.</summary>
    public static double Distance((int x, int z) a, (int x, int z) b)
    {
        double dx = a.x - b.x, dz = a.z - b.z;
        return Math.Sqrt(dx * dx + dz * dz);
    }
}
