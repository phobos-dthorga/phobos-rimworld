namespace Phobos.BurnsAndSmoke.Rules;

/// <summary>
/// Constricting eschar on a deeply burned limb. Game-free. Deep burns all round a limb harden into a crust that cannot stretch; as the
/// tissue beneath swells it squeezes the limb until blood stops reaching it, and an escharotomy (cutting through the crust) releases
/// it (NCBI Bookshelf NBK482120, "Escharotomy"). The game has burn depth per injury, not whether a burn goes all round, so the
/// stand-in is the burn damage on the limb against its health: past the rules Def's fraction (60%, owner's go of 7 Nov 2026) the
/// burn is treated as deep and circumferential. A labelled simplification.
/// </summary>
public static class EscharRule
{
    /// <summary>Whether a limb with this much open burn damage forms (or keeps) an eschar.</summary>
    public static bool Forms(float burnDamageOnPart, float partMaxHealth, float fractionOfHealth) =>
        partMaxHealth > 0f && fractionOfHealth > 0f && burnDamageOnPart >= fractionOfHealth * partMaxHealth;

    /// <summary>
    /// Ischaemic damage in <paramref name="hours"/> from an eschar of <paramref name="severity"/>: none below the severity where blood
    /// flow fails, rising to the full rate at the tightest. Authored.
    /// </summary>
    public static float IschaemicDamage(float severity, float fromSeverity, float damagePerHourAtFull, float hours)
    {
        if (hours <= 0f || damagePerHourAtFull <= 0f || severity <= fromSeverity || fromSeverity >= 1f) return 0f;
        float share = (severity - fromSeverity) / (1f - fromSeverity);
        if (share > 1f) share = 1f;
        return damagePerHourAtFull * share * hours;
    }
}
