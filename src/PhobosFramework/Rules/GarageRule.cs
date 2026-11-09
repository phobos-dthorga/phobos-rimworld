using System;

namespace Phobos.Framework.Rules;

/// <summary>What stopped a repair cycle from doing all the work there was.</summary>
public enum GarageRepairLimit
{
    None,
    Steel,
    Components,
}

/// <summary>What one repair cycle may do: the points it can spend, what they cost, and what held it back.</summary>
public readonly record struct GarageRepairBudget(float Points, int Steel, int Components, GarageRepairLimit Limit);

/// <summary>
/// The rules of a garage that holds and mends vehicles, game-free (docs/development/vehicle-garage-design.md). Every figure comes from
/// the caller, which reads it from a Def; all of them are authored balance, not measurements.
/// </summary>
public static class GarageRule
{
    /// <summary>How many vehicles a garage can hold: a number of bays for each person stationed there, up to its limit.</summary>
    public static int Bays(int staff, int baysPerPerson, int maxBays) =>
        Math.Clamp(Math.Max(staff, 0) * Math.Max(baysPerPerson, 0), 0, Math.Max(maxBays, 0));

    /// <summary>Whether the free people can fill every seat the vehicles need to move: one seat short and the whole convoy cannot leave.</summary>
    public static bool EnoughDrivers(int seatsToOperate, int freePeople) => freePeople >= seatsToOperate;

    /// <summary>The repair points one cycle has to spend, from the stationed people's total skill.</summary>
    public static float PointsPerCycle(int totalSkill, float pointsPerSkillLevel) =>
        Math.Max(totalSkill, 0) * Math.Max(pointsPerSkillLevel, 0f);

    /// <summary>
    /// What a cycle can spend on <paramref name="needed"/> points of damage, given its <paramref name="available"/> points and the stores.
    /// A cost per material of zero or less means that material is not used. Costs round up, so a small repair still uses something.
    /// </summary>
    public static GarageRepairBudget Spend(float needed, float available, int steelHeld, int componentsHeld, float pointsPerSteel,
        float pointsPerComponent)
    {
        float points = Math.Max(Math.Min(needed, available), 0f);
        GarageRepairLimit limit = GarageRepairLimit.None;
        if (pointsPerSteel > 0f)
        {
            float bySteel = Math.Max(steelHeld, 0) * pointsPerSteel;
            if (bySteel < points)
            {
                points = bySteel;
                limit = GarageRepairLimit.Steel;
            }
        }
        if (pointsPerComponent > 0f)
        {
            float byComponents = Math.Max(componentsHeld, 0) * pointsPerComponent;
            if (byComponents < points)
            {
                points = byComponents;
                limit = GarageRepairLimit.Components;
            }
        }
        return new GarageRepairBudget(points, Cost(points, pointsPerSteel), Cost(points, pointsPerComponent), limit);
    }

    static int Cost(float points, float pointsPerUnit) => pointsPerUnit > 0f ? (int)Math.Ceiling(points / pointsPerUnit) : 0;
}
