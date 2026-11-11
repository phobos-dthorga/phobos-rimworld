using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// What a helicopter's cargo hook can lift, game-free (Framework 0.92.0, sling loads; docs/development/vehicle-sling-design.md). A hook has a
/// rated load; on a hot day the air is thinner and the rotor and engines give less, so the load it can lift falls (the FAA's density
/// altitude: air density falls with rising temperature). The derating's rate and cap are authored, from that direction only.
/// </summary>
public static class SlingRule
{
    /// <summary>
    /// The load the hook can lift now, in kilograms: the rated load, less <paramref name="lossPerDegree"/> of it for each degree above
    /// <paramref name="fromCelsius"/>, never more than <paramref name="maxLoss"/> less. With derating off, the rated load.
    /// </summary>
    public static double Capacity(double ratedKilograms, double celsius, bool derate, double fromCelsius, double lossPerDegree, double maxLoss)
    {
        if (ratedKilograms <= 0) return 0;
        if (!derate) return ratedKilograms;
        double loss = Math.Min(Math.Max(0, maxLoss), Math.Max(0, (celsius - fromCelsius) * lossPerDegree));
        return ratedKilograms * (1 - loss);
    }

    /// <summary>Whether a load of this weight can be lifted by a hook that takes this much now.</summary>
    public static bool CanLift(double loadKilograms, double capacityKilograms) => loadKilograms > 0 && loadKilograms <= capacityKilograms;

    /// <summary>The share of the hook's capacity a load uses, for the inspect line (above 1 is too heavy).</summary>
    public static double Share(double loadKilograms, double capacityKilograms) => capacityKilograms <= 0 ? double.PositiveInfinity : loadKilograms / capacityKilograms;
}
