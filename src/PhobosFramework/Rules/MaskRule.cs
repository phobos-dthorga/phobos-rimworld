namespace Phobos.Framework.Rules;

/// <summary>
/// How much of a gas at a pawn's cell the pawn actually breathes, 0 to 1, from what the pawn wears. Game-free; the framework
/// reads the stats and tags and passes them in.
/// </summary>
public static class MaskRule
{
    /// <summary>
    /// For gases a filter mask stops (smoke, toxic gas): the game's own toxic environment resistance is the mask, so 0.8
    /// resistance (a vanilla gas mask) leaves 0.2 breathed. A worn item tagged as a gas mask stops everything.
    /// </summary>
    public static float Filtered(float toxicEnvironmentResistance, bool wearsGasMaskTag)
    {
        if (wearsGasMaskTag) return 0f;
        float factor = 1f - toxicEnvironmentResistance;
        if (factor < 0f) return 0f;
        if (factor > 1f) return 1f;
        return factor;
    }

    /// <summary>
    /// For gases a filter mask does not stop (carbon monoxide): only sealed headgear with its own air helps. The framework
    /// treats a pawn as sealed when Combat Extended's smoke sensitivity, counting only gear that is not a filter mask, is at
    /// or below 0 (its power and recon helmets set it to -1), or without Combat Extended when toxic environment resistance
    /// reaches 1. Combat Extended's gas masks also take the sensitivity to 0, so the figure passed in must leave them out
    /// (<see cref="SensitivityFromSealingGear"/>), or a filter mask would stop carbon monoxide.
    /// </summary>
    public static float Unfiltered(bool sealedHeadgear) => sealedHeadgear ? 0f : 1f;

    public static bool IsSealed(bool combatExtendedActive, float smokeSensitivityFromSealingGear, float toxicEnvironmentResistance, float sealedToxicResistance) =>
        combatExtendedActive ? smokeSensitivityFromSealingGear <= 0f : toxicEnvironmentResistance >= sealedToxicResistance;

    /// <summary>
    /// Combat Extended's smoke sensitivity as it would be without any gas-mask-tagged item: the stat's base value plus the
    /// offsets of the other worn items, never below 0.
    /// </summary>
    public static float SensitivityFromSealingGear(float baseValue, System.Collections.Generic.IReadOnlyList<float> offsetsOfUntaggedItems)
    {
        float value = baseValue;
        for (int i = 0; i < offsetsOfUntaggedItems.Count; i++) value += offsetsOfUntaggedItems[i];
        return value < 0f ? 0f : value;
    }
}
