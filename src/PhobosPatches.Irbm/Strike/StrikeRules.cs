using System;

namespace Phobos.MissilePatches.Strike;

/// <summary>Where a cell or thing lies relative to ground zero.</summary>
public enum StrikeZone
{
    /// <summary>Everything vanishes, every roof goes, the ground is scoured.</summary>
    Fireball,
    /// <summary>Buildings, items and plants vanish; people take the blast and burns by distance.</summary>
    Blast,
    /// <summary>Things stand; people burn, plants wilt, fires start, buildings take what the blast still carries.</summary>
    Thermal,
}

/// <summary>Fires beyond the blast (a setting).</summary>
public enum FireMode
{
    None,
    Sparse,
    Moderate,
    Firestorm,
}

/// <summary>Fallout and radiation (a setting; owner, 7 November 2026: Rimatomics' when it is active).</summary>
public enum FalloutMode
{
    /// <summary>Dubs Rimatomics' fallout and radiation when it is active, otherwise the missile mod's nuclear winter.</summary>
    Automatic,
    MissileWinterOnly,
    None,
}

/// <summary>The geometry and falloff of D'thorga's strike. Game-free: compiled into the tests without the game.</summary>
public static class StrikeRule
{
    public static StrikeZone Zone(float distance, float fireballRadius, float blastRadius) =>
        distance <= fireballRadius ? StrikeZone.Fireball : distance <= blastRadius ? StrikeZone.Blast : StrikeZone.Thermal;

    /// <summary>
    /// Damage by distance, Dubs Rimatomics' curve (NuclearStrike.DamageAt, read 7 Nov 2026): reach times minus the natural log of
    /// distance over reach. Zero at <paramref name="reach"/> and beyond; held at one cell's value inside one cell. A gameplay curve,
    /// not a physical model: at one metre per cell a real strategic warhead's fireball would cover the whole map.
    /// </summary>
    public static float Falloff(float distance, float reach)
    {
        if (reach <= 1f) return 0f;
        float d = distance < 1f ? 1f : distance;
        return d >= reach ? 0f : reach * -(float)Math.Log(d / reach);
    }

    /// <summary>The falloff as a fraction: 1 at the fireball's edge and inside it, 0 at reach.</summary>
    public static float Strength(float distance, float fireballRadius, float reach)
    {
        float edge = Falloff(fireballRadius, reach);
        if (edge <= 0f) return 0f;
        float strength = Falloff(distance, reach) / edge;
        return strength < 0f ? 0f : strength > 1f ? 1f : strength;
    }

    /// <summary>
    /// The chance a cell catches fire: the full base chance inside the blast zone, falling in a straight line to base times
    /// <paramref name="edgeFactor"/> at reach and beyond.
    /// </summary>
    public static float FireChance(float baseChance, float distance, float blastRadius, float reach, float edgeFactor)
    {
        if (baseChance <= 0f) return 0f;
        if (distance <= blastRadius || reach <= blastRadius) return baseChance;
        float t = (distance - blastRadius) / (reach - blastRadius);
        if (t > 1f) t = 1f;
        return baseChance * (1f - t * (1f - edgeFactor));
    }

    /// <summary>How many separate burns a total of burn damage is split into, so it spreads over the skin rather than one part.</summary>
    public static int BurnHits(float total, float maxPerHit, int maxHits)
    {
        if (total <= 0f || maxPerHit <= 0f || maxHits <= 0) return 0;
        int hits = (int)Math.Ceiling(total / maxPerHit);
        return hits > maxHits ? maxHits : hits;
    }
}
