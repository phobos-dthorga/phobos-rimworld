using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// How Combat Extended (CE) reads figures the game and Vehicle Framework read differently, so a Phobos mod gives the same
/// intent under both. Game-free: compiled into the rule tests. Read from CE's vehicle compatibility code and its Defs on
/// 8 November 2026 (D'thorga Framework's CE notes in docs/development/vehicle-framework-mechanics.md).
/// </summary>
public static class CombatExtendedRules
{
    /// <summary>The game's explosion penetration per point of damage when none is given (GenExplosion.DoExplosion).</summary>
    public const float GameBlastPenetrationPerDamage = 0.015f;
    /// <summary>CE's Bomb damage: 156 damage with 62 penetration (its Damages_LocalInjury.xml). Penetration there is in MPa, blunt.</summary>
    public const float CeBombDamage = 156f;
    public const float CeBombPenetration = 62f;

    /// <summary>
    /// A vehicle part's hit weight for the damage model in use. Vehicle Framework hits a bigger weight more often; CE sorts
    /// parts by a random draw times the weight, smallest first, so there a smaller weight is hit more. An explicit CE weight
    /// wins; without one, the Vehicle Framework weight is inverted so the intent (rarely hit) holds under both.
    /// </summary>
    public static double HitWeight(double vfWeight, double ceWeight, bool ceActive)
    {
        if (!ceActive) return vfWeight;
        if (!double.IsNaN(ceWeight) && ceWeight > 0) return ceWeight;
        return 1.0 / Math.Max(0.01, vfWeight);
    }

    /// <summary>
    /// The armour penetration to give an explosion of a damage, on the scale in use: the game's default per point of damage,
    /// or CE's Bomb ratio, so an explosion of ours is as armour-worthy as CE's own blasts of the same damage.
    /// </summary>
    public static float BlastPenetrationForDamage(int damage, bool ceActive) =>
        ceActive ? damage * CeBombPenetration / CeBombDamage : damage * GameBlastPenetrationPerDamage;
}
