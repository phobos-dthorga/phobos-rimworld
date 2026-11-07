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
    public float flashBlindChance = 0.6f;
    public FloatRange falloutDays = new(10f, 30f);
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
