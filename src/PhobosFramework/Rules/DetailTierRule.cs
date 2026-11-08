namespace Phobos.Framework.Rules;

/// <summary>How closely a thing is simulated: fully, lightly, or not at all.</summary>
public enum DetailTier
{
    /// <summary>Nothing runs: a thing nobody is near (a raider's vehicle on the world map).</summary>
    Off,
    /// <summary>Only what a hit or an event sets; nothing evolves on a cadence (a raider's vehicle on a map).</summary>
    Lite,
    /// <summary>Everything (the player's own).</summary>
    Full,
}

/// <summary>
/// The rule behind "player things in full detail, everyone else's diffuse" (owner, 8 Nov 2026; docs/development/vehicle-health-design.md),
/// game-free: the player's things are always Full, wherever they are; another faction's are Lite while spawned on a map and Off
/// otherwise, unless the player asks for full detail for everyone. Re-read on a slow cadence, so a captured thing wakes up.
/// </summary>
public static class DetailTierRule
{
    public static DetailTier For(bool playerFaction, bool spawnedOnMap, bool fullForEveryone)
    {
        if (playerFaction) return DetailTier.Full;
        if (!spawnedOnMap) return DetailTier.Off;
        return fullForEveryone ? DetailTier.Full : DetailTier.Lite;
    }
}
