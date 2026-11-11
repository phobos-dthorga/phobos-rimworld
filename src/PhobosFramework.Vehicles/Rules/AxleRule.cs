using System;
using System.Collections.Generic;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Which wheels share an axle, game-free (Framework 0.89.0; owner, 11 Nov 2026: the fittings window groups the wheels into axles). Wheels
/// whose hit areas sit at the same distance along the vehicle, within a tolerance, are one axle; axles come front first. Tracks, which run
/// the vehicle's length side by side, come out as one group, which is right for a track pair too.
/// </summary>
public static class AxleRule
{
    /// <summary>
    /// The wheels' indexes grouped into axles, front first, given each wheel's place along the vehicle (larger is further forward).
    /// </summary>
    public static List<List<int>> Group(IReadOnlyList<float> along, float tolerance)
    {
        var order = new List<int>();
        for (int i = 0; i < along.Count; i++) order.Add(i);
        order.Sort((a, b) => along[b].CompareTo(along[a]));
        var axles = new List<List<int>>();
        foreach (int wheel in order)
        {
            List<int>? last = axles.Count > 0 ? axles[axles.Count - 1] : null;
            if (last != null && Math.Abs(along[last[0]] - along[wheel]) <= tolerance) last.Add(wheel);
            else axles.Add(new List<int> { wheel });
        }
        return axles;
    }
}
