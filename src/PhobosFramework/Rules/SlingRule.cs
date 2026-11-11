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

    /// <summary>
    /// Where a slung load is drawn while its helicopter takes off or lands (Framework 0.99.0), in cells from the spot on the ground the
    /// helicopter left or is coming down to. A picture only: it changes nothing a lift does. The map shows height as distance up the
    /// screen, so a load on strops of <paramref name="stropCells"/> hangs that far below the helicopter's picture, and stays on the
    /// ground until the helicopter has moved that far from its spot. On a take-off the load starts where it stood
    /// (<paramref name="stoodX"/>, <paramref name="stoodZ"/>) and is drawn in under the helicopter as the strops take up (authored: the
    /// helicopter does not yet hover over its load first). On a landing, or when where it stood is not known, pass null for both.
    /// </summary>
    /// <returns>The load's place, and how much of the strops' length is paid out (0 on the ground, 1 hanging free).</returns>
    public static (double X, double Z, double PaidOut) Hang(double helicopterX, double helicopterZ, double stropCells, double? stoodX, double? stoodZ)
    {
        double moved = Math.Sqrt(helicopterX * helicopterX + helicopterZ * helicopterZ);
        if (stropCells <= 0) return (helicopterX, helicopterZ, 1);
        double paidOut = Math.Min(1, moved / stropCells);
        double hangX = helicopterX, hangZ = helicopterZ - paidOut * stropCells;
        if (stoodX is not { } fromX || stoodZ is not { } fromZ) return (hangX, hangZ, paidOut);
        double eased = paidOut * paidOut * (3 - 2 * paidOut);
        return (fromX + (hangX - fromX) * eased, fromZ + (hangZ - fromZ) * eased, paidOut);
    }
}
