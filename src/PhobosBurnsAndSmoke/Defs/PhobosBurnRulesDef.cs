using Phobos.Framework.Defs;
using Verse;

namespace Phobos.BurnsAndSmoke.Defs;

/// <summary>
/// The starting values the design says to tune in play. One Def; the first loaded wins. Every figure is authored unless its
/// comment names a source. Player preferences (the toggles, the display mode) are mod settings, not rules.
/// </summary>
public class PhobosBurnRulesDef : PhobosRulesDef
{
    /// <summary>How often a pawn's burns, shock, smoke and carbon monoxide are recomputed. Givers run every 60 ticks; this is a multiple.</summary>
    public int recomputeIntervalTicks = 300;

    // Burned body fraction (the game's version of total body surface area).
    public float burnLoadVisibleTbsa = 0.05f;
    /// <summary>Burn shock starts here: 15 to 20% is the threshold for fluid resuscitation (Parkland formula). Approximate.</summary>
    public float shockStartTbsa = 0.15f;

    // Revised Baux score (Osler and colleagues, 2010): age + percent burned + 17 with an inhalation injury.
    public float bauxNoDeathScore = 36f;
    public float bauxNoSurvivorScore = 110f;
    public float inhalationBauxPoints = 17f;
    /// <summary>Carbon monoxide severity from which the score counts an inhalation injury.</summary>
    public float inhalationCarbonMonoxideThreshold = 0.2f;
    public float shockRisePerDay = 1f;
    public float shockFallPerDay = 0.35f;
    public float tendedShockRelief = 0.6f;
    public float bedRestShockRelief = 0.3f;
    public float shockTreatmentFloor = 0.1f;

    // Airway burn: delayed swelling over hours, resolving in about two days (approximate).
    public float airwayBaseChance = 0.15f;
    /// <summary>Fresh burn damage to the head and neck that gives half the base chance.</summary>
    public float airwayReferenceDamage = 10f;

    // Carbon monoxide: half-life about 320 minutes on room air (Weaver and colleagues, 2000); faster in open air (authored).
    public float coUptakePerHourAtFullGas = 0.3f;
    public float coCap = 0.8f;

    // Lung injury and the acute smoke condition (the acute one runs only when Combat Extended's does not).
    public float lungInjuryPerHourAtFullSmoke = 0.15f;
    /// <summary>Combat Extended's own figure per 50-tick check, reused for ours.</summary>
    public float acuteSmokePerCheck = 0.045f;

    public static PhobosBurnRulesDef Current => Current<PhobosBurnRulesDef>();
}

/// <summary>Marks an injury Def as a burn for the burn load. Patched onto the game's burns and Combat Extended's; other mods add it in XML.</summary>
public class PhobosBurnLikeExtension : DefModExtension
{
    /// <summary>How much of a burn this counts as for burned area; 1 for a plain burn.</summary>
    public float tbsaWeight = 1f;
}
