using Phobos.Framework.Defs;
using Verse;

namespace Phobos.MissilePatches.Strike;

/// <summary>
/// Marks a strategic warhead (the missile mod's MissileWarheadDef) as a blast that D'thorga's strike model handles, with its zones
/// in cells. Added to the extinction-class warhead by XML patch; another modder can add it to theirs the same way. All radii are
/// gameplay choices (authored, after Dubs Rimatomics' high-yield strike: a fireball of a tenth of its reach).
/// </summary>
public class PhobosStrikeExtension : DefModExtension
{
    public float fireballRadius = 25f;
    public float blastRadius = 110f;
    /// <summary>Where the falloff reaches zero.</summary>
    public float reach = 250f;
    public bool fallout = true;
    public bool emp = true;
    public bool pollution = true;
    /// <summary>The strike's dose times this (Patches 0.47.0): more than 1 for an enhanced-radiation warhead, whose small blast carries a
    /// large dose (IRBM's Clean warhead). Authored.</summary>
    public float doseFactor = 1f;
}

/// <summary>
/// The strike model's figures to tune in play, one Def whose first loaded instance wins (Mods/IRBM/Defs/Phobos/Strike/Rules.xml).
/// Every figure is authored unless its XML comment names a source. Player preferences are mod settings, never these.
/// </summary>
public class PhobosStrikeRulesDef : PhobosRulesDef
{
    public float pawnBlastFactor = 1f;
    public float pawnThermalBlastFactor = 0.3f;
    public float burnFactor = 0.25f;
    public float maxBurnPerHit = 12f;
    public int maxBurnHits = 10;
    /// <summary>The armour penetration of the strike's burns (Patches 0.50.0). Authored: above every heat rating Combat Extended gives armour
    /// (a flak vest 0.27, power armour 0.54), as a flash hot enough to burn skin sets clothing alight; at 0 any armour stopped them all.</summary>
    public float burnArmourPenetration = 1f;
    public float buildingThermalFactor = 1f;
    public float neighbourDistance = 200f;
    public float sparseFireChance = 0.02f;
    public float moderateFireChance = 0.08f;
    public float firestormFireChance = 0.3f;
    public float fireEdgeFactor = 0.15f;
    public float fireSize = 0.5f;
    public float blastAshChance = 0.5f;
    public float blastPollutionChance = 0.35f;
    public float empAmount = 400f;
    public float radiationStrength = 5f;
    /// <summary>With D'thorga Medical's radiation model: the dose at the fireball's edge, in grays, falling with the strike's curve
    /// (Patches 0.35.0; radiation-model.md, RM9). Authored: at one metre a cell, a real warhead's prompt dose would be lethal far further out.</summary>
    public float radiationGraysAtFireballEdge = 10f;
    public float flashBlindChance = 0.6f;
    public FloatRange falloutDays = new(10f, 30f);
    /// <summary>Our fallout's dose rate in the open an hour after the strike, in grays an hour, falling by the 7:10 rule (Patches 0.50.0; the
    /// owner's choice of 28 Nov 2026, "our own fallout, squeezed"). Authored: about 5 Gy over the first day and 7 over two weeks in the open.</summary>
    public float falloutGraysPerHour = 1.5f;
    /// <summary>A neighbouring map's share of the target's fallout (authored).</summary>
    public float neighbourFalloutShare = 0.3f;
    public IntRange cloudLifespanDays = new(2, 40);
    public float holdWhiteMaxSeconds = 8f;
    public float whiteScreenBudgetMs = 40f;

    public static PhobosStrikeRulesDef Rules => Current<PhobosStrikeRulesDef>();

    public float FireChance(FireMode mode) => mode switch
    {
        FireMode.Sparse => sparseFireChance,
        FireMode.Moderate => moderateFireChance,
        FireMode.Firestorm => firestormFireChance,
        _ => 0f,
    };
}
