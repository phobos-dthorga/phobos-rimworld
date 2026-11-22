using System;

namespace Phobos.Framework.Vehicles.Rules;

/// <summary>Where on a vehicle a fire burns (docs/development/vehicle-fire.md, FR10; the owner's card of 23 Nov 2026: "zones and spread").</summary>
[Flags]
public enum FireZones
{
    None = 0,
    /// <summary>The engine and what sits by it: fuel, wiring, the battery, the radiator.</summary>
    EngineBay = 1,
    /// <summary>Where the crew sits, and the ammunition they carry.</summary>
    CrewCompartment = 2,
    /// <summary>Tyres, wheels, brakes and tracks: outside both, and covered by no fitted system.</summary>
    RunningGear = 4,
}

/// <summary>
/// Fighting a fire on a vehicle (D'thorga Framework, FR10; the owner's answers of 23 Nov 2026): where it burns and how it spreads, what a
/// fitted suppression system does when its zone burns, and what a hand extinguisher does. Game-free.
///
/// The game has one fire on a burning vehicle, with no place on it; the zones are ours. A fire starts in the zone of the part hit just
/// before it caught, or of whatever else set it alight, and spreads: from the running gear into the engine bay, and from the engine bay into
/// the crew compartment, as fires in real vehicles reach the passenger compartment minutes after starting in the engine (Papageorge and
/// Colwell, SAE 2024-01-2502, the abstract). Real crew-compartment systems put a fire out in a fraction of a second (161 to 384 ms with
/// Halon; McCormick and Clauson, US Army TARDEC, 2006). Every figure the callers pass is authored, a gameplay simplification of those.
/// </summary>
public static class FireFightingRule
{
    /// <summary>The game destroys a fire smaller than this, so a fire knocked down below it is out.</summary>
    public const double SmallestFire = 0.1;

    /// <summary>The zone a new fire starts in: the last part hit, if it was hit recently enough to have lit it; otherwise where fires on
    /// vehicles mostly start, the caller's fallback.</summary>
    public static FireZones StartZone(FireZones lastHitZone, double hoursSinceHit, double recentHours, FireZones fallback) =>
        lastHitZone != FireZones.None && hoursSinceHit >= 0 && hoursSinceHit <= recentHours ? lastHitZone : fallback;

    /// <summary>The zones burning once a fire has spread: a zone that has burned long enough sets the next one alight. Running gear
    /// spreads into the engine bay, and the engine bay into the crew compartment.</summary>
    public static FireZones Spread(FireZones burning, double gearHours, double engineHours, double gearToEngineHours, double engineToCrewHours)
    {
        FireZones result = burning;
        if ((burning & FireZones.RunningGear) != 0 && gearHours >= gearToEngineHours) result |= FireZones.EngineBay;
        if ((burning & FireZones.EngineBay) != 0 && engineHours >= engineToCrewHours) result |= FireZones.CrewCompartment;
        return result;
    }

    /// <summary>A fitted, ready system covering <paramref name="covers"/> discharges when that zone burns. Returns the zones still burning
    /// and the share of the fire left: none left, the fire is out; otherwise the fire shrinks by the share of its zones put out.</summary>
    public static (FireZones left, double shareLeft) Suppress(FireZones burning, FireZones covers)
    {
        if ((burning & covers) == 0) return (burning, 1);
        FireZones left = burning & ~covers;
        int before = Count(burning), after = Count(left);
        return (left, before == 0 ? 0 : after / (double)before);
    }

    /// <summary>Whether a system covering <paramref name="covers"/> should discharge now.</summary>
    public static bool Discharges(FireZones burning, FireZones covers, bool ready) => ready && (burning & covers) != 0;

    /// <summary>A fire's size after a hand extinguisher's use knocks it down: none (out) once below the smallest fire the game keeps. A
    /// hand extinguisher knocks a fire down but may not put it out, as the record's sources found.</summary>
    public static double AfterExtinguisher(double size, double knockDown)
    {
        double left = size - Math.Max(0, knockDown);
        return left < SmallestFire ? 0 : left;
    }

    /// <summary>Charges left after a use: never below none.</summary>
    public static int AfterUse(int charges) => Math.Max(0, charges - 1);

    /// <summary>Whether another author's vehicle counts as armoured for suppression: its outer armour at or above the figure, unless the
    /// vehicle is marked in or out by its extension.</summary>
    public static bool Armoured(double outerArmour, double armouredFrom, bool? marked) => marked ?? outerArmour >= armouredFrom;

    static int Count(FireZones zones)
    {
        int count = 0;
        for (int bits = (int)zones; bits != 0; bits &= bits - 1) count++;
        return count;
    }
}
