using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How fit the person at the wheel is to drive (cross-links round three, G1 with G2; Framework 0.113.0; owner, 13 Nov 2026: "gently", for
/// health, drugs and tiredness, at worst ×0.7 of speed and no crashes), game-free. The game's own capacities carry every cause: pain,
/// drugs, a concussion, hypothermia and heatstroke lower consciousness; a lost eye lowers sight; a broken hand lowers manipulation. Each
/// capacity reads through its own curve and the worst decides; a tired driver's fitness is multiplied by <see cref="TiredFactor"/>. The
/// vehicle's stages turn fitness into a speed cap (Systems_Cabin.xml). The curves are authored, a gameplay choice: a one-eyed driver
/// (sight about half) still drives near full pace, as monocular drivers do on real roads; consciousness counts in full, since a dulled
/// driver is the commonest real case (NHTSA and the AAA Foundation, cited in docs/development/vehicle-crosslinks.md).
/// </summary>
public static class DriverRule
{
    static readonly (double capacity, double fitness)[] ConsciousnessPoints = { (0, 0), (1, 1) };
    static readonly (double capacity, double fitness)[] SightPoints = { (0, 0.3), (0.2, 0.5), (0.5, 0.85), (1, 1) };
    static readonly (double capacity, double fitness)[] ManipulationPoints = { (0, 0.5), (0.5, 0.85), (1, 1) };

    /// <summary>What tiredness multiplies fitness by (G2, authored): the game's "tired" rest level and below.</summary>
    public const double TiredFactor = 0.9;

    /// <summary>The driver's fitness, 0 to 1: the worst of the three capacities through their curves (each above 1 counts as 1), times
    /// <see cref="TiredFactor"/> when tired.</summary>
    public static double Fitness(double consciousness, double sight, double manipulation, bool tired)
    {
        double worst = Math.Min(Read(ConsciousnessPoints, consciousness), Math.Min(Read(SightPoints, sight), Read(ManipulationPoints, manipulation)));
        return worst * (tired ? TiredFactor : 1);
    }

    static double Read((double x, double y)[] points, double value)
    {
        value = Math.Max(0, Math.Min(1, value));
        for (int i = 1; i < points.Length; i++)
        {
            (double x1, double y1) = points[i];
            if (value > x1) continue;
            (double x0, double y0) = points[i - 1];
            return y0 + (y1 - y0) * (value - x0) / (x1 - x0);
        }
        return points[points.Length - 1].y;
    }
}
