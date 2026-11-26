using System;

namespace Phobos.Framework.Rules;

/// <summary>What a warned convoy does about an ambush (EM44).</summary>
public enum AmbushChoice
{
    /// <summary>The game's own default: the ambush map opens and the convoy fights.</summary>
    Fight,
    /// <summary>Keep going, at speed, and try to get past.</summary>
    DriveThrough,
    /// <summary>Turn round and fall back, losing the way made towards the next tile and some hours.</summary>
    TurnBack,
}

/// <summary>
/// Driving through or away from an ambush, game-free (docs/development/vehicle-road-work.md, EM44). <b>The one named roll</b> the owner
/// accepted for it (26 Nov 2026, by card: "one named roll", shaped by the convoy's state, with its own setting; the game's default, to
/// fight, stays): <see cref="Chance"/>, rolled once when the player picks a way out. Its shape is the agent's: the convoy's speed over
/// walking pace and the armour on its vehicles raise it; any vehicle that cannot move rules a way out out. Every figure is the agent's.
/// </summary>
public static class AmbushRule
{
    /// <summary>The odds of getting through with no armour, at walking pace.</summary>
    public const float DriveThroughBase = 0.35f;
    /// <summary>Turning back is the easier way out: the ambushers' trap faces the way the convoy was going.</summary>
    public const float TurnBackBase = 0.55f;
    /// <summary>What full armour adds to each way out.</summary>
    public const float DriveThroughArmour = 0.30f;
    public const float TurnBackArmour = 0.15f;
    /// <summary>What a convoy at three times walking pace or faster adds to each way out.</summary>
    public const float DriveThroughSpeed = 0.25f;
    public const float TurnBackSpeed = 0.20f;
    /// <summary>Never certain either way.</summary>
    public const float Least = 0.05f;
    public const float Most = 0.9f;
    /// <summary>Hours a convoy that turned back waits before it can drive on, while the ambushers give up the road.</summary>
    public const float TurnBackHoldHours = 4f;
    /// <summary>How many times walking pace counts as full speed.</summary>
    public const float FullSpeedOverWalking = 3f;
    /// <summary>Hits each vehicle takes on its outer parts driving through (Framework 0.324.0; the owner's answer of 26 Nov 2026 on the
    /// Decisions page: "Some hits on the way").</summary>
    public const int GauntletHits = 3;
    /// <summary>Damage a hit does to an unarmoured part.</summary>
    public const float GauntletDamage = 12f;
    /// <summary>The share of that full armour turns away.</summary>
    public const float GauntletArmourStops = 0.75f;

    /// <summary>Damage one hit does to a vehicle with this armour term (0 to 1).</summary>
    public static float HitDamage(float armourTerm) => GauntletDamage * (1f - GauntletArmourStops * Math.Clamp(armourTerm, 0f, 1f));

    /// <summary>0 at walking pace or slower, 1 at <see cref="FullSpeedOverWalking"/> times it or faster, from ticks a world tile takes.</summary>
    public static float SpeedTerm(int convoyTicksPerMove, int walkingTicksPerMove)
    {
        if (convoyTicksPerMove <= 0 || walkingTicksPerMove <= 0) return 0f;
        float over = walkingTicksPerMove / (float)convoyTicksPerMove;
        return Math.Clamp((over - 1f) / (FullSpeedOverWalking - 1f), 0f, 1f);
    }

    /// <summary>0 for no armour, 1 at the reference rating or more: the vehicles' mean outer armour against sharp hits.</summary>
    public static float ArmourTerm(float meanSharpRating, float reference) => reference <= 0f ? 0f : Math.Clamp(meanSharpRating / reference, 0f, 1f);

    /// <summary>What a warning by radio from home adds to each way out (Framework 0.325.0, EM33; agent's figure).</summary>
    public const float WarnedBonus = 0.1f;

    /// <summary>The chance the way out works; 0 to fight, and 0 when a vehicle of the convoy cannot move. A convoy warned by radio from home
    /// gets <see cref="WarnedBonus"/> more, within the same limits.</summary>
    public static float Chance(AmbushChoice choice, float speedTerm, float armourTerm, bool allCanMove, bool warned) =>
        choice == AmbushChoice.Fight || !allCanMove ? 0f
            : Math.Clamp(Chance(choice, speedTerm, armourTerm, allCanMove) + (warned ? WarnedBonus : 0f), Least, Most);

    /// <summary>The chance the way out works; 0 to fight, and 0 when a vehicle of the convoy cannot move.</summary>
    public static float Chance(AmbushChoice choice, float speedTerm, float armourTerm, bool allCanMove)
    {
        if (choice == AmbushChoice.Fight || !allCanMove) return 0f;
        float speed = Math.Clamp(speedTerm, 0f, 1f), armour = Math.Clamp(armourTerm, 0f, 1f);
        float chance = choice == AmbushChoice.DriveThrough
            ? DriveThroughBase + DriveThroughArmour * armour + DriveThroughSpeed * speed
            : TurnBackBase + TurnBackArmour * armour + TurnBackSpeed * speed;
        return Math.Clamp(chance, Least, Most);
    }
}
