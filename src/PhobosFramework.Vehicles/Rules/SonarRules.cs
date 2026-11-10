using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// The arithmetic of an active sonar set, free of the game (docs/development/vehicle-seabed-crawler.md, "The sonar set as built"). A ping finds what a passive array cannot: anything in
/// the water, moving or still, and a boat however quiet. It also tells everyone in the water where it came from, further away than its own echoes return. Every figure that feeds it is
/// authored on the vehicle's Def and is a gameplay choice, not a measurement.
/// </summary>
public static class SonarRule
{
    /// <summary>How far a ping finds things, in cells: the fitted range, or nothing without the set or without anyone awake aboard to work it.</summary>
    public static float Range(float fittedRange, bool anyoneToWorkIt) => anyoneToWorkIt && fittedRange > 0f ? fittedRange : 0f;

    /// <summary>
    /// Whether a ping finds something at this distance. Anything in the water is found, moving or not, and a boat that is under however quiet it is; a boat resting on the bottom is lost
    /// among the seabed's own echoes beyond a share of the range. Nothing on land: sound does not cross into the air and back.
    /// </summary>
    public static bool Finds(ContactKind kind, float distance, float range, bool settledOnBottom, float settledShare)
    {
        if (range <= 0f || distance < 0f) return false;
        return kind switch
        {
            ContactKind.OnLand => false,
            ContactKind.HiddenBoat when settledOnBottom => distance < range * Math.Clamp(settledShare, 0f, 1f),
            _ => distance < range,
        };
    }

    /// <summary>
    /// How far away the ping itself is heard, in cells. Its sound travels one way to a listener but has to go out and back to find anything, so it carries further than the set's own
    /// reach. Anyone in the water with ears hears it, with or without a set of their own.
    /// </summary>
    public static float HeardWithin(float range, float heardFactor) => range <= 0f ? 0f : range * Math.Max(1f, heardFactor);

    /// <summary>Whether the set is ready to ping again.</summary>
    public static bool Ready(int now, int lastPingTick, int cooldownTicks) => lastPingTick < 0 || now - lastPingTick >= cooldownTicks;

    /// <summary>Whole seconds until it is ready, rounded up, for the button's reason; 0 when ready.</summary>
    public static int SecondsUntilReady(int now, int lastPingTick, int cooldownTicks, float ticksPerSecond)
    {
        if (Ready(now, lastPingTick, cooldownTicks) || ticksPerSecond <= 0f) return 0;
        return (int)Math.Ceiling((cooldownTicks - (now - lastPingTick)) / ticksPerSecond);
    }

    /// <summary>
    /// Whether a boat not crewed by the player pings at this look: only when it is under (a boat on the surface is seen anyway), the set is ready and something hostile is within reach,
    /// and then by chance, so a hunting boat gives itself away now and then and not every few seconds. The roll is a number from 0 to 1.
    /// </summary>
    public static bool CrewPingsNow(bool under, bool ready, bool hostileWithinRange, float roll, float chancePerLook) =>
        under && ready && hostileWithinRange && roll < Math.Clamp(chancePerLook, 0f, 1f);
}
