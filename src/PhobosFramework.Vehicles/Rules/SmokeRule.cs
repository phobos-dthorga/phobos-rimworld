namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// Smoke dischargers (Framework 0.319.0; docs/development/vehicle-militarised-service-vehicles.md, "Smoke dischargers"; the owner's cards of
/// 26 Nov 2026: chemfuel each shot without Combat Extended, and a switch to pop smoke when hit, on by default, raiders' trucks too). When a
/// vehicle with dischargers pops smoke by itself: hit while enemies are a threat on its map, not again until a gap has passed, the player's
/// only with its switch on, and another faction's once a visit (it comes with its dischargers loaded for one salvo, since it carries no
/// chemfuel to reload). Game-free and tested.
/// </summary>
public static class SmokeRule
{
    /// <summary>
    /// The least time between two salvos it pops by itself, in ticks: 10 seconds [A], so a burst of hits makes one screen, not one a hit. The
    /// dischargers' own reload is longer still, from outside.
    /// </summary>
    public const int PopGapTicks = 600;

    /// <summary>
    /// Whether it pops smoke now. A hit with no enemy threat (a fire, a falling roof, a friend's stray shot) never does; nor does a hit within
    /// <see cref="PopGapTicks"/> of the last salvo. The player's vehicle needs its switch on; another faction's pops once a visit.
    /// </summary>
    public static bool PopsWhenHit(bool players, bool switchOn, bool threat, int ticksSinceLastPop, bool otherPoppedThisVisit)
    {
        if (!threat || ticksSinceLastPop < PopGapTicks) return false;
        return players ? switchOn : !otherPoppedThisVisit;
    }
}

/// <summary>
/// The ammunition locker (Framework 0.319.0; the owner's card of 26 Nov 2026: rounds only). A truck's cargo room counts its own load bed and a
/// locker that holds only the rounds its own guns and dischargers fire. Vehicle Framework has one cargo figure for everything aboard, so the
/// locker's room is added to it while the other cargo fits the bed; once other cargo spills past the bed, the locker counts only the rounds in
/// it, and the truck reads as overloaded by what spilled. Game-free and tested.
/// </summary>
public static class AmmoLockerRule
{
    /// <summary>
    /// The room the locker adds to the cargo figure, kg. <paramref name="otherKg"/> is everything aboard that is not the truck's rounds,
    /// <paramref name="roundsKg"/> its rounds, <paramref name="bedKg"/> the room it has without the locker.
    /// </summary>
    public static double Room(double lockerKg, double roundsKg, double otherKg, double bedKg)
    {
        if (lockerKg <= 0) return 0;
        if (otherKg <= bedKg) return lockerKg;
        return System.Math.Min(lockerKg, System.Math.Max(0, roundsKg));
    }

    /// <summary>Whether other cargo has spilled past the bed into a locker that holds rounds only.</summary>
    public static bool Spilled(double lockerKg, double otherKg, double bedKg) => lockerKg > 0 && otherKg > bedKg;
}
