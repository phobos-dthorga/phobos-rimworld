using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The prompt radiation dose of a burst by distance (Framework 0.358.0; docs/development/radiation-bursts.md; owner, 27 Nov 2026). The shape is
/// the real one for a point burst, the inverse square times air's attenuation (Glasstone and Dolan, The Effects of Nuclear Weapons, 1977,
/// chapter VIII): D(r) = D0 (r0 / r)^2 exp(-(r - r0) / airLength) beyond the edge r0, and D0 inside it. The scale (D0 and airLength for each
/// weapon) is authored and squeezed onto a map: at one metre a cell, a real weapon's dose would pass every map's edge. Game-free.
/// </summary>
public static class RadiationBurstRule
{
    /// <summary>Doses below this are not given (agent's figure): far under anything D'thorga Medical's model would show, and it bounds the work.</summary>
    public const float LeastGrays = 0.01f;

    /// <summary>The dose in the open, in grays, at <paramref name="distance"/> cells from the centre.</summary>
    public static float Grays(float distance, float edge, float graysAtEdge, float airLength)
    {
        if (graysAtEdge <= 0f) return 0f;
        if (edge < 1f) edge = 1f;
        if (distance <= edge) return graysAtEdge;
        double ratio = edge / distance;
        double air = airLength > 0f ? Math.Exp(-(distance - edge) / airLength) : 1.0;
        return (float)(graysAtEdge * ratio * ratio * air);
    }

    /// <summary>How far the dose stays at or above <see cref="LeastGrays"/>, in whole cells: no one further out is worth visiting.</summary>
    public static float Reach(float edge, float graysAtEdge, float airLength)
    {
        if (graysAtEdge < LeastGrays) return 0f;
        if (edge < 1f) edge = 1f;
        // The dose falls steadily beyond the edge, so halve the gap until it closes.
        float low = edge, high = edge * 2f;
        while (Grays(high, edge, graysAtEdge, airLength) >= LeastGrays && high < 100000f) high *= 2f;
        while (high - low > 0.5f)
        {
            float mid = (low + high) / 2f;
            if (Grays(mid, edge, graysAtEdge, airLength) >= LeastGrays) low = mid; else high = mid;
        }
        return (float)Math.Ceiling(low);
    }
}
