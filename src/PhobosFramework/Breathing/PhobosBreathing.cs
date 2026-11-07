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
    /// <summary>How much faster than plain oxygen this treatment heals the conditions oxygen helps; 1 for a mask, more for a chamber.</summary>
    public float healingFactor = 1f;
}

/// <summary>Marks a condition as one that supplied oxygen helps, from a given severity. Matched by effect, never by name.</summary>
public class PhobosOxygenResponsiveExtension : DefModExtension
{
    /// <summary>The severity from which a pawn with this condition needs oxygen.</summary>
    public float minSeverity;
    /// <summary>
    /// Severity healed per day on supplied oxygen, on top of the condition's own healing; 0 for none. Never heals a permanent condition:
    /// oxygen lets living tissue recover, it does not bring dead tissue back.
    /// </summary>
    public float healingPerDayOnOxygen;
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

    /// <summary>
    /// Heals the conditions oxygen helps, for <paramref name="ticks"/> spent on the treatment <paramref name="treatment"/>. Only the
    /// pawn's first supplied-oxygen condition heals, so two treatments at once never double it. Called by the treatment itself.
    /// </summary>
    public static void HealOnSuppliedOxygen(Pawn pawn, Hediff treatment, int ticks)
    {
        List<Hediff>? hediffs = pawn.health?.hediffSet?.hediffs;
        PhobosSuppliedOxygenExtension? supplied = treatment.def.GetModExtension<PhobosSuppliedOxygenExtension>();
        if (hediffs == null || supplied == null || FirstSuppliedOxygen(hediffs) != treatment) return;
        for (int i = hediffs.Count - 1; i >= 0; i--)
        {
            if (i >= hediffs.Count) continue;
            Hediff hediff = hediffs[i];
            PhobosOxygenResponsiveExtension? responsive = hediff.def.GetModExtension<PhobosOxygenResponsiveExtension>();
            if (responsive == null || hediff.IsPermanent()) continue;
            float amount = BreathingRule.HealingFor(responsive.healingPerDayOnOxygen, supplied.healingFactor, ticks);
            if (amount <= 0f) continue;
            if (hediff is Hediff_Injury injury) injury.Heal(amount);
            else hediff.Severity -= amount;
        }
    }

    static Hediff? FirstSuppliedOxygen(List<Hediff> hediffs)
    {
        for (int i = 0; i < hediffs.Count; i++)
        {
            if (hediffs[i].def.HasModExtension<PhobosSuppliedOxygenExtension>()) return hediffs[i];
        }
        return null;
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
