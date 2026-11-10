using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// What worn tread does to grip (T3 of docs/development/vehicle-crosslinks-round-two.md, Framework 0.73.0), game-free. Worn tyres hold
/// less on soft and wet ground long before they fail on a road (common knowledge; no figure claimed). The points are authored: full grip
/// new, 0.85 when the tread reads worn (60 %), 0.7 when worn through (90 %), 0.5 bald. The drivetrain multiplies the ground's grip by it,
/// so worn tyres bog and spin in mud sooner and matter little on a road, where grip is rarely what limits.
/// </summary>
public static class TreadRules
{
    static readonly (double wear, double grip)[] Points = { (0, 1), (0.6, 0.85), (0.9, 0.7), (1, 0.5) };

    public static double GripFactor(double wear)
    {
        wear = Math.Max(0, Math.Min(1, wear));
        for (int i = 1; i < Points.Length; i++)
        {
            (double x1, double y1) = Points[i];
            if (wear > x1) continue;
            (double x0, double y0) = Points[i - 1];
            return y0 + (y1 - y0) * (wear - x0) / (x1 - x0);
        }
        return Points[Points.Length - 1].grip;
    }
}
