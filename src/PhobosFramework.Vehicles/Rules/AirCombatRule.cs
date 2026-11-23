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
    /// <summary>
    /// What lopsided thrust leaves of a twin jet's dodge (Framework 0.247.0, per-engine realism step 2; the owner's card of 23 Nov 2026): one
    /// less the aircraft's yaw figure times the difference between its two engines' outputs (each 0 to 1), so one engine out costs the whole
    /// figure and two matched engines cost nothing. Only a twin pulls: with one engine or more than two it is 1, as it is with no figure.
    /// The figure grows with how far the engines sit from the centreline and is authored per aircraft.
    /// </summary>
    public static double LopsidedThrust(double yaw, double firstOutput, double secondOutput, int engines)
    {
        if (engines != 2 || yaw <= 0) return 1;
        return Clamp01(1 - Clamp01(yaw) * Math.Abs(Clamp01(firstOutput) - Clamp01(secondOutput)));
    }

    /// <summary>The extra fuel per unit of an aircraft's yaw figure with one engine fully out (AUTHORED, Framework 0.248.0): a yaw figure of
    /// 0.15 burns 7.5 % more. No published figure was found for these jets; a twin's single-engine cruise in the manuals is slower and
    /// thirstier per mile for more reasons than trim alone, which this leaves out.</summary>
    public const double TrimFuelPerYaw = 0.5;

    /// <summary>
    /// What holding a lopsided twin jet straight costs in fuel (Framework 0.248.0, the owner's card of 23 Nov 2026: the no-drag rule's third
    /// exception): the rudder held against the pull adds drag, so the engines burn more for the same flight. One plus the yaw figure times
    /// <see cref="TrimFuelPerYaw"/> times the difference between the two engines' outputs; 1 for anything but a twin with a figure.
    /// </summary>
    public static double LopsidedThrustFuel(double yaw, double firstOutput, double secondOutput, int engines)
    {
        if (engines != 2 || yaw <= 0) return 1;
        return 1 + Clamp01(yaw) * TrimFuelPerYaw * Math.Abs(Clamp01(firstOutput) - Clamp01(secondOutput));
    }

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

    /// <summary>
    /// The cartridges another faction's aircraft arrives with (Framework 0.180.0; owner, 20 Nov 2026: "partly full"): a share of its magazine
    /// between <paramref name="minShare"/> and <paramref name="maxShare"/>, placed by <paramref name="roll"/> (0 to 1), rounded down to whole
    /// salvos and never past the magazine.
    /// </summary>
    public static int OtherFactionStock(int capacity, int perSalvo, double minShare, double maxShare, double roll)
    {
        if (capacity <= 0) return 0;
        int salvo = Math.Max(1, perSalvo);
        double low = Clamp01(Math.Min(minShare, maxShare));
        double high = Clamp01(Math.Max(minShare, maxShare));
        double share = low + (high - low) * Clamp01(roll);
        int salvos = (int)Math.Floor(capacity * share / salvo + 1e-9);
        return Math.Min(capacity, salvos * salvo);
    }

    /// <summary>
    /// How many cartridges move once from an aircraft's cargo into its new magazine (Framework 0.180.0, a save from before magazines): all of
    /// them while there is room, the rest left as cargo.
    /// </summary>
    public static int CargoIntoMagazine(int inCargo, int held, int capacity) => Math.Max(0, Math.Min(inCargo, capacity - Math.Max(0, held)));

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

/// <summary>How well an aircraft's sight lets it aim (avionics A10, A11; aircraft service-life upgrades, 21 Nov 2026). Game-free; the figures
/// are the rules Def's, authored.</summary>
public enum SightState
{
    /// <summary>No sight, or one shot out.</summary>
    Out,
    /// <summary>A basic sight working.</summary>
    Basic,
    /// <summary>A full sight working: the weapon's own accuracy.</summary>
    Full,
}

public static class AvionicsAimRule
{
    /// <summary>
    /// The factor on an aircraft weapon's accuracy: the sight's (A10), times the dark's without a working night sight and the weather's (A11),
    /// of which a working radar gives back a share. 1 is the weapon as its author made it.
    /// </summary>
    public static double Factor(SightState sight, bool dark, bool badWeather, bool nightSight, bool radar, double basicSight, double noSight,
        double blind, double radarGivesBack)
    {
        double factor = sight switch
        {
            SightState.Full => 1,
            SightState.Basic => Clamp(basicSight),
            _ => Clamp(noSight),
        };
        if (dark && !nightSight) factor *= Clamp(blind);
        if (badWeather)
        {
            double loss = 1 - Clamp(blind);
            factor *= 1 - loss * (radar ? 1 - Math.Min(1, Math.Max(0, radarGivesBack)) : 1);
        }
        return factor;
    }

    /// <summary>A bomb's or rocket's scatter for the factor: the weapon's own miss radius, widened as its accuracy falls.</summary>
    public static double MissRadius(double radius, double factor) => factor >= 1 || factor <= 0 ? radius : radius / factor;

    static double Clamp(double value) => value < 0.05 ? 0.05 : value > 1 ? 1 : value;
}

/// <summary>Whether a landing goes hard (avionics A3, A4; aircraft service-life upgrades, 21 Nov 2026). Game-free; figures are the rules Def's.</summary>
public static class RoughLandingRule
{
    /// <summary>The chance a landing goes hard: none while the crew can see to land with its controls answering; otherwise the rules'
    /// chance, less with a working autopilot.</summary>
    public static double Chance(bool poorVisibility, bool instruments, double flightControl, bool autopilot, double chance, double controlBelow,
        double autopilotFactor)
    {
        bool blind = poorVisibility && !instruments;
        bool loose = flightControl < controlBelow;
        if (!blind && !loose) return 0;
        double c = Math.Min(1, Math.Max(0, chance));
        return autopilot ? c * Math.Min(1, Math.Max(0, autopilotFactor)) : c;
    }
}
