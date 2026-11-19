namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Lights that switch themselves (Framework 0.171.0; owner, 19 Nov 2026: "a setting that's on by default, but when enabled, it handles the
/// turning on/off of lights automatically and as appropriate"), game-free. The work lamps' part: whether someone stands within their light,
/// and how long they stay lit after the last person leaves, so a colonist walking past does not flick them on and off. The figures are the
/// Def's; the rule is an agent's choice, for the owner to revise (docs/development/vehicle-upgrades-round-two.md, "Lights that switch themselves").
/// </summary>
public static class AutoLightRule
{
    /// <summary>Whether a person <paramref name="dx"/>, <paramref name="dz"/> cells from the vehicle stands within <paramref name="radius"/>
    /// cells of it (a round pool, as the game's light is).</summary>
    public static bool Within(int dx, int dz, double radius) => radius > 0 && (double)dx * dx + (double)dz * dz <= radius * radius;

    /// <summary>Whether the lamps are still wanted at <paramref name="now"/>, someone having last been near at <paramref name="lastSeen"/>:
    /// for <paramref name="holdTicks"/> after, never before it (a tick count that went back, as after a load, holds nothing).</summary>
    public static bool Held(int lastSeen, int now, int holdTicks) => now >= lastSeen && now - lastSeen <= System.Math.Max(0, holdTicks);
}
