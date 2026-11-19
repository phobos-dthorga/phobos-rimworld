using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Where each of several aircraft sent to one spot goes (Framework 0.174.0; the audit of 19 Nov 2026, finding 2, after VTOL Vehicle's
/// formation targets): the first to the spot itself, the rest in rings round it, six to the first ring, twelve to the second and so on,
/// each ring one spacing further out. Game-free. The spacing is the caller's (the aircraft's size and a margin, authored).
/// </summary>
public static class FormationRule
{
    /// <summary>The offset of the <paramref name="index"/>th aircraft (0 the first) from the spot, in cells, x east and z north.</summary>
    public static (double X, double Z) Offset(int index, double spacing)
    {
        if (index <= 0 || spacing <= 0) return (0, 0);
        int ring = 1, before = 0;
        while (index > before + 6 * ring)
        {
            before += 6 * ring;
            ring++;
        }
        int inRing = index - before - 1;
        double angle = 2 * Math.PI * inRing / (6 * ring);
        double radius = spacing * ring;
        return (Math.Sin(angle) * radius, Math.Cos(angle) * radius);
    }
}
