using System.Collections.Generic;

namespace Phobos.VehicleFrameworkPatches.Rules;

/// <summary>
/// Which of a vehicle's attached fires cannot be real. The game keeps one fire per thing and finds it with "the first": a second fire is never put out, never
/// seen by foam, and never leaves with the vehicle. A fire on another map than its vehicle was left behind when the vehicle flew or drove away. Game-free.
/// </summary>
public static class AttachedFireRule
{
    /// <summary>The positions, in the given order, of the fires to put out: every fire not on its vehicle's map, and every fire after the first that is.</summary>
    public static List<int> Surplus(IReadOnlyList<bool> fireOnVehiclesMap)
    {
        var surplus = new List<int>();
        bool kept = false;
        for (int i = 0; i < fireOnVehiclesMap.Count; i++)
        {
            if (fireOnVehiclesMap[i] && !kept)
            {
                kept = true;
                continue;
            }
            surplus.Add(i);
        }
        return surplus;
    }
}
