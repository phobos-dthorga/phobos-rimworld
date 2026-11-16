using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>
/// How an aircraft in flight on the world map looks to another mod's air defence, and what that defence does to it (Framework 0.119.0;
/// docs/development/air-defence-tie-ins.md, AD2, AD3 and avionics A12, A14 and A16). Game-free: the tie-in reads the game and calls these.
/// Every figure here is authored for play unless a comment names a source.
/// </summary>
public static class AirCombatRule
{
    /// <summary>
    /// Vehicle Framework moves an aircraft this many tiles each tick for each point of its flight speed (it adds 0.001 × speed ÷ the leg's
    /// length in tiles to its progress along the leg; read from Vehicle Framework 1.7, 17 Nov 2026).
    /// </summary>
    public const double TilesPerTickPerSpeed = 0.001;

    /// <summary>The ticks an aircraft needs for the distance left, at its flight speed; a stopped aircraft is "a long way off".</summary>
    public static int TicksToArrival(double tilesLeft, double flightSpeed)
    {
        if (tilesLeft <= 0) return 0;
        double perTick = TilesPerTickPerSpeed * flightSpeed;
        if (perTick <= 0) return int.MaxValue / 2;
        double ticks = Math.Ceiling(tilesLeft / perTick);
        return ticks >= int.MaxValue / 2 ? int.MaxValue / 2 : (int)ticks;
    }

    /// <summary>The aircraft's speed as an angle on the world sphere each tick (radians), as a missile track gives it.</summary>
    public static double AngularSpeed(double flightSpeed, double tilesPerRadian) =>
        tilesPerRadian <= 0 ? 0 : TilesPerTickPerSpeed * flightSpeed / tilesPerRadian;

    /// <summary>
    /// The height the aircraft flies at, in metres, this far into its flight: it climbs from the ground over the first stretch and comes
    /// down over the last, so a radar sees a low aircraft near both ends. A cruise without working oxygen (A14) is flown low.
    /// </summary>
    public static double Altitude(double cruiseMetres, double lowCruiseMetres, bool low, double tilesFlown, double tilesLeft, double climbTiles)
    {
        double cruise = low ? Math.Min(lowCruiseMetres, cruiseMetres) : cruiseMetres;
        if (cruise <= 0) return 0;
        if (climbTiles <= 0) return cruise;
        double nearEnd = Math.Min(Math.Max(tilesFlown, 0), Math.Max(tilesLeft, 0));
        double share = Math.Min(1, nearEnd / climbTiles);
        // Never on the ground while flying: the least height is a tenth of the cruise.
        return cruise * Math.Max(0.1, share);
    }

    /// <summary>
    /// The factor countermeasures put on an air defence's chance to hit (A16): chaff and flares fired on the warning receiver's cue break more
    /// shots than those fired late, by eye, when the receiver is out. No cartridges aboard, or no dispenser: 1.
    /// </summary>
    public static double HitChanceFactor(bool dispenserWorks, bool cartridgesAboard, bool warningWorks, double cuedFactor, double lateFactor) =>
        !dispenserWorks || !cartridgesAboard ? 1 : warningWorks ? Clamp01(cuedFactor) : Clamp01(lateFactor);

    /// <summary>
    /// The factor countermeasures put on the time a fire-control radar needs to lock on (A16): chaff fired when the receiver hears the radar
    /// searching makes the lock take longer. Only on the receiver's cue: without it the crew does not know a lock is coming.
    /// </summary>
    public static double LockTimeFactor(bool dispenserWorks, bool cartridgesAboard, bool warningWorks, double cuedFactor) =>
        dispenserWorks && cartridgesAboard && warningWorks ? Math.Max(1, cuedFactor) : 1;

    /// <summary>
    /// The Harrier's chance to dodge a shot (A12, with A16's salvo): its author's figure, scaled by how well its flight controls answer (the
    /// FlightControl stat, 1 when sound) and by its warning receiver (1 working, else the factor); a salvo of countermeasures fired on a map
    /// adds its bonus while it lasts. Never above the cap.
    /// </summary>
    public static double Evasion(double authorsChance, double flightControl, bool warningWorks, double noWarningFactor, double salvoBonus, double cap)
    {
        if (authorsChance <= 0) return authorsChance;
        double controls = Clamp01(flightControl);
        double warning = warningWorks ? 1 : Clamp01(noWarningFactor);
        double chance = authorsChance * controls * warning + Math.Max(0, salvoBonus);
        return Math.Min(Math.Max(0, cap), Math.Max(0, chance));
    }

    /// <summary>
    /// How a hit from an interceptor is shared out (AD3): the damage is split into this many hits of equal size, each landing where the
    /// vehicle's own hit areas put it. Never fewer than one hit nor less than one point a hit.
    /// </summary>
    public static (int hits, double each) SplitDamage(double totalDamage, int hits)
    {
        int count = Math.Max(1, hits);
        if (totalDamage <= 0) return (0, 0);
        double each = Math.Max(1, totalDamage / count);
        return (count, each);
    }

    /// <summary>
    /// Whether a damaged aircraft can stay up (AD3: owner, 17 Nov 2026: "down only if unflyable"): its flight controls answer at least the
    /// launch floor, it has an engine turning, and its airframe has not failed.
    /// </summary>
    public static bool StaysUp(double flightControl, int enginesWorking, int engines, bool airframeFailed) =>
        !airframeFailed && flightControl >= 0.35 && (engines <= 0 || enginesWorking > 0);

    static double Clamp01(double value) => value < 0 ? 0 : value > 1 ? 1 : value;
}

/// <summary>
/// A turret's accuracy when its power is lost (Framework 0.119.0; docs/development/vehicle-knocked-out-systems.md, T5 and T6). A powered turret
/// that is cranked by hand has no stabiliser, so it shoots worse on the move (T5), and its powered sights are dark, so it shoots worse at night
/// and in poor weather (T6). A weapon swung by hand anyway, or a turret with its power, loses nothing. Game-free; the factors are authored.
/// </summary>
public static class TurretAimRule
{
    /// <summary>The factor on a turret's accuracy: 1 with power; with none, the stabiliser's factor while the vehicle moves and the sights'
    /// factor in poor visibility, multiplied when both apply.</summary>
    public static double Factor(bool cranked, bool moving, bool poorVisibility, double noStabiliser, double noSights)
    {
        if (!cranked) return 1;
        double factor = 1;
        if (moving) factor *= Clamp(noStabiliser);
        if (poorVisibility) factor *= Clamp(noSights);
        return factor;
    }

    /// <summary>
    /// Combat Extended aims through a sway angle: a weapon aimed less steadily sways more. The sway is divided by the accuracy factor, so half
    /// the accuracy is twice the sway, up to the cap. A weapon whose own sway is less than <paramref name="least"/> is widened from that
    /// (Framework 0.120.5, fault-read A8): many turrets carry no sway of their own under Combat Extended, and nothing times nothing left
    /// them aiming as well cranked as powered.
    /// </summary>
    public static double CombatExtendedSway(double sway, double factor, double capFactor, double least = 0) =>
        factor >= 1 || factor <= 0 ? sway : Math.Max(sway, Math.Max(0, least)) * Math.Min(Math.Max(1, capFactor), 1 / factor);

    static double Clamp(double value) => value < 0.05 ? 0.05 : value > 1 ? 1 : value;
}
