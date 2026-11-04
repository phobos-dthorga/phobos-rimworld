namespace Phobos.Framework.Rules;

/// <summary>
/// Which smoke-inhalation signal a pawn gets. The owner's rule: reuse an effect that already exists before making one.
/// Combat Extended's own smoke inhalation condition already accounts for masks, sleep, being downed and breathing capacity,
/// so when it runs it is the signal; otherwise the game's smoke at the cell, through the mask rule. Game-free.
/// </summary>
public static class SignalRule
{
    /// <summary>True when Combat Extended is active and its "smoke effects" setting is on.</summary>
    public static bool UseCombatExtendedSmoke(bool combatExtendedActive, bool combatExtendedSmokeEffectsOn) =>
        combatExtendedActive && combatExtendedSmokeEffectsOn;

    /// <summary>The breathed smoke, 0 to 1.</summary>
    public static float BreathedSmoke(bool useCombatExtendedSmoke, float combatExtendedSeverity, float vanillaDensity, float maskFactor)
    {
        if (useCombatExtendedSmoke) return Clamp01(combatExtendedSeverity);
        return Clamp01(vanillaDensity) * Clamp01(maskFactor);
    }

    static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
}
