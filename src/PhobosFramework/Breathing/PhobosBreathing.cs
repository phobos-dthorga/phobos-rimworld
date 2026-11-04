using System.Collections.Generic;
using Phobos.Framework.Rules;
using Verse;

namespace Phobos.Framework.Breathing;

/// <summary>
/// Marks a condition as "this pawn is breathing supplied oxygen". A medical mod puts it on its treatment condition, so a mod that
/// models a poison can ask for the effect without depending on the mod that gives it. Oxygen here is a treatment, not a gas in the
/// room: there is no air model (owner's gas rule, 4 Nov 2026).
/// </summary>
public class PhobosSuppliedOxygenExtension : DefModExtension
{
    /// <summary>Carbon monoxide's half-life on this oxygen, in hours. A better treatment sets a shorter one.</summary>
    public float carbonMonoxideHalfLifeHours = 1.25f;
}

/// <summary>Marks a condition as one that supplied oxygen helps, from a given severity. Matched by effect, never by name.</summary>
public class PhobosOxygenResponsiveExtension : DefModExtension
{
    public float minSeverity;
}

/// <summary>The one place to ask about supplied oxygen. Reads a pawn's conditions; it stores nothing and needs no ticking.</summary>
public static class PhobosBreathing
{
    /// <summary>Carbon monoxide's half-life from the oxygen this pawn is on, or null if it is on none.</summary>
    public static float? SuppliedCarbonMonoxideHalfLifeHours(Pawn pawn)
    {
        List<Hediff>? hediffs = pawn.health?.hediffSet?.hediffs;
        if (hediffs == null) return null;
        float? best = null;
        for (int i = 0; i < hediffs.Count; i++)
        {
            PhobosSuppliedOxygenExtension? supplied = hediffs[i].def.GetModExtension<PhobosSuppliedOxygenExtension>();
            if (supplied != null) best = BreathingRule.Shortest(best, supplied.carbonMonoxideHalfLifeHours);
        }
        return best;
    }

    /// <summary>Whether this pawn has a condition that supplied oxygen helps.</summary>
    public static bool HasOxygenResponsiveCondition(Pawn pawn)
    {
        List<Hediff>? hediffs = pawn.health?.hediffSet?.hediffs;
        if (hediffs == null) return false;
        for (int i = 0; i < hediffs.Count; i++)
        {
            PhobosOxygenResponsiveExtension? responsive = hediffs[i].def.GetModExtension<PhobosOxygenResponsiveExtension>();
            if (responsive != null && hediffs[i].Severity >= responsive.minSeverity) return true;
        }
        return false;
    }
}
